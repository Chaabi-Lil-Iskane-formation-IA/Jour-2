# 🎤 Whisper.NET Transcripteur de Fichiers

Une application C# professionnelle pour la transcription audio hors ligne en utilisant le modèle Whisper d'OpenAI.

## 📖 Table des Matières

- [Aperçu](#aperçu)
- [Concepts Clés](#concepts-clés)
- [Prérequis](#prérequis)
- [Installation](#installation)
- [Utilisation](#utilisation)
- [Sélection du Modèle](#sélection-du-modèle)
- [Configuration](#configuration)
- [Dépannage](#dépannage)
- [Détails Techniques](#détails-techniques)

---

## 🎯 Aperçu

Cette application fournit une transcription vocale locale et hors ligne sans coûts d'API ni dépendance internet. Elle exploite le modèle Whisper d'OpenAI via la bibliothèque Whisper.NET, offrant une transcription de qualité professionnelle pour le français et plus de 99 autres langues.

### Fonctionnalités

- ✅ **100% Hors ligne** - Pas d'internet requis après l'installation initiale
- ✅ **Gratuit** - Pas de coûts d'API ou de limites d'utilisation
- ✅ **Rapide** - Inférence accélérée par le matériel
- ✅ **Multilingue** - Supporte 99+ langues dont le français
- ✅ **Horodaté** - Obtient des timestamps précis pour chaque segment
- ✅ **Respect de la vie privée** - L'audio ne quitte jamais votre machine

---

## 🧠 Concepts Clés

### Qu'est-ce que Whisper ?

**Whisper** est un système de reconnaissance automatique de la parole (ASR) développé par OpenAI, entraîné sur 680 000 heures de données multilingues et multitâches supervisées. Il est conçu pour être robuste face aux accents, au bruit de fond et au langage technique.

**Capacités clés :**
- Reconnaissance vocale multilingue
- Traduction de la parole
- Identification de la langue
- Ponctuation et majuscules automatiques

### Qu'est-ce que GGML ?

**GGML** (Georgi Gerganov Machine Learning) est une bibliothèque de tenseurs pour l'apprentissage automatique qui permet d'exécuter de grands modèles d'IA efficacement sur du matériel grand public.

**Caractéristiques clés :**
- Optimisé pour l'inférence CPU (supporte aussi GPU)
- Faible empreinte mémoire
- Vitesse d'inférence rapide
- Compatible multiplateforme

### Qu'est-ce que les fichiers .bin ?

Les fichiers `.bin` (ex : `ggml-base.bin`) sont des **modèles de réseaux de neurones pré-entraînés** au format GGML contenant :

- **Poids du réseau de neurones** : Des milliards de paramètres entraînés sur des données vocales
- **Vocabulaire** : Tokens pour la génération de texte
- **Modèles acoustiques** : Pour l'extraction de caractéristiques audio
- **Modèles de langage** : Pour 99+ langues

**Compromis Taille vs Performance :**

```
Tiny (75MB)    ━━━━━━━━━━          Rapide, précision basique
Base (140MB)   ━━━━━━━━━━━━━━      Équilibré (recommandé)
Small (460MB)  ━━━━━━━━━━━━━━━━    Meilleure précision
Medium (1.5GB) ━━━━━━━━━━━━━━━━━━  Haute précision
Large (2.9GB)  ━━━━━━━━━━━━━━━━━━━ Meilleure précision possible
```

### Comment ça marche ?

```
┌─────────────┐
│ Fichier     │
│  Audio      │
│  (.wav)     │
└──────┬──────┘
       │
       ▼
┌─────────────────┐
│  Bibliothèque   │  ← Wrapper C#
│  Whisper.NET    │
└──────┬──────────┘
       │
       ▼
┌─────────────────┐
│ ggml-base.bin   │  ← Modèle IA (le "cerveau")
│  (140MB)        │
└──────┬──────────┘
       │
       ▼
┌─────────────────┐
│ Texte           │
│ Transcrit       │
└─────────────────┘
```

**Processus :**
1. L'audio est chargé et prétraité (rééchantillonné à 16kHz)
2. Les caractéristiques audio sont extraites (spectrogrammes Mel)
3. Les caractéristiques sont envoyées au réseau de neurones
4. Le modèle génère des tokens de texte de manière probabiliste
5. Les tokens sont décodés en texte lisible avec des timestamps

---

## 📋 Prérequis

### Configuration Système

- **OS** : Windows 10/11, Linux, ou macOS
- **RAM** : 4GB minimum (8GB recommandé)
- **Espace Disque** : 
  - Tiny : 75MB
  - Base : 140MB
  - Small : 460MB
  - Medium : 1.5GB
  - Large : 2.9GB
- **CPU** : N'importe quel processeur x64 moderne
- **GPU** (optionnel) : GPU NVIDIA compatible CUDA pour l'accélération

### Logiciels Requis

- **.NET 8.0 SDK** ou ultérieur
  - Téléchargement : https://dotnet.microsoft.com/download
  - Vérifier l'installation : `dotnet --version`

---

## 🚀 Installation

### Étape 1 : Cloner ou Télécharger le Projet

```bash
git clone <url-de-votre-dépôt>
cd WhisperFileTranscriber
```

Ou téléchargez et extrayez le fichier ZIP.

### Étape 2 : Installer les Dépendances

```bash
dotnet restore
```

Cela installe :
- `Whisper.net` (v1.4.7) - Wrapper C#
- `Whisper.net.Runtime` (v1.4.7) - Bibliothèques natives
- `NAudio` (v2.2.1) - Conversion et traitement audio
- `NAudio` (v2.2.1) - Conversion audio automatique

### Étape 3 : Télécharger un Modèle Whisper

**Option A : Utiliser PowerShell (Windows)**

```powershell
# Télécharger le modèle base (recommandé)
Invoke-WebRequest -Uri "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin" -OutFile "ggml-base.bin"
```

**Option B : Utiliser curl (Linux/Mac)**

```bash
curl -L "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin" -o ggml-base.bin
```

**Option C : Téléchargement Manuel**

1. Visitez : https://huggingface.co/ggerganov/whisper.cpp/tree/main
2. Téléchargez le fichier du modèle choisi
3. Placez-le dans le répertoire racine du projet

### Étape 4 : Vérifier l'Installation

```bash
dotnet build
```

Vous devriez voir : `Build succeeded. 0 Warning(s)`

---

## 🎬 Utilisation

### Utilisation Basique

```bash
dotnet run
```

Par défaut, il transcrira `audio_16k.wav` dans le répertoire du projet.

### Transcrire un Fichier Spécifique

```bash
# Fichiers WAV
dotnet run chemin/vers/audio.wav

# Fichiers MP3
dotnet run interview.mp3

# Fichiers FLAC
dotnet run podcast.flac

# Fichiers M4A
dotnet run conference.m4a

# Avec chemin complet
dotnet run "C:\Users\Achraf\Documents\audio.mp3"
```

L'application accepte n'importe quel format audio et le convertit automatiquement si nécessaire.

### Formats Audio Supportés

- **WAV** (16-bit PCM, mono/stéréo)
- **MP3**
- **FLAC**
- **OGG**
- **M4A**
- **AAC**
- **WMA**

**Note :** L'application convertit automatiquement tous les formats audio en WAV 16kHz mono avant la transcription. Aucune conversion manuelle n'est nécessaire !

### Exemple de Sortie

**Avec un fichier MP3 :**
```
🎤 Whisper Transcripteur avec Conversion Auto
=============================================

📁 Fichier: interview.mp3
ℹ️  Format non-WAV détecté
⚠️  Conversion nécessaire vers WAV 16kHz mono
🔄 Conversion en cours...
✅ Conversion réussie!
   Taille: 5.20MB → 3.84MB

🔄 Chargement du modèle Whisper...
✅ Modèle chargé!
🎤 Transcription en cours...

[00:00:00.000 -> 00:00:03.500] Bonjour, bienvenue à cette interview.

[00:00:03.500 -> 00:00:07.200] Aujourd'hui nous allons parler de l'intelligence artificielle.

[00:00:07.200 -> 00:00:11.800] C'est un sujet fascinant qui transforme notre société.

================================================================================
📝 TRANSCRIPTION COMPLÈTE
================================================================================
Bonjour, bienvenue à cette interview. Aujourd'hui nous allons parler de 
l'intelligence artificielle. C'est un sujet fascinant qui transforme notre société.
================================================================================
Total segments: 3

✅ Terminé.
```

---

## 🔄 Conversion Audio Automatique

### Comment ça marche ?

L'application intègre un système de conversion automatique qui :

1. **Détecte le format** de votre fichier audio
2. **Vérifie les spécifications** (taux d'échantillonnage, nombre de canaux)
3. **Convertit automatiquement** si nécessaire en WAV 16kHz mono
4. **Transcrit** le fichier converti
5. **Nettoie** les fichiers temporaires après utilisation

### Processus de Conversion

```
Fichier Audio (n'importe quel format)
         ↓
┌────────────────────┐
│  Détection Format  │ → Est-ce déjà WAV 16kHz mono ?
└────────┬───────────┘
         │
         ├─→ OUI → Transcription directe
         │
         └─→ NON → Conversion automatique
                   ↓
              WAV 16kHz mono
                   ↓
              Transcription
                   ↓
              Nettoyage du fichier temporaire
```

### Formats Supportés par NAudio

| Format | Extension | Support |
|--------|-----------|---------|
| WAV | .wav | ✅ Natif |
| MP3 | .mp3 | ✅ Excellent |
| FLAC | .flac | ✅ Excellent |
| AAC | .aac, .m4a | ✅ Excellent |
| WMA | .wma | ✅ Bon |
| OGG | .ogg | ✅ Bon |
| AIFF | .aif, .aiff | ✅ Bon |

### Exemple de Conversion

**Commande :**
```bash
dotnet run interview.mp3
```

**Sortie :**
```
🎤 Whisper Transcripteur avec Conversion Auto
=============================================

📁 Fichier: interview.mp3
ℹ️  Format non-WAV détecté
⚠️  Conversion nécessaire vers WAV 16kHz mono
🔄 Conversion en cours...
✅ Conversion réussie!
   Taille: 5.42MB → 3.84MB

🔄 Chargement du modèle Whisper...
✅ Modèle chargé!
🎤 Transcription en cours...

[00:00:00.000 -> 00:00:03.500] Bonjour...
```

### Spécifications de Conversion

Les fichiers sont convertis avec les paramètres suivants :
- **Taux d'échantillonnage** : 16000 Hz (16 kHz)
- **Canaux** : 1 (mono)
- **Profondeur** : 16-bit PCM
- **Qualité de rééchantillonnage** : 60 (haute qualité)

### Avantages

✅ **Simplicité** : Pas besoin de préparer vos fichiers audio  
✅ **Universel** : Accepte presque tous les formats audio  
✅ **Automatique** : Conversion transparente en arrière-plan  
✅ **Efficace** : Suppression automatique des fichiers temporaires  
✅ **Qualité** : Rééchantillonnage haute qualité avec NAudio  

### Désactiver la Conversion (Optionnel)

Si vous voulez utiliser uniquement des fichiers WAV 16kHz pré-convertis, commentez la logique de conversion dans `Program.cs` :

```csharp
// Désactiver la conversion automatique
// string processedFile = await PrepareAudioFile(audioFile);
string processedFile = audioFile;
```

### Performances de Conversion

**Temps de conversion approximatifs :**

| Format Source | Taille | Durée Audio | Temps Conversion |
|---------------|--------|-------------|------------------|
| MP3 (128kbps) | 5MB | 5 minutes | ~2 secondes |
| FLAC | 25MB | 5 minutes | ~3 secondes |
| WAV (44.1kHz) | 50MB | 5 minutes | ~4 secondes |
| M4A (256kbps) | 10MB | 5 minutes | ~2 secondes |

**Impact sur la qualité :**
- La conversion de NAudio préserve la qualité audio
- Le rééchantillonnage à 16kHz est optimal pour Whisper
- La conversion mono n'affecte pas la précision de transcription
- Qualité de rééchantillonnage réglée à 60 (haute qualité)

---

## 🎛️ Sélection du Modèle

### Modèles Disponibles

| Modèle | Taille | Vitesse | Précision | Mémoire | Cas d'Usage |
|--------|--------|---------|-----------|---------|-------------|
| **tiny** | 75MB | ⚡⚡⚡⚡⚡ | ⭐⭐ | 1GB | Brouillons rapides, tests |
| **base** | 140MB | ⚡⚡⚡⚡ | ⭐⭐⭐ | 1GB | **Recommandé pour la plupart** |
| **small** | 460MB | ⚡⚡⚡ | ⭐⭐⭐⭐ | 2GB | Transcription haute qualité |
| **medium** | 1.5GB | ⚡⚡ | ⭐⭐⭐⭐⭐ | 5GB | Travail professionnel |
| **large** | 2.9GB | ⚡ | ⭐⭐⭐⭐⭐ | 10GB | Précision maximale |

### Changer de Modèle

**Étape 1 :** Télécharger le modèle désiré

```powershell
# Modèle Tiny
Invoke-WebRequest -Uri "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin" -OutFile "ggml-tiny.bin"

# Modèle Small
Invoke-WebRequest -Uri "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin" -OutFile "ggml-small.bin"

# Modèle Medium
Invoke-WebRequest -Uri "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin" -OutFile "ggml-medium.bin"

# Modèle Large (v3)
Invoke-WebRequest -Uri "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin" -OutFile "ggml-large-v3.bin"
```

**Étape 2 :** Mettre à jour `Program.cs`

```csharp
// Ligne 12 dans Program.cs
private const string MODEL_NAME = "ggml-small.bin";  // Changez cette ligne
```

**Étape 3 :** Recompiler et exécuter

```bash
dotnet build
dotnet run
```

### Comparaison des Performances

**Transcription de 1 minute d'audio :**

| Modèle | Temps CPU | Précision (WER) |
|--------|-----------|-----------------|
| tiny | ~5 secondes | ~15% d'erreur |
| base | ~10 secondes | ~10% d'erreur |
| small | ~30 secondes | ~7% d'erreur |
| medium | ~60 secondes | ~5% d'erreur |
| large | ~120 secondes | ~4% d'erreur |

*WER = Word Error Rate / Taux d'erreur sur les mots (plus bas = meilleur)*

---

## 🔄 Conversion Audio Automatique

L'application intègre un système de conversion audio automatique qui vous permet d'utiliser n'importe quel format audio sans préparation manuelle.

### Comment ça marche ?

Lorsque vous fournissez un fichier audio, l'application :

1. **Vérifie le format** - Détecte si le fichier est déjà en WAV 16kHz mono
2. **Convertit si nécessaire** - Convertit automatiquement vers le format requis
3. **Transcrit** - Lance la transcription sur le fichier préparé
4. **Nettoie** - Supprime automatiquement les fichiers temporaires

### Formats Acceptés

| Format | Extension | Support | Qualité |
|--------|-----------|---------|---------|
| WAV | .wav | ✅ Natif | Excellente |
| MP3 | .mp3 | ✅ Auto | Très bonne |
| FLAC | .flac | ✅ Auto | Excellente |
| OGG | .ogg | ✅ Auto | Très bonne |
| M4A | .m4a | ✅ Auto | Très bonne |
| AAC | .aac | ✅ Auto | Bonne |
| WMA | .wma | ✅ Auto | Bonne |

### Exemple d'Utilisation

**Avec un fichier MP3 :**
```bash
dotnet run interview.mp3
```

**Sortie :**
```
📁 Fichier: interview.mp3
ℹ️  Format non-WAV détecté
⚠️  Conversion nécessaire vers WAV 16kHz mono
🔄 Conversion en cours...
✅ Conversion réussie!
   Taille: 5.20MB → 3.84MB

🔄 Chargement du modèle Whisper...
✅ Modèle chargé!
🎤 Transcription en cours...
```

### Paramètres de Conversion

La conversion utilise les paramètres optimaux pour Whisper :

- **Fréquence d'échantillonnage** : 16000 Hz (16kHz)
- **Canaux** : 1 (mono)
- **Profondeur de bits** : 16-bit PCM
- **Qualité de rééchantillonnage** : 60 (haute qualité)

### Performance

| Fichier Source | Taille | Temps de Conversion |
|----------------|--------|---------------------|
| MP3 5min (5MB) | 5MB | ~2-3 secondes |
| FLAC 5min (30MB) | 30MB | ~3-5 secondes |
| WAV 16kHz | N/A | Instantané (pas de conversion) |

### Technologie Utilisée

La conversion audio est gérée par **NAudio**, une bibliothèque audio .NET puissante qui utilise :

- **MediaFoundation** (Windows) - Pour le décodage et rééchantillonnage
- **Support multi-format** - Gère automatiquement les différents codecs
- **Qualité professionnelle** - Algorithmes de rééchantillonnage de haute qualité

### Désactiver la Conversion Automatique

Si vous souhaitez utiliser uniquement des fichiers WAV 16kHz pré-convertis, vous pouvez désactiver la conversion en modifiant `Program.cs` :

```csharp
static Task<string> PrepareAudioFile(string inputFile)
{
    // Toujours retourner le fichier tel quel
    return Task.FromResult(inputFile);
}
```

---

## ⚙️ Configuration

### Paramètres de Langue

Modifiez `Program.cs` pour changer la langue de transcription :

```csharp
// Ligne 13
private const string LANGUAGE = "fr";  // Français
```

**Codes de langue courants :**
- `"en"` - Anglais
- `"fr"` - Français
- `"es"` - Espagnol
- `"de"` - Allemand
- `"it"` - Italien
- `"pt"` - Portugais
- `"ar"` - Arabe
- `"ja"` - Japonais
- `"zh"` - Chinois

**Détection automatique de la langue :**
```csharp
private const string LANGUAGE = "auto";  // Détection automatique
```

### Configuration Avancée

Dans la méthode `TranscribeFile`, vous pouvez personnaliser :

```csharp
using var processor = whisperFactory.CreateBuilder()
    .WithLanguage(LANGUAGE)
    .WithPrompt("Transcription en français. Ponctuation automatique.")
    .WithTemperature(0.0f)      // 0.0 = déterministe, 1.0 = créatif
    .WithMaxLength(448)          // Longueur max du segment
    .WithNoContext(false)        // Utiliser le contexte précédent
    .WithSingleSegment(false)    // Forcer sortie en un seul segment
    .Build();
```

### Emplacement du Fichier Audio

Changer le fichier audio par défaut :

```csharp
// Ligne 11
private const string AUDIO_FILE = "mon_audio.wav";
```

---

## 🔧 Dépannage

### Problème : "Modèle non trouvé"

**Symptôme :**
```
❌ Error: Model not found: ggml-base.bin
```

**Solution :**
1. Téléchargez le fichier du modèle (voir Installation Étape 3)
2. Assurez-vous que le fichier `.bin` est dans le répertoire racine du projet
3. Vérifiez que le nom du fichier correspond à `MODEL_NAME` dans `Program.cs`

---

### Problème : "Bibliothèque native non trouvée"

**Symptôme :**
```
❌ Error: Failed to load native whisper library
```

**Solution :**
```bash
# Nettoyer et restaurer les packages
dotnet clean
dotnet restore
dotnet build
```

Si ça ne fonctionne toujours pas :
```bash
# Forcer la réinstallation du runtime
dotnet remove package Whisper.net.Runtime
dotnet add package Whisper.net.Runtime --version 1.4.7
dotnet restore
```

---

### Problème : "Fichier audio non trouvé"

**Symptôme :**
```
❌ Error: File not found: audio_16k.wav
```

**Solution :**
1. Placez votre fichier audio dans la racine du projet
2. Ou exécutez avec un chemin explicite : `dotnet run "C:\chemin\vers\audio.wav"`
3. Ou mettez à jour la constante `AUDIO_FILE` dans `Program.cs`

---

### Problème : Échec de conversion audio

**Symptôme :**
```
❌ Échec de conversion: Could not load file or assembly 'NAudio'
```

**Solution :**
```bash
# Réinstaller NAudio
dotnet remove package NAudio
dotnet add package NAudio --version 2.2.1
dotnet restore
dotnet build
```

**Symptôme :**
```
❌ Échec de conversion: The request is not supported
```

**Solution :** Assurez-vous que Windows Media Foundation est installé (intégré dans Windows 10/11).

Pour les anciens systèmes, installez : [Media Feature Pack](https://support.microsoft.com/en-us/topic/media-feature-pack-list-for-windows-n-editions-c1c6fffa-d052-8338-7a79-a4bb980a700a)

---

### Problème : Transcription lente

**Solutions :**
1. Utilisez un modèle plus petit (`tiny` ou `base`)
2. Assurez-vous qu'il n'y a pas de processus lourds en arrière-plan
3. Fermez d'autres applications pour libérer de la RAM
4. Envisagez l'accélération GPU (nécessite une configuration CUDA)

---

### Problème : Qualité de transcription médiocre

**Solutions :**
1. Utilisez un modèle plus grand (`small`, `medium`, ou `large`)
2. Assurez-vous que la qualité audio est bonne (voix claire, bruit minimal)
3. Définissez le bon code de langue
4. Utilisez un prompt pour guider le modèle :
   ```csharp
   .WithPrompt("Interview technique sur l'intelligence artificielle en français.")
   ```

---

### Problème : Erreur de conversion audio

**Symptôme :**
```
❌ Error: Échec de conversion: [message d'erreur]
```

**Solutions :**
1. Vérifiez que le fichier audio n'est pas corrompu
2. Assurez-vous que NAudio est correctement installé :
   ```bash
   dotnet add package NAudio --version 2.2.1
   dotnet restore
   ```
3. Sur Windows, assurez-vous que Media Foundation est disponible (intégré depuis Windows 7)
4. Si le problème persiste, convertissez manuellement avec FFmpeg :
   ```bash
   ffmpeg -i input.mp3 -ar 16000 -ac 1 output.wav
   dotnet run output.wav
   ```

---

## 🔬 Détails Techniques

### Architecture

```
WhisperFileTranscriber/
├── Program.cs              # Logique principale + conversion audio
├── WhisperFileTranscriber.csproj  # Configuration du projet
├── ggml-base.bin           # Modèle IA (téléchargement séparé)
├── audio_16k.wav           # Fichier audio d'exemple
└── Properties/
    └── launchSettings.json # Paramètres de débogage

Flux de traitement:
Fichier Audio (MP3/WAV/FLAC/etc.) → NAudio → WAV 16kHz → Whisper → Texte
```

### Dépendances

```xml
<PackageReference Include="Whisper.net" Version="1.4.7" />
<PackageReference Include="Whisper.net.Runtime" Version="1.4.7" />
<PackageReference Include="NAudio" Version="2.2.1" />
```

**Whisper.net** : Wrapper C# fournissant une interface managée vers la bibliothèque Whisper native.

**Whisper.net.Runtime** : Contient les binaires natifs spécifiques à la plateforme :
- `whisper.dll` (Windows)
- `libwhisper.so` (Linux)
- `libwhisper.dylib` (macOS)

**NAudio** : Bibliothèque audio .NET pour la conversion et le traitement des fichiers audio.

### Pipeline de Traitement Audio

1. **Entrée** : Fichier audio dans divers formats (MP3, WAV, FLAC, etc.)
2. **Vérification** : Détection du format et des spécifications audio
3. **Conversion automatique** : Si nécessaire, conversion en WAV 16kHz mono via NAudio
4. **Décodage** : Conversion en données PCM brutes
5. **Extraction de Caractéristiques** : Calcul des spectrogrammes Mel
6. **Inférence** : Passage des caractéristiques dans le réseau de neurones
7. **Décodage** : Conversion des tokens de sortie en texte
8. **Post-traitement** : Ajout de ponctuation, timestamps
9. **Nettoyage** : Suppression des fichiers temporaires

### Utilisation de la Mémoire

| Modèle | Utilisation RAM | VRAM (GPU) |
|--------|-----------------|------------|
| tiny | ~400MB | ~200MB |
| base | ~600MB | ~300MB |
| small | ~1.2GB | ~600MB |
| medium | ~2.5GB | ~1.5GB |
| large | ~4.5GB | ~3GB |

### Optimisation des Performances

**Optimisation CPU :**
- Whisper.net utilise le multi-threading automatiquement
- Les performances augmentent avec le nombre de cœurs CPU
- Les instructions AVX2 fournissent une accélération ~2x

**Accélération GPU :**
- Actuellement limitée dans Whisper.net
- Pour le support GPU, envisagez d'utiliser Python avec OpenAI Whisper

### Facteurs de Précision

**Impacts positifs :**
- Audio clair et de haute qualité
- Bruit de fond minimal
- Accents natifs ou quasi-natifs
- Vocabulaire technique dans le prompt
- Modèles plus grands

**Impacts négatifs :**
- Mauvaise qualité audio
- Bruit de fond important
- Accents prononcés
- Plusieurs locuteurs
- Parole très rapide
- Modèles plus petits

---

## 🎓 Ressources d'Apprentissage

### Comprendre Whisper
- [Article Whisper d'OpenAI](https://arxiv.org/abs/2212.04356)
- [Whisper GitHub](https://github.com/openai/whisper)

### Comprendre GGML
- [GGML GitHub](https://github.com/ggerganov/ggml)
- [Implémentation Whisper.cpp](https://github.com/ggerganov/whisper.cpp)

### Documentation Whisper.NET
- [Whisper.net GitHub](https://github.com/sandrohanea/whisper.net)
- [Package NuGet](https://www.nuget.org/packages/Whisper.net/)

---

## 📄 Licence

Ce projet utilise :
- **Modèle Whisper** : Licence MIT (OpenAI)
- **Whisper.net** : Licence MIT
- **GGML** : Licence MIT

---

## 🤝 Contribuer

Les contributions sont les bienvenues ! Domaines d'amélioration :
- Support d'accélération GPU
- Transcription en streaming temps réel
- Traitement par lots de plusieurs fichiers
- Options de format de sortie (SRT, VTT, JSON)
- Interface web

---

## ⚡ Référence Rapide

### Commandes Courantes

```bash
# Compiler le projet
dotnet build

# Exécuter avec un fichier WAV
dotnet run audio.wav

# Exécuter avec un fichier MP3
dotnet run podcast.mp3

# Exécuter avec un fichier FLAC
dotnet run interview.flac

# Avec chemin complet
dotnet run "D:\Audio\conference.m4a"

# Nettoyer et recompiler
dotnet clean && dotnet restore && dotnet build

# Vérifier la version .NET
dotnet --version

# Installer/réinstaller NAudio
dotnet add package NAudio --version 2.2.1
```

### Liens de Téléchargement des Modèles

```
Tiny:   https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin
Base:   https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin
Small:  https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin
Medium: https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin
Large:  https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin
```

### Support des Formats de Fichiers

| Format | Supporté | Conversion | Notes |
|--------|----------|------------|-------|
| WAV (16kHz) | ✅ | ❌ Pas nécessaire | Traitement direct |
| WAV (autre) | ✅ | ✅ Automatique | Rééchantillonnage |
| MP3 | ✅ | ✅ Automatique | Très courant |
| FLAC | ✅ | ✅ Automatique | Haute qualité |
| OGG | ✅ | ✅ Automatique | Open source |
| M4A | ✅ | ✅ Automatique | Format Apple |
| AAC | ✅ | ✅ Automatique | Compression moderne |
| WMA | ✅ | ✅ Automatique | Format Windows |

---

**Construit avec ❤️ en utilisant Whisper.NET**