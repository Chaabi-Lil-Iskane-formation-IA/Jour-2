import React, { useState, useRef, useEffect } from "react";
import axios from "axios";

const ChaabiLilIskaneAgent = ({ api_endpoint = "http://localhost:5000/api/transcription" }) => {
  const [isRecording, setIsRecording] = useState(false);
  const [isTranscribing, setIsTranscribing] = useState(false);
  const [error, setError] = useState(null);
  const [chatHistory, setChatHistory] = useState([]);
  const [currentPlayingIndex, setCurrentPlayingIndex] = useState(null);
  
  const mediaRecorderRef = useRef(null);
  const audioChunks = useRef([]);
  const chatEndRef = useRef(null);
  const audioRefs = useRef({});

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [chatHistory]);

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
        uploadAudio(audioBlob);
      };

      mediaRecorder.start();
      setIsRecording(true);
      setError(null);
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
    formData.append("history", JSON.stringify(chatHistory));

    setIsTranscribing(true);
    setError(null);

    try {
      const response = await axios.post(api_endpoint, formData, {
        headers: { "Content-Type": "multipart/form-data" },
      });
      
      console.log("Response:", response.data);
      
      if (response.data.success) {
        // Convert base64 audio to blob URL
        let assistantAudioURL = null;
        if (response.data.audio && response.data.audio.audioBase64) {
          const audioBase64 = response.data.audio.audioBase64;
          const mimeType = response.data.audio.mimeType || "audio/wav";
          
          const binaryString = atob(audioBase64);
          const bytes = new Uint8Array(binaryString.length);
          for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
          }
          const audioBlob = new Blob([bytes], { type: mimeType });
          assistantAudioURL = URL.createObjectURL(audioBlob);
        }

        // Update chat history with new messages
        const newMessages = [
          {
            role: "user",
            content: response.data.transcript,
            timestamp: new Date().toISOString(),
            audioURL: URL.createObjectURL(audioBlob)
          },
          {
            role: "assistant",
            content: response.data.reply,
            timestamp: new Date().toISOString(),
            audioURL: assistantAudioURL
          }
        ];

        setChatHistory(prev => [...prev, ...newMessages]);

        // Auto-play assistant response
        if (assistantAudioURL) {
          setTimeout(() => {
            const lastIndex = chatHistory.length + 1;
            setCurrentPlayingIndex(lastIndex);
            audioRefs.current[lastIndex]?.play();
          }, 300);
        }
      }
    } catch (error) {
      console.error("Transcription failed:", error);
      setError(error.response?.data?.error || "Erreur de transcription");
    } finally {
      setIsTranscribing(false);
    }
  };

  const clearConversation = () => {
    // Clean up audio URLs
    chatHistory.forEach(msg => {
      if (msg.audioURL) {
        URL.revokeObjectURL(msg.audioURL);
      }
    });
    setChatHistory([]);
    setCurrentPlayingIndex(null);
  };

  const playAudio = (index) => {
    // Stop currently playing audio
    if (currentPlayingIndex !== null && audioRefs.current[currentPlayingIndex]) {
      audioRefs.current[currentPlayingIndex].pause();
      audioRefs.current[currentPlayingIndex].currentTime = 0;
    }

    // Play new audio
    setCurrentPlayingIndex(index);
    audioRefs.current[index]?.play();
  };

  const handleAudioEnded = () => {
    setCurrentPlayingIndex(null);
  };

  return (
    <div style={{
      display: "flex",
      flexDirection: "column",
      height: "80vh",
      width: "450px",
      margin: "0 auto",
      background: "#fff",
      position: "absolute",
        bottom: 0,
        right: 0,
      fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif"
    }}>
      {/* Header */}
      <div style={{
        padding: "1rem 1rem",
        borderBottom: "1px solid #e5e7eb",
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        background: "#fff",
        position: "sticky",
        top: 0,
        zIndex: 10
      }}>
        <h1 style={{
          fontSize: "1.25rem",
          fontWeight: "600",
          margin: 0,
          color: "#55ab26"
        }}>
          Assistant Vocal Chaabi Lil Iskane
        </h1>
        {chatHistory.length > 0 && (
          <button
            onClick={clearConversation}
            disabled={isTranscribing || isRecording}
            style={{
              padding: "0.5rem 1rem",
              borderRadius: "0.5rem",
              fontSize: "0.875rem",
              fontWeight: "500",
              color: "#6b7280",
              border: "1px solid #d1d5db",
              background: "#fff",
              cursor: (isTranscribing || isRecording) ? "not-allowed" : "pointer",
              opacity: (isTranscribing || isRecording) ? 0.5 : 1,
            }}
          >
            Nouvelle conversation
          </button>
        )}
      </div>

      {/* Chat Messages */}
      <div style={{
        flex: 1,
        overflowY: "auto",
        padding: "1rem",
        background: "#f9fafb"
      }}>
        {chatHistory.length === 0 && !isTranscribing && (
          <div style={{
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
            height: "100%",
            color: "#6b7280",
            textAlign: "center",
            gap: "1rem"
          }}>
            <div style={{ fontSize: "3rem" }}>
                <img width="130px" src="https://cdn3d.iconscout.com/3d/premium/thumb/robot-character-wearing-a-counseling-headset-3d-icon-png-download-11431383.png" alt="" />
            </div>
            <p style={{ fontSize: "1.125rem", fontWeight: "500", margin: 0 }}>
              Commencez une conversation vocale
            </p>
            <p style={{ fontSize: "0.875rem", margin: 0 }}>
              Appuyez sur le bouton microphone pour commencer
            </p>
          </div>
        )}

        {chatHistory.map((message, index) => (
          <div
            key={index}
            style={{
              display: "flex",
              gap: "1rem",
              marginBottom: "1.5rem",
              flexDirection: message.role === "user" ? "row-reverse" : "row"
            }}
          >
            {/* Avatar */}
            <div style={{
              width: "36px",
              height: "36px",
              borderRadius: "50%",
              background: message.role === "user" ? "#3b82f6" : "#10b981",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              color: "#fff",
              fontSize: "1.25rem",
              flexShrink: 0
            }}>
              {message.role === "user" ? "👤" : "🤖"}
            </div>

            {/* Message Content */}
            <div style={{
              flex: 1,
              maxWidth: "75%"
            }}>
              <div style={{
                background: message.role === "user" ? "#005693" : "#55ab26",
                padding: "1rem",
                borderRadius: "1rem",
                border: "1px solid " + (message.role === "user" ? "#dbeafe" : "#e5e7eb"),
                boxShadow: "0 1px 2px rgba(0,0,0,0.05)"
              }}>
                <p style={{
                  margin: 0,
                  lineHeight: "1.6",
                  color: "#fff",
                  whiteSpace: "pre-wrap",
                  fontSize: "0.9375rem"
                }}>
                  {message.content}
                </p>
              </div>

              {/* Audio Player */}
              {message.audioURL && (
                <div style={{
                  marginTop: "0.75rem",
                  display: "flex",
                  alignItems: "center",
                  gap: "0.75rem"
                }}>
                  <button
                    onClick={() => playAudio(index)}
                    style={{
                      width: "32px",
                      height: "32px",
                      borderRadius: "50%",
                      border: "none",
                      background: currentPlayingIndex === index ? "#3b82f6" : "#f3f4f6",
                      color: currentPlayingIndex === index ? "#fff" : "#6b7280",
                      cursor: "pointer",
                      display: "flex",
                      alignItems: "center",
                      justifyContent: "center",
                      fontSize: "0.875rem",
                      transition: "all 0.2s"
                    }}
                  >
                    {currentPlayingIndex === index ? "⏸" : "▶"}
                  </button>
                  <audio
                    ref={el => audioRefs.current[index] = el}
                    src={message.audioURL}
                    onEnded={handleAudioEnded}
                    onPause={() => {
                      if (currentPlayingIndex === index) {
                        setCurrentPlayingIndex(null);
                      }
                    }}
                    style={{ display: "none" }}
                  />
                  <div style={{
                    height: "2px",
                    flex: 1,
                    background: "#e5e7eb",
                    borderRadius: "1px",
                    position: "relative",
                    overflow: "hidden"
                  }}>
                    {currentPlayingIndex === index && (
                      <div style={{
                        position: "absolute",
                        left: 0,
                        top: 0,
                        height: "100%",
                        width: "30%",
                        background: "#3b82f6",
                        animation: "audioProgress 2s ease-in-out infinite"
                      }} />
                    )}
                  </div>
                  <span style={{
                    fontSize: "0.75rem",
                    color: "#9ca3af"
                  }}>
                    {currentPlayingIndex === index ? "En lecture..." : "Audio"}
                  </span>
                </div>
              )}

              {/* Timestamp */}
              <div style={{
                marginTop: "0.5rem",
                fontSize: "0.75rem",
                color: "#9ca3af",
                textAlign: message.role === "user" ? "right" : "left"
              }}>
                {new Date(message.timestamp).toLocaleTimeString('fr-FR', {
                  hour: '2-digit',
                  minute: '2-digit'
                })}
              </div>
            </div>
          </div>
        ))}

        {/* Transcribing Indicator */}
        {isTranscribing && (
          <div style={{
            display: "flex",
            gap: "1rem",
            marginBottom: "1.5rem"
          }}>
            <div style={{
              width: "36px",
              height: "36px",
              borderRadius: "50%",
              background: "#10b981",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              color: "#fff",
              fontSize: "1.25rem",
              flexShrink: 0
            }}>
              🤖
            </div>
            <div style={{
              background: "#fff",
              padding: "1rem",
              borderRadius: "1rem",
              border: "1px solid #e5e7eb",
              display: "flex",
              alignItems: "center",
              gap: "0.5rem"
            }}>
              <div style={{
                width: "8px",
                height: "8px",
                borderRadius: "50%",
                background: "#9ca3af",
                animation: "bounce 1.4s infinite ease-in-out both"
              }} />
              <div style={{
                width: "8px",
                height: "8px",
                borderRadius: "50%",
                background: "#9ca3af",
                animation: "bounce 1.4s infinite ease-in-out both 0.2s"
              }} />
              <div style={{
                width: "8px",
                height: "8px",
                borderRadius: "50%",
                background: "#9ca3af",
                animation: "bounce 1.4s infinite ease-in-out both 0.4s"
              }} />
            </div>
          </div>
        )}

        <div ref={chatEndRef} />
      </div>

      {/* Error Message */}
      {error && (
        <div style={{
          padding: "1rem 1.5rem",
          background: "#fef2f2",
          borderTop: "1px solid #fecaca",
          color: "#991b1b",
          fontSize: "0.875rem",
          display: "flex",
          alignItems: "center",
          gap: "0.5rem"
        }}>
          <span>⚠️</span>
          {error}
        </div>
      )}

      {/* Recording Controls */}
      <div style={{
        padding: "1rem",
        borderTop: "1px solid #e5e7eb",
        background: "#fff",
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        gap: "1rem"
      }}>
        {isRecording && (
          <div style={{
            display: "flex",
            alignItems: "center",
            gap: "0.5rem",
            color: "#ef4444",
            fontSize: "0.875rem",
            fontWeight: "500"
          }}>
            <div style={{
              width: "8px",
              height: "8px",
              borderRadius: "50%",
              background: "#ef4444",
              animation: "pulse 1.5s ease-in-out infinite"
            }} />
            Enregistrement...
          </div>
        )}

        <button
          onClick={isRecording ? stopRecording : startRecording}
          disabled={isTranscribing}
          style={{
            width: "64px",
            height: "64px",
            borderRadius: "50%",
            border: "none",
            background: isRecording ? "#ef4444" : "#55ab26",
            color: "#fff",
            fontSize: "1.75rem",
            cursor: isTranscribing ? "not-allowed" : "pointer",
            opacity: isTranscribing ? 0.5 : 1,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            boxShadow: "0 4px 6px rgba(0,0,0,0.1)",
            transition: "all 0.2s",
            transform: isRecording ? "scale(1.1)" : "scale(1)"
          }}
          onMouseOver={(e) => {
            if (!isTranscribing) {
              e.currentTarget.style.transform = "scale(1.05)";
            }
          }}
          onMouseOut={(e) => {
            e.currentTarget.style.transform = isRecording ? "scale(1.1)" : "scale(1)";
          }}
        >
          {
          isRecording ? "⏹" : <svg width="45px" fill="#ffffff" version="1.1" id="Layer_1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 300 300" xml:space="preserve"><g id="SVGRepo_bgCarrier" stroke-width="0"></g><g id="SVGRepo_tracerCarrier" stroke-linecap="round" stroke-linejoin="round"></g><g id="SVGRepo_iconCarrier"> <g> <g> <g> <path d="M150,222.581c32.018,0,58.065-26.047,58.065-58.065V58.065C208.065,26.047,182.018,0,150,0S91.935,26.047,91.935,58.065 v106.452C91.935,196.534,117.982,222.581,150,222.581z M101.615,96.775c2.671,0,4.839-2.168,4.839-4.839 c0-2.671-2.168-4.839-4.839-4.839V62.903c2.671,0,4.839-2.168,4.839-4.839c0-2.584-2.042-4.655-4.597-4.79 c1.706-17.293,12.583-31.897,27.666-38.985c0.368,0.092,0.726,0.227,1.123,0.227c1.723,0,3.165-0.953,4.021-2.308 c3.373-1.132,6.89-1.926,10.544-2.289c0.135,2.555,2.206,4.597,4.79,4.597c2.584,0,4.655-2.042,4.79-4.597 c3.653,0.363,7.171,1.156,10.544,2.289c0.856,1.355,2.298,2.308,4.021,2.308c0.397,0,0.755-0.135,1.123-0.227 c15.082,7.089,25.955,21.692,27.663,38.985c-2.55,0.135-4.592,2.206-4.592,4.79c0,2.671,2.168,4.839,4.839,4.839v24.194 c-2.671,0-4.839,2.168-4.839,4.839c0,2.671,2.168,4.839,4.839,4.839v9.677h-96.774V96.775z M101.613,116.129h96.774v9.677 h-96.774V116.129z M101.613,135.484h96.774v29.032c0,26.681-21.706,48.387-48.387,48.387c-26.681,0-48.387-21.706-48.387-48.387 V135.484z"></path> <path d="M217.744,164.516h-0.002c0,37.355-30.387,67.742-67.742,67.742c-37.355,0-67.742-30.387-67.742-67.742v-62.903h-9.677 v62.903c0,35.995,24.731,66.242,58.065,74.869v50.937H72.581V300h58.065h38.71h58.065v-9.677h-58.065v-50.937 c33.334-8.627,58.065-38.874,58.065-74.869v-62.903h-9.677V164.516z M140.322,290.323v-49.06 c3.179,0.402,6.392,0.673,9.677,0.673c3.285,0,6.498-0.271,9.677-0.673v49.06H140.322z"></path> <rect x="53.226" y="101.613" width="9.677" height="33.871"></rect> <rect x="237.097" y="101.613" width="9.677" height="33.871"></rect> <circle cx="120.968" cy="91.935" r="4.839"></circle> <circle cx="140.323" cy="91.935" r="4.839"></circle> <circle cx="159.677" cy="91.935" r="4.839"></circle> <circle cx="179.032" cy="91.935" r="4.839"></circle> <circle cx="111.29" cy="72.581" r="4.839"></circle> <circle cx="130.645" cy="72.581" r="4.839"></circle> <circle cx="150" cy="72.581" r="4.839"></circle> <circle cx="169.355" cy="72.581" r="4.839"></circle> <circle cx="188.71" cy="72.581" r="4.839"></circle> <circle cx="120.968" cy="58.065" r="4.839"></circle> <circle cx="140.323" cy="58.065" r="4.839"></circle> <circle cx="159.677" cy="58.065" r="4.839"></circle> <circle cx="179.032" cy="58.065" r="4.839"></circle> <circle cx="111.29" cy="43.548" r="4.839"></circle> <circle cx="130.645" cy="43.548" r="4.839"></circle> <circle cx="150" cy="43.548" r="4.839"></circle> <circle cx="169.355" cy="43.548" r="4.839"></circle> <circle cx="188.71" cy="43.548" r="4.839"></circle> <circle cx="120.968" cy="29.032" r="4.839"></circle> <circle cx="140.323" cy="29.032" r="4.839"></circle> <circle cx="159.677" cy="29.032" r="4.839"></circle> <circle cx="179.032" cy="29.032" r="4.839"></circle> </g> </g> </g> </g></svg>
        }
        </button>
      </div>

      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.5; }
        }
        @keyframes bounce {
          0%, 80%, 100% { 
            transform: scale(0);
          } 
          40% { 
            transform: scale(1);
          }
        }
        @keyframes audioProgress {
          0% { left: 0; width: 0; }
          50% { left: 0; width: 100%; }
          100% { left: 100%; width: 0; }
        }
      `}</style>
    </div>
  );
};

export default ChaabiLilIskaneAgent;