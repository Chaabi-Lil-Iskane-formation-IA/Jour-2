using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Whisper.net;
using NAudio.Wave;

// Créer le builder pour configurer l'application web
var builder = WebApplication.CreateBuilder(args);

// Configure CORS (Cross-Origin Resource Sharing) pour permettre les requêtes depuis React
builder.Services.AddCors(options =>
{
    // Créer une politique CORS qui permet toutes les origines
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()       // Accepter les requêtes de n'importe quelle origine (ex: localhost:3000)
              .AllowAnyMethod()        // Accepter toutes les méthodes HTTP (GET, POST, etc.)
              .AllowAnyHeader();       // Accepter tous les headers HTTP
    });
});

// Construire l'application
var app = builder.Build();

// Activer CORS pour toutes les routes
app.UseCors("AllowAll");

// Configuration - Définir les constantes pour le modèle et la langue
const string MODEL_PATH = "ggml-base.bin";    // Chemin vers le modèle Whisper
const string LANGUAGE = "fr";                  // Langue par défaut pour la transcription (français)
WhisperFactory? whisperFactory = null;         // Factory Whisper (null au début, sera initialisé)
SemaphoreSlim semaphore = new(1, 1);          // Sémaphore pour gérer une seule transcription à la fois

// Initialiser Whisper au démarrage de l'application
if (!File.Exists(MODEL_PATH))
{
    // Si le modèle n'existe pas, afficher un message d'erreur
    Console.WriteLine($"❌ Model not found: {MODEL_PATH}");
    Console.WriteLine("Please download the model first:");
    Console.WriteLine("Invoke-WebRequest -Uri 'https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin' -OutFile 'ggml-base.bin'");
}
else
{
    // Si le modèle existe, le charger en mémoire
    Console.WriteLine("🔄 Loading Whisper model...");
    whisperFactory = WhisperFactory.FromPath(MODEL_PATH);  // Charger le modèle depuis le fichier .bin
    Console.WriteLine("✅ Whisper model loaded!");
}

// Endpoint pour vérifier la santé de l'API (GET /api/health)
app.MapGet("/api/health", () =>
{
    // Retourner un JSON avec le statut de l'API
    return Results.Ok(new
    {
        status = "healthy",                    // Statut de l'API
        message = "Whisper API is running",    // Message de confirmation
        modelLoaded = whisperFactory != null   // Indique si le modèle est chargé
    });
});

