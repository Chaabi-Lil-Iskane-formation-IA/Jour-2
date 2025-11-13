using System;
using System.IO;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using NAudio.Wave;

namespace WhisperFileTranscriber
{
    class Program
    {
        // Configuration
        private const string AUDIO_FILE = "audio-mp3.mp3";
        private const string MODEL_NAME = "ggml-base.bin";  // Options: tiny, base, small, medium, large
        private const string LANGUAGE = "fr";  // French

        static async Task Main(string[] args)
        {
            Console.WriteLine("🎤 Whisper Local File Transcriber");
            Console.WriteLine("=================================\n");

            string audioFile = args.Length > 0 ? args[0] : AUDIO_FILE;

            if (!File.Exists(audioFile))
            {
                Console.WriteLine($"❌ Error: File not found: {audioFile}");
                Console.WriteLine($"Usage: WhisperFileTranscriber <path-to-audio-file>");
                return;
            }

            Console.WriteLine($"📁 Audio file: {audioFile}");
            Console.WriteLine($"🌍 Language: {LANGUAGE} (French)");
            Console.WriteLine($"🤖 Model: {MODEL_NAME}\n");

            try
            {
                // Check if model exists, if not provide download instructions
                if (!File.Exists(MODEL_NAME))
                {
                    Console.WriteLine($"❌ Model not found: {MODEL_NAME}");
                    ShowDownloadInstructions();
                    return;
                }
                if (!IsWav16kHz(audioFile))
                {
                    audioFile = ConvertToWav16kHz(audioFile);
                }

                await TranscribeFile(audioFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.WriteLine($"Details: {ex}");
            }

            Console.WriteLine("\n✅ Done. Press any key to exit...");
            Console.ReadKey();
        }

        static void ShowDownloadInstructions()
        {
            Console.WriteLine("\n📥 Please download a Whisper model:");
            Console.WriteLine("\nOption 1 - Download via PowerShell:");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("# For base model (recommended):");
            Console.WriteLine("Invoke-WebRequest -Uri \"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin\" -OutFile \"ggml-base.bin\"");
            Console.WriteLine("\n# For tiny model (faster, less accurate):");
            Console.WriteLine("Invoke-WebRequest -Uri \"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin\" -OutFile \"ggml-tiny.bin\"");
            Console.WriteLine("\n# For small model (better accuracy):");
            Console.WriteLine("Invoke-WebRequest -Uri \"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin\" -OutFile \"ggml-small.bin\"");
            
            Console.WriteLine("\n\nOption 2 - Manual Download:");
            Console.WriteLine("---------------------------");
            Console.WriteLine("Visit: https://huggingface.co/ggerganov/whisper.cpp/tree/main");
            Console.WriteLine("Download the model file and place it in the same folder as this program.");
            
            Console.WriteLine("\n\nModel Comparison:");
            Console.WriteLine("----------------");
            Console.WriteLine("  tiny   (~75MB)   - Fastest, least accurate");
            Console.WriteLine("  base   (~140MB)  - Good balance (RECOMMENDED)");
            Console.WriteLine("  small  (~460MB)  - Better accuracy");
            Console.WriteLine("  medium (~1.5GB)  - High accuracy");
            Console.WriteLine("  large  (~2.9GB)  - Best accuracy");
        }

        static async Task TranscribeFile(string audioFile)
        {
            Console.WriteLine("🔄 Loading Whisper model...");
            
            // Initialize Whisper factory
            using var whisperFactory = WhisperFactory.FromPath(MODEL_NAME);
            
            Console.WriteLine("✅ Model loaded successfully!");
            Console.WriteLine("🎤 Starting transcription...\n");

            // Create processor with configuration
            using var processor = whisperFactory.CreateBuilder()
                .WithLanguage(LANGUAGE)
                .WithPrompt("Transcription en français. Ponctuation automatique.")
                .Build();

            var fullTranscript = "";
            var segmentCount = 0;

            // Open and process audio file as stream
            using var fileStream = File.OpenRead(audioFile);
            
            await foreach (var segment in processor.ProcessAsync(fileStream))
            {
                segmentCount++;
                var startTime = FormatTime(segment.Start);
                var endTime = FormatTime(segment.End);
                
                Console.WriteLine($"[{startTime} -> {endTime}] {segment.Text}");
                
                fullTranscript += segment.Text.Trim() + " ";
                Console.WriteLine();
            }

            // Display final results
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("📝 FULL TRANSCRIPT");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine(fullTranscript.Trim());
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"Total segments: {segmentCount}");
        }
        static string ConvertToWav16kHz(string inputFile)
        {
            string outputFile = Path.GetTempFileName().Replace(".tmp", ".wav");

            Console.WriteLine($"🔄 Conversion en cours...");

            try
            {
                using var reader = new AudioFileReader(inputFile);
                
                var outFormat = new WaveFormat(16000, 1); // 16kHz mono
                using var resampler = new MediaFoundationResampler(reader, outFormat)
                {
                    ResamplerQuality = 60
                };

                // Utiliser WaveFileWriter au lieu de CreateWaveFile16
                using (var writer = new WaveFileWriter(outputFile, resampler.WaveFormat))
                {
                    var buffer = new byte[resampler.WaveFormat.AverageBytesPerSecond * 4];
                    int bytesRead;
                    while ((bytesRead = resampler.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }
                }

                var inputSize = new FileInfo(inputFile).Length / 1024.0 / 1024.0;
                var outputSize = new FileInfo(outputFile).Length / 1024.0 / 1024.0;

                Console.WriteLine($"✅ Conversion réussie!");
                Console.WriteLine($"   Taille: {inputSize:F2}MB → {outputSize:F2}MB\n");

                return outputFile;
            }
            catch (Exception ex)
            {
                if (File.Exists(outputFile))
                    File.Delete(outputFile);
                
                throw new Exception($"Échec de conversion: {ex.Message}");
            }
        }
        static bool IsWav16kHz(string file)
        {
            try
            {
                using var reader = new WaveFileReader(file);
                return reader.WaveFormat.SampleRate == 16000 && 
                    reader.WaveFormat.Channels == 1;
            }
            catch
            {
                return false; // Pas un WAV ou erreur
            }
        }
        static string FormatTime(TimeSpan time)
        {
            return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds:000}";
        }
    }
}