using PdfParserApi.Services;
using System.Text.Json;

// ==============================================================================
// CRÉATION ET CONFIGURATION DE L'APPLICATION WEB
// ==============================================================================

// Créer un builder pour configurer l'application web
// WebApplication.CreateBuilder() initialise tous les services nécessaires
var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------------------------------------------
// CONFIGURATION DES SERVICES (Dependency Injection)
// ------------------------------------------------------------------------------

// Ajouter le service PDF comme un service "Singleton"
// Singleton = une seule instance partagée pour toute l'application
// Cela signifie que PdfService sera créé une fois et réutilisé
builder.Services.AddSingleton<PdfService>();

// Activer les "endpoints" de l'API (les routes HTTP)
builder.Services.AddEndpointsApiExplorer();

// Ajouter Swagger pour la documentation automatique de l'API
// Swagger génère une page web interactive pour tester l'API
builder.Services.AddSwaggerGen(options =>
{
    // Configurer les informations de base de l'API dans Swagger
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PDF Parser API",
        Version = "v1",
        Description = "API pour extraire et structurer le contenu des fichiers PDF",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "TP1 - Lecture & Structuration d'un PDF"
        }
    });
});

// Configurer CORS (Cross-Origin Resource Sharing)
// CORS permet aux applications web (ex: React) d'accéder à l'API depuis un autre domaine
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()      // Autoriser toutes les origines (pour le développement)
              .AllowAnyMethod()      // Autoriser tous les verbes HTTP (GET, POST, etc.)
              .AllowAnyHeader();     // Autoriser tous les en-têtes HTTP
    });
});

// ------------------------------------------------------------------------------
// CONSTRUCTION DE L'APPLICATION
// ------------------------------------------------------------------------------

// Construire l'application avec toutes les configurations définies ci-dessus
var app = builder.Build();

// ------------------------------------------------------------------------------
// CONFIGURATION DU PIPELINE HTTP (Middleware)
// ------------------------------------------------------------------------------

// Activer Swagger uniquement en mode développement
// En production, on désactive généralement Swagger pour des raisons de sécurité
if (app.Environment.IsDevelopment())
{
    // Activer l'interface Swagger UI (accessible à /swagger)
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PDF Parser API v1");
        options.RoutePrefix = "swagger"; // URL : http://localhost:5000/swagger
    });
}

// Activer CORS avec la politique "AllowAll" définie plus haut
app.UseCors("AllowAll");

// ------------------------------------------------------------------------------
// DÉFINITION DES ENDPOINTS (Routes de l'API)
// ------------------------------------------------------------------------------

// Endpoint racine (page d'accueil de l'API)
// GET http://localhost:5000/
app.MapGet("/", () => new
{
    message = "Bienvenue sur l'API PDF Parser",
    version = "1.0",
    endpoints = new
    {
        parse = "POST /pdf/parse - Upload et parse un fichier PDF",
        swagger = "GET /swagger - Documentation interactive de l'API"
    }
})
.WithName("Root")
.WithTags("Info")
.Produces(200); // Code HTTP 200 = Succès

// ------------------------------------------------------------------------------
// ENDPOINT PRINCIPAL : POST /pdf/parse
// ------------------------------------------------------------------------------

