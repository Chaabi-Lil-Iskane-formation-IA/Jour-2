# 📘 Guide Étudiant - TP1 : Lecture & Structuration d'un PDF

## 🎯 Objectif du TP

Créer une API REST simple qui peut :
1. Recevoir un fichier PDF
2. Extraire le texte (natif ou avec OCR)
3. Structurer le contenu en JSON
4. Retourner le résultat structuré

---

## 📝 Étapes à Suivre (Pas à Pas)

### Étape 1️⃣ : Vérifier les Prérequis

Avant de commencer, ouvrez un terminal et vérifiez :

```bash
# Vérifier .NET (devrait afficher 8.0.x ou 9.0.x)
dotnet --version

# Vérifier Tesseract (devrait afficher la version)
tesseract --version
```

❌ **Si une commande échoue**, retournez dans le README.md section "Prérequis" pour l'installer.

---

### Étape 2️⃣ : Créer le Projet

Ouvrez un terminal dans le dossier où vous voulez créer votre projet :

```bash
# Créer le dossier principal
mkdir TP1_PDF_Parser
cd TP1_PDF_Parser

# Créer le projet .NET Minimal API
dotnet new web -n PdfParserApi

# Entrer dans le dossier du projet
cd PdfParserApi
```

✅ **Vérification** : Vous devriez voir un fichier `Program.cs` et `PdfParserApi.csproj`

---

### Étape 3️⃣ : Ajouter les Packages NuGet

Dans le même terminal, exécutez :

```bash
# Package pour lire les PDF
dotnet add package PdfPig --version 0.1.11

# Package pour l'OCR (reconnaissance de texte dans les images)
dotnet add package Tesseract

# Package pour manipuler les images
dotnet add package System.Drawing.Common

# Package pour la documentation Swagger
dotnet add package Swashbuckle.AspNetCore
```

✅ **Vérification** : La commande `dotnet list package` devrait afficher les 4 packages.

---

### Étape 4️⃣ : Créer la Structure des Dossiers

```bash
# Créer les dossiers pour organiser le code
mkdir Models
mkdir Services
```

Votre structure devrait ressembler à :
```
PdfParserApi/
├── Models/
├── Services/
├── Program.cs
└── PdfParserApi.csproj
```

---

### Étape 5️⃣ : Créer les Fichiers du Projet

#### 5.1 - Créer `Models/PdfResponse.cs`

Ce fichier définit la structure de nos données (le "modèle").

**Créez le fichier** : `Models/PdfResponse.cs`

**Copiez le contenu** depuis le fichier fourni dans le dossier `Models/`.

**Explication rapide** :
- `PdfSection` : Représente une section du document (titre + texte)
- `PdfMetadata` : Informations sur le document (nombre de pages)
- `PdfResponse` : La réponse complète envoyée au client

---

#### 5.2 - Créer `Services/PdfService.cs`

Ce fichier contient toute la logique pour traiter le PDF.

**Créez le fichier** : `Services/PdfService.cs`

**Copiez le contenu** depuis le fichier fourni dans le dossier `Services/`.

**Explication rapide** :
- `ParsePdfAsync()` : Fonction principale qui traite le PDF
- `ExtractTextFromPage()` : Extrait le texte natif d'une page
- `ExtractTextFromImagePageAsync()` : Utilise l'OCR si la page est une image
- `ParseIntoSections()` : Découpe le texte en sections logiques

---

#### 5.3 - Remplacer `Program.cs`

Ce fichier est le point d'entrée de l'application.

**Remplacez le contenu** de `Program.cs` par le fichier fourni.

**Explication rapide** :
- Configure l'application web
- Ajoute les services (PdfService, Swagger, CORS)
- Définit les routes de l'API :
  - `GET /` : Page d'accueil
  - `POST /pdf/parse` : Endpoint principal pour parser les PDF

---

### Étape 6️⃣ : Compiler le Projet

