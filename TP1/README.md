# TP1 : Lecture & Structuration d'un PDF

## 📋 Description du Projet

Ce projet est une API REST minimaliste en .NET qui permet d'extraire et structurer le contenu textuel d'un document PDF (texte natif ou image avec OCR).

## 🎯 Objectifs

- Extraire le texte d'un PDF
- Extraire le texte d'images dans un PDF (OCR)
- Structurer le contenu en JSON
- Créer une API REST simple

## 📦 Prérequis

Avant de commencer, vous devez avoir installé :

### 1. SDK .NET
- Télécharger et installer .NET 8 ou 9 : https://dotnet.microsoft.com/download
- Vérifier l'installation :
```bash
dotnet --version
```

### 2. IDE (un seul suffit)
- **Visual Studio 2022** (recommandé pour Windows) : https://visualstudio.microsoft.com/
- **JetBrains Rider** : https://www.jetbrains.com/rider/
- **Visual Studio Code** : https://code.visualstudio.com/
  - Avec l'extension "C# Dev Kit"

### 3. Tesseract OCR (pour l'extraction de texte depuis les images)

#### Windows :
1. Télécharger l'installeur depuis : https://github.com/UB-Mannheim/tesseract/wiki
2. **IMPORTANT** : Pendant l'installation, cocher "Additional language data" → **English** (obligatoire)
3. Installer dans : `C:\Program Files\Tesseract-OCR`
4. Ajouter au PATH système :
   - Panneau de configuration → Système → Variables d'environnement
   - Dans "Variables système", éditer "Path"
   - Ajouter : `C:\Program Files\Tesseract-OCR`
5. **Redémarrer votre terminal/IDE**

#### macOS :
```bash
brew install tesseract
brew install tesseract-lang  # Pour le français (optionnel)
```

#### Linux (Ubuntu/Debian) :
```bash
sudo apt-get update
sudo apt-get install tesseract-ocr
sudo apt-get install tesseract-ocr-eng  # Anglais (obligatoire)
sudo apt-get install tesseract-ocr-fra  # Français (optionnel)
```

Vérifier l'installation :
```bash
tesseract --version
```

### 4. Postman (pour tester l'API)
- Télécharger : https://www.postman.com/downloads/

## 🚀 Étapes de Création du Projet

### Étape 1 : Créer le Projet .NET
```bash
# Créer un nouveau dossier pour le projet
mkdir TP1_PDF_Parser
cd TP1_PDF_Parser

# Créer une Minimal API .NET
dotnet new web -n PdfParserApi

# Aller dans le dossier du projet
cd PdfParserApi
```

### Étape 2 : Installer les Packages NuGet Nécessaires
```bash
# Installation de PdfPig (lecture de PDF et analyse de structure)
dotnet add package PdfPig --version 0.1.11

# Installation de Tesseract (OCR pour images)
dotnet add package Tesseract --version 5.2.0

# Installation de System.Drawing.Common (manipulation d'images)
dotnet add package System.Drawing.Common --version 8.0.0

# Installation de Docnet.Core (conversion PDF vers image pour OCR)
dotnet add package Docnet.Core --version 2.6.0

# Installation de Swashbuckle (documentation Swagger)
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
```

**OU en une seule commande :**
```bash
dotnet add package UglyToad.PdfPig --version 0.1.8 && dotnet add package Tesseract --version 5.2.0 && dotnet add package System.Drawing.Common --version 8.0.0 && dotnet add package Docnet.Core --version 2.6.0 && dotnet add package Swashbuckle.AspNetCore --version 6.5.0
```

### Étape 3 : Créer la Structure du Projet

Votre projet devra avoir la structure suivante :
```
PdfParserApi/
│
├── Program.cs                 # Point d'entrée de l'API
├── Models/
│   └── PdfResponse.cs        # Modèles de données pour la réponse JSON
├── Services/
│   └── PdfService.cs         # Logique d'extraction de PDF
├── appsettings.json          # Configuration (optionnel)
├── PdfParserApi.csproj       # Fichier de projet
└── README.md                 # Ce fichier
```

### Étape 4 : Créer les Dossiers
```bash
# Dans le dossier PdfParserApi
mkdir Models
mkdir Services
```

## 📝 Fichiers du Projet

### 1. Models/PdfResponse.cs

Ce fichier contient les modèles de données pour structurer la réponse JSON.
```csharp
// Voir le fichier Models/PdfResponse.cs fourni
```

### 2. Services/PdfService.cs

Ce fichier contient toute la logique d'extraction et de structuration du PDF.

