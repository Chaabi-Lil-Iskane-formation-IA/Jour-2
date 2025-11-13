# Importation du framework FastAPI pour créer l'API
from fastapi import FastAPI, Response, HTTPException

# Pydantic permet de définir et valider les données d'entrée (ici : la requête TTS)
from pydantic import BaseModel

# PyTorch est le backend utilisé pour exécuter le modèle TTS (CPU ou GPU)
import torch

# tempfile permet de créer un dossier temporaire pour générer le fichier audio
import tempfile

# os est utilisé pour manipuler les chemins de fichiers
import os

# Ces variables seront utilisées pour charger dynamiquement le modèle TTS
TTS = None
tts = None

# Nom du modèle vocal Coqui TTS à utiliser (XTTS v2 - multilingue)
MODEL_NAME = "tts_models/multilingual/multi-dataset/xtts_v2"

# Dossier pour stocker les voix par défaut
DEFAULT_SPEAKERS_DIR = "./default_speakers"

# Création de l'application FastAPI
app = FastAPI()

# Définition du schéma de données attendu par l'API (JSON envoyé par le client)
class TTSRequest(BaseModel):
    text: str                           # Texte à synthétiser en audio
    speaker_wav: str | None = None      # Chemin vers un fichier WAV personnalisé (optionnel)
    speaker_name: str | None = "default" # Nom du speaker par défaut (male, female, default)
    language: str = "fr"                # Langue du texte
    speed: float = 1.0                  # Vitesse de lecture (1.0 = normal)

# Fonction pour télécharger les voix par défaut
def setup_default_speakers():
    """Télécharge des exemples de voix si elles n'existent pas"""
    os.makedirs(DEFAULT_SPEAKERS_DIR, exist_ok=True)
    
    # Voix par défaut à télécharger
    default_voices = {
        "default": "https://github.com/mozilla/TTS/raw/dev/tests/data/ljspeech/wavs/LJ001-0001.wav",
        "male": "https://github.com/mozilla/TTS/raw/dev/tests/data/ljspeech/wavs/LJ001-0002.wav",
        "female": "https://github.com/mozilla/TTS/raw/dev/tests/data/ljspeech/wavs/LJ001-0003.wav"
    }
    
    import requests
    
    for name, url in default_voices.items():
        filepath = os.path.join(DEFAULT_SPEAKERS_DIR, f"{name}.wav")
        if not os.path.exists(filepath):
            print(f"Downloading {name} speaker voice...")
            try:
                response = requests.get(url, timeout=30)
                response.raise_for_status()
                with open(filepath, "wb") as f:
                    f.write(response.content)
                print(f"✓ {name} speaker downloaded")
            except Exception as e:
                print(f"✗ Failed to download {name} speaker: {e}")

# Fonction exécutée automatiquement au démarrage du serveur FastAPI
@app.on_event("startup")
def load_model():
    global TTS, tts

    print("Initializing XTTS v2 model…")
    
    # Télécharger les voix par défaut
    setup_default_speakers()

    # Importation retardée du module TTS
    from TTS.api import TTS as _TTS
    TTS = _TTS

    # Forcer l'utilisation du CPU
    device = "cpu"
    print(f"Using device: {device}")

    # Création de l'instance du modèle XTTS v2 sur CPU
    tts = TTS(MODEL_NAME, gpu=False)
    
    print("XTTS v2 model loaded successfully.")

# Endpoint POST permettant de générer un fichier WAV à partir d'un texte
@app.post("/tts/wav")
def synthesize(req: TTSRequest):
    global tts

    # Si le modèle n'est pas encore prêt, on renvoie une erreur 503
    if tts is None:
        raise HTTPException(status_code=503, detail="Model not loaded yet")

    # Déterminer quel fichier speaker utiliser
    if req.speaker_wav:
        # Utiliser le fichier WAV fourni par l'utilisateur
        speaker_path = req.speaker_wav
        if not os.path.exists(speaker_path):
            raise HTTPException(status_code=400, detail=f"Speaker WAV file not found: {speaker_path}")
    else:
        # Utiliser une voix par défaut
        speaker_path = os.path.join(DEFAULT_SPEAKERS_DIR, f"{req.speaker_name}.wav")
        if not os.path.exists(speaker_path):
            # Si la voix demandée n'existe pas, utiliser "default"
            speaker_path = os.path.join(DEFAULT_SPEAKERS_DIR, "default.wav")
            if not os.path.exists(speaker_path):
                raise HTTPException(
                    status_code=500, 
                    detail="No default speaker voices available. Please provide speaker_wav or restart the server."
                )

    # Création d'un dossier temporaire pour sauvegarder le fichier WAV généré
    with tempfile.TemporaryDirectory() as td:
        out_path = os.path.join(td, "out.wav")

        try:
            # Synthèse vocale avec clonage de voix
            tts.tts_to_file(
                text=req.text,
                speaker_wav=speaker_path,
                language=req.language,
                file_path=out_path,
                speed=req.speed
            )
        except Exception as e:
            raise HTTPException(status_code=500, detail=f"TTS generation failed: {str(e)}")

        # Lecture du fichier WAV généré pour le renvoyer dans la réponse
        with open(out_path, "rb") as f:
            audio_data = f.read()

        # On retourne le contenu audio au client, au format WAV
        return Response(content=audio_data, media_type="audio/wav")

# Endpoint simple pour vérifier si l'API est prête
@app.get("/health")
def health():
    return {
        "ready": tts is not None,
        "model": MODEL_NAME,
        "device": "cpu"
    }

# Endpoint pour lister les voix disponibles
@app.get("/speakers")
def get_speakers():
    """Liste les voix par défaut disponibles"""
    if not os.path.exists(DEFAULT_SPEAKERS_DIR):
        return {"speakers": []}
    
    speakers = [
        f.replace(".wav", "") 
        for f in os.listdir(DEFAULT_SPEAKERS_DIR) 
        if f.endswith(".wav")
    ]
    return {"speakers": speakers}

# Endpoint pour lister les langues supportées
@app.get("/languages")
def get_languages():
    return {
        "languages": ["fr", "en", "es", "de", "it", "pt", "pl", "tr", "ru", "nl", "cs", "ar", "zh-cn", "ja", "hu", "ko"]
    }