/// <summary>
/// Endpoint pour uploader et parser un fichier PDF
/// </summary>
/// <param name="file">Le fichier PDF uploadé (multipart/form-data)</param>
/// <param name="pdfService">Le service PDF injecté automatiquement</param>
/// <returns>Un objet JSON structuré avec le contenu du PDF</returns>
app.MapPost("/pdf/parse", async (IFormFile file, PdfService pdfService) =>
{
    // ----------------------------------------------------------------------
    // VALIDATION DU FICHIER UPLOADÉ
    // ----------------------------------------------------------------------

    // Vérifier si un fichier a été uploadé
    if (file == null || file.Length == 0)
    {
        // Retourner une erreur 400 (Bad Request) si pas de fichier
        return Results.BadRequest(new
        {
            error = "Aucun fichier n'a été uploadé",
            message = "Veuillez fournir un fichier PDF valide"
        });
    }

    // Vérifier l'extension du fichier (doit être .pdf)
    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (fileExtension != ".pdf")
    {
        // Retourner une erreur 400 si le fichier n'est pas un PDF
        return Results.BadRequest(new
        {
            error = "Type de fichier invalide",
            message = $"Le fichier doit être un PDF. Type reçu : {fileExtension}"
        });
    }

    // Vérifier la taille du fichier (limite : 10 MB)
    const long maxFileSize = 10 * 1024 * 1024; // 10 MB en bytes
    if (file.Length > maxFileSize)
    {
        return Results.BadRequest(new
        {
            error = "Fichier trop volumineux",
            message = $"La taille maximale autorisée est de 10 MB. Taille du fichier : {file.Length / 1024 / 1024} MB"
        });
    }

    // ----------------------------------------------------------------------
    // TRAITEMENT DU FICHIER PDF
    // ----------------------------------------------------------------------

    try
    {
        // Afficher un message dans la console pour le suivi
        Console.WriteLine($"📄 Traitement du fichier : {file.FileName} ({file.Length / 1024} KB)");

        // Ouvrir le flux du fichier uploadé
        // "using" garantit que le flux sera fermé automatiquement
        using var stream = file.OpenReadStream();

        // Appeler le service PDF pour parser le fichier
        // Cette opération peut prendre du temps selon la taille du PDF
        var result = await pdfService.ParsePdfAsync(stream, file.FileName);

        // Afficher un message de succès
        Console.WriteLine($"✅ Fichier traité avec succès : {result.Sections.Count} sections extraites");

        // Retourner le résultat en JSON avec code 200 (OK)
        // JsonSerializer.Serialize permet de contrôler le format JSON
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        // En cas d'erreur, afficher l'erreur dans la console
        Console.WriteLine($"❌ Erreur lors du traitement du PDF : {ex.Message}");
        Console.WriteLine($"Stack trace : {ex.StackTrace}");

        // Retourner une erreur 500 (Internal Server Error)
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Erreur lors du traitement du PDF"
        );
    }
})
.WithName("ParsePdf")                          // Nom de l'endpoint (pour Swagger)
.WithTags("PDF")                              // Tag/catégorie dans Swagger
.Accepts<IFormFile>("multipart/form-data")    // Type de contenu accepté
.Produces(200)                                // Code HTTP de succès
.Produces(400)                                // Code HTTP pour requête invalide
.Produces(500)                                // Code HTTP pour erreur serveur
.DisableAntiforgery();                        // Désactiver la vérification antiforgery (nécessaire pour les uploads)

// ==============================================================================
// DÉMARRAGE DE L'APPLICATION
// ==============================================================================

// Afficher les URLs où l'application est accessible
Console.WriteLine("========================================");
Console.WriteLine("🚀 API PDF Parser démarrée !");
Console.WriteLine("========================================");
Console.WriteLine($"📍 URL : http://localhost:{builder.Configuration["ASPNETCORE_HTTP_PORT"] ?? "5000"}");
Console.WriteLine($"📖 Swagger : http://localhost:{builder.Configuration["ASPNETCORE_HTTP_PORT"] ?? "5000"}/swagger");
Console.WriteLine("========================================");
Console.WriteLine();
Console.WriteLine("Endpoints disponibles :");
Console.WriteLine("  GET  /           - Informations sur l'API");
Console.WriteLine("  POST /pdf/parse  - Parser un fichier PDF");
Console.WriteLine();
Console.WriteLine("Appuyez sur Ctrl+C pour arrêter l'application");
Console.WriteLine("========================================");

// Démarrer l'application et écouter les requêtes HTTP
// Cette ligne bloque le programme jusqu'à ce qu'il soit arrêté
app.Run();