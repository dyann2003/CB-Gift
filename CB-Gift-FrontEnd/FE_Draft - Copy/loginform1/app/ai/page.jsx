"use client"

import { useState, useRef } from "react"
import { Upload, Download, Sparkles, AlertCircle } from "lucide-react"

export default function Home() {
  const [selectedFile, setSelectedFile] = useState(null)
  const [previewUrl, setPreviewUrl] = useState("")
  const [generatedImageUrl, setGeneratedImageUrl] = useState("")
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState("")
  const [promptText, setPromptText] = useState("")
  const fileInputRef = useRef(null)

  const handleFileSelect = (file) => {
    if (!file.type.startsWith("image/")) {
      setError("Vui lòng chọn một tệp hình ảnh hợp lệ.")
      return
    }

    setSelectedFile(file)
    setError("")
    setGeneratedImageUrl("")

    const reader = new FileReader()
    reader.onload = (e) => {
      setPreviewUrl(e.target.result)
    }
    reader.readAsDataURL(file)
  }

  const handleDragOver = (e) => {
    e.preventDefault()
    e.stopPropagation()
  }

  const handleDrop = (e) => {
    e.preventDefault()
    e.stopPropagation()

    const files = e.dataTransfer.files
    if (files.length > 0) {
      handleFileSelect(files[0])
    }
  }

  const handleFileInputChange = (e) => {
    if (e.target.files && e.target.files.length > 0) {
      handleFileSelect(e.target.files[0])
    }
  }

  const handleGenerateImage = async (e) => {
    e.preventDefault()

    if (!selectedFile) {
      setError("Vui lòng chọn một hình ảnh.")
      return
    }

    setIsLoading(true)
    setError("")

    try {
      const formData = new FormData()
      formData.append("ImageFile", selectedFile)
      formData.append("Prompt", promptText)

      const response = await fetch("https://localhost:7015/api/AiStudio/generate", {
        method: "POST",
        body: formData,
      })

      // 🔒 Xử lý các lỗi Authentication đặc biệt
      if (response.status === 401 || response.status === 403) {
        throw new Error(
          "Hệ thống AI hiện không thể xử lý yêu cầu. Có thể API key đã hết hạn hoặc vượt giới hạn. Vui lòng thử lại sau hoặc liên hệ quản trị viên."
        )
      }

      if (!response.ok) {
        // Nếu Backend trả về lỗi có message
        const errData = await response.json().catch(() => null)
        if (errData?.message) throw new Error(errData.message)
        throw new Error(`Lỗi API: ${response.status}`)
      }

      const data = await response.json()
      const imageUrl = data?.image || data?.imageUrl

      if (!imageUrl) {
        throw new Error("Không nhận được kết quả từ server.")
      }

      setGeneratedImageUrl(imageUrl)
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Đã xảy ra lỗi không xác định khi tạo hình ảnh Chibi."
      )
      setGeneratedImageUrl("")
    } finally {
      setIsLoading(false)
    }
  }

  const handleDownload = async () => {
    if (!generatedImageUrl) return

    try {
      const response = await fetch(generatedImageUrl)
      const blob = await response.blob()
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement("a")
      a.href = url
      a.download = "chibi-image.png"
      document.body.appendChild(a)
      a.click()
      window.URL.revokeObjectURL(url)
      document.body.removeChild(a)
    } catch {
      setError("Không thể tải xuống hình ảnh.")
    }
  }

  return (
    <main className="min-h-screen bg-gradient-to-br from-slate-50 via-white to-blue-50">
      <div className="w-full py-8 px-4 sm:px-6 lg:px-8">
        <div className="max-w-7xl mx-auto">
          {/* Header */}
          <div className="text-center mb-12">
            <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-blue-100 mb-4">
              <Sparkles className="w-8 h-8 text-blue-600" />
            </div>
            <h1 className="text-4xl sm:text-5xl font-bold text-slate-900 mb-3">
              Chibi Generator
            </h1>
            <p className="text-lg text-slate-600 max-w-2xl mx-auto">
              Biến ảnh của bạn thành nhân vật chibi xinh xắn với AI. Tải lên, thêm mô tả, và nhận kết quả trong vài giây.
            </p>
          </div>

          {/* Main Content */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 items-start">
            {/* Upload Section */}
            <div className="space-y-6">
              <form onSubmit={handleGenerateImage} className="space-y-6">
                <div className="space-y-2">
                  <label className="block text-sm font-semibold text-slate-900">
                    Tải lên hình ảnh của bạn
                  </label>
                  <div
                    onDragOver={handleDragOver}
                    onDrop={handleDrop}
                    onClick={() => fileInputRef.current?.click()}
                    className="relative border-2 border-dashed border-blue-200 rounded-lg p-8 text-center cursor-pointer transition-all hover:border-blue-400 hover:bg-blue-50/50"
                  >
                    <input
                      ref={fileInputRef}
                      type="file"
                      accept="image/*"
                      onChange={handleFileInputChange}
                      className="hidden"
                    />
                    <div className="flex flex-col items-center justify-center">
                      <Upload className="w-10 h-10 text-blue-500 mb-3" />
                      <p className="text-sm font-medium text-slate-900 mb-1">
                        Kéo thả hình ảnh hoặc nhấp để chọn
                      </p>
                      <p className="text-xs text-slate-500">
                        Hỗ trợ JPG, PNG, WEBP, GIF, BMP, TIFF (tối đa 50MB)
                      </p>
                    </div>
                  </div>
                </div>

                {/* Prompt Input */}
                <div className="space-y-2">
                  <label className="block text-sm font-semibold text-slate-900">
                    Mô tả (Tùy chọn)
                  </label>
                  <textarea
                    value={promptText}
                    onChange={(e) => setPromptText(e.target.value)}
                    placeholder="Ví dụ: Một cô gái tóc tím với đôi mắt to, phong cách chibi..."
                    rows={4}
                    className="w-full px-4 py-3 border border-slate-200 rounded-lg text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition resize-none"
                  />
                  <p className="text-xs text-slate-500">
                    Thêm mô tả chi tiết để cải thiện kết quả của bạn.
                  </p>
                </div>

                {/* Error Message */}
                {error && (
                  <div className="p-4 bg-red-50 border border-red-200 rounded-lg flex gap-3">
                    <AlertCircle className="w-5 h-5 text-red-600 flex-shrink-0 mt-0.5" />
                    <p className="text-sm text-red-700">{error}</p>
                  </div>
                )}

                {/* Generate Button */}
                <button
                  type="submit"
                  disabled={!selectedFile || isLoading}
                  className="w-full bg-blue-600 hover:bg-blue-700 disabled:bg-slate-300 disabled:cursor-not-allowed text-white font-semibold py-3 rounded-lg transition-all duration-200 transform hover:scale-[1.02] active:scale-100 flex items-center justify-center gap-2"
                >
                  {isLoading ? (
                    <>
                      <div className="animate-spin">
                        <Sparkles className="w-5 h-5" />
                      </div>
                      <span>Đang tạo...</span>
                    </>
                  ) : (
                    <>
                      <Sparkles className="w-5 h-5" />
                      <span>Tạo Chibi</span>
                    </>
                  )}
                </button>
              </form>

              {/* Preview Section */}
              {previewUrl && (
                <div className="space-y-2">
                  <h3 className="text-sm font-semibold text-slate-900">Hình ảnh gốc</h3>
                  <div className="relative w-full aspect-square rounded-lg overflow-hidden border border-slate-200 bg-slate-50">
                    <img src={previewUrl} alt="Preview" className="w-full h-full object-cover" />
                  </div>
                </div>
              )}
            </div>

            {/* Result Section */}
            <div className="space-y-6">
              <div className="space-y-2">
                <h3 className="text-sm font-semibold text-slate-900">Kết quả Chibi</h3>

                {isLoading ? (
                  <div className="relative w-full aspect-square rounded-lg overflow-hidden border-2 border-blue-200 bg-gradient-to-br from-blue-50 to-slate-50 flex items-center justify-center">
                    <div className="flex flex-col items-center gap-4">
                      <div className="relative w-16 h-16">
                        <div className="absolute inset-0 bg-gradient-to-r from-blue-400 to-blue-600 rounded-full animate-spin" />
                        <div className="absolute inset-1 bg-gradient-to-br from-blue-50 to-slate-50 rounded-full" />
                        <Sparkles className="absolute inset-0 m-auto w-6 h-6 text-blue-600" />
                      </div>
                      <p className="text-sm font-medium text-slate-600">Đang tạo chibi xinh của bạn...</p>
                    </div>
                  </div>
                ) : generatedImageUrl ? (
                  <div className="space-y-4">
                    <div className="relative w-full aspect-square rounded-lg overflow-hidden border-2 border-blue-200 bg-white shadow-lg">
                      <img src={generatedImageUrl} alt="Generated Chibi" className="w-full h-full object-cover" />
                    </div>
                    <button
                      onClick={handleDownload}
                      className="w-full bg-green-600 hover:bg-green-700 text-white font-semibold py-3 rounded-lg transition-all duration-200 transform hover:scale-[1.02] active:scale-100 flex items-center justify-center gap-2"
                    >
                      <Download className="w-5 h-5" />
                      <span>Tải xuống hình ảnh</span>
                    </button>
                  </div>
                ) : (
                  <div className="relative w-full aspect-square rounded-lg overflow-hidden border-2 border-dashed border-slate-300 bg-slate-50/50 flex items-center justify-center">
                    <div className="text-center">
                      <Sparkles className="w-12 h-12 text-slate-300 mx-auto mb-2" />
                      <p className="text-sm font-medium text-slate-600">Kết quả sẽ xuất hiện ở đây</p>
                      <p className="text-xs text-slate-500 mt-1">Tải lên hình ảnh và nhấp “Tạo Chibi”</p>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>
  )
}