**Fonctionnalités :**
- Extraction de texte natif avec analyse de structure (taille de police, position)
- OCR avec Tesseract via Docnet.Core pour les PDF scannés
- Détection intelligente des titres (par taille de police pour texte natif, par patterns pour OCR)
- Structuration en sections avec titres
```csharp
// Voir le fichier Services/PdfService.cs fourni
```

### 3. Program.cs

Le point d'entrée principal de l'application avec la définition de l'API.
```csharp
// Voir le fichier Program.cs fourni
```

## 🏃 Exécution du Projet

### Méthode 1 : Ligne de Commande
```bash
# Dans le dossier PdfParserApi
dotnet run
```

L'API sera accessible à : `http://localhost:5000` ou `http://localhost:5001` (HTTPS)

### Méthode 2 : Visual Studio

1. Ouvrir le fichier `PdfParserApi.sln` ou `PdfParserApi.csproj`
2. Appuyer sur `F5` ou cliquer sur "▶ Start"

### Méthode 3 : VS Code

1. Ouvrir le dossier du projet
2. Appuyer sur `F5` et sélectionner ".NET Core"

## 🧪 Tester l'API avec Postman

### Configuration de la Requête

1. **Ouvrir Postman**
2. **Créer une nouvelle requête** :
   - Méthode : `POST`
   - URL : `http://localhost:5000/pdf/parse`

3. **Configuration du Body** :
   - Sélectionner l'onglet "Body"
   - Choisir "form-data"
   - Ajouter une clé : `file`
   - Changer le type de "Text" à "File"
   - Sélectionner un fichier PDF

4. **Envoyer la Requête** :
   - Cliquer sur "Send"
   - Observer la réponse JSON

### Exemple de Réponse Attendue
```json
{
  "title": "document.pdf",
  "sections": [
    {
      "heading": "Introduction",
      "text": "Ceci est le contenu de l'introduction..."
    },
    {
      "heading": "Section 1",
      "text": "Contenu de la section 1..."
    },
    {
      "heading": null,
      "text": "Un paragraphe sans titre..."
    }
  ],
  "meta": {
    "pages": 3
  }
}
```

## 🧪 Tester l'API avec cURL

### Windows PowerShell :
```powershell
curl -X POST http://localhost:5000/pdf/parse `
  -F "file=@C:\chemin\vers\votre\fichier.pdf"
```

### Linux/macOS :
```bash
curl -X POST http://localhost:5000/pdf/parse \
  -F "file=@/chemin/vers/votre/fichier.pdf"
```

## 🧪 Tester avec Swagger

Ouvrez votre navigateur à : **http://localhost:5000/swagger**

Vous verrez une interface interactive pour tester l'API directement !

## 🐛 Résolution des Problèmes Courants

### Erreur : "Tesseract not found" ou "Error opening data file"

**Cause** : Tesseract n'est pas installé ou le fichier de langue `eng.traineddata` est manquant.

**Solution Windows** :
1. Télécharger `eng.traineddata` depuis : https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
2. Placer le fichier dans : `C:\Program Files\Tesseract-OCR\tessdata\`
3. Vérifier que le dossier `tessdata` existe et contient `eng.traineddata`
4. Redémarrer votre terminal et IDE

**Solution Linux** :
```bash
sudo apt-get install tesseract-ocr-eng
```

**Solution macOS** :
```bash
brew reinstall tesseract
```

### Erreur : "Port already in use"

**Solution** : Changer le port dans `Program.cs` :
```csharp
builder.WebHost.UseUrls("http://localhost:5002");
```

### Erreur : "Unable to load DLL 'pdfium'"

**Cause** : Le package Docnet.Core nécessite des dépendances natives.

**Solution** :
```bash
# Réinstaller le package
dotnet remove package Docnet.Core
dotnet add package Docnet.Core --version 2.6.0

# Nettoyer et rebuild
dotnet clean
dotnet build
```

### PDF scannés ne retournent aucun texte

**Vérifications** :
1. Tesseract est-il installé ? `tesseract --version`
2. Le fichier `eng.traineddata` existe-t-il dans `tessdata` ?
3. Les logs dans la console montrent-ils "Exécution de l'OCR..." ?

**Si le problème persiste** :
```bash
# Windows - Définir la variable d'environnement
$env:TESSDATA_PREFIX = "C:\Program Files\Tesseract-OCR\tessdata"

