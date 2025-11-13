import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";

import VoiceRecorder_D2T2 from "./day2/tp2/VoiceRecorder";
import ChaabiLilIskaneAgent from "./day2/tp4/ChaabiLilIskaneAgent";

export default function AppRoutes() {
  return (
    <BrowserRouter>
      <Routes>
        <Route
          path="/day2/tp2"
          element={
            <VoiceRecorder_D2T2 api_endpoint="http://localhost:5000/api/transcription" />
          }
        />
        <Route
          path="/day2/tp4"
          element={
            <ChaabiLilIskaneAgent api_endpoint="http://localhost:5000/api/transcription" />
          }
        />
      </Routes>
    </BrowserRouter>
  );
}
