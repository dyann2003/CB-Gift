"use client";

import React, { useState } from "react";

function ChibiGeneration() {
  const [promptText, setPromptText] = useState(""); // cho mode text
  const [selectedFile, setSelectedFile] = useState(null);
  const [mode, setMode] = useState("text");
  const [imageUrl, setImageUrl] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  // Gọi API tạo ảnh từ text
  const generateFromText = async (prompt) => {
    const res = await fetch("https://localhost:7015/api/HuggingFace/generate", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ prompt }),
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}: ${await res.text()}`);
    return res.json();
  };

  // Gọi API tạo ảnh từ image (prompt mặc định ở backend)
  const generateFromImage = async (file) => {
    const formData = new FormData();
    formData.append("ImageFile", file);

    const res = await fetch("https://localhost:7015/api/AiStudio/generate-chibi", {
      method: "POST",
      body: formData,
    });

    if (!res.ok) throw new Error(`HTTP ${res.status}: ${await res.text()}`);
    return res.json();
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setIsLoading(true);
    setError("");
    setImageUrl("");

    try {
      const data =
        mode === "text"
          ? await generateFromText(promptText)
          : await generateFromImage(selectedFile);

      setImageUrl(data?.image || data?.imageUrl || "");
    } catch (err) {
      setError(err.message || "Có lỗi xảy ra khi tạo ảnh.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 flex flex-col items-center justify-center p-6 font-sans">
      <div className="bg-white shadow-xl rounded-2xl p-8 w-full max-w-xl">
        <h2 className="text-3xl font-bold text-center text-indigo-600 mb-6">
          🎨 Chibi Generator
        </h2>

        {/* Mode switch */}
        <div className="flex justify-center gap-2 mb-6 p-1 bg-gray-200 rounded-lg">
          <button
            onClick={() => { setMode("text"); setError(""); setImageUrl(""); }}
            className={`w-1/2 px-4 py-2 rounded-md font-semibold transition-all ${
              mode === "text" ? "bg-white text-indigo-600 shadow" : "bg-transparent text-gray-600 hover:bg-gray-300"
            }`}
          >
            Văn Bản
          </button>
          <button
            onClick={() => { setMode("image"); setError(""); setImageUrl(""); }}
            className={`w-1/2 px-4 py-2 rounded-md font-semibold transition-all ${
              mode === "image" ? "bg-white text-indigo-600 shadow" : "bg-transparent text-gray-600 hover:bg-gray-300"
            }`}
          >
            Hình Ảnh
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          {mode === "text" ? (
            <div>
              <label className="block font-semibold text-gray-700">Mô tả Chibi:</label>
              <textarea
                value={promptText}
                onChange={(e) => setPromptText(e.target.value)}
                placeholder="Một cô gái anime tóc tím, phong cách chibi, mắt to, cầm một cây kem..."
                required
                rows={3}
                className="border w-full p-3 rounded-md mt-2 focus:ring-2 focus:ring-indigo-500 focus:outline-none transition"
              />
            </div>
          ) : (
            <div className="space-y-4">
              <div>
                <label className="block font-semibold text-gray-700">Tải ảnh gốc:</label>
                <input
                  type="file"
                  accept="image/png, image/jpeg"
                  onChange={(e) => setSelectedFile(e.target.files[0])}
                  required
                  className="mt-2 text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-indigo-50 file:text-indigo-600 hover:file:bg-indigo-100"
                />
              </div>
              <p className="text-gray-500 text-sm">
                (Prompt sẽ được tự động thêm để tạo line art/Chibi, bạn không cần nhập)
              </p>
            </div>
          )}

          <button
            type="submit"
            disabled={isLoading}
            className="w-full bg-indigo-600 text-white py-3 rounded-lg font-bold text-lg hover:bg-indigo-700 transition transform hover:scale-105 active:scale-100 disabled:bg-gray-400 disabled:cursor-not-allowed"
          >
            {isLoading ? "Đang tạo..." : "✨ Tạo ảnh"}
          </button>
        </form>

        <div className="mt-6 min-h-[350px] w-full flex flex-col items-center justify-center bg-gray-50 rounded-lg p-4 border border-dashed">
          {isLoading && (
            <div className="flex flex-col items-center text-indigo-600">
              <svg 
                className="animate-spin h-10 w-10 text-indigo-500" 
                xmlns="http://www.w3.org/2000/svg" 
                fill="none" 
                viewBox="0 0 24 24"
              >
                <circle 
                  className="opacity-25" 
                  cx="12" 
                  cy="12" 
                  r="10" 
                  stroke="currentColor" 
                  strokeWidth="4"
                ></circle>
                <path 
                  className="opacity-75" 
                  fill="currentColor" 
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                ></path>
              </svg>
              <p className="mt-3 font-medium text-gray-600">Đang tạo kiệt tác chibi...</p>
            </div>
          )}

          {error && !isLoading && (
            <div className="text-center text-red-600">
              <span className="text-3xl">⚠️</span>
              <p className="font-bold mt-2">Đã xảy ra lỗi</p>
              <p className="text-sm text-gray-500 mt-1 max-w-md">{error}</p>
            </div>
          )}

          {imageUrl && !isLoading && (
            <div className="flex flex-col items-center w-full">
              <h3 className="font-semibold text-lg text-gray-800 mb-3">Kết quả của bạn:</h3>
              <img
                src={imageUrl}
                alt="Generated Chibi"
                className="w-full max-w-sm h-auto object-contain rounded-lg shadow-md border"
              />
              <a
                href={imageUrl}
                download="chibi_image.png"
                className="mt-5 px-6 py-2 bg-green-600 text-white font-semibold rounded-lg hover:bg-green-700 transition transform hover:scale-105"
              >
                Tải ảnh về
              </a>
            </div>
          )}

          {!isLoading && !error && !imageUrl && (
            <p className="text-gray-500 text-center">Nhập mô tả hoặc tải ảnh lên để bắt đầu!</p>
          )}
        </div>
      </div>
    </div>
  );
}

export default ChibiGeneration;
