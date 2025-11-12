"use client";

import { useState, useEffect } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { Separator } from "@/components/ui/separator";
import { UserPlus, Save, X, AlertCircle, XCircle } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
export default function AssignAccountModal({
  isOpen,
  onClose,
  onAssignAccount,
}) {
  const [formData, setFormData] = useState({
    fullName: "",
    email: "",
    username: "",
    role: "",
    department: "",
    notes: "",
  });
import { Card, CardContent } from "@/components/ui/card";
import { Search, Plus, XCircle } from "lucide-react";

export default function AddRelationshipModal({ isOpen, onClose, onAdd }) {
  const [sellers, setSellers] = useState([]);
  const [designers, setDesigners] = useState([]);
  const [selectedSeller, setSelectedSeller] = useState("");
  const [selectedDesigner, setSelectedDesigner] = useState("");
  const [sellerSearch, setSellerSearch] = useState("");
  const [designerSearch, setDesignerSearch] = useState("");
  const [loading, setLoading] = useState(false);

  // Popup UI state
  const [showSuccess, setShowSuccess] = useState(false);
  const [showError, setShowError] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  /* ------------------- SỬA 1: Reset khi modal mở ------------------- */
  useEffect(() => {
    if (isOpen) {
      fetchSellers();
      fetchDesigners();
      setShowSuccess(false);
      setShowError(false);
      setErrorMessage("");
      setSelectedSeller("");
      setSelectedDesigner("");
      setSellerSearch("");
      setDesignerSearch("");
    }
  }, [isOpen]);

  const fetchSellers = async () => {
    try {
      const res = await fetch("https://localhost:7015/api/Auth/all-sellers", {
        credentials: "include",
      });
      if (!res.ok) throw new Error("Failed to load sellers");
      const data = await res.json();
      setSellers(data);
    } catch (err) {
      console.error("Error loading sellers:", err);
      setShowError(true);
      setErrorMessage("Unable to load sellers. Please try again later.");
    }
  };

  const fetchDesigners = async () => {
    try {
      const res = await fetch("https://localhost:7015/api/Auth/all-designers", {
        credentials: "include",
      });
      if (!res.ok) throw new Error("Failed to load designers");
      const data = await res.json();
      setDesigners(data);
    } catch (err) {
      console.error("Error loading designers:", err);
      setShowError(true);
      setErrorMessage("Unable to load designers. Please try again later.");
    }
  };

  const filteredSellers = sellers.filter((s) =>
    (s.sellerName || "").toLowerCase().includes(sellerSearch.toLowerCase())
  );

  const filteredDesigners = designers.filter((d) =>
    (d.designerName || "").toLowerCase().includes(designerSearch.toLowerCase())
  );

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!selectedSeller || !selectedDesigner) {
      setErrorMessage("Please select both a seller and a designer.");
      setShowError(true);
      return;
    }

    setLoading(true);
    setShowError(false);

    try {
      const body = {
        sellerUserId: selectedSeller,
        designerUserId: selectedDesigner,
      };

      const res = await fetch(
        "https://localhost:7015/api/manager/assignments",
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(body),
        }
      );

      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.message || "Failed to create relationship.");
      }

      /* ------------------- HIỂN THỊ SUCCESS ------------------- */
      setShowSuccess(true);
      onAdd(); // refresh danh sách

      /* ------------------- SỬA 2: Tự đóng sau 2s (không gọi handleClose ngay) ------------------- */
      setTimeout(() => {
        // Đóng modal và reset success
        setShowSuccess(false);
        onClose();
      }, 2000);
    } catch (error) {
      console.error("Create relationship error:", error);
      setErrorMessage(error.message || "Failed to create relationship.");
      setShowError(true);
    } finally {
      setLoading(false);
    }
  };

  /* ------------------- SỬA 3: handleClose chỉ reset khi không ở success ------------------- */
  const handleClose = () => {
    if (loading) return;

    // Nếu đang hiện success → để setTimeout đóng, không làm gì ở đây
    if (showSuccess) return;

    // Reset form khi đóng bình thường
    setSelectedSeller("");
    setSelectedDesigner("");
    setSellerSearch("");
    setDesignerSearch("");
    setShowError(false);
    setShowSuccess(false);
    onClose();
  };

  return (
    <Dialog open={isOpen} onOpenChange={handleClose}>
      <DialogContent className="max-w-2xl p-6">
        {/* ------------------- SUCCESS POPUP ------------------- */}
        {showSuccess && (
          <div className="p-6 text-center bg-green-50 border border-green-200 rounded-lg animate-fadeIn">
            <div className="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-3">
              <svg
                className="w-6 h-6 text-green-600"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M5 13l4 4L19 7"
                />
              </svg>
            </div>
            <h3 className="text-lg font-semibold text-green-800 mb-2">
              Relationship Created
            </h3>
            <p className="text-green-700 mb-4">
              The seller-designer relationship has been successfully created.
            </p>
            {/* Người dùng có thể bấm OK để đóng ngay, hoặc đợi 2s */}
            <Button
              onClick={() => {
                setShowSuccess(false);
                onClose();
              }}
              className="bg-green-600 hover:bg-green-700 text-white"
            >
              OK
            </Button>
          </div>
        )}

        {/* ------------------- ERROR POPUP ------------------- */}
        {showError && !showSuccess && (
          <div className="p-6 text-center bg-red-50 border border-red-200 rounded-lg animate-fadeIn">
            <div className="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-3">
              <XCircle className="w-6 h-6 text-red-600" />
            </div>
            <h3 className="text-lg font-semibold text-red-800 mb-2">Error</h3>
            <p className="text-red-700 mb-4">{errorMessage}</p>
            <Button
              onClick={() => setShowError(false)}
              variant="outline"
              className="border-red-300 text-red-600 hover:bg-red-50"
            >
              Try Again
            </Button>
          </div>
        )}

        {/* ------------------- FORM ------------------- */}
        {!showSuccess && !showError && (
          <form onSubmit={handleSubmit} className="space-y-6 animate-fadeIn">
            <DialogHeader>
              <DialogTitle className="text-xl flex items-center gap-2">
                <Plus className="h-5 w-5" />
                Create Relationship
              </DialogTitle>
            </DialogHeader>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* Seller */}
              <Card>
                <CardContent className="p-4 space-y-3">
                  <Label className="text-sm font-medium">Select Seller</Label>
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 h-4 w-4" />
                    <Input
                      placeholder="Search sellers..."
                      value={sellerSearch}
                      onChange={(e) => setSellerSearch(e.target.value)}
                      className="pl-10"
                    />
                  </div>
                  <Select
                    value={selectedSeller}
                    onValueChange={setSelectedSeller}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Choose a seller" />
                    </SelectTrigger>
                    <SelectContent>
                      {filteredSellers.map((seller) => (
                        <SelectItem
                          key={seller.sellerId}
                          value={seller.sellerId}
                        >
                          {seller.sellerName}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </CardContent>
              </Card>

              {/* Designer */}
              <Card>
                <CardContent className="p-4 space-y-3">
                  <Label className="text-sm font-medium">Select Designer</Label>
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 h-4 w-4" />
                    <Input
                      placeholder="Search designers..."
                      value={designerSearch}
                      onChange={(e) => setDesignerSearch(e.target.value)}
                      className="pl-10"
                    />
                  </div>
                  <Select
                    value={selectedDesigner}
                    onValueChange={setSelectedDesigner}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Choose a designer" />
                    </SelectTrigger>
                    <SelectContent>
                      {filteredDesigners.map((designer) => (
                        <SelectItem
                          key={designer.designerId}
                          value={designer.designerId}
                        >
                          {designer.designerName}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </CardContent>
              </Card>
            </div>

            <div className="flex justify-end gap-3 pt-4">
              <Button
                type="button"
                variant="outline"
                onClick={handleClose}
                disabled={loading}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={loading}
                className="bg-blue-600 hover:bg-blue-700 text-white"
              >
                {loading ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2" />
                    Creating...
                  </>
                ) : (
                  <>
                    <Plus className="h-4 w-4 mr-2" />
                    Create Relationship
                  </>
                )}
              </Button>
            </div>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
