using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Ollama;
using Ollama.Core;
using Ollama.Core.Models; // NuGet: Ollama.NET (1.0.6)

namespace TTSCoqui
{
    public class Program
    {
        const string TTS_ENDPOINT = "http://127.0.0.1:5005";
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Built-in minimal OpenAPI for .NET 9 templates
            builder.Services.AddOpenApi();

            // ================== CORS POLICY ==================
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy
                        .AllowAnyOrigin()      // Allow all origins (*)
                        .AllowAnyMethod()      // Allow all HTTP methods
                        .AllowAnyHeader();     // Allow all headers
                });
            });
            


            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }
            // Apply CORS before authorization
            app.UseCors("AllowAll");
            
            app.MapGet("/python-tts/health", async () =>
            {
                var http = new HttpClient { BaseAddress = new Uri(TTS_ENDPOINT) };
                var resp = await http.GetAsync("/health");
                if (!resp.IsSuccessStatusCode)
                    return Results.Problem($"Python TTS health error: {(int)resp.StatusCode} {resp.ReasonPhrase}");
                var json = await resp.Content.ReadAsStringAsync();
                return Results.Text(json, "application/json");
            })
            .WithName("PythonTtsHealth");

            // ========== WAV PASS-THROUGH (recommended) ==========
            // Returns the WAV bytes directly (audio/wav)
            app.MapPost("/python-tts/wav", async (TtsProxyRequest dto) =>
            {
                if (string.IsNullOrWhiteSpace(dto.Text))
                    return Results.BadRequest("`text` is required.");

                var http = new HttpClient { BaseAddress = new Uri(TTS_ENDPOINT) };

                var reqBody = new
                {
                    text = dto.Text,
                    speaker_wav = dto.SpeakerWav,
                    speaker = dto.Speaker,
                    speed = dto.Speed
                };

                var resp = await http.PostAsJsonAsync("/tts/wav", reqBody);
                if (!resp.IsSuccessStatusCode)
                    return Results.Problem($"Python TTS error: {(int)resp.StatusCode} {resp.ReasonPhrase}");

                var wav = await resp.Content.ReadAsByteArrayAsync();
                // Return as file (inline). You can add a filename if you want a download:
                // return Results.File(wav, "audio/wav", "speech.wav");
                return Results.File(wav, "audio/wav");
            })
            .WithName("PythonTtsWav");

            // ========== JSON (Base64) RESPONSE (optional) ==========
            // If you prefer a JSON payload with base64 audio for easy transport:
            app.MapPost("/python-tts/base64", async (TtsProxyRequest dto) =>
            {
                if (string.IsNullOrWhiteSpace(dto.Text))
                    return Results.BadRequest("`text` is required.");

                var http = new HttpClient { BaseAddress = new Uri(TTS_ENDPOINT) };

                var reqBody = new
                {
                    text = dto.Text,
                    speaker_wav = dto.SpeakerWav,
                    speaker = dto.Speaker,
                    speed = dto.Speed
                };

                var resp = await http.PostAsJsonAsync("/tts/wav", reqBody);
                if (!resp.IsSuccessStatusCode)
                    return Results.Problem($"Python TTS error: {(int)resp.StatusCode} {resp.ReasonPhrase}");

                var wav = await resp.Content.ReadAsByteArrayAsync();
                var b64 = Convert.ToBase64String(wav);
                return Results.Ok(new TtsProxyResponse("audio/wav", b64));
            })
            .WithName("PythonTtsBase64");

            app.Run();
        }
    }

    // ========== DTOs ==========
    public record PythonTtsRequest(
        string Text,
        string? SpeakerWav = null,
        string? Speaker = null,
        float? Speed = null
    );

    public record TtsProxyRequest( // what your frontend posts to .NET
        string Text,
        float? Speed,
        string? Speaker,          // rarely used with CSS10
        string? SpeakerWav        // local path accessible by the Python server
    );

    public record TtsProxyResponse( // if you want JSON (base64)
        string MimeType,
        string AudioBase64
    );

}