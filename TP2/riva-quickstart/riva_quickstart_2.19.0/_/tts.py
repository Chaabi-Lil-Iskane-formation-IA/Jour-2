"""
Real-time Text-to-Speech with NVIDIA Riva and WebSocket
This script receives text via WebSocket, generates speech using Riva TTS,
and streams the audio back to connected clients.
"""

# Import Riva client library for text-to-speech
import riva.client
import riva.client.proto.riva_tts_pb2 as rtts

# Import asyncio for asynchronous programming
import asyncio

# Import websockets library for WebSocket server
import websockets

# Check websockets version for compatibility
import sys
WEBSOCKETS_VERSION = tuple(map(int, websockets.__version__.split('.')[:2]))
print(f"📦 Websockets version: {websockets.__version__}")

# Import json to parse/format JSON data
import json

# Import wave to save audio files (optional)
import wave

# Import io for in-memory file operations
import io

# Configuration
RIVA_SERVER = "localhost:50051"  # Address of Riva server
SAMPLE_RATE = 22050  # Audio sample rate for TTS (22.05kHz is common for TTS)
WEBSOCKET_HOST = "0.0.0.0"  # Listen on all network interfaces
WEBSOCKET_PORT = 8766  # Port for WebSocket server (different from transcription)

# TTS Configuration
DEFAULT_VOICE = "French-FR-Laetitia-22khz"  # Voice model to use
# Available voices (examples - check your Riva installation):
# English: "English-US-Female-1", "English-US-Male-1"
# French: "French-FR-Laetitia-22khz", "French-FR-Loic-22khz"

# Global set to store all connected WebSocket clients
connected_clients = set()

# Initialize Riva TTS service (global to reuse connection)
print("🔌 Connexion au serveur Riva TTS...")
auth = riva.client.Auth(uri=RIVA_SERVER)
tts_service = riva.client.SpeechSynthesisService(auth)
print("✅ Connecté à Riva TTS")


def generate_audio(text, voice=DEFAULT_VOICE, language_code="fr-FR"):
    """
    Generate audio from text using Riva TTS.
    
    Parameters:
    - text: Text to convert to speech
    - voice: Voice model to use
    - language_code: Language code (fr-FR for French, en-US for English)
    
    Returns:
    - bytes: WAV audio data
    """
    try:
        # Validate input
        if not text or not text.strip():
            raise ValueError("Le texte ne peut pas être vide")
        
        print(f"🔊 Génération audio pour: '{text[:50]}...'")
        print(f"   Voice: {voice}")
        print(f"   Language: {language_code}")
        
        # Try synthesize method (newer API)
        try:
            req = rtts.SynthesizeSpeechRequest()
            req.text = text
            req.language_code = language_code
            req.encoding = riva.client.AudioEncoding.LINEAR_PCM
            req.sample_rate_hz = SAMPLE_RATE
            req.voice_name = voice
            
            resp = tts_service.stub.Synthesize(req)
            audio_samples = resp.audio
            
        except Exception as e1:
            print(f"⚠️  Méthode Synthesize échouée, essai avec SynthesizeOnline...")
            
            # Try synthesize_online method (alternative API)
            try:
                responses = tts_service.synthesize_online(
                    text=text,
                    voice_name=voice,
                    language_code=language_code,
                    encoding=riva.client.AudioEncoding.LINEAR_PCM,
                    sample_rate_hz=SAMPLE_RATE
                )
                
                # Collect all audio chunks
                audio_samples = b""
                for response in responses:
                    audio_samples += response.audio
                    
            except Exception as e2:
                print(f"⚠️  SynthesizeOnline échouée, essai sans voice_name...")
                
                # Try without voice_name (use default voice)
                responses = tts_service.synthesize_online(
                    text=text,
                    language_code=language_code,
                    encoding=riva.client.AudioEncoding.LINEAR_PCM,
                    sample_rate_hz=SAMPLE_RATE
                )
                
                audio_samples = b""
                for response in responses:
                    audio_samples += response.audio
        
        if not audio_samples:
            raise ValueError("Aucun audio généré par Riva")
        
        # Create WAV file in memory
        wav_buffer = io.BytesIO()
        with wave.open(wav_buffer, 'wb') as wav_file:
            wav_file.setnchannels(1)  # Mono
            wav_file.setsampwidth(2)  # 16-bit
            wav_file.setframerate(SAMPLE_RATE)
            wav_file.writeframes(audio_samples)
        
        # Get the complete WAV file
        wav_data = wav_buffer.getvalue()
        
        print(f"✅ Audio généré: {len(wav_data)} bytes")
        return wav_data
    
    except Exception as e:
        error_msg = str(e)
        print(f"❌ Erreur lors de la génération audio: {error_msg}")
        
        # Check if it's an UNIMPLEMENTED error
        if "UNIMPLEMENTED" in error_msg or "StatusCode.UNIMPLEMENTED" in error_msg:
            raise ValueError("Le service TTS n'est pas disponible sur ce serveur Riva. Vérifiez que les modèles TTS sont installés et que le service est activé.")
        
        raise


