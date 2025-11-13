import React, { useState, useRef } from "react";
import axios from "axios";

const VoiceRecorder = ({ api_endpoint = "http://localhost:5000/api/transcription" }) => {
  const [isRecording, setIsRecording] = useState(false);
  const [audioURL, setAudioURL] = useState(null);
  const [isTranscribing, setIsTranscribing] = useState(false);
  const [transcription, setTranscription] = useState(null);
  const [error, setError] = useState(null);
  
  const mediaRecorderRef = useRef(null);
  const audioChunks = useRef([]);

  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });

      const mediaRecorder = new MediaRecorder(stream);
      mediaRecorderRef.current = mediaRecorder;
      audioChunks.current = [];

      mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          audioChunks.current.push(event.data);
        }
      };

      mediaRecorder.onstop = () => {
        const audioBlob = new Blob(audioChunks.current, { type: "audio/webm" });
        const url = URL.createObjectURL(audioBlob);
        setAudioURL(url);
        uploadAudio(audioBlob);
      };

      mediaRecorder.start();
      setIsRecording(true);
      setError(null);
      setTranscription(null);
    } catch (err) {
      console.error("Microphone access denied:", err);
      setError("Impossible d'accéder au microphone");
    }
  };

  const stopRecording = () => {
    mediaRecorderRef.current?.stop();
    mediaRecorderRef.current?.stream.getTracks().forEach(track => track.stop());
    setIsRecording(false);
  };

  const uploadAudio = async (audioBlob) => {
    const formData = new FormData();
    formData.append("file", audioBlob, "recording.webm");

    setIsTranscribing(true);
    setError(null);

    try {
      const response = await axios.post(api_endpoint, formData, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      
      console.log("Transcription success:", response.data);
      setTranscription(response.data);
    } catch (error) {
      console.error("Transcription failed:", error);
      setError(error.response?.data?.error || "Erreur de transcription");
    } finally {
      setIsTranscribing(false);
    }
  };

  return (
    <div style={{ padding: "2rem", fontFamily: "Arial, sans-serif" }}>
      <h1 style={{ textAlign: "center", marginBottom: "1.5rem" }}>
        Enregistreur Vocal avec Transcription
      </h1>
      
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          gap: "1rem",
          padding: "1.5rem",
          borderRadius: "1rem",
          background: "#f9fafb",
          boxShadow: "0 2px 6px rgba(0,0,0,0.1)",
          width: "100%",
          maxWidth: "600px",
          margin: "2rem auto",
        }}
      >
        <h2 style={{ fontSize: "1.2rem", fontWeight: "600" }}>
          🎙️ Enregistreur Vocal avec Transcription
        </h2>

        <button
          onClick={isRecording ? stopRecording : startRecording}
          disabled={isTranscribing}
          style={{
            padding: "0.6rem 1.5rem",
            borderRadius: "9999px",
            fontWeight: "500",
            color: "#fff",
            border: "none",
            cursor: isTranscribing ? "not-allowed" : "pointer",
            backgroundColor: isRecording ? "#ef4444" : "#3b82f6",
            opacity: isTranscribing ? 0.5 : 1,
          }}
        >
          {isRecording ? "⏹️ Arrêter l'Enregistrement" : "🎤 Démarrer l'Enregistrement"}
        </button>

        {isRecording && (
          <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
            <div
              style={{
                width: "10px",
                height: "10px",
                borderRadius: "50%",
                background: "#ef4444",
                animation: "pulse 1.5s ease-in-out infinite",
              }}
            />
            <span style={{ color: "#ef4444", fontWeight: "500" }}>
              Enregistrement en cours...
            </span>
          </div>
        )}

        {isTranscribing && (
          <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
            <div
              style={{
                width: "20px",
                height: "20px",
                border: "3px solid #3b82f6",
                borderTopColor: "transparent",
                borderRadius: "50%",
                animation: "spin 1s linear infinite",
              }}
            />
            <span style={{ color: "#3b82f6", fontWeight: "500" }}>
              Transcription en cours...
            </span>
          </div>
        )}

        {error && (
          <div
            style={{
              padding: "0.75rem",
              borderRadius: "0.5rem",
              background: "#fee2e2",
              color: "#991b1b",
              width: "100%",
              textAlign: "center",
            }}
          >
            ❌ {error}
          </div>
        )}

        {audioURL && (
          <div style={{ width: "100%", textAlign: "center" }}>
            <audio
              controls
              src={audioURL}
              style={{ width: "100%", marginTop: "0.5rem" }}
            />
            <p style={{ fontSize: "0.85rem", color: "#6b7280", marginTop: "0.5rem" }}>
              Aperçu de votre enregistrement
            </p>
          </div>
        )}

        {transcription && transcription.success && (
          <div
            style={{
              width: "100%",
              padding: "1rem",
              borderRadius: "0.5rem",
              background: "#fff",
              border: "1px solid #e5e7eb",
            }}
          >
            <h3 style={{ fontSize: "1rem", fontWeight: "600", marginBottom: "0.5rem" }}>
              📝 Transcription
            </h3>
            <p
              style={{
                fontSize: "0.95rem",
                lineHeight: "1.5",
                color: "#374151",
                marginBottom: "1rem",
              }}
            >
              {transcription.transcript}
            </p>

            <details style={{ fontSize: "0.85rem", color: "#6b7280" }}>
              <summary style={{ cursor: "pointer", fontWeight: "500" }}>
                Segments ({transcription.segmentCount})
              </summary>
              <div style={{ marginTop: "0.5rem", maxHeight: "200px", overflowY: "auto" }}>
                {transcription.segments.map((segment, index) => (
                  <div
                    key={index}
                    style={{
                      padding: "0.5rem",
                      marginBottom: "0.25rem",
                      background: "#f9fafb",
                      borderRadius: "0.25rem",
                    }}
                  >
                    <div style={{ fontSize: "0.75rem", color: "#9ca3af" }}>
                      {formatTime(segment.start)} → {formatTime(segment.end)}
                    </div>
                    <div>{segment.text}</div>
                  </div>
                ))}
              </div>
            </details>
          </div>
        )}

        <style>{`
          @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.5; }
          }
          @keyframes spin {
            to { transform: rotate(360deg); }
          }
        `}</style>
      </div>
    </div>
  );
};

const formatTime = (seconds) => {
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  const ms = Math.floor((seconds % 1) * 1000);
  return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}.${ms.toString().padStart(3, '0')}`;
};

export default VoiceRecorder;