Avant de lancer, vérifions qu'il n'y a pas d'erreurs :

```bash
dotnet build
```

✅ **Succès si** : "Build succeeded. 0 Warning(s). 0 Error(s)"

❌ **Si erreur** :
- Vérifiez que tous les fichiers sont bien créés
- Vérifiez les `using` en haut des fichiers
- Relisez les messages d'erreur attentivement

---

### Étape 7️⃣ : Lancer l'Application

```bash
dotnet run
```

✅ **Succès si vous voyez** :
```
🚀 API PDF Parser démarrée !
📍 URL : http://localhost:5000
📖 Swagger : http://localhost:5000/swagger
```

❌ **Si erreur "Port already in use"** :
- Un autre programme utilise déjà le port 5000
- Changez le port dans `Program.cs` ou tuez l'autre processus

---

### Étape 8️⃣ : Tester l'API avec le Navigateur

Ouvrez votre navigateur et allez à :
```
http://localhost:5000
```

Vous devriez voir :
```json
{
  "message": "Bienvenue sur l'API PDF Parser",
  "version": "1.0",
  "endpoints": { ... }
}
```

Ensuite, allez à :
```
http://localhost:5000/swagger
```

Vous verrez l'interface Swagger pour tester l'API visuellement ! 🎉

---

### Étape 9️⃣ : Tester avec Postman

#### 9.1 - Ouvrir Postman

#### 9.2 - Créer une nouvelle requête
- Cliquer sur "New" → "HTTP Request"

#### 9.3 - Configurer la requête
1. **Méthode** : Changer de `GET` à `POST`
2. **URL** : `http://localhost:5000/pdf/parse`
3. **Body** :
   - Cliquer sur l'onglet "Body"
   - Sélectionner "form-data"
   - Ajouter une clé : `file`
   - Changer le type de "Text" à "File" (à droite)
   - Cliquer sur "Select Files" et choisir un PDF

#### 9.4 - Envoyer la requête
- Cliquer sur "Send"
- Observer la réponse JSON en bas

#### ✅ Exemple de réponse attendue :
```json
{
  "title": "document.pdf",
  "sections": [
    {
      "heading": null,
      "text": "Ceci est le premier paragraphe du document..."
    },
    {
      "heading": "Introduction",
      "text": "Le texte de l'introduction..."
    }
  ],
  "meta": {
    "pages": 3
  }
}
```

---

### Étape 🔟 : Tester avec un PDF Image (OCR)

Pour tester l'OCR :
1. Créez un PDF contenant uniquement une image de texte (scanné)
2. Uploadez-le via Postman
3. L'API devrait extraire le texte avec Tesseract

**Dans la console**, vous devriez voir :
```
Page 1 : Texte natif vide, utilisation de l'OCR...
```

---

## 🐛 Problèmes Courants et Solutions

### Problème 1 : "Tesseract not found"

**Cause** : Tesseract n'est pas installé ou pas dans le PATH

**Solution Windows** :
1. Télécharger depuis : https://github.com/UB-Mannheim/tesseract/wiki
2. Installer dans `C:\Program Files\Tesseract-OCR`
3. Ajouter au PATH système
4. **Redémarrer** le terminal et l'IDE

**Solution Linux/Mac** :
```bash
# Linux
sudo apt-get install tesseract-ocr tesseract-ocr-fra

# Mac
brew install tesseract tesseract-lang
```

---

### Problème 2 : "Port 5000 is already in use"

**Solution 1** : Tuer le processus qui utilise le port
```bash
# Windows
netstat -ano | findstr :5000
taskkill /PID <le_numéro_du_processus> /F

# Linux/Mac
lsof -ti:5000 | xargs kill -9
```

**Solution 2** : Changer le port dans `Program.cs`
```csharp
builder.WebHost.UseUrls("http://localhost:5002");
```

---

### Problème 3 : L'OCR ne fonctionne pas