# Linux/Mac
export TESSDATA_PREFIX="/usr/share/tesseract-ocr/4.00/tessdata"
```

### Les titres ne sont pas détectés

**Pour PDF avec texte natif** : Les titres sont détectés par taille de police (20% plus grands que la moyenne).

**Pour PDF scannés (OCR)** : Les titres sont détectés par patterns :
- Commence par un chiffre : "1. Introduction"
- Tout en majuscules : "INTRODUCTION"
- Ligne courte (<80 chars) commençant par majuscule

**Astuce** : Regardez les logs dans la console pour voir ce qui est détecté.

## 📚 Structure du Code Expliquée

### PdfService.cs
```
ParsePdfAsync()
    ├── Ouvre le PDF avec PdfPig
    ├── Pour chaque page:
    │   ├── Tente d'extraire le texte natif
    │   │   └── Si texte trouvé → ExtractStructuredSections()
    │   │       ├── Analyse les blocs de texte
    │   │       ├── Détecte titres par taille de police
    │   │       └── Structure en sections
    │   │
    │   └── Si vide (PDF scanné) → ExtractTextFromImagePageAsync()
    │       ├── Convertit PDF en image (Docnet.Core)
    │       ├── Lance OCR (Tesseract)
    │       └── ParseIntoSectionsSimple()
    │           ├── Détecte titres par patterns
    │           └── Structure en sections
    │
    └── Retourne PdfResponse structuré
```

### Technologies Utilisées

| Package | Version | Rôle |
|---------|---------|------|
| UglyToad.PdfPig | 0.1.8 | Extraction texte + analyse structure |
| Tesseract | 5.2.0 | OCR (reconnaissance caractères) |
| System.Drawing.Common | 8.0.0 | Manipulation images |
| Docnet.Core | 2.6.0 | Conversion PDF → Image |
| Swashbuckle.AspNetCore | 6.5.0 | Documentation Swagger |

## 📖 Pour Aller Plus Loin

### Améliorations Possibles

1. **Détection automatique de la langue** pour l'OCR
2. **Extraction des images** et des tableaux
3. **Identification des en-têtes et pieds de page**
4. **Support de formats supplémentaires** (DOCX, TXT)
5. **Pagination** pour gros documents
6. **Cache** des résultats
7. **Amélioration de la détection des titres** avec machine learning

### Ressources Utiles

- Documentation PdfPig : https://github.com/UglyToad/PdfPig
- Documentation Tesseract : https://tesseract-ocr.github.io/
- Documentation Docnet.Core : https://github.com/GowenGit/docnet
- Documentation .NET Minimal APIs : https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis

## 🎓 Notes pour l'Enseignant

Ce projet est conçu pour être :
- **Simple** : Minimal API sans complexité inutile
- **Commenté** : Chaque ligne expliquée en français
- **Progressif** : Base pour les TPs suivants (IA, voix)
- **Pratique** : Résultats visibles immédiatement
- **Robuste** : Gère texte natif ET PDF scannés

### Points d'Attention pour les Étudiants

1. **Installation Tesseract** : C'est souvent la source de problèmes
2. **Fichiers de langue** : `eng.traineddata` doit être présent
3. **PATH système** : Doit être configuré correctement
4. **Redémarrage** : Souvent nécessaire après installation Tesseract

### Démonstrations Recommandées

1. **PDF texte natif** : Montrer la détection de titres par taille de police
2. **PDF scanné** : Montrer l'OCR en action (logs dans la console)
3. **Swagger** : Montrer l'interface de test interactive

## ✅ Checklist de Validation

- [ ] Le projet compile sans erreurs (`dotnet build`)
- [ ] L'API démarre sur le port 5000 (`dotnet run`)
- [ ] GET / retourne les infos de l'API
- [ ] POST /pdf/parse accepte un fichier PDF
- [ ] La réponse JSON est bien formatée
- [ ] **Texte natif** : Les titres sont détectés par taille de police
- [ ] **PDF scanné** : L'OCR fonctionne et extrait le texte
- [ ] **PDF scanné** : Les titres sont détectés par patterns
- [ ] Swagger accessible à /swagger
- [ ] Le code est commenté et compréhensible

## 📞 Support

En cas de problème :
1. Vérifier les versions des packages (`dotnet list package`)
2. Consulter les logs dans la console
3. Tester avec un PDF simple (1-2 pages)
4. Vérifier que Tesseract est installé (`tesseract --version`)
5. Vérifier que `eng.traineddata` existe dans le dossier `tessdata`

### Logs Importants à Surveiller
```
✅ Tesseract tessdata trouvé à : C:\Program Files\Tesseract-OCR\tessdata
✅ Taille de police moyenne: 12.0, seuil titre: 14.4
✅ → Titre détecté: 'Introduction' (taille: 18.0)
✅ Page 1 : Texte natif vide, utilisation de l'OCR...
✅ OCR terminé avec 92% de confiance
✅ → Titre détecté (OCR): '1. Introduction'
```

---

**Durée estimée du TP** : 1h30 - 2h  
**Niveau** : Débutant à Intermédiaire  
**Prérequis** : Bases de C# et HTTP  
**Fonctionnalités** : Texte natif + OCR + Détection titres intelligente