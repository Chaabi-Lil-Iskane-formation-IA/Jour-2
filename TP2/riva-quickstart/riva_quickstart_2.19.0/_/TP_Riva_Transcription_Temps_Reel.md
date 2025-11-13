# TP : Transcription Vocale en Temps Réel avec NVIDIA Riva

## Table des Matières

1. [Introduction](#introduction)
2. [Qu'est-ce que NVIDIA Riva ?](#quest-ce-que-nvidia-riva)
3. [Prérequis](#prérequis)
4. [Installation de NVIDIA Riva](#installation-de-nvidia-riva)
5. [Code Python : Explication Détaillée](#code-python--explication-détaillée)
6. [WebSocket pour la Diffusion en Temps Réel](#websocket-pour-la-diffusion-en-temps-réel)
7. [Client HTML pour Tester](#client-html-pour-tester)
8. [Support Multilingue](#support-multilingue)
9. [Dépannage](#dépannage)
10. [Exercices Pratiques](#exercices-pratiques)

---

## Introduction

Ce TP vous guide dans la création d'un système de transcription vocale en temps réel utilisant NVIDIA Riva. Vous apprendrez à :

- Installer et configurer NVIDIA Riva
- Capturer l'audio depuis un microphone
- Transcrire la parole en texte en temps réel
- Diffuser les transcriptions via WebSocket
- Créer une interface web pour visualiser les résultats

**Durée estimée :** 3-4 heures

**Niveau :** Intermédiaire

---

## Qu'est-ce que NVIDIA Riva ?

### Présentation

**NVIDIA Riva** est un SDK (Software Development Kit) pour créer des applications d'IA vocale avec :

- **ASR (Automatic Speech Recognition)** : Reconnaissance vocale automatique
- **TTS (Text-to-Speech)** : Synthèse vocale
- **NLP (Natural Language Processing)** : Traitement du langage naturel
- **NMT (Neural Machine Translation)** : Traduction automatique

### Caractéristiques Principales

1. **Faible Latence** : Conçu pour les applications en temps réel (300-500ms)
2. **Haute Précision** : Utilise des modèles d'IA de pointe (Conformer, Parakeet)
3. **Multilingue** : Support de plus de 15 langues
4. **Optimisé GPU** : Utilise NVIDIA TensorRT pour des performances maximales
5. **Streaming** : Transcription continue en temps réel

### Architecture

```
┌─────────────┐
│ Microphone  │
└──────┬──────┘
       │ Audio brut (PCM 16kHz)
       ▼
┌─────────────────┐
│  Riva Client    │ (Python/C++/Java)
│  (votre app)    │
└────────┬────────┘
         │ gRPC (streaming bidirectionnel)
         ▼
┌─────────────────┐
│  Riva Server    │
│  ┌───────────┐  │
│  │  Triton   │  │ (Serveur d'inférence)
│  └─────┬─────┘  │
│        │        │
│  ┌─────▼─────┐  │
│  │  Modèles  │  │ (Conformer ASR, etc.)
│  │    GPU    │  │
│  └───────────┘  │
└────────┬────────┘
         │ Texte transcrit
         ▼
┌─────────────────┐
│  Application    │
└─────────────────┘
```

### Cas d'Usage

- Sous-titrage en direct
- Assistants vocaux
- Transcription de réunions
- Centres d'appels
- Accessibilité (malentendants)
- Commande vocale

---

## Prérequis

### Matériel

- **GPU NVIDIA** (Compute Capability 7.0+)
  - RTX 20xx, 30xx, 40xx, 50xx
  - Tesla T4, V100, A100
  - Minimum 6 GB VRAM (12 GB recommandé)
- **RAM** : 16 GB minimum (32 GB recommandé)
- **Disque** : 20 GB d'espace libre
- **Microphone** : N'importe quel microphone USB ou intégré

### Logiciels

- **Système d'exploitation** :
  - Windows 10/11 (avec WSL2 optionnel)
  - Linux (Ubuntu 20.04+, CentOS 8+)
  - macOS non supporté (pas de GPU NVIDIA)

- **Docker** : Version 20.10+
- **NVIDIA Container Toolkit** (pour accès GPU dans Docker)
- **Python** : 3.8+
- **Pilotes NVIDIA** : Version récente (525+)

### Connaissances Requises

- Bases de Python
- Ligne de commande (bash/cmd)
- Concepts de base en réseaux (ports, IP)
- Notions de Docker (utile mais pas obligatoire)

---

## Installation de NVIDIA Riva

### Étape 1 : Vérifier le GPU et Docker

#### 1.1 Vérifier que Docker peut accéder au GPU

```bash
docker run --rm --gpus all nvidia/cuda:11.8.0-base-ubuntu22.04 nvidia-smi
```

**Résultat attendu :**
```
+-----------------------------------------------------------------------------------------+
| NVIDIA-SMI 577.02                 Driver Version: 577.02         CUDA Version: 12.9     |
|-----------------------------------------+------------------------+----------------------+
| GPU  Name                 Persistence-M | Bus-Id          Disp.A | Volatile Uncorr. ECC |
|=========================================+========================+======================|
|   0  NVIDIA GeForce RTX 5070 ...    On  |   00000000:01:00.0 Off |                  N/A |
+-----------------------------------------+------------------------+----------------------+
```

✅ **Si vous voyez votre GPU, continuez.**  
❌ **Si erreur, installez NVIDIA Container Toolkit :**

**Linux :**
```bash
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | \
    sudo tee /etc/apt/sources.list.d/nvidia-docker.list

sudo apt-get update
sudo apt-get install -y nvidia-docker2
sudo systemctl restart docker
```

**Windows :**
- Docker Desktop doit être configuré avec WSL2
- Les pilotes NVIDIA Windows incluent déjà le support

---

### Étape 2 : Créer un Compte NGC

**NGC (NVIDIA GPU Cloud)** est nécessaire pour télécharger les modèles Riva.

1. Allez sur https://ngc.nvidia.com/
2. Créez un compte gratuit
3. Cliquez sur votre profil (en haut à droite) → **Setup**
4. Cliquez sur **"Generate API Key"**
5. **Copiez et sauvegardez** votre clé API (exemple : `abc123xyz...`)

---

### Étape 3 : Télécharger Riva Quick Start

```bash
# Créer un répertoire pour Riva
mkdir riva-quickstart
cd riva-quickstart

# Télécharger la version 2.19.0
# Option 1 : Via navigateur web
# Allez sur : https://catalog.ngc.nvidia.com/orgs/nvidia/teams/riva/resources/riva_quickstart
# Téléchargez et extrayez le fichier ZIP

# Option 2 : Via NGC CLI (si installé)
ngc registry resource download-version "nvidia/riva/riva_quickstart:2.19.0"
```

---

### Étape 4 : Configurer Riva

#### 4.1 Éditer config.sh

Ouvrez le fichier `config.sh` avec un éditeur de texte :

```bash
# Windows
notepad config.sh

# Linux
nano config.sh
# ou
vim config.sh
```

#### 4.2 Configuration Minimale

Trouvez et modifiez ces lignes :

```bash
# Votre clé API NGC (OBLIGATOIRE)
NGC_API_KEY="votre_clé_api_ici"

# Activer uniquement ASR (reconnaissance vocale)
service_enabled_asr=true
service_enabled_nlp=false    # Désactiver si autre langue que anglais
service_enabled_tts=false    # Désactiver synthèse vocale
service_enabled_nmt=false    # Désactiver traduction

# GPU à utiliser (0 = premier GPU)
gpus_to_use="device=0"

# Langues pour ASR
asr_language_code=("en-US")  # Anglais US
# Pour le français : asr_language_code=("fr-FR")
# Pour plusieurs langues : asr_language_code=("en-US" "fr-FR")
```

**⚠️ Important :** Si vous utilisez une langue autre que l'anglais, mettez `service_enabled_nlp=false`

---

### Étape 5 : Initialiser Riva (Télécharger les Modèles)

```bash
bash riva_init.sh
```

**Ce script va :**
1. Se connecter à NGC avec votre clé API
2. Télécharger l'image Docker Riva (~4 GB)
3. Télécharger les modèles ASR (~900 MB par langue)
4. Convertir les modèles pour votre GPU (TensorRT)

**Durée :** 10-30 minutes selon votre connexion Internet

**Sortie attendue :**
```
Logging into NGC docker registry if necessary...
Pulling required docker images if necessary...
Downloading models (RMIRs) from NGC...
  > Downloading nvidia/riva/rmir_asr_conformer_en_us_str:2.19.0...
  > Downloading nvidia/riva/rmir_asr_conformer_en_us_ofl:2.19.0...
Converting RMIRs to Riva Model repository...
Riva initialization complete. Run ./riva_start.sh to launch services.
```

---

### Étape 6 : Démarrer le Serveur Riva

```bash
bash riva_start.sh
```

**Attendre le message :**
```
Riva server is ready...
```

Le serveur Riva écoute maintenant sur `localhost:50051`

---

### Étape 7 : Vérifier l'Installation

```bash
bash riva_start_client.sh
```

Ou testez manuellement :

```bash
docker exec riva-speech riva_model_status
```

**Résultat attendu :**
```
+-----------------------------------------------------------+---------+--------+
| Model                                                     | Version | Status |
+-----------------------------------------------------------+---------+--------+
| conformer-en-US-asr-streaming-asr-bls-ensemble            | 1       | READY  |
| conformer-en-US-asr-offline-asr-bls-ensemble              | 1       | READY  |
+-----------------------------------------------------------+---------+--------+
```

✅ **Installation réussie !**

---

## Code Python : Explication Détaillée

### Vue d'Ensemble du Code

Notre application comporte **trois composants principaux** :

1. **Capture Audio** : Lit le microphone en continu
2. **Transcription** : Envoie l'audio à Riva et reçoit le texte
3. **Diffusion WebSocket** : Partage les transcriptions avec les clients web

**Architecture du code :**

```
┌─────────────────────────────────────────────────────────┐
│                    Programme Python                      │
│                                                          │
│  ┌────────────────┐         ┌──────────────────┐       │
│  │ Thread Capture │────────▶│      Queue       │       │
│  │   Microphone   │         │   (audio_queue)  │       │
│  └────────────────┘         └─────────┬────────┘       │
│                                        │                │
│  ┌────────────────────────────────────▼───────────┐    │
│  │         Thread Principal (Transcription)       │    │
│  │  1. Lit audio depuis queue                     │    │
│  │  2. Envoie à Riva via gRPC                     │    │
│  │  3. Reçoit transcription                       │    │
│  │  4. Diffuse via WebSocket                      │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
│  ┌──────────────────────────────────────────────┐      │
│  │       Serveur WebSocket (asyncio)            │      │
│  │  - Accepte connexions clients                │      │
│  │  - Diffuse transcriptions à tous             │      │
│  └──────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────┘
                         │
                         ▼
              ┌──────────────────┐
              │  Clients Web     │
              │  (navigateurs)   │
              └──────────────────┘
```

---

### Installation des Dépendances Python

```bash
# Installer les bibliothèques nécessaires
pip install nvidia-riva-client pyaudio websockets
```

**Dépendances :**
- `nvidia-riva-client` : Client officiel pour communiquer avec Riva
- `pyaudio` : Accès au microphone
- `websockets` : Serveur WebSocket pour diffusion en temps réel

**Si PyAudio pose problème sur Windows :**
```bash
pip install pipwin
pipwin install pyaudio
```

---

### Code Complet Commenté

```python
"""
TP : Transcription Vocale en Temps Réel avec NVIDIA Riva
Auteur : [Votre Nom]
Date : Novembre 2025

Ce script capture l'audio du microphone, le transcrit en temps réel
avec NVIDIA Riva, et diffuse les résultats via WebSocket.
"""

# ============================================================
# SECTION 1 : IMPORTS
# ============================================================

# Client Riva pour la reconnaissance vocale
import riva.client

# PyAudio pour accéder au microphone
import pyaudio

# asyncio pour la programmation asynchrone (WebSockets)
import asyncio

# websockets pour créer un serveur WebSocket
import websockets

# json pour formater les données en JSON
import json

# queue pour passer des données entre threads de manière sûre
import queue

# threading pour exécuter des tâches en parallèle
import threading

# ============================================================
# SECTION 2 : CONFIGURATION
# ============================================================

# Adresse du serveur Riva
# localhost = même machine, 50051 = port par défaut
RIVA_SERVER = "localhost:50051"

# Fréquence d'échantillonnage audio (Hz)
# 16000 Hz = 16 kHz est standard pour la parole
# Signification : 16000 mesures par seconde
SAMPLE_RATE = 16000

# Taille d'un bloc audio (nombre d'échantillons)
# 1600 échantillons = 100 ms à 16 kHz
# Calcul : 1600 / 16000 = 0.1 seconde
CHUNK_SIZE = 1600

# Configuration WebSocket
# 0.0.0.0 = écouter sur toutes les interfaces réseau
WEBSOCKET_HOST = "0.0.0.0"
# Port pour les connexions WebSocket
WEBSOCKET_PORT = 8765

# ============================================================
# SECTION 3 : GESTION DES CLIENTS WEBSOCKET
# ============================================================

# Ensemble (set) pour stocker tous les clients WebSocket connectés
# Un set évite automatiquement les doublons
connected_clients = set()


async def broadcast_transcription(message_type, text, is_final=False):
    """
    Diffuse un message de transcription à tous les clients WebSocket.
    
    Paramètres:
        message_type (str): Type de message ("transcription", "status", "error")
        text (str): Le texte à envoyer
        is_final (bool): True si transcription finale, False si provisoire
    
    Cette fonction est asynchrone (async) car l'envoi réseau peut prendre du temps.
    Le mot-clé 'await' permet d'attendre sans bloquer le programme.
    """
    
    # Si aucun client connecté, ne rien faire
    # Optimisation : évite de créer le message inutilement
    if not connected_clients:
        return
    
    # Créer un message JSON avec toutes les informations
    message = json.dumps({
        "type": message_type,           # Genre de message
        "text": text,                   # Contenu textuel
        "is_final": is_final,          # Final ou provisoire?
        "timestamp": asyncio.get_event_loop().time()  # Horodatage
    })
    
    # Liste pour stocker les clients déconnectés
    # On ne peut pas modifier un set pendant qu'on itère dessus
    disconnected_clients = set()
    
    # Envoyer le message à chaque client
    for client in connected_clients:
        try:
            # Tentative d'envoi du message
            # await = attendre que l'envoi soit terminé
            await client.send(message)
        except websockets.exceptions.ConnectionClosed:
            # Si la connexion est fermée, marquer pour suppression
            disconnected_clients.add(client)
    
    # Retirer tous les clients déconnectés de l'ensemble
    # difference_update = suppression en masse efficace
    connected_clients.difference_update(disconnected_clients)


async def websocket_handler(websocket, path):
    """
    Gère une nouvelle connexion WebSocket.
    
    Cette fonction est appelée automatiquement chaque fois
    qu'un client se connecte au serveur WebSocket.
    
    Paramètres:
        websocket: L'objet de connexion WebSocket
        path: Le chemin URL demandé (non utilisé ici)
    """
    
    # Ajouter ce nouveau client à notre ensemble
    connected_clients.add(websocket)
    
    # Obtenir l'adresse IP du client pour logging
    client_ip = websocket.remote_address[0] if websocket.remote_address else "unknown"
    
    # Message de log dans la console
    print(f"✓ Nouveau client connecté depuis {client_ip} (Total: {len(connected_clients)})")
    
    # Envoyer un message de bienvenue au client
    await websocket.send(json.dumps({
        "type": "status",
        "text": "Connecté au serveur de transcription",
        "is_final": True
    }))
    
    try:
        # Boucle d'écoute des messages du client
        # async for = itération asynchrone
        async for message in websocket:
            # Dans cette application, on n'attend pas de messages des clients
            # On pourrait ici gérer des commandes (pause, langue, etc.)
            pass
    except websockets.exceptions.ConnectionClosed:
        # Déconnexion normale du client
        pass
    finally:
        # Cette section s'exécute toujours, même en cas d'erreur
        # Retirer le client de l'ensemble
        connected_clients.discard(websocket)
        print(f"✗ Client déconnecté de {client_ip} (Total: {len(connected_clients)})")


async def start_websocket_server():
    """
    Démarre le serveur WebSocket.
    
    Cette fonction crée un serveur qui écoute les connexions WebSocket
    et appelle websocket_handler pour chaque nouvelle connexion.
    """
    
    # Créer et démarrer le serveur WebSocket
    # websocket_handler sera appelé pour chaque connexion
    server = await websockets.serve(
        websocket_handler,      # Fonction à appeler pour chaque connexion
        WEBSOCKET_HOST,         # Interface d'écoute
        WEBSOCKET_PORT          # Port d'écoute
    )
    
    # Message informatif
    print(f"🌐 Serveur WebSocket démarré sur ws://{WEBSOCKET_HOST}:{WEBSOCKET_PORT}")
    print(f"   Les clients peuvent se connecter via : ws://localhost:{WEBSOCKET_PORT}")
    
    # Garder le serveur actif indéfiniment
    # asyncio.Future() crée une "promesse" qui ne se résout jamais
    await asyncio.Future()

# ============================================================
# SECTION 4 : TRANSCRIPTION AVEC RIVA
# ============================================================


def transcription_worker(audio_queue, stop_event, loop):
    """
    Fonction principale de transcription.
    
    Cette fonction :
    1. Se connecte à Riva
    2. Lit l'audio depuis la queue
    3. Envoie l'audio à Riva pour transcription
    4. Diffuse les résultats via WebSocket
    
    Paramètres:
        audio_queue: Queue contenant les blocs audio du microphone
        stop_event: Event pour signaler l'arrêt
        loop: Boucle d'événements asyncio pour WebSocket
    
    Cette fonction s'exécute dans un thread séparé.
    """
    
    # ===== Connexion à Riva =====
    
    # Créer un objet d'authentification avec l'adresse du serveur
    auth = riva.client.Auth(uri=RIVA_SERVER)
    
    # Créer un service ASR (Automatic Speech Recognition)
    asr_service = riva.client.ASRService(auth)
    
    # ===== Configuration de la Reconnaissance Vocale =====
    
    # StreamingRecognitionConfig = configuration pour streaming en temps réel
    config = riva.client.StreamingRecognitionConfig(
        # RecognitionConfig = paramètres détaillés
        config=riva.client.RecognitionConfig(
            # Format audio : PCM linéaire (audio brut, non compressé)
            encoding=riva.client.AudioEncoding.LINEAR_PCM,
            
            # Code de langue : en-US pour anglais américain
            # Changez en "fr-FR" pour français
            language_code="en-US",
            
            # Nombre d'alternatives à retourner (1 = meilleure hypothèse uniquement)
            max_alternatives=1,
            
            # Filtre de grossièretés (False = tout montrer tel quel)
            profanity_filter=False,
            
            # Ponctuation automatique (True = ajouter . , ? ! etc.)
            enable_automatic_punctuation=True,
            
            # ⚠️ CRITIQUE : Fréquence d'échantillonnage
            # DOIT correspondre à SAMPLE_RATE, sinon erreur "Invalid sample rate"
            sample_rate_hertz=SAMPLE_RATE,
            
            # Nombre de canaux audio (1 = mono, 2 = stéréo)
            audio_channel_count=1,
            
            # Format verbatim ou formaté (False = formaté, plus lisible)
            verbatim_transcripts=False,
        ),
        
        # interim_results = recevoir des résultats provisoires
        # True = voir la transcription en temps réel pendant que la personne parle
        # False = voir seulement quand la personne a fini de parler
        interim_results=True,
    )
    
    # ===== Générateur Audio =====
    
    def audio_generator():
        """
        Générateur qui fournit des blocs audio à Riva.
        
        Un générateur est une fonction qui utilise 'yield' au lieu de 'return'.
        Elle peut produire plusieurs valeurs successivement sans se terminer.
        
        C'est idéal pour le streaming : on envoie l'audio morceau par morceau.
        """
        # Boucle tant que stop_event n'est pas activé
        while not stop_event.is_set():
            try:
                # Essayer de récupérer un bloc audio de la queue
                # timeout=0.1 = attendre max 0.1 seconde
                chunk = audio_queue.get(timeout=0.1)
                
                # Si on reçoit None, c'est un signal d'arrêt
                if chunk is None:
                    break
                
                # 'yield' retourne le bloc audio mais garde la fonction active
                # C'est comme 'return' mais sans terminer la fonction
                yield chunk
                
            except queue.Empty:
                # Queue vide après 0.1s, continuer la boucle
                continue
    
    # ===== Message de Démarrage =====
    
    # Envoyer un message de statut aux clients WebSocket
    # asyncio.run_coroutine_threadsafe permet d'appeler une fonction async
    # depuis un thread synchrone (celui-ci)
    asyncio.run_coroutine_threadsafe(
        broadcast_transcription("status", "Transcription démarrée", True),
        loop  # La boucle asyncio dans laquelle exécuter
    )
    
    # ===== Boucle Principale de Transcription =====
    
    try:
        # Démarrer la reconnaissance vocale en streaming
        # Cette fonction retourne un itérateur de réponses
        responses = asr_service.streaming_response_generator(
            audio_chunks=audio_generator(),  # Notre générateur d'audio
            streaming_config=config          # Configuration définie plus haut
        )
        
        # Traiter chaque réponse de Riva
        # Cette boucle continue tant que audio_generator produit des données
        for response in responses:
            # Ignorer les réponses vides
            if not response.results:
                continue
            
            # Traiter chaque résultat dans la réponse
            for result in response.results:
                # Ignorer si pas d'alternatives
                if not result.alternatives:
                    continue
                
                # Extraire le texte transcrit (meilleure hypothèse)
                transcript = result.alternatives[0].transcript
                
                # Vérifier si c'est un résultat final ou provisoire
                if result.is_final:
                    # ===== RÉSULTAT FINAL =====
                    # La transcription est confirmée, ne changera plus
                    
                    # Afficher dans la console
                    print(f"✓ FINAL : {transcript}")
                    
                    # Diffuser aux clients WebSocket
                    asyncio.run_coroutine_threadsafe(
                        broadcast_transcription("transcription", transcript, True),
                        loop
                    )
                else:
                    # ===== RÉSULTAT PROVISOIRE =====
                    # La transcription est en cours, peut changer
                    
                    # Afficher dans la console (écrase la ligne précédente)
                    # end='\r' = retour chariot sans nouvelle ligne
                    # flush=True = forcer l'affichage immédiat
                    print(f"  provisoire : {transcript}          ", end='\r', flush=True)
                    
                    # Diffuser aux clients WebSocket
                    asyncio.run_coroutine_threadsafe(
                        broadcast_transcription("transcription", transcript, False),
                        loop
                    )
    
    except Exception as e:
        # En cas d'erreur, afficher et diffuser
        error_msg = f"Erreur de transcription : {e}"
        print(f"\n❌ {error_msg}")
        asyncio.run_coroutine_threadsafe(
            broadcast_transcription("error", error_msg, True),
            loop
        )

# ============================================================
# SECTION 5 : CAPTURE AUDIO DEPUIS LE MICROPHONE
# ============================================================


def capture_audio(audio_queue, stop_event):
    """
    Capture l'audio depuis le microphone et le met dans la queue.
    
    Cette fonction s'exécute dans un thread séparé pour ne pas bloquer
    la transcription ou le serveur WebSocket.
    
    Paramètres:
        audio_queue: Queue où placer les blocs audio capturés
        stop_event: Event pour signaler l'arrêt
    """
    
    # ===== Initialisation PyAudio =====
    
    # PyAudio est une bibliothèque qui gère l'audio sur tous les OS
    audio = pyaudio.PyAudio()
    
    # Ouvrir un flux audio depuis le microphone
    stream = audio.open(
        # Format : paInt16 = entiers 16 bits signés (-32768 à 32767)
        # Standard pour l'audio de qualité vocale
        format=pyaudio.paInt16,
        
        # Canaux : 1 = mono (un seul microphone)
        # 2 serait stéréo (gauche + droite)
        channels=1,
        
        # Fréquence d'échantillonnage en Hz
        rate=SAMPLE_RATE,
        
        # input=True signifie qu'on enregistre (vs playback)
        input=True,
        
        # Nombre d'échantillons à lire à la fois
        frames_per_buffer=CHUNK_SIZE
    )
    
    print("🎤 Microphone démarré - parlez maintenant !")
    
    # ===== Boucle de Capture =====
    
    # Continuer tant que stop_event n'est pas activé
    while not stop_event.is_set():
        try:
            # Lire un bloc audio depuis le microphone
            # CHUNK_SIZE échantillons = 100 ms d'audio
            # exception_on_overflow=False évite les crashes si on rate des données
            data = stream.read(CHUNK_SIZE, exception_on_overflow=False)
            
            # Placer les données audio dans la queue
            # Le thread de transcription les récupérera
            audio_queue.put(data)
            
        except Exception as e:
            # En cas d'erreur (microphone déconnecté, etc.)
            print(f"Erreur de capture audio : {e}")
            break
    
    # ===== Nettoyage =====
    
    # Arrêter le flux audio
    stream.stop_stream()
    
    # Fermer le flux
    stream.close()
    
    # Libérer les ressources PyAudio
    audio.terminate()
    
    print("🎤 Microphone arrêté")

# ============================================================
# SECTION 6 : FONCTION PRINCIPALE ASYNCHRONE
# ============================================================


async def main_async():
    """
    Fonction principale qui coordonne tous les composants.
    
    Cette fonction :
    1. Démarre le serveur WebSocket
    2. Lance la capture audio dans un thread
    3. Lance la transcription dans un thread
    4. Attend l'arrêt (Ctrl+C)
    5. Nettoie proprement toutes les ressources
    """
    
    # ===== Initialisation =====
    
    # Créer une queue pour passer l'audio entre threads
    # Une queue est thread-safe (plusieurs threads peuvent y accéder sans problème)
    audio_queue = queue.Queue()
    
    # Créer un Event pour signaler l'arrêt à tous les threads
    # Quand on fait stop_event.set(), tous les threads le verront
    stop_event = threading.Event()
    
    # Obtenir la boucle d'événements asyncio actuelle
    # Nécessaire pour exécuter des coroutines depuis d'autres threads
    loop = asyncio.get_event_loop()
    
    # ===== Démarrage du Serveur WebSocket =====
    
    # Créer une tâche asynchrone pour le serveur WebSocket
    # create_task lance la fonction en arrière-plan
    websocket_task = asyncio.create_task(start_websocket_server())
    
    # ===== Démarrage de la Capture Audio =====
    
    # Créer un thread pour capturer l'audio
    capture_thread = threading.Thread(
        target=capture_audio,           # Fonction à exécuter
        args=(audio_queue, stop_event), # Arguments de la fonction
        daemon=True                     # Thread daemon = s'arrête si le programme principal s'arrête
    )
    # Démarrer le thread
    capture_thread.start()
    
    # ===== Démarrage de la Transcription =====
    
    # Créer un thread pour la transcription
    transcription_thread = threading.Thread(
        target=transcription_worker,
        args=(audio_queue, stop_event, loop),
        daemon=True
    )
    # Démarrer le thread
    transcription_thread.start()
    
    # ===== Affichage des Informations =====
    
    print("\n" + "=" * 60)
    print("Serveur de Transcription en Temps Réel avec WebSocket")
    print("=" * 60)
    print(f"URL WebSocket : ws://localhost:{WEBSOCKET_PORT}")
    print("Appuyez sur Ctrl+C pour arrêter")
    print("=" * 60 + "\n")
    
    # ===== Attente et Arrêt =====
    
    try:
        # Attendre que le serveur WebSocket se termine
        # (ce qui n'arrivera jamais sauf interruption)
        await websocket_task
        
    except KeyboardInterrupt:
        # L'utilisateur a appuyé sur Ctrl+C
        print("\n\n🛑 Arrêt en cours...")
        
    finally:
        # Cette section s'exécute toujours, même en cas d'erreur
        
        # Signaler à tous les threads de s'arrêter
        stop_event.set()
        
        # Envoyer None dans la queue pour débloquer le générateur
        audio_queue.put(None)
        
        # Attendre que les threads se terminent (max 1 seconde chacun)
        capture_thread.join(timeout=1)
        transcription_thread.join(timeout=1)
        
        # Fermer toutes les connexions WebSocket
        for client in list(connected_clients):
            await client.close()
        
        print("Arrêt terminé !")

# ============================================================
# SECTION 7 : POINT D'ENTRÉE DU PROGRAMME
# ============================================================


def main():
    """
    Point d'entrée du programme.
    
    Cette fonction lance simplement la fonction asynchrone principale.
    """
    try:
        # asyncio.run() exécute une fonction asynchrone jusqu'à sa fin
        # C'est le point d'entrée standard pour les programmes asyncio
        asyncio.run(main_async())
    except KeyboardInterrupt:
        # Gérer Ctrl+C proprement
        print("\nArrêt...")


# Cette condition vérifie si le fichier est exécuté directement
# (et non importé comme module)
if __name__ == "__main__":
    main()
```

---

### Concepts Clés Expliqués

#### 1. Threading vs Asyncio

**Threading (Threads)** :
- Pour les tâches **bloquantes** (I/O, attente)
- Exemple : Lecture du microphone (PyAudio est bloquant)
- Utilise `threading.Thread()`

**Asyncio (Asynchrone)** :
- Pour les tâches **non-bloquantes** avec beaucoup d'attente
- Exemple : Serveur WebSocket (beaucoup de connexions, peu d'activité par connexion)
- Utilise `async`/`await`

**Dans notre code :**
```
Thread 1 : Capture audio (PyAudio) → Bloquant, nécessite un thread
Thread 2 : Transcription (Riva)   → Bloquant, nécessite un thread
Asyncio  : Serveur WebSocket       → Non-bloquant, utilise asyncio
```

#### 2. Queue (File d'Attente)

Une **queue** est comme une file d'attente au supermarché :
- **FIFO** (First In, First Out) : Premier arrivé, premier servi
- **Thread-safe** : Plusieurs threads peuvent y accéder simultanément sans conflit
- **Bloquante** : Si vide, `get()` attend qu'il y ait quelque chose

```python
# Thread 1 (Capture)
audio_queue.put(audio_data)  # Ajouter à la fin

# Thread 2 (Transcription)
audio_data = audio_queue.get()  # Retirer du début
```

#### 3. Event (Événement)

Un **Event** est comme un interrupteur :
- **set()** : Allumer l'interrupteur
- **clear()** : Éteindre
- **is_set()** : Vérifier s'il est allumé
- **wait()** : Attendre qu'il soit allumé

```python
# Pour arrêter tous les threads
stop_event.set()

# Dans les threads
while not stop_event.is_set():
    # Continuer à travailler
```

#### 4. Générateur (Generator)

Un **générateur** est une fonction qui peut **suspendre et reprendre** son exécution :

```python
def compteur():
    for i in range(5):
        yield i  # Pause et retourne i

for nombre in compteur():
    print(nombre)  # 0, 1, 2, 3, 4
```

**Avantages :**
- Économie de mémoire (ne génère qu'une valeur à la fois)
- Parfait pour le streaming (flux infini de données)

**Dans notre code :**
```python
def audio_generator():
    while not stop_event.is_set():
        chunk = audio_queue.get()
        yield chunk  # Retourner un bloc, puis attendre le prochain
```

#### 5. gRPC Streaming

**gRPC** est un protocole de communication :
- **Bidirectionnel** : Client et serveur s'envoient des données simultanément
- **Efficace** : Utilise HTTP/2 et Protocol Buffers
- **Streaming** : Flux continu de données

```
Client (vous)                   Serveur (Riva)
    │                                │
    ├──── Audio chunk 1 ─────────────▶
    ├──── Audio chunk 2 ─────────────▶
    ◀──── Interim result 1 ───────────┤
    ├──── Audio chunk 3 ─────────────▶
    ◀──── Interim result 2 ───────────┤
    ├──── Audio chunk 4 ─────────────▶
    ◀──── Final result ───────────────┤
```

---

## WebSocket pour la Diffusion en Temps Réel

### Qu'est-ce que WebSocket ?

**WebSocket** est un protocole de communication bidirectionnelle en temps réel :

**HTTP classique** (requête-réponse) :
```
Client : "Donne-moi les données"
Serveur : "Voici les données"
[Connexion fermée]
```

**WebSocket** (connexion persistante) :
```
Client ←──────────────→ Serveur
   │                      │
   │  Données en continu  │
   │  ←──────────────────  │
   │  ──────────────────→  │
   │                      │
[Connexion ouverte en permanence]
```

### Avantages pour la Transcription

1. **Faible latence** : Pas besoin de réouvrir la connexion
2. **Bidirectionnel** : Serveur peut pousser des données
3. **Léger** : Moins de overhead qu'HTTP
4. **Temps réel** : Parfait pour les transcriptions progressives

### Architecture WebSocket dans Notre Code

```
┌────────────────────────────────────────────────────┐
│             Serveur Python                         │
│                                                    │
│  ┌──────────────────────────────────────────┐    │
│  │  Serveur WebSocket                       │    │
│  │  - Écoute sur port 8765                  │    │
│  │  - Accepte nouvelles connexions          │    │
│  └──────────────┬───────────────────────────┘    │
│                 │                                  │
│  ┌──────────────▼───────────────────────────┐    │
│  │  connected_clients (set)                 │    │
│  │  ┌─────┐  ┌─────┐  ┌─────┐             │    │
│  │  │ WS1 │  │ WS2 │  │ WS3 │  ...        │    │
│  │  └─────┘  └─────┘  └─────┘             │    │
│  └──────────────────────────────────────────┘    │
│                                                    │
│  ┌──────────────────────────────────────────┐    │
│  │  broadcast_transcription()               │    │
│  │  Envoie à tous les clients               │    │
│  └──────────────────────────────────────────┘    │
└────────────────────────────────────────────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
         ▼           ▼           ▼
    ┌────────┐  ┌────────┐  ┌────────┐
    │Client 1│  │Client 2│  │Client 3│
    │(Chrome)│  │(Firefox│  │ (App)  │
    └────────┘  └────────┘  └────────┘
```

### Format des Messages JSON

```json
{
  "type": "transcription",
  "text": "Bonjour comment allez-vous",
  "is_final": false,
  "timestamp": 1699029384.5
}
```

**Champs :**
- `type` : Type de message (`"transcription"`, `"status"`, `"error"`)
- `text` : Contenu textuel
- `is_final` : `true` = transcription finale, `false` = provisoire
- `timestamp` : Horodatage (secondes depuis epoch)

---

## Client HTML pour Tester

### Code HTML Complet

Créez un fichier `client_transcription.html` :

```html
<!DOCTYPE html>
<html lang="fr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Client de Transcription en Temps Réel</title>
    <style>
        /* ============================================
           STYLES CSS
           ============================================ */
        
        /* Style général de la page */
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            max-width: 900px;
            margin: 50px auto;
            padding: 20px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #333;
        }
        
        /* Conteneur principal */
        .container {
            background: white;
            border-radius: 15px;
            padding: 30px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.3);
        }
        
        /* Titre principal */
        h1 {
            color: #667eea;
            text-align: center;
            margin-bottom: 10px;
            font-size: 2em;
        }
        
        .subtitle {
            text-align: center;
            color: #666;
            margin-bottom: 30px;
        }
        
        /* Indicateur de statut de connexion */
        #status {
            text-align: center;
            padding: 15px;
            margin: 20px 0;
            border-radius: 10px;
            font-weight: bold;
            font-size: 1.1em;
            transition: all 0.3s ease;
        }
        
        /* Statut connecté (vert) */
        .connected {
            background-color: #4CAF50;
            color: white;
            box-shadow: 0 4px 15px rgba(76, 175, 80, 0.4);
        }
        
        /* Statut déconnecté (rouge) */
        .disconnected {
            background-color: #f44336;
            color: white;
            box-shadow: 0 4px 15px rgba(244, 67, 54, 0.4);
        }
        
        /* Zone d'affichage des transcriptions */
        #transcription {
            background: linear-gradient(to bottom, #f9f9f9, #ffffff);
            border: 2px solid #e0e0e0;
            border-radius: 12px;
            padding: 25px;
            min-height: 400px;
            max-height: 600px;
            overflow-y: auto;
            box-shadow: inset 0 2px 10px rgba(0,0,0,0.05);
        }
        
        /* Ligne de transcription individuelle */
        .transcript-line {
            padding: 15px;
            margin: 10px 0;
            border-radius: 8px;
            animation: slideIn 0.3s ease-out;
            transition: all 0.2s ease;
        }
        
        /* Animation d'apparition */
        @keyframes slideIn {
            from {
                opacity: 0;
                transform: translateY(-10px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
        
        /* Hover effect */
        .transcript-line:hover {
            transform: translateX(5px);
        }
        
        /* Transcription finale (verte) */
        .final {
            background: linear-gradient(135deg, #e8f5e9 0%, #c8e6c9 100%);
            border-left: 5px solid #4CAF50;
            font-weight: 500;
        }
        
        /* Transcription provisoire (grise) */
        .interim {
            background: linear-gradient(135deg, #f5f5f5 0%, #e0e0e0 100%);
            border-left: 5px solid #9e9e9e;
            font-style: italic;
            opacity: 0.85;
        }
        
        /* Horodatage */
        .timestamp {
            font-size: 0.85em;
            color: #666;
            margin-right: 12px;
            font-weight: bold;
        }
        
        /* Icônes */
        .icon {
            margin-right: 8px;
        }
        
        /* Message d'erreur */
        .error-message {
            background: linear-gradient(135deg, #ffebee 0%, #ffcdd2 100%);
            border-left: 5px solid #f44336;
            padding: 15px;
            margin: 10px 0;
            border-radius: 8px;
        }
        
        /* Barre de défilement personnalisée */
        #transcription::-webkit-scrollbar {
            width: 10px;
        }
        
        #transcription::-webkit-scrollbar-track {
            background: #f1f1f1;
            border-radius: 10px;
        }
        
        #transcription::-webkit-scrollbar-thumb {
            background: #888;
            border-radius: 10px;
        }
        
        #transcription::-webkit-scrollbar-thumb:hover {
            background: #555;
        }
        
        /* Message vide */
        .empty-message {
            text-align: center;
            color: #999;
            font-style: italic;
            margin-top: 150px;
            font-size: 1.2em;
        }
        
        /* Statistiques */
        .stats {
            display: flex;
            justify-content: space-around;
            margin-top: 20px;
            padding: 15px;
            background: #f5f5f5;
            border-radius: 10px;
        }
        
        .stat-item {
            text-align: center;
        }
        
        .stat-value {
            font-size: 2em;
            font-weight: bold;
            color: #667eea;
        }
        
        .stat-label {
            color: #666;
            font-size: 0.9em;
            margin-top: 5px;
        }
    </style>
</head>
<body>
    <div class="container">
        <!-- En-tête -->
        <h1>🎤 Transcription en Temps Réel</h1>
        <p class="subtitle">Propulsé par NVIDIA Riva</p>
        
        <!-- Indicateur de connexion -->
        <div id="status" class="disconnected">
            <span class="icon">🔴</span> Déconnecté
        </div>
        
        <!-- Zone de transcription -->
        <div id="transcription">
            <div class="empty-message">
                En attente de transcriptions...
            </div>
        </div>
        
        <!-- Statistiques -->
        <div class="stats">
            <div class="stat-item">
                <div class="stat-value" id="final-count">0</div>
                <div class="stat-label">Transcriptions finales</div>
            </div>
            <div class="stat-item">
                <div class="stat-value" id="word-count">0</div>
                <div class="stat-label">Mots transcrits</div>
            </div>
            <div class="stat-item">
                <div class="stat-value" id="connection-time">0s</div>
                <div class="stat-label">Temps connecté</div>
            </div>
        </div>
    </div>

    <script>
        /* ============================================
           CODE JAVASCRIPT
           ============================================ */
        
        // Références aux éléments HTML
        const statusDiv = document.getElementById('status');
        const transcriptionDiv = document.getElementById('transcription');
        const finalCountDiv = document.getElementById('final-count');
        const wordCountDiv = document.getElementById('word-count');
        const connectionTimeDiv = document.getElementById('connection-time');
        
        // Variables globales
        let ws = null;  // Connexion WebSocket
        let currentInterim = null;  // Élément de transcription provisoire actuel
        let finalCount = 0;  // Nombre de transcriptions finales
        let totalWords = 0;  // Nombre total de mots
        let connectionStartTime = null;  // Temps de début de connexion
        let connectionTimer = null;  // Timer pour le temps de connexion
        
        /**
         * Fonction de connexion au serveur WebSocket
         * 
         * Cette fonction :
         * 1. Crée une connexion WebSocket
         * 2. Gère les événements (ouverture, message, fermeture, erreur)
         * 3. Tente une reconnexion automatique en cas de déconnexion
         */
        function connect() {
            console.log('🔌 Tentative de connexion au serveur...');
            
            // Créer la connexion WebSocket
            // ws:// = WebSocket non sécurisé (wss:// serait sécurisé)
            ws = new WebSocket('ws://localhost:8765');
            
            // ===== Événement : Connexion ouverte =====
            ws.onopen = function() {
                console.log('✅ Connecté au serveur de transcription');
                
                // Mettre à jour l'interface
                statusDiv.innerHTML = '<span class="icon">🟢</span> Connecté';
                statusDiv.className = 'connected';
                
                // Démarrer le compteur de temps
                connectionStartTime = Date.now();
                startConnectionTimer();
                
                // Supprimer le message vide si présent
                const emptyMsg = transcriptionDiv.querySelector('.empty-message');
                if (emptyMsg) {
                    emptyMsg.remove();
                }
            };
            
            // ===== Événement : Réception de message =====
            ws.onmessage = function(event) {
                // Parser le JSON reçu
                const data = JSON.parse(event.data);
                
                console.log('📨 Message reçu:', data);
                
                // Gérer selon le type de message
                switch(data.type) {
                    case 'transcription':
                        // Afficher la transcription
                        displayTranscription(data.text, data.is_final);
                        break;
                        
                    case 'status':
                        // Message de statut
                        console.log('ℹ️ Statut:', data.text);
                        break;
                        
                    case 'error':
                        // Message d'erreur
                        console.error('❌ Erreur:', data.text);
                        addErrorMessage(data.text);
                        break;
                        
                    default:
                        console.warn('⚠️ Type de message inconnu:', data.type);
                }
            };
            
            // ===== Événement : Connexion fermée =====
            ws.onclose = function(event) {
                console.log('🔌 Déconnecté du serveur');
                console.log('   Code:', event.code, 'Raison:', event.reason);
                
                // Mettre à jour l'interface
                statusDiv.innerHTML = '<span class="icon">🔴</span> Déconnecté';
                statusDiv.className = 'disconnected';
                
                // Arrêter le compteur de temps
                stopConnectionTimer();
                
                // Tentative de reconnexion après 3 secondes
                console.log('🔄 Reconnexion dans 3 secondes...');
                setTimeout(connect, 3000);
            };
            
            // ===== Événement : Erreur =====
            ws.onerror = function(error) {
                console.error('❌ Erreur WebSocket:', error);
            };
        }
        
        /**
         * Affiche une transcription dans l'interface
         * 
         * @param {string} text - Le texte transcrit
         * @param {boolean} isFinal - True si transcription finale, False si provisoire
         */
        function displayTranscription(text, isFinal) {
            // Obtenir l'heure actuelle
            const now = new Date();
            const timestamp = now.toLocaleTimeString('fr-FR');
            
            if (isFinal) {
                // ===== TRANSCRIPTION FINALE =====
                
                // Supprimer la transcription provisoire actuelle
                if (currentInterim) {
                    currentInterim.remove();
                    currentInterim = null;
                }
                
                // Créer un nouvel élément HTML
                const div = document.createElement('div');
                div.className = 'transcript-line final';
                div.innerHTML = `
                    <span class="timestamp">🕒 ${timestamp}</span>
                    <strong>${escapeHtml(text)}</strong>
                `;
                
                // Ajouter à la zone de transcription
                transcriptionDiv.appendChild(div);
                
                // Défiler vers le bas
                scrollToBottom();
                
                // Mettre à jour les statistiques
                finalCount++;
                totalWords += text.split(' ').length;
                updateStats();
                
            } else {
                // ===== TRANSCRIPTION PROVISOIRE =====
                
                if (currentInterim) {
                    // Mettre à jour la transcription provisoire existante
                    currentInterim.innerHTML = `
                        <span class="timestamp">🕒 ${timestamp}</span>
                        ${escapeHtml(text)}
                    `;
                } else {
                    // Créer une nouvelle transcription provisoire
                    currentInterim = document.createElement('div');
                    currentInterim.className = 'transcript-line interim';
                    currentInterim.innerHTML = `
                        <span class="timestamp">🕒 ${timestamp}</span>
                        ${escapeHtml(text)}
                    `;
                    transcriptionDiv.appendChild(currentInterim);
                }
                
                // Défiler vers le bas
                scrollToBottom();
            }
        }
        
        /**
         * Ajoute un message d'erreur à l'interface
         * 
         * @param {string} text - Le message d'erreur
         */
        function addErrorMessage(text) {
            const div = document.createElement('div');
            div.className = 'error-message';
            div.innerHTML = `
                <strong>❌ Erreur :</strong> ${escapeHtml(text)}
            `;
            transcriptionDiv.appendChild(div);
            scrollToBottom();
        }
        
        /**
         * Échappe les caractères HTML pour éviter les injections XSS
         * 
         * @param {string} text - Le texte à échapper
         * @returns {string} - Le texte échappé
         */
        function escapeHtml(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }
        
        /**
         * Fait défiler la zone de transcription vers le bas
         */
        function scrollToBottom() {
            transcriptionDiv.scrollTop = transcriptionDiv.scrollHeight;
        }
        
        /**
         * Met à jour les statistiques affichées
         */
        function updateStats() {
            finalCountDiv.textContent = finalCount;
            wordCountDiv.textContent = totalWords;
        }
        
        /**
         * Démarre le compteur de temps de connexion
         */
        function startConnectionTimer() {
            connectionTimer = setInterval(() => {
                const elapsed = Math.floor((Date.now() - connectionStartTime) / 1000);
                connectionTimeDiv.textContent = `${elapsed}s`;
            }, 1000);
        }
        
        /**
         * Arrête le compteur de temps de connexion
         */
        function stopConnectionTimer() {
            if (connectionTimer) {
                clearInterval(connectionTimer);
                connectionTimer = null;
            }
        }
        
        // Connexion automatique au chargement de la page
        connect();
        
        // Log pour indiquer que le script est chargé
        console.log('✅ Client de transcription chargé et prêt');
    </script>
</body>
</html>
```

### Comment Utiliser le Client HTML

1. **Sauvegarder** le code dans un fichier `client_transcription.html`
2. **Démarrer** le serveur Python :
   ```bash
   python realtime_transcription_websocket.py
   ```
3. **Ouvrir** le fichier HTML dans un navigateur (double-clic)
4. **Parler** dans votre microphone
5. **Observer** les transcriptions apparaître en temps réel !

### Interface Utilisateur

```
┌─────────────────────────────────────────────────┐
│  🎤 Transcription en Temps Réel                 │
│      Propulsé par NVIDIA Riva                   │
├─────────────────────────────────────────────────┤
│  🟢 Connecté                                    │
├─────────────────────────────────────────────────┤
│                                                 │
│  🕒 14:32:15  Bonjour comment allez-vous       │
│  (gris, italique = provisoire)                  │
│                                                 │
│  🕒 14:32:17  Bonjour, comment allez-vous ?    │
│  (vert, gras = final)                          │
│                                                 │
│  🕒 14:32:20  Je vais très bien merci          │
│  (vert, gras = final)                          │
│                                                 │
├─────────────────────────────────────────────────┤
│  Transcriptions finales: 2                      │
│  Mots transcrits: 12                           │
│  Temps connecté: 45s                           │
└─────────────────────────────────────────────────┘
```

---

## Support Multilingue

### Langues Supportées par Riva 2.19.0

| Code Langue | Langue | Qualité |
|-------------|--------|---------|
| `en-US` | Anglais (États-Unis) | ⭐⭐⭐⭐⭐ |
| `en-GB` | Anglais (Royaume-Uni) | ⭐⭐⭐⭐⭐ |
| `fr-FR` | Français (France) | ⭐⭐⭐⭐ |
| `de-DE` | Allemand (Allemagne) | ⭐⭐⭐⭐ |
| `es-ES` | Espagnol (Espagne) | ⭐⭐⭐⭐ |
| `es-US` | Espagnol (États-Unis) | ⭐⭐⭐⭐ |
| `it-IT` | Italien (Italie) | ⭐⭐⭐⭐ |
| `pt-BR` | Portugais (Brésil) | ⭐⭐⭐⭐ |
| `ru-RU` | Russe (Russie) | ⭐⭐⭐⭐ |
| `ja-JP` | Japonais (Japon) | ⭐⭐⭐⭐ |
| `zh-CN` | Chinois (Simplifié) | ⭐⭐⭐⭐ |
| `ko-KR` | Coréen (Corée du Sud) | ⭐⭐⭐ |
| `hi-IN` | Hindi (Inde) | ⭐⭐⭐ |
| `ar-AR` | Arabe | ⭐⭐⭐ |

### Ajouter une Langue

#### Étape 1 : Arrêter Riva

```bash
bash riva_stop.sh
```

#### Étape 2 : Modifier config.sh

```bash
# Éditer le fichier
notepad config.sh  # Windows
nano config.sh     # Linux

# Trouver la ligne asr_language_code
# Ajouter la langue souhaitée
asr_language_code=("en-US" "fr-FR" "es-ES")
```

#### Étape 3 : Télécharger les Modèles

```bash
bash riva_init.sh
```

Cela télécharge uniquement les nouvelles langues (pas de re-téléchargement).

#### Étape 4 : Redémarrer Riva

```bash
bash riva_start.sh
```

#### Étape 5 : Modifier le Code Python

```python
# Dans transcription_worker(), changer :
language_code="fr-FR",  # Nouvelle langue
```

### Application Multilingue

Pour supporter plusieurs langues dynamiquement :

```python
import sys

# Paramètre de ligne de commande
LANGUAGE = sys.argv[1] if len(sys.argv) > 1 else "en-US"

# Dans transcription_worker()
language_code=LANGUAGE,
```

**Utilisation :**
```bash
# Anglais
python realtime_transcription_websocket.py en-US

# Français
python realtime_transcription_websocket.py fr-FR

# Espagnol
python realtime_transcription_websocket.py es-ES
```

---

## Dépannage

### Problème 1 : "Invalid sample rate 0"

**Symptôme :**
```
Error: Unavailable model requested given these parameters: 
language_code=fr; sample_rate=16000; type=online;
```

**Cause :** Le code de langue est mal formaté ou les modèles ne sont pas installés.

**Solution :**
1. Vérifier que `language_code` utilise le format complet (`"fr-FR"` pas `"fr"`)
2. Vérifier que les modèles sont téléchargés :
   ```bash
   docker exec riva-speech riva_model_status
   ```
3. Si modèles manquants, réinstaller :
   ```bash
   bash riva_init.sh
   ```

---

### Problème 2 : PyAudio Installation Échoue

**Symptôme :**
```
ERROR: Could not build wheels for pyaudio
```

**Solution Windows :**
```bash
pip install pipwin
pipwin install pyaudio
```

**Solution Linux :**
```bash
sudo apt-get install portaudio19-dev python3-pyaudio
pip install pyaudio
```

---

### Problème 3 : GPU Non Détecté

**Symptôme :**
```
docker: Error response from daemon: could not select device driver "" 
with capabilities: [[gpu]]
```

**Solution :**
1. Vérifier les pilotes NVIDIA :
   ```bash
   nvidia-smi
   ```
2. Installer NVIDIA Container Toolkit (voir section Installation)
3. Redémarrer Docker :
   ```bash
   sudo systemctl restart docker
   ```

---

### Problème 4 : WebSocket Ne Se Connecte Pas

**Symptôme :** Page HTML affiche "Déconnecté" en rouge

**Solutions :**
1. Vérifier que le serveur Python est lancé
2. Vérifier le port dans le code HTML (doit correspondre à `WEBSOCKET_PORT`)
3. Désactiver pare-feu/antivirus temporairement
4. Vérifier la console JavaScript (F12 dans le navigateur)

---

### Problème 5 : Pas de Transcription

**Symptôme :** Microphone fonctionne mais pas de texte

**Solutions :**
1. Vérifier que le microphone est le bon :
   ```python
   # Lister les micros disponibles
   import pyaudio
   p = pyaudio.PyAudio()
   for i in range(p.get_device_count()):
       print(i, p.get_device_info_by_index(i)['name'])
   ```
2. Parler **plus fort** et **plus proche** du micro
3. Vérifier que Riva server est bien démarré :
   ```bash
   docker logs riva-speech --tail 50
   ```
4. Tester avec un fichier audio connu :
   ```bash
   # Via client Riva
   bash riva_start_client.sh
   ```

---

## Exercices Pratiques

### Exercice 1 : Modifier la Langue (Facile)

**Objectif :** Changer la langue de transcription en français.

**Étapes :**
1. Modifier `config.sh` pour ajouter `"fr-FR"`
2. Exécuter `bash riva_init.sh`
3. Redémarrer avec `bash riva_start.sh`
4. Modifier le code Python : `language_code="fr-FR"`
5. Tester en parlant français

**Questions :**
- Quelle est la différence de précision entre anglais et français ?
- Combien de temps prend le téléchargement des modèles français ?

---

### Exercice 2 : Ajouter un Bouton Pause/Reprise (Moyen)

**Objectif :** Ajouter un bouton dans le client HTML pour mettre en pause la transcription.

**Indications :**
1. Ajouter un bouton dans le HTML :
   ```html
   <button id="pauseBtn" onclick="togglePause()">⏸️ Pause</button>
   ```
2. Ajouter une variable d'état :
   ```javascript
   let isPaused = false;
   ```
3. Modifier `displayTranscription()` pour vérifier `isPaused`
4. Implémenter `togglePause()` pour changer l'état

**Bonus :** Envoyer l'état au serveur Python pour vraiment arrêter la transcription.

---

### Exercice 3 : Sauvegarder les Transcriptions (Moyen)

**Objectif :** Sauvegarder toutes les transcriptions finales dans un fichier texte.

**Indications :**
1. Importer le module `datetime` :
   ```python
   from datetime import datetime
   ```
2. Dans `transcription_worker()`, après chaque transcription finale :
   ```python
   if result.is_final:
       with open('transcriptions.txt', 'a', encoding='utf-8') as f:
           timestamp = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
           f.write(f"[{timestamp}] {transcript}\n")
   ```
3. Tester et vérifier le fichier créé

**Bonus :** Format JSON avec métadonnées (durée audio, confiance, etc.)

---

### Exercice 4 : Afficher la Confiance (Difficile)

**Objectif :** Afficher le score de confiance de chaque transcription.

**Indications :**
1. Riva fournit un score de confiance :
   ```python
   confidence = result.alternatives[0].confidence
   ```
2. Modifier le message JSON pour inclure le score :
   ```python
   message = json.dumps({
       "type": "transcription",
       "text": transcript,
       "is_final": is_final,
       "confidence": confidence  # NOUVEAU
   })
   ```
3. Afficher dans le HTML avec une couleur selon le score :
   - Vert : confiance > 0.8
   - Orange : confiance 0.5-0.8
   - Rouge : confiance < 0.5

---

### Exercice 5 : Détection de Mots-Clés (Difficile)

**Objectif :** Mettre en évidence certains mots-clés dans les transcriptions.

**Exemple :** Détecter "urgent", "important", "problème"

**Indications :**
1. Définir une liste de mots-clés :
   ```python
   KEYWORDS = ["urgent", "important", "problème", "critique"]
   ```
2. Dans `transcription_worker()`, vérifier si le texte contient des mots-clés :
   ```python
   has_keyword = any(kw in transcript.lower() for kw in KEYWORDS)
   ```
3. Envoyer cette info dans le JSON :
   ```python
   "has_keyword": has_keyword
   ```
4. Dans le HTML, appliquer un style spécial (fond rouge, clignotant)

**Bonus :** Notifier par son ou notification navigateur.

---

### Exercice 6 : Support Multi-Utilisateurs (Avancé)

**Objectif :** Permettre à plusieurs personnes de transcrire simultanément.

**Architecture :**
- Chaque utilisateur a sa propre "session"
- Chaque session a son propre thread de transcription
- Le serveur WebSocket diffuse uniquement aux clients de la même session

**Indications :**
1. Ajouter un paramètre `session_id` à la connexion WebSocket
2. Modifier `connected_clients` en dictionnaire :
   ```python
   connected_clients = {}  # {session_id: set([client1, client2])}
   ```
3. Créer une fonction `broadcast_to_session(session_id, message)`
4. Démarrer un thread de transcription par session

**Défis :**
- Gestion de la mémoire (limiter le nombre de sessions)
- Nettoyage des sessions inactives
- Isolation audio (chaque personne a son propre micro)

---

## Conclusion

### Ce que Vous Avez Appris

✅ **Reconnaissance vocale en temps réel** avec NVIDIA Riva  
✅ **Programmation asynchrone** avec asyncio  
✅ **Communication WebSocket** pour diffusion en temps réel  
✅ **Multi-threading** en Python  
✅ **Capture audio** avec PyAudio  
✅ **Protocole gRPC** pour streaming bidirectionnel  
✅ **Architecture client-serveur** moderne  

### Applications Possibles

🎯 **Sous-titrage en direct** pour vidéos ou streaming  
🎯 **Assistant virtuel** à commande vocale  
🎯 **Transcription de réunions** professionnelles  
🎯 **Accessibilité** pour personnes malentendantes  
🎯 **Analyse de sentiment** en temps réel  
🎯 **Traduction vocale** (avec NMT Riva)  

### Pour Aller Plus Loin

📚 **Documentation Riva** : https://docs.nvidia.com/deeplearning/riva/  
📚 **Modèles sur NGC** : https://catalog.ngc.nvidia.com/  
📚 **Forum Riva** : https://forums.developer.nvidia.com/c/ai/riva/  
📚 **GitHub Riva** : https://github.com/nvidia-riva/  

### Support et Communauté

💬 **Discord NVIDIA Developers** : https://discord.gg/nvidia  
💬 **Stack Overflow** : Tag `nvidia-riva`  
📧 **Email** : riva-support@nvidia.com (pour clients entreprise)  

---

## Annexes

### Glossaire

- **ASR** : Automatic Speech Recognition (Reconnaissance Vocale Automatique)
- **TTS** : Text-to-Speech (Synthèse Vocale)
- **NLP** : Natural Language Processing (Traitement du Langage Naturel)
- **gRPC** : Google Remote Procedure Call (Protocole de communication)
- **WebSocket** : Protocole de communication bidirectionnelle en temps réel
- **PCM** : Pulse Code Modulation (Format audio non compressé)
- **Sample Rate** : Fréquence d'échantillonnage (Hz)
- **Chunk** : Bloc de données audio
- **Interim Result** : Résultat provisoire de transcription
- **Final Result** : Résultat définitif de transcription
- **Thread** : Fil d'exécution parallèle
- **Asyncio** : Bibliothèque Python pour programmation asynchrone
- **Queue** : File d'attente thread-safe
- **Event** : Mécanisme de synchronisation entre threads

---

### Références

1. NVIDIA Riva Documentation (2025). *Riva Speech Skills User Guide*.  
   https://docs.nvidia.com/deeplearning/riva/

2. Python Software Foundation (2025). *asyncio — Asynchronous I/O*.  
   https://docs.python.org/3/library/asyncio.html

3. WebSocket Protocol (RFC 6455). *The WebSocket Protocol*.  
   https://datatracker.ietf.org/doc/html/rfc6455

4. gRPC Documentation (2025). *gRPC Core Concepts*.  
   https://grpc.io/docs/what-is-grpc/core-concepts/

---

**Fin du TP**

*Bonne chance et bon coding ! 🚀*