async def send_audio_to_client(websocket, audio_data):
    """
    Send audio data to a WebSocket client.
    
    Parameters:
    - websocket: WebSocket connection
    - audio_data: WAV audio bytes to send
    """
    try:
        # Send audio as binary data
        await websocket.send(audio_data)
    except websockets.exceptions.ConnectionClosed:
        print("⚠️  Client déconnecté pendant l'envoi audio")
    except Exception as e:
        print(f"❌ Erreur lors de l'envoi audio: {str(e)}")


async def send_status(websocket, status_type, message):
    """
    Send a status message to a client.
    
    Parameters:
    - websocket: WebSocket connection
    - status_type: Type of status ("success", "error", "info")
    - message: Status message text
    """
    try:
        status = json.dumps({
            "type": status_type,
            "message": message,
            "timestamp": asyncio.get_event_loop().time()
        })
        await websocket.send(status)
    except Exception as e:
        print(f"❌ Erreur lors de l'envoi du statut: {str(e)}")


async def websocket_handler(websocket):
    """
    Handle WebSocket connections and TTS requests.
    
    Parameters:
    - websocket: The WebSocket connection
    """
    # Add client to connected set
    connected_clients.add(websocket)
    client_ip = websocket.remote_address[0] if websocket.remote_address else "unknown"
    
    print(f"✓ Nouveau client TTS connecté depuis {client_ip} (Total: {len(connected_clients)})")
    
    # Send welcome message
    await send_status(websocket, "info", "Connecté au serveur TTS - Envoyez du texte pour générer de l'audio")
    
    try:
        # Listen for messages from client
        async for message in websocket:
            try:
                # Try to parse as JSON
                data = json.loads(message)
                
                # Extract parameters
                text = data.get("text", "").strip()
                voice = data.get("voice", DEFAULT_VOICE)
                language = data.get("language", "fr-FR")
                
                if not text:
                    await send_status(websocket, "error", "Le texte ne peut pas être vide")
                    continue
                
                print(f"📝 Requête TTS de {client_ip}: '{text[:50]}...'")
                
                # Send processing status
                await send_status(websocket, "info", "Génération de l'audio en cours...")
                
                # Generate audio
                audio_data = generate_audio(text, voice, language)
                
                # Send audio to client
                await send_audio_to_client(websocket, audio_data)
                
                # Send success status
                await send_status(websocket, "success", f"Audio généré: {len(audio_data)} bytes")
                
            except json.JSONDecodeError:
                # If not JSON, treat as plain text
                text = message.strip()
                
                if not text:
                    await send_status(websocket, "error", "Le texte ne peut pas être vide")
                    continue
                
                print(f"📝 Requête TTS (texte brut) de {client_ip}: '{text[:50]}...'")
                
                # Generate audio with default settings
                audio_data = generate_audio(text)
                
                # Send audio to client
                await send_audio_to_client(websocket, audio_data)
                
            except Exception as e:
                error_msg = f"Erreur: {str(e)}"
                print(f"❌ {error_msg}")
                await send_status(websocket, "error", error_msg)
    
    except websockets.exceptions.ConnectionClosed:
        print(f"✗ Client TTS déconnecté: {client_ip}")
    
    finally:
        # Remove client from set
        connected_clients.discard(websocket)
        print(f"📊 Clients TTS restants: {len(connected_clients)}")


async def start_websocket_server():
    """
    Start the WebSocket TTS server.
    """
    server = await websockets.serve(
        websocket_handler,
        WEBSOCKET_HOST,
        WEBSOCKET_PORT
    )
    
    print("\n" + "=" * 70)
    print("🔊 Serveur TTS Riva avec WebSocket")
    print("=" * 70)
    print(f"🌐 WebSocket URL: ws://localhost:{WEBSOCKET_PORT}")
    print(f"🎤 Voix par défaut: {DEFAULT_VOICE}")
    print(f"📊 Fréquence d'échantillonnage: {SAMPLE_RATE} Hz")
    print("\n💡 Format de requête JSON:")
    print('   {')
    print('     "text": "Votre texte ici",')
    print('     "voice": "French-FR-Laetitia-22khz",  # Optionnel')
    print('     "language": "fr-FR"  # Optionnel')
    print('   }')
    print("\n💡 Ou envoyez simplement du texte brut pour utiliser les paramètres par défaut")
    print("\nAppuyez sur Ctrl+C pour arrêter")
    print("=" * 70 + "\n")
    
    # Keep server running
    await asyncio.Future()


async def main():
    """
    Main function to run the TTS server.
    """
    try:
        await start_websocket_server()
    except KeyboardInterrupt:
        print("\n\n🛑 Arrêt du serveur TTS...")
    finally:
        # Close all client connections
        if connected_clients:
            print("🔌 Fermeture des connexions clients...")
            for client in list(connected_clients):
                await client.close()
        print("✅ Arrêt complet!")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n👋 Au revoir!")