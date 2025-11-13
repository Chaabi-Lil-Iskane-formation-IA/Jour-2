using PdfParserApi.Models;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Tesseract;
using System.Drawing;
using System.Drawing.Imaging;
using Docnet.Core;
using Docnet.Core.Models;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace PdfParserApi.Services;

/// <summary>
/// Service responsable de l'extraction et de la structuration du contenu des PDF
/// Ce service gère à la fois l'extraction de texte natif et l'OCR pour les images
/// </summary>
public class PdfService
{
    /// <summary>
    /// Chemin vers les données linguistiques de Tesseract (pour l'OCR)
    /// Tesseract a besoin de fichiers de données pour reconnaître le texte dans différentes langues
    /// </summary>
    private readonly string _tessDataPath;

    /// <summary>
    /// Constructeur du service PDF
    /// </summary>
    public PdfService()
    {
        // Détecter automatiquement le chemin vers les données Tesseract selon le système d'exploitation
        // Windows : C:\Program Files\Tesseract-OCR\tessdata
        // Linux/Mac : /usr/share/tesseract-ocr/4.00/tessdata ou /usr/local/share/tessdata
        _tessDataPath = GetTessDataPath();
    }

    /// <summary>
    /// Méthode principale pour parser un PDF et extraire son contenu structuré
    /// </summary>
    /// <param name="stream">Le flux de données du fichier PDF uploadé</param>
    /// <param name="fileName">Le nom du fichier PDF</param>
    /// <returns>Un objet PdfResponse contenant toutes les informations extraites</returns>
    public async Task<PdfResponse> ParsePdfAsync(Stream stream, string fileName)
    {
        // Créer l'objet de réponse avec le titre du document
        var response = new PdfResponse
        {
            Title = fileName
        };

        // Copier le stream en mémoire pour pouvoir le réutiliser (pour l'OCR)
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        // Ouvrir le document PDF avec la bibliothèque PdfPig
        // "using" assure que le document sera fermé automatiquement à la fin
        using (var document = PdfDocument.Open(memoryStream))
        {
            // Stocker le nombre total de pages dans les métadonnées
            response.Meta.Pages = document.NumberOfPages;

            // Parcourir chaque page du document (les pages commencent à 1, pas 0)
            foreach (var page in document.GetPages())
            {
                // Tenter d'extraire le texte natif de la page avec analyse de structure
                var pageText = ExtractTextFromPage(page);
                List<PdfSection> sections;

                // Si la page ne contient pas de texte (probablement une image scannée)
                // alors utiliser l'OCR pour extraire le texte
                if (string.IsNullOrWhiteSpace(pageText))
                {
                    Console.WriteLine($"Page {page.Number} : Texte natif vide, utilisation de l'OCR...");
                    memoryStream.Position = 0; // Réinitialiser le stream pour Docnet
                    pageText = await ExtractTextFromImagePageAsync(memoryStream, page.Number);
                    
                    // Pour l'OCR, utiliser l'analyse de texte simple car on n'a pas les infos de police
                    sections = ParseIntoSectionsSimple(pageText);
                }
                else
                {
                    // Pour le texte natif, utiliser l'analyse avancée avec les infos de structure
                    sections = ExtractStructuredSections(page);
                }

                // Ajouter toutes les sections trouvées à la liste de sections de la réponse
                if (sections.Count > 0)
                {
                    response.Sections.AddRange(sections);
                }
            }
        }

        // Si aucune section n'a été trouvée, créer une section par défaut
        // Cela évite de retourner un tableau vide
        if (response.Sections.Count == 0)
        {
            response.Sections.Add(new PdfSection
            {
                Heading = null,
                Text = "Aucun contenu textuel détecté dans ce PDF."
            });
        }

        return response;
    }

