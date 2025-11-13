# Importation du framework FastAPI pour créer l’API
from fastapi import FastAPI, Response

# Pydantic permet de définir et valider les données d’entrée (ici : la requête TTS)
from pydantic import BaseModel

# PyTorch est le backend utilisé pour exécuter le modèle TTS (CPU ou GPU)
import torch

# tempfile permet de créer un dossier temporaire pour générer le fichier audio
import tempfile

# os est utilisé pour manipuler les chemins de fichiers
import os

# Ces variables seront utilisées pour charger dynamiquement le modèle TTS
# On les définit à None pour éviter les erreurs tant que le modèle n’est pas encore chargé
TTS = None
tts = None

# Nom du modèle vocal Coqui TTS à utiliser (français, modèle VITS CSS10)
MODEL_NAME = "tts_models/fr/css10/vits"
# MODEL_NAME = "tts_models/multilingual/multi-dataset/xtts_v2" this need to purchased

# Création de l'application FastAPI
app = FastAPI()

# Définition du schéma de données attendu par l’API (JSON envoyé par le client)
class TTSRequest(BaseModel):
    text: str                    # Texte à synthétiser en audio
    speaker_wav: str | None = None  # Chemin vers un fichier WAV pour utiliser une voix personnalisée (optionnel)
    speaker: str | None = None      # Nom d’un speaker interne au modèle (rare pour CSS10)
    speed: float | None = None      # Vitesse de lecture (1.0 = normal)

# Fonction exécutée automatiquement au démarrage du serveur FastAPI
@app.on_event("startup")
def load_model():
    global TTS, tts   # On indique que l’on va modifier les variables globales

    print("Initializing TTS model…")

    # Importation retardée du module TTS pour éviter les erreurs si le modèle n'est pas prêt
    from TTS.api import TTS as _TTS
    TTS = _TTS  # On assigne la classe importée à la variable globale

    # Détection automatique du périphérique : GPU si disponible, sinon CPU
    device = "cuda" if torch.cuda.is_available() else "cpu"
    print("Using device:", device)

    # Création de l’instance du modèle TTS et transfert vers CPU ou GPU
    tts = TTS(MODEL_NAME).to(device)
    print("TTS model loaded.")  # Indique que le modèle est prêt

# Endpoint POST permettant de générer un fichier WAV à partir d’un texte
@app.post("/tts/wav")
def synthesize(req: TTSRequest):
    global tts

    # Si le modèle n'est pas encore prêt, on renvoie une erreur 503
    if tts is None:
        return Response(content=b"", media_type="text/plain", status_code=503)

    # Création d'un dossier temporaire pour sauvegarder le fichier WAV généré
    with tempfile.TemporaryDirectory() as td:
        out_path = os.path.join(td, "out.wav")  # chemin complet du fichier audio temporaire

        # Si l'utilisateur fournit un chemin WAV externe pour une voix personnalisée
        if req.speaker_wav:
            tts.tts_to_file(
                text=req.text,
                speaker_wav=req.speaker_wav,  # Utilisation d'une voix issue d'un fichier audio
                file_path=out_path,           # Où sauvegarder la sortie
                speed=req.speed               # Vitesse de lecture
            )
        else:
            # Synthèse classique avec la voix du modèle
            tts.tts_to_file(
                text=req.text,
                speaker=req.speaker,          # Speaker interne du modèle (rarement utilisé ici)
                file_path=out_path,
                speed=req.speed
            )

        # Lecture du fichier WAV généré pour le renvoyer dans la réponse
        with open(out_path, "rb") as f:
            audio_data = f.read()

        # On retourne le contenu audio au client, au format WAV
        return Response(content=audio_data, media_type="audio/wav")

# Endpoint simple pour vérifier si l’API est prête (ex : monitoring)
@app.get("/health")
def health():
    return {"ready": tts is not None}  # True si le modèle est chargé