**Vérifications** :
1. Tesseract est-il installé ? `tesseract --version`
2. Les données de langue sont-elles présentes ?
   - Windows : `C:\Program Files\Tesseract-OCR\tessdata\`
   - Linux : `/usr/share/tesseract-ocr/*/tessdata/`
3. Le fichier `fra.traineddata` existe-t-il pour le français ?

---

### Problème 4 : Erreur de compilation

**Vérifiez** :
- Tous les fichiers sont créés dans les bons dossiers
- Les `namespace` correspondent : `PdfParserApi.Models`, `PdfParserApi.Services`
- Les packages sont installés : `dotnet list package`

---

## 📊 Comprendre le Flux de Données

```
1. Client (Postman) 
   ↓ 
   Envoie un fichier PDF via POST /pdf/parse
   ↓
2. Program.cs (Endpoint)
   ↓
   Valide le fichier (taille, type)
   ↓
3. PdfService.ParsePdfAsync()
   ↓
   Ouvre le PDF avec PdfPig
   ↓
4. Pour chaque page :
   ├─→ Tente d'extraire le texte natif
   │   └─→ Si vide → Lance l'OCR avec Tesseract
   ↓
5. ParseIntoSections()
   ↓
   Détecte les titres et paragraphes
   ↓
6. Retour JSON structuré
   ↓
7. Client reçoit la réponse
```

---

## ✅ Checklist de Validation Finale

Avant de rendre votre TP, vérifiez :

- [ ] Le projet compile sans erreurs (`dotnet build`)
- [ ] L'application démarre (`dotnet run`)
- [ ] L'endpoint GET / fonctionne (navigateur)
- [ ] Swagger est accessible à /swagger
- [ ] POST /pdf/parse accepte un PDF avec du texte
- [ ] La réponse JSON est bien formatée
- [ ] L'OCR fonctionne avec un PDF scanné
- [ ] Tous les fichiers sont commentés
- [ ] Le code est indenté proprement
- [ ] Le projet est sur Git/GitHub

---

## 🎓 Questions de Compréhension

Pour vérifier votre compréhension :

1. **Quelle est la différence entre extraction de texte natif et OCR ?**
   - Texte natif : Le PDF contient du texte sélectionnable
   - OCR : Le PDF contient des images, il faut reconnaître le texte

2. **Pourquoi utilise-t-on `using` avec les streams ?**
   - Pour libérer automatiquement les ressources (mémoire)

3. **Qu'est-ce qu'un endpoint dans une API REST ?**
   - Une URL spécifique qui accepte des requêtes HTTP

4. **À quoi sert Swagger ?**
   - Générer une documentation interactive de l'API

5. **Pourquoi structurer en sections ?**
   - Pour faciliter la lecture vocale ultérieure (TP suivants)

---

## 📚 Pour Aller Plus Loin

Si vous finissez en avance, essayez d'ajouter :

1. **Extraction des images du PDF**
2. **Détection automatique de la langue** (eng, fra, ara)
3. **Support de formats supplémentaires** (DOCX, TXT)
4. **Validation plus poussée** des fichiers
5. **Logs détaillés** avec `ILogger`
6. **Tests unitaires** avec xUnit

---

## 💡 Conseils Pratiques

### Pour Déboguer
1. Utilisez `Console.WriteLine()` partout
2. Testez avec un PDF simple (1 page) d'abord
3. Regardez les messages d'erreur dans la console

### Pour Apprendre
1. Lisez tous les commentaires dans le code
2. Modifiez une chose à la fois et testez
3. Utilisez le débogueur de Visual Studio (F5 puis F10/F11)

### Pour Réussir
1. Suivez les étapes dans l'ordre
2. Testez après chaque modification
3. N'hésitez pas à demander de l'aide

---

**Bon courage ! 🚀**

Si vous avez des questions, relisez d'abord :
1. Les commentaires dans le code
2. Ce guide
3. Le README.md principal

Ensuite, demandez de l'aide à votre enseignant.