    /// <summary>
    /// Extrait le texte natif d'une page PDF
    /// Cette méthode fonctionne pour les PDF contenant du texte réel (pas des images)
    /// </summary>
    /// <param name="page">La page PDF à traiter</param>
    /// <returns>Le texte extrait de la page</returns>
    private string ExtractTextFromPage(UglyToad.PdfPig.Content.Page page)
    {
        try
        {
            // Récupérer le texte de la page
            // PdfPig extrait automatiquement le texte dans l'ordre de lecture
            var text = page.Text;

            // Nettoyer le texte :
            // - Trim() : enlève les espaces au début et à la fin
            return text.Trim();
        }
        catch (Exception ex)
        {
            // En cas d'erreur, afficher le message et retourner une chaîne vide
            Console.WriteLine($"Erreur lors de l'extraction du texte de la page {page.Number}: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Extrait les sections structurées d'une page PDF en utilisant l'analyse de layout
    /// Détecte les titres basés sur la taille de police et la position
    /// </summary>
    /// <param name="page">La page PDF à analyser</param>
    /// <returns>Liste de sections avec titres détectés</returns>
    private List<PdfSection> ExtractStructuredSections(UglyToad.PdfPig.Content.Page page)
    {
        var sections = new List<PdfSection>();

        try
        {
            var letters = page.Letters;
            
            // Si pas de lettres, retourner vide
            if (letters == null || !letters.Any())
            {
                return sections;
            }

            // 1. Extraire les mots
            var wordExtractor = NearestNeighbourWordExtractor.Instance;
            var words = wordExtractor.GetWords(letters);

            // 2. Segmenter la page en blocs de texte
            var pageSegmenter = DocstrumBoundingBoxes.Instance;
            var textBlocks = pageSegmenter.GetBlocks(words);

            // 3. Ordonner les blocs selon l'ordre de lecture
            var readingOrder = UnsupervisedReadingOrderDetector.Instance;
            var orderedTextBlocks = readingOrder.Get(textBlocks);

            // Calculer la taille de police moyenne pour détecter les titres
            var allFontSizes = letters.Select(l => l.PointSize).ToList();
            var averageFontSize = allFontSizes.Any() ? allFontSizes.Average() : 12;
            var headingThreshold = averageFontSize * 1.2; // Les titres sont généralement 20% plus grands

            Console.WriteLine($"Taille de police moyenne: {averageFontSize:F1}, seuil titre: {headingThreshold:F1}");

            string? currentHeading = null;
            var currentText = new StringBuilder();

            // 4. Parcourir les blocs dans l'ordre de lecture
            foreach (var block in orderedTextBlocks)
            {
                var blockText = block.Text.Trim();
                
                if (string.IsNullOrWhiteSpace(blockText))
                    continue;

                // Obtenir la taille de police moyenne du bloc
                var blockLetters = block.TextLines
                    .SelectMany(line => line.Words)
                    .SelectMany(word => word.Letters);
                
                var blockFontSize = blockLetters.Any() 
                    ? blockLetters.Average(l => l.PointSize) 
                    : averageFontSize;

                // Vérifier si c'est probablement un titre
                var isHeading = IsLikelyHeadingByFont(blockText, blockFontSize, headingThreshold);

                if (isHeading)
                {
                    // Sauvegarder la section précédente si elle existe
                    if (currentText.Length > 0)
                    {
                        sections.Add(new PdfSection
                        {
                            Heading = currentHeading,
                            Text = currentText.ToString().Trim()
                        });
                        currentText.Clear();
                    }

                    // Ce bloc est un nouveau titre
                    currentHeading = blockText;
                    Console.WriteLine($"  → Titre détecté: '{blockText}' (taille: {blockFontSize:F1})");
                }
                else
                {
                    // Ce bloc est du texte normal
                    if (currentText.Length > 0)
                    {
                        currentText.Append(" ");
                    }
                    currentText.Append(blockText);
                }
            }

            // Ajouter la dernière section
            if (currentText.Length > 0)
            {
                sections.Add(new PdfSection
                {
                    Heading = currentHeading,
                    Text = currentText.ToString().Trim()
                });
            }

            Console.WriteLine($"  → {sections.Count} section(s) extraite(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'extraction structurée: {ex.Message}");
            // Fallback sur l'extraction simple
            var text = page.Text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                sections = ParseIntoSectionsSimple(text);
            }
        }

        return sections;
    }

    /// <summary>
    /// Détermine si un bloc de texte est probablement un titre basé sur la taille de police
    /// </summary>
    private bool IsLikelyHeadingByFont(string text, double fontSize, double threshold)
    {
        // Critère 1: Taille de police supérieure au seuil
        if (fontSize < threshold)
            return false;

        // Critère 2: Texte court (moins de 100 caractères)
        if (text.Length > 100)
            return false;

        // Critère 3: Peu de mots (moins de 15)
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount > 15)
            return false;

        return true;
    }

    /// <summary>
    /// Extrait le texte d'une page PDF image en utilisant l'OCR (Optical Character Recognition)
    /// Cette méthode utilise Docnet pour rendre le PDF en image, puis Tesseract pour l'OCR
    /// </summary>
    /// <param name="pdfStream">Le flux du PDF complet</param>
    /// <param name="pageNumber">Le numéro de la page à traiter (commence à 1)</param>
    /// <returns>Le texte reconnu par l'OCR</returns>
    private async Task<string> ExtractTextFromImagePageAsync(Stream pdfStream, int pageNumber)
    {
        try
        {
            // Vérifier si Tesseract est disponible sur le système
            if (!IsTesseractAvailable())
            {
                Console.WriteLine("Tesseract OCR n'est pas installé ou n'est pas dans le PATH système.");
                return string.Empty;
            }

            // Créer un chemin temporaire pour sauvegarder l'image de la page
            var tempImagePath = Path.Combine(Path.GetTempPath(), $"ocr_page_{pageNumber}_{Guid.NewGuid()}.png");

            try
            {
                // Utiliser Docnet pour convertir la page PDF en image
                using (var library = DocLib.Instance)
                {
                    // Copier le stream en byte array pour Docnet
                    byte[] pdfBytes;
                    if (pdfStream is MemoryStream ms)
                    {
                        pdfBytes = ms.ToArray();
                    }
                    else
                    {
                        using (var tempMs = new MemoryStream())
                        {
                            await pdfStream.CopyToAsync(tempMs);
                            pdfBytes = tempMs.ToArray();
                        }
                    }

                    // Ouvrir le PDF avec Docnet
                    using (var docReader = library.GetDocReader(pdfBytes, new PageDimensions(1.5)))
                    {
                        // Obtenir le lecteur de la page (index 0-based, donc pageNumber - 1)
                        using (var pageReader = docReader.GetPageReader(pageNumber - 1))
                        {
                            // Obtenir les dimensions de la page
                            var width = pageReader.GetPageWidth();
                            var height = pageReader.GetPageHeight();
                            
                            Console.WriteLine($"Rendu de la page {pageNumber} en image ({width}x{height} pixels)...");

                            // Obtenir les bytes de l'image de la page
                            var rawBytes = pageReader.GetImage();

                            // Créer un Bitmap à partir des bytes
                            using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                            {
                                // Verrouiller les bits du bitmap pour pouvoir écrire dedans
                                var bmpData = bitmap.LockBits(
                                    new Rectangle(0, 0, width, height),
                                    ImageLockMode.WriteOnly,
                                    bitmap.PixelFormat);

                                // Copier les bytes de l'image dans le bitmap
                                System.Runtime.InteropServices.Marshal.Copy(
                                    rawBytes, 0, bmpData.Scan0, rawBytes.Length);

                                // Déverrouiller les bits
                                bitmap.UnlockBits(bmpData);

                                // Sauvegarder l'image temporairement
                                bitmap.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.Png);
                                Console.WriteLine($"Image sauvegardée temporairement : {tempImagePath}");
                            }
                        }
                    }
                }

                // Maintenant utiliser Tesseract pour faire l'OCR sur l'image sauvegardée
                Console.WriteLine($"Exécution de l'OCR sur la page {pageNumber}...");
                
                using (var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default))
                {
                    // Charger l'image depuis le fichier temporaire
                    using (var pix = Pix.LoadFromFile(tempImagePath))
                    {
                        // Effectuer la reconnaissance de texte
                        using (var recognizedPage = engine.Process(pix))
                        {
                            // Extraire le texte reconnu
                            var text = recognizedPage.GetText();
                            
                            // Obtenir le niveau de confiance de la reconnaissance
                            var confidence = recognizedPage.GetMeanConfidence();
                            Console.WriteLine($"✅ OCR terminé avec {confidence:P0} de confiance");
                            
                            // Nettoyer et retourner le texte
                            return text.Trim();
                        }
                    }
                }
            }
            finally
            {
                // Nettoyer : supprimer le fichier temporaire
                if (File.Exists(tempImagePath))
                {
                    try
                    {
                        File.Delete(tempImagePath);
                        Console.WriteLine($"Fichier temporaire supprimé : {tempImagePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Impossible de supprimer le fichier temporaire : {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // En cas d'erreur OCR, afficher le message
            Console.WriteLine($"❌ Erreur OCR sur la page {pageNumber}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Découpe le texte extrait en sections logiques (version simple pour OCR)
    /// Détecte les titres basés sur des heuristiques de texte
    /// </summary>
    /// <param name="text">Le texte brut extrait</param>
    /// <returns>Une liste de sections structurées</returns>
    private List<PdfSection> ParseIntoSectionsSimple(string text)
    {
        var sections = new List<PdfSection>();

        // Diviser le texte en lignes
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // Variables pour construire les sections
        string? currentHeading = null;
        var currentText = new StringBuilder();

        foreach (var line in lines)
        {
            // Nettoyer la ligne (enlever espaces superflus)
            var cleanLine = line.Trim();

            // Ignorer les lignes vides
            if (string.IsNullOrWhiteSpace(cleanLine))
                continue;

            // Heuristique améliorée pour détecter un titre avec OCR
            if (IsLikelyHeadingFromOCR(cleanLine))
            {
                // Si on a du texte accumulé, créer une section avec ce texte
                if (currentText.Length > 0)
                {
                    sections.Add(new PdfSection
                    {
                        Heading = currentHeading,
                        Text = currentText.ToString().Trim()
                    });

                    // Réinitialiser le texte accumulé
                    currentText.Clear();
                }

                // Définir le nouveau titre actuel
                currentHeading = cleanLine;
                Console.WriteLine($"  → Titre détecté (OCR): '{cleanLine}'");
            }
            else
            {
                // Ajouter la ligne au texte en cours
                if (currentText.Length > 0)
                {
                    currentText.Append(" ");
                }
                currentText.Append(cleanLine);
            }
        }

        // Ajouter la dernière section accumulée
        if (currentText.Length > 0)
        {
            sections.Add(new PdfSection
            {
                Heading = currentHeading,
                Text = currentText.ToString().Trim()
            });
        }

        // Si aucune section n'a été créée, créer une section unique avec tout le texte
        if (sections.Count == 0 && !string.IsNullOrWhiteSpace(text))
        {
            sections.Add(new PdfSection
            {
                Heading = null,
                Text = text.Trim()
            });
        }

        Console.WriteLine($"  → {sections.Count} section(s) extraite(s) (OCR)");

        return sections;
    }

    /// <summary>
    /// Détermine si une ligne est probablement un titre (pour texte OCR)
    /// Heuristiques améliorées
    /// </summary>
    private bool IsLikelyHeadingFromOCR(string line)
    {
        // Ligne courte (moins de 80 caractères pour être plus permissif)
        if (line.Length > 80)
            return false;

        // Commence par une majuscule, un chiffre, ou un pattern de numérotation
        if (!char.IsUpper(line[0]) && !char.IsDigit(line[0]))
            return false;

        // Ne se termine pas par un point (sauf si c'est un nombre comme "1.")
        if (line.EndsWith('.') && !char.IsDigit(line[line.Length - 2]))
            return false;

        // Peu de mots (moins de 12 mots)
        var wordCount = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount > 12)
            return false;

        // Patterns communs de titres
        var headingPatterns = new[]
        {
            @"^\d+\.",           // "1. Introduction"
            @"^[A-Z][a-z]+:",    // "Introduction:"
            @"^[A-Z\s]+$",       // "INTRODUCTION" (tout en majuscules)
            @"^\d+\s+[A-Z]",     // "1 Introduction"
            @"^Chapter\s+\d+",   // "Chapter 1"
            @"^Chapitre\s+\d+",  // "Chapitre 1"
        };

        foreach (var pattern in headingPatterns)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(line, pattern))
            {
                return true;
            }
        }

        // Si ligne très courte (< 30 chars) et commence par majuscule
        if (line.Length < 30 && char.IsUpper(line[0]) && wordCount <= 5)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Vérifie si Tesseract OCR est installé et accessible sur le système
    /// </summary>
    /// <returns>True si Tesseract est disponible, False sinon</returns>
    private bool IsTesseractAvailable()
    {
        try
        {
            // Tenter de créer une instance de TesseractEngine
            // Si cela échoue, Tesseract n'est pas installé
            using (var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default))
            {
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Détecte automatiquement le chemin vers les données Tesseract
    /// selon le système d'exploitation
    /// </summary>
    /// <returns>Le chemin vers le dossier tessdata</returns>
    private string GetTessDataPath()
    {
        // Liste des chemins possibles où Tesseract peut être installé
        var possiblePaths = new List<string>();

        // Chemins Windows
        if (OperatingSystem.IsWindows())
        {
            possiblePaths.Add(@"C:\Program Files\Tesseract-OCR\tessdata");
            possiblePaths.Add(@"C:\Program Files (x86)\Tesseract-OCR\tessdata");
            possiblePaths.Add(@"C:\Tesseract-OCR\tessdata");
        }

        // Chemins Linux
        if (OperatingSystem.IsLinux())
        {
            possiblePaths.Add("/usr/share/tesseract-ocr/4.00/tessdata");
            possiblePaths.Add("/usr/share/tesseract-ocr/5/tessdata");
            possiblePaths.Add("/usr/share/tessdata");
            possiblePaths.Add("/usr/local/share/tessdata");
        }

        // Chemins macOS
        if (OperatingSystem.IsMacOS())
        {
            possiblePaths.Add("/usr/local/share/tessdata");
            possiblePaths.Add("/opt/homebrew/share/tessdata");
            possiblePaths.Add("/usr/local/Cellar/tesseract/");
        }

        // Chercher le premier chemin qui existe
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                Console.WriteLine($"Tesseract tessdata trouvé à : {path}");
                return path;
            }
        }

        // Si aucun chemin n'est trouvé, retourner un chemin par défaut
        // L'utilisateur devra configurer manuellement
        Console.WriteLine("Attention : Chemin tessdata par défaut utilisé. Configurez TESSDATA_PREFIX si nécessaire.");
        return "/usr/share/tessdata"; // Chemin par défaut
    }
}