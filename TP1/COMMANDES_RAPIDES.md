# 🚀 Commandes Rapides - TP1

## Création du Projet (À faire une seule fois)

```bash
# 1. Créer le dossier et le projet
mkdir TP1_PDF_Parser
cd TP1_PDF_Parser
dotnet new web -n PdfParserApi
cd PdfParserApi

# 2. Ajouter les packages
dotnet add package UglyToad.PdfPig
dotnet add package Tesseract
dotnet add package System.Drawing.Common
dotnet add package Swashbuckle.AspNetCore

# 3. Créer les dossiers
mkdir Models
mkdir Services

# 4. Copier les fichiers fournis :
#    - Models/PdfResponse.cs
#    - Services/PdfService.cs
#    - Program.cs
#    - PdfParserApi.csproj (remplacer l'existant)
```

---

## Commandes de Développement (Quotidiennes)

```bash
# Compiler le projet (vérifier les erreurs)
dotnet build

# Lancer l'application
dotnet run

# Nettoyer et recompiler
dotnet clean
dotnet build

# Restaurer les packages
dotnet restore

# Voir les packages installés
dotnet list package
```

---

## URLs Importantes

Quand l'application tourne (`dotnet run`) :

- **API racine** : http://localhost:5000
- **Swagger UI** : http://localhost:5000/swagger
- **Endpoint Parse** : POST http://localhost:5000/pdf/parse

---

## Test avec cURL

### Windows PowerShell
```powershell
curl -X POST http://localhost:5000/pdf/parse `
  -F "file=@C:\chemin\vers\fichier.pdf"
```

### Linux / macOS / Git Bash
```bash
curl -X POST http://localhost:5000/pdf/parse \
  -F "file=@/chemin/vers/fichier.pdf"
```

---

## Vérification de l'Installation

```bash
# Vérifier .NET
dotnet --version
# Attendu : 8.0.x ou 9.0.x

# Vérifier Tesseract
tesseract --version
# Attendu : tesseract 4.x.x ou 5.x.x

# Vérifier Git (optionnel)
git --version
```

---

## Git (Gestion de Version)

```bash
# Initialiser Git (première fois)
git init
git add .
git commit -m "Initial commit - TP1 PDF Parser"

# Créer .gitignore
cat > .gitignore << EOL
bin/
obj/
.vs/
.vscode/
*.user
*.suo
EOL

# Pousser sur GitHub (si vous avez un repo)
git remote add origin https://github.com/votre-username/TP1_PDF_Parser.git
git branch -M main
git push -u origin main
```

---

## Débogage

```bash
# Voir les logs détaillés
dotnet run --verbosity detailed

# Lancer en mode développement
export ASPNETCORE_ENVIRONMENT=Development  # Linux/Mac
$env:ASPNETCORE_ENVIRONMENT="Development"  # Windows PowerShell
dotnet run

# Nettoyer complètement
dotnet clean
rm -rf bin obj  # Linux/Mac
rmdir /s /q bin obj  # Windows
```

---

## Structure des Fichiers

```
TP1_PDF_Parser/
└── PdfParserApi/
    ├── Models/
    │   └── PdfResponse.cs
    ├── Services/
    │   └── PdfService.cs
    ├── bin/                    (généré)
    ├── obj/                    (généré)
    ├── Program.cs
    ├── PdfParserApi.csproj
    ├── appsettings.json        (optionnel)
    └── .gitignore              (recommandé)
```

---

## Raccourcis Clavier (Visual Studio)

- **F5** : Démarrer avec débogage
- **Ctrl + F5** : Démarrer sans débogage
- **F9** : Placer un point d'arrêt
- **F10** : Pas à pas principal
- **F11** : Pas à pas détaillé
- **Shift + F5** : Arrêter le débogage

---

## Raccourcis Clavier (VS Code)

- **F5** : Démarrer le débogage
- **Ctrl + Shift + B** : Build
- **Ctrl + `** : Ouvrir le terminal
- **Ctrl + Shift + P** : Palette de commandes

---

## Résolution Rapide des Problèmes

| Problème | Solution Rapide |
|----------|-----------------|
| Port 5000 occupé | Changer le port dans `Program.cs` ou tuer le processus |
| Tesseract not found | Vérifier installation et PATH système |
| Package not found | `dotnet restore` puis `dotnet build` |
| Erreur de compilation | Vérifier les `using` et les namespaces |
| OCR ne marche pas | Vérifier tessdata et fichiers .traineddata |

---

## Tests Postman - Configuration Rapide

1. **Nouvelle requête** : POST
2. **URL** : `http://localhost:5000/pdf/parse`
3. **Body** : form-data
4. **Clé** : `file` (type : File)
5. **Valeur** : Sélectionner un PDF
6. **Send** !

---

## Commandes Utiles Windows

```powershell
# Trouver un processus sur le port 5000
netstat -ano | findstr :5000

# Tuer un processus
taskkill /PID <numéro_pid> /F

# Vérifier le PATH
echo $env:Path

# Ajouter Tesseract au PATH (temporaire)
$env:Path += ";C:\Program Files\Tesseract-OCR"
```

---

## Commandes Utiles Linux/Mac

```bash
# Trouver un processus sur le port 5000
lsof -ti:5000

# Tuer un processus
kill -9 $(lsof -ti:5000)

# Vérifier le PATH
echo $PATH

# Ajouter au PATH (temporaire)
export PATH=$PATH:/usr/local/bin
```

---

## Variables d'Environnement

```bash
# Définir le port
export ASPNETCORE_URLS="http://localhost:5002"  # Linux/Mac
$env:ASPNETCORE_URLS="http://localhost:5002"    # Windows

# Définir l'environnement
export ASPNETCORE_ENVIRONMENT="Development"     # Linux/Mac
$env:ASPNETCORE_ENVIRONMENT="Development"       # Windows

# Chemin Tesseract (si besoin)
export TESSDATA_PREFIX="/usr/share/tessdata"    # Linux/Mac
$env:TESSDATA_PREFIX="C:\Program Files\Tesseract-OCR\tessdata"  # Windows
```

---

## Mémo JSON - Réponse Attendue

```json
{
  "title": "nom_du_fichier.pdf",
  "sections": [
    {
      "heading": "Titre de la section (ou null)",
      "text": "Contenu textuel de la section..."
    }
  ],
  "meta": {
    "pages": 5
  }
}
```

---

**Gardez ce fichier à portée de main pendant le développement !**
