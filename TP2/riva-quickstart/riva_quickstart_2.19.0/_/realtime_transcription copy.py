"""
Real-time Speech Transcription with WebSocket Broadcasting
This script captures audio from the microphone, sends it to Riva for transcription,
and broadcasts the results to all connected WebSocket clients.
"""

# Import Riva client library for speech recognition
import riva.client

# Import PyAudio for microphone access
import pyaudio

# Import asyncio for asynchronous programming (needed for WebSockets)
import asyncio

# Import websockets library for WebSocket server
import websockets

# Import json to format data as JSON
import json

# Import queue for thread-safe data passing
import queue

# Import threading to run audio capture in a separate thread
import threading

# Configuration
RIVA_SERVER = "localhost:50051"  # Address of Riva server
SAMPLE_RATE = 16000  # Audio sample rate (16kHz is standard for speech)
CHUNK_SIZE = 1600  # Number of samples per chunk (100ms at 16kHz)
WEBSOCKET_HOST = "0.0.0.0"  # Listen on all network interfaces
WEBSOCKET_PORT = 8765  # Port for WebSocket server

# Global set to store all connected WebSocket clients
# A set automatically handles adding/removing clients without duplicates
connected_clients = set()

async def broadcast_transcription(message_type, text, is_final=False):
    """
    Broadcast a transcription message to all connected WebSocket clients.
    
    Parameters:
    - message_type: Type of message ("transcription", "status", "error")
    - text: The actual text content to send
    - is_final: Whether this is a final transcription (True) or interim (False)
    """
    
    # If there are no connected clients, don't waste time creating the message
    if not connected_clients:
        return
    
    # Create a JSON message with all the information
    message = json.dumps({
        "type": message_type,  # What kind of message is this?
        "text": text,  # The actual content
        "is_final": is_final,  # Is this the final version or still processing?
        "timestamp": asyncio.get_event_loop().time()  # When was this sent?
    })
    
    # Create a list to store clients that have disconnected
    # We can't modify the set while iterating over it, so we collect disconnected clients
    disconnected_clients = set()
    
    # Send the message to all connected clients
    for client in connected_clients:
        try:
            # Try to send the message to this client
            await client.send(message)
        except websockets.exceptions.ConnectionClosed:
            # If the connection is closed, mark this client for removal
            disconnected_clients.add(client)
    
    # Remove all disconnected clients from our set
    connected_clients.difference_update(disconnected_clients)

async def websocket_handler(websocket, path):
    """
    Handle a new WebSocket connection.
    This function is called whenever a client connects.
    
    Parameters:
    - websocket: The WebSocket connection object
    - path: The URL path the client connected to (we don't use this)
    """
    
    # Add this new client to our set of connected clients
    connected_clients.add(websocket)
    
    # Get the client's IP address for logging
    client_ip = websocket.remote_address[0] if websocket.remote_address else "unknown"
    
    # Print a message when a client connects
    print(f"✓ New client connected from {client_ip} (Total clients: {len(connected_clients)})")
    
    # Send a welcome message to the new client
    await websocket.send(json.dumps({
        "type": "status",
        "text": "Connected to transcription server",
        "is_final": True
    }))
    
    try:
        # Keep the connection open and wait for messages from the client
        # In this case, we don't expect messages from clients, but we need to keep the connection alive
        async for message in websocket:
            # If the client sends a message, we could handle it here
            # For now, we just ignore it
            pass
    except websockets.exceptions.ConnectionClosed:
        # This happens when the client disconnects normally
        pass
    finally:
        # Remove this client from our set when they disconnect
        connected_clients.discard(websocket)
        print(f"✗ Client disconnected from {client_ip} (Total clients: {len(connected_clients)})")

async def start_websocket_server():
    """
    Start the WebSocket server.
    This creates a server that listens for WebSocket connections.
    """
    
    # Create and start the WebSocket server
    # websocket_handler: Function to call when a client connects
    # WEBSOCKET_HOST: IP address to listen on (0.0.0.0 means all interfaces)
    # WEBSOCKET_PORT: Port number to listen on
    server = await websockets.serve(websocket_handler, WEBSOCKET_HOST, WEBSOCKET_PORT)
    
    # Print a message showing the server is running
    print(f"🌐 WebSocket server started on ws://{WEBSOCKET_HOST}:{WEBSOCKET_PORT}")
    print(f"   Clients can connect using: ws://localhost:{WEBSOCKET_PORT}")
    
    # Keep the server running forever
    await asyncio.Future()  # This creates a future that never completes

