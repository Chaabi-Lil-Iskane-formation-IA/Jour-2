# API FastAPI + Coqui TTS (Français) — GPU / CPU

Ce projet expose une API HTTP de **synthèse vocale (TTS)** en français à l’aide de **FastAPI** et **Coqui TTS**, avec accélération optionnelle sur **GPU NVIDIA**.

Compatible Windows, Linux et WSL.  
Testé avec **Python 3.11** et **PyTorch CUDA 12.8** (nécessaire pour les cartes RTX série 50xx).

---

## 🎯 Objectif

- Transformer du texte en audio **WAV**.
- Exécuter le modèle TTS entièrement **en local**, sans Internet (après premier téléchargement).
- Supporter le **GPU NVIDIA** pour des performances élevées.
- Fournir une API simple à appeler depuis un frontend, une application mobile, un script Python ou Postman.

---

## ✅ 1. Rôle de chaque dépendance

| Dépendance | Rôle |
|-----------|------|
| **fastapi** | Framework web ultra-rapide. Définit les endpoints (`/tts/wav`). |
| **uvicorn[standard]** | Serveur ASGI qui exécute FastAPI (hot reload). |
| **TTS==0.22.0** | Librairie Coqui TTS (accès aux modèles vocaux). |
| **torch / torchvision / torchaudio** | Backend PyTorch utilisé pour exécuter les modèles TTS. |
| **numpy / scipy / soundfile** | Traitement audio interne. |
| **librosa** (optionnel) | Analyse audio (utile pour `speaker_wav`). |
| **setuptools < 81** (optionnel) | Supprime un warning lié à `pkg_resources`. |

---

## ✅ 2. Installation (Windows / Python 3.11)

### 👉 2.1 Créer un environnement virtuel
```bat
py -3.11 -m venv tts_env
tts_env\Scripts\activate
python -m pip install -U pip setuptools wheel
```

---

### 👉 2.2 Installer PyTorch (choisir UNE version)

### ✅ GPU (RTX 50xx, recommandé)
```bat
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu128
```

### ✅ CPU uniquement
```bat
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cpu
```

---

### 👉 2.3 Installer les dépendances restantes
```bat
pip install "TTS==0.22.0" fastapi uvicorn[standard] numpy scipy soundfile
pip install librosa               # optionnel
pip install "setuptools<81"       # optionnel
```

---

## ✅ 3. Vérifier la disponibilité GPU

```python
import torch
print(torch.__version__, "CUDA:", torch.version.cuda, "avail:", torch.cuda.is_available())
if torch.cuda.is_available():
    print("capability:", torch.cuda.get_device_capability(0))
```

Vous devez voir :
```
CUDA 12.8
avail: True
capability: (12, 0)
```

---

## ✅ 4. Lancer l’API

```bat
uvicorn main:app --reload --port 5005
```

Ouvrir :  
👉 http://127.0.0.1:5005/docs

---

## ✅ 5. Exemples d’utilisation

### ✅ cURL (Windows)
```bat
curl -X POST "http://127.0.0.1:5005/tts/wav" ^
  -H "Content-Type: application/json" ^
  -d "{\"text\":\"Bonjour, je suis Achraf.\",\"speed\":1.0}" ^
  --output out.wav
```

### ✅ Python client
```python
import requests
r = requests.post("http://127.0.0.1:5005/tts/wav",
                  json={"text": "Bonjour, test TTS.", "speed": 1.0})
open("out.wav","wb").write(r.content)
```

### ✅ Test dans navigateur
```js
fetch("http://127.0.0.1:5005/tts/wav", {
  method: "POST",
  headers: {"Content-Type":"application/json"},
  body: JSON.stringify({ text: "Bonjour, test TTS." })
})
.then(r => r.blob())
.then(b => new Audio(URL.createObjectURL(b)).play());
```

---

## ✅ 6. Explication du fonctionnement Coqui TTS

### 🔹 1. Chargement du modèle
```python
MODEL_NAME = "tts_models/fr/css10/vits"
tts = TTS(MODEL_NAME).to(device)
```

### 🔹 2. Choix GPU / CPU
```python
device = "cuda" if torch.cuda.is_available() else "cpu"
```

### 🔹 3. Synthèse vocale
```python
tts.tts_to_file(
    text=req.text,
    speaker=req.speaker,
    speaker_wav=req.speaker_wav,
    speed=req.speed,
    file_path=out_path
)
```

### 🔹 4. Endpoint FastAPI
```python
@app.post("/tts/wav")
def synthesize(req: TTSRequest):
    return Response(open(out_file, "rb").read(), media_type="audio/wav")
```

### 🔹 5. Chargement au démarrage
```python
@app.on_event("startup")
def load_model():
    global tts
    tts = TTS(MODEL_NAME).to(device)
```

---

## ✅ 7. Optimisations

```python
torch.backends.cudnn.benchmark = True
torch.set_float32_matmul_precision("high")
```

---

## ✅ 8. requirements.txt

```
fastapi
uvicorn[standard]
TTS==0.22.0
numpy
scipy
soundfile
librosa
torch
torchvision
torchaudio
setuptools<81
```

---

## ✅ 9. Dépannage

| Problème | Solution |
|----------|-----------|
| TTS ne s’installe pas | Installer Python 3.11 |
| GPU non supporté sm_120 | Installer torch cu128 |
| torch.cuda.is_available() = False | Installer torch cu128 + drivers NVIDIA |
| Audio lent | Découper texte |

---

Bonne utilisation 🎤🚀
