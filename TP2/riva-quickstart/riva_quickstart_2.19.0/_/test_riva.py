import riva.client
import pyaudio
import numpy as np

RIVA_SERVER = "localhost:50051"
SAMPLE_RATE = 16000

def record_audio_with_feedback(duration=3):
    """Record audio and show volume feedback"""
    audio = pyaudio.PyAudio()
    
    print(f"\n🎤 Recording for {duration} seconds...")
    print("   SPEAK NOW into your microphone!\n")
    
    stream = audio.open(
        format=pyaudio.paInt16,
        channels=1,
        rate=SAMPLE_RATE,
        input=True,
        frames_per_buffer=1600
    )
    
    frames = []
    for i in range(0, int(SAMPLE_RATE / 1600 * duration)):
        data = stream.read(1600, exception_on_overflow=False)
        frames.append(data)
        
        # Show volume level
        audio_data = np.frombuffer(data, dtype=np.int16)
        volume = np.abs(audio_data).mean()
        bar = '█' * int(volume / 100)
        print(f"   Volume: {bar} ({int(volume)})", end='\r')
    
    print("\n   ✓ Recording complete!\n")
    
    stream.stop_stream()
    stream.close()
    audio.terminate()
    
    return b''.join(frames)

def test_offline_transcription():
    """Test with offline recognition"""
    print("=" * 60)
    print("Testing Offline Transcription")
    print("=" * 60)
    
    # Connect to Riva
    auth = riva.client.Auth(uri=RIVA_SERVER)
    asr_service = riva.client.ASRService(auth)
    
    # Record audio
    audio_data = record_audio_with_feedback(duration=5)
    
    # Configure ASR
    config = riva.client.RecognitionConfig(
        encoding=riva.client.AudioEncoding.LINEAR_PCM,
        language_code="en-US",
        max_alternatives=3,
        sample_rate_hertz=SAMPLE_RATE,
        audio_channel_count=1,
        enable_automatic_punctuation=True,
        profanity_filter=False,
    )
    
    print("Transcribing...")
    response = asr_service.offline_recognize(audio_data, config)
    
    if response.results and len(response.results) > 0:
        print("\n✓ TRANSCRIPTION RESULTS:")
        for i, result in enumerate(response.results):
            for j, alt in enumerate(result.alternatives):
                print(f"   Alternative {j+1}: '{alt.transcript}' (confidence: {alt.confidence:.2f})")
    else:
        print("\n⚠ No transcription results.")
        print("   Tips:")
        print("   - Make sure you spoke during the recording")
        print("   - Try speaking louder and clearer")
        print("   - Check if the correct microphone is selected")
        print("   - Ensure your microphone isn't muted")

if __name__ == "__main__":
    test_offline_transcription()