def transcription_worker(audio_queue, stop_event, loop):
    """
    Worker function that handles the transcription.
    This runs in the main thread and processes audio from the queue.
    
    Parameters:
    - audio_queue: Queue containing audio chunks from the microphone
    - stop_event: Event to signal when to stop
    - loop: The asyncio event loop for broadcasting messages
    """
    
    # Connect to Riva server
    auth = riva.client.Auth(uri=RIVA_SERVER)
    asr_service = riva.client.ASRService(auth)
    
    # Configure streaming speech recognition
    config = riva.client.StreamingRecognitionConfig(
        config=riva.client.RecognitionConfig(
            # Audio format: Linear PCM (raw audio)
            encoding=riva.client.AudioEncoding.LINEAR_PCM,
            
            # Language to transcribe (change to "fr-FR" for French)
            language_code="fr-FR",
            
            # Only return the best guess
            max_alternatives=1,
            
            # Don't filter profanity
            profanity_filter=False,
            
            # Add punctuation automatically
            enable_automatic_punctuation=True,
            
            # IMPORTANT: Sample rate must match SAMPLE_RATE
            sample_rate_hertz=SAMPLE_RATE,
            
            # Mono audio (1 channel)
            audio_channel_count=1,
            
            # Format the output nicely
            verbatim_transcripts=False,
        ),
        # Return partial results as we transcribe
        interim_results=True,
    )
    
    def audio_generator():
        """
        Generator function that yields audio chunks for Riva.
        This pulls audio from the queue and feeds it to Riva.
        """
        while not stop_event.is_set():
            try:
                # Try to get audio from the queue (wait up to 0.1 seconds)
                chunk = audio_queue.get(timeout=0.1)
                
                # If we get None, it's a signal to stop
                if chunk is None:
                    break
                
                # Yield the audio chunk to Riva
                yield chunk
            except queue.Empty:
                # If queue is empty, just continue
                continue
    
    # Send a status message to all connected clients
    asyncio.run_coroutine_threadsafe(
        broadcast_transcription("status", "Transcription started", True),
        loop
    )
    
    try:
        # Start streaming recognition with Riva
        responses = asr_service.streaming_response_generator(
            audio_chunks=audio_generator(),
            streaming_config=config
        )
        
        # Process each response from Riva
        for response in responses:
            # Skip if no results
            if not response.results:
                continue
            
            # Process each result
            for result in response.results:
                # Skip if no alternatives
                if not result.alternatives:
                    continue
                
                # Get the transcribed text
                transcript = result.alternatives[0].transcript
                
                # Check if this is a final result or interim
                if result.is_final:
                    # Final result - print to console with checkmark
                    print(f"✓ FINAL: {transcript}")
                    
                    # Broadcast final transcription to all WebSocket clients
                    asyncio.run_coroutine_threadsafe(
                        broadcast_transcription("transcription", transcript, True),
                        loop
                    )
                else:
                    # Interim result - print to console (overwrites previous line)
                    print(f"  interim: {transcript}          ", end='\r', flush=True)
                    
                    # Broadcast interim transcription to all WebSocket clients
                    asyncio.run_coroutine_threadsafe(
                        broadcast_transcription("transcription", transcript, False),
                        loop
                    )
    
    except Exception as e:
        # If there's an error, print it and broadcast to clients
        error_msg = f"Transcription error: {e}"
        print(f"\n❌ {error_msg}")
        asyncio.run_coroutine_threadsafe(
            broadcast_transcription("error", error_msg, True),
            loop
        )

def capture_audio(audio_queue, stop_event):
    """
    Capture audio from the microphone and put it in the queue.
    This runs in a separate thread.
    
    Parameters:
    - audio_queue: Queue to put captured audio chunks into
    - stop_event: Event to signal when to stop capturing
    """
    
    # Initialize PyAudio
    audio = pyaudio.PyAudio()
    
    # Open the microphone stream
    stream = audio.open(
        format=pyaudio.paInt16,  # 16-bit audio
        channels=1,  # Mono
        rate=SAMPLE_RATE,  # Sample rate (16kHz)
        input=True,  # We're recording
        frames_per_buffer=CHUNK_SIZE  # Chunk size
    )
    
    print("🎤 Microphone started - speak now!")
    
    # Keep capturing until stop_event is set
    while not stop_event.is_set():
        try:
            # Read audio data from microphone
            data = stream.read(CHUNK_SIZE, exception_on_overflow=False)
            
            # Put the audio in the queue for transcription
            audio_queue.put(data)
        except Exception as e:
            print(f"Error capturing audio: {e}")
            break
    
    # Clean up when done
    stream.stop_stream()
    stream.close()
    audio.terminate()
    print("🎤 Microphone stopped")

async def main_async():
    """
    Main asynchronous function that coordinates everything.
    """
    
    # Create a queue for audio data
    audio_queue = queue.Queue()
    
    # Create an event to signal when to stop
    stop_event = threading.Event()
    
    # Get the current asyncio event loop
    loop = asyncio.get_event_loop()
    
    # Start the WebSocket server (runs in the background)
    websocket_task = asyncio.create_task(start_websocket_server())
    
    # Start audio capture in a separate thread
    capture_thread = threading.Thread(
        target=capture_audio,
        args=(audio_queue, stop_event),
        daemon=True
    )
    capture_thread.start()
    
    # Start transcription worker in a separate thread
    transcription_thread = threading.Thread(
        target=transcription_worker,
        args=(audio_queue, stop_event, loop),
        daemon=True
    )
    transcription_thread.start()
    
    print("\n" + "=" * 60)
    print("Real-time Transcription Server with WebSocket Broadcasting")
    print("=" * 60)
    print(f"WebSocket URL: ws://localhost:{WEBSOCKET_PORT}")
    print("Press Ctrl+C to stop")
    print("=" * 60 + "\n")
    
    try:
        # Wait for the WebSocket server to finish (which is never, unless interrupted)
        await websocket_task
    except KeyboardInterrupt:
        # User pressed Ctrl+C
        print("\n\n🛑 Shutting down...")
    finally:
        # Signal all threads to stop
        stop_event.set()
        
        # Put None in queue to signal generator to stop
        audio_queue.put(None)
        
        # Wait a bit for threads to finish
        capture_thread.join(timeout=1)
        transcription_thread.join(timeout=1)
        
        # Close all WebSocket connections
        for client in list(connected_clients):
            await client.close()
        
        print("Shutdown complete!")

def main():
    """
    Entry point of the program.
    """
    try:
        # Run the async main function
        asyncio.run(main_async())
    except KeyboardInterrupt:
        # Handle Ctrl+C gracefully
        print("\nExiting...")

# This runs when the script is executed directly
if __name__ == "__main__":
    main()