// Endpoint principal pour la transcription (POST /api/transcription)
app.MapPost("/api/transcription", async (HttpContext context) =>
{
    // Vérifier si le modèle Whisper est chargé
    if (whisperFactory == null)
    {
        // Si le modèle n'est pas chargé, retourner une erreur 500
        return Results.Json(new { success = false, error = "Model not loaded" }, statusCode: 500);
    }

    // Lire le formulaire envoyé par le client (multipart/form-data)
    var form = await context.Request.ReadFormAsync();
    // Récupérer le fichier audio depuis le formulaire
    var file = form.Files.GetFile("file");

    // Vérifier si un fichier a été envoyé
    if (file == null || file.Length == 0)
    {
        // Si aucun fichier, retourner une erreur 400 (Bad Request)
        return Results.Json(new { success = false, error = "No file uploaded" }, statusCode: 400);
    }

    // Afficher les informations du fichier reçu
    Console.WriteLine($"📁 Received: {file.FileName} ({file.Length / 1024.0 / 1024.0:F2}MB)");

    // Attendre que le sémaphore soit disponible (une seule transcription à la fois)
    await semaphore.WaitAsync();

    try
    {
        // Créer un fichier temporaire pour sauvegarder l'audio uploadé
        var tempInputFile = Path.GetTempFileName();
        // Copier le contenu du fichier uploadé dans le fichier temporaire
        using (var stream = File.Create(tempInputFile))
        {
            await file.CopyToAsync(stream);
        }

        // Convertir l'audio en WAV 16kHz si nécessaire
        var processedFile = ConvertToWav16kHz(tempInputFile);

        // Démarrer la transcription
        Console.WriteLine("🎤 Transcribing...");
        // Créer une liste pour stocker les segments de transcription
        var segments = new List<object>();
        // Variable pour stocker la transcription complète
        var fullTranscript = "";

        // Créer le processeur Whisper avec les paramètres
        using (var processor = whisperFactory.CreateBuilder()
            .WithLanguage(LANGUAGE)                                              // Définir la langue
            .WithPrompt("Transcription en français. Ponctuation automatique.")   // Prompt pour guider le modèle
            .Build())
        {
            // Ouvrir le fichier audio traité en lecture
            using (var fileStream = File.OpenRead(processedFile))
            {
                // Traiter l'audio segment par segment (streaming)
                await foreach (var segment in processor.ProcessAsync(fileStream))
                {
                    // Créer un objet pour chaque segment avec timestamp et texte
                    var seg = new
                    {
                        start = segment.Start.TotalSeconds,  // Temps de début en secondes
                        end = segment.End.TotalSeconds,      // Temps de fin en secondes
                        text = segment.Text.Trim()           // Texte transcrit (sans espaces inutiles)
                    };
                    // Ajouter le segment à la liste
                    segments.Add(seg);
                    // Ajouter le texte à la transcription complète
                    fullTranscript += segment.Text.Trim() + " ";
                }
            }
        }

        // Attendre un peu pour s'assurer que tous les fichiers sont libérés
        await Task.Delay(100);
        
        // Nettoyer les fichiers temporaires
        try
        {
            // Supprimer le fichier d'entrée temporaire
            if (File.Exists(tempInputFile))
                File.Delete(tempInputFile);
            
            // Supprimer le fichier converti si différent du fichier d'entrée
            if (processedFile != tempInputFile && File.Exists(processedFile))
                File.Delete(processedFile);
        }
        catch (IOException)
        {
            // Ignorer les erreurs de suppression (fichier peut-être encore utilisé)
        }

        // Afficher le nombre de segments trouvés
        Console.WriteLine($"✅ Transcription complete: {segments.Count} segments");

        // Retourner le résultat en JSON
        return Results.Json(new
        {
            success = true,                          // Succès de l'opération
            transcript = fullTranscript.Trim(),      // Transcription complète
            segments = segments,                     // Liste des segments avec timestamps
            language = LANGUAGE,                     // Langue utilisée
            segmentCount = segments.Count            // Nombre total de segments
        });
    }
    catch (Exception ex)
    {
        // En cas d'erreur, afficher le message dans la console
        Console.WriteLine($"❌ Error: {ex.Message}");
        // Retourner une erreur 500 avec le message
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
    finally
    {
        // Libérer le sémaphore pour permettre une nouvelle transcription
        semaphore.Release();
    }
});

// Afficher les informations de démarrage dans la console
Console.WriteLine("🚀 API started on http://localhost:5000");
Console.WriteLine("📡 Endpoints:");
Console.WriteLine("   GET  /api/health");
Console.WriteLine("   POST /api/transcription");

// Démarrer l'application sur le port 5000
app.Run("http://localhost:5000");

// Fonction helper pour convertir l'audio en WAV 16kHz mono
string ConvertToWav16kHz(string inputFile)
{
    try
    {
        // Essayer de lire le fichier comme un WAV
        using (var reader = new WaveFileReader(inputFile))
        {
            // Vérifier si le format est déjà correct (16kHz, mono)
            if (reader.WaveFormat.SampleRate == 16000 && reader.WaveFormat.Channels == 1)
            {
                Console.WriteLine("✅ Audio already in correct format");
                return inputFile;  // Pas besoin de conversion
            }
        }
    }
    catch
    {
        // Si erreur de lecture, c'est probablement pas un WAV, on va le convertir
    }

    // Conversion nécessaire
    Console.WriteLine("🔄 Converting to WAV 16kHz...");
    // Créer un nom de fichier temporaire pour le résultat
    var outputFile = Path.GetTempFileName().Replace(".tmp", ".wav");

    try
    {
        // Ouvrir le fichier audio d'entrée (supporte MP3, FLAC, etc.)
        using var reader = new AudioFileReader(inputFile);
        // Définir le format de sortie : 16kHz, mono (1 canal)
        var outFormat = new WaveFormat(16000, 1);
        // Créer un resampler pour convertir le format
        using var resampler = new MediaFoundationResampler(reader, outFormat)
        {
            ResamplerQuality = 60  // Qualité de rééchantillonnage (0-60, 60 = meilleure qualité)
        };

        // Écrire le fichier WAV de sortie
        using (var writer = new WaveFileWriter(outputFile, resampler.WaveFormat))
        {
            // Créer un buffer pour lire les données par morceaux
            var buffer = new byte[resampler.WaveFormat.AverageBytesPerSecond * 4];
            int bytesRead;
            // Lire et écrire les données jusqu'à la fin du fichier
            while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
            {
                writer.Write(buffer, 0, bytesRead);
            }
        }

        // Afficher confirmation de conversion
        Console.WriteLine("✅ Conversion complete");
        return outputFile;  // Retourner le chemin du fichier converti
    }
    catch (Exception ex)
    {
        // En cas d'erreur, supprimer le fichier de sortie s'il existe
        if (File.Exists(outputFile))
            File.Delete(outputFile);
        // Relancer l'exception avec un message plus clair
        throw new Exception($"Conversion failed: {ex.Message}");
    }
}