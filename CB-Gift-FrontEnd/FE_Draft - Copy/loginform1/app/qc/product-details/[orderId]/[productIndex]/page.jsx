"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import QcHeader from "@/components/layout/qc/header";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Check, X, Download, ArrowLeft } from "lucide-react";
import QcSidebar from "@/components/layout/qc/sidebar";

export default function ProductDetailsPage({ params }) {
  const router = useRouter();
  const [currentPage, setCurrentPage] = useState("check-product");
  const [showRejectDialog, setShowRejectDialog] = useState(false);
  const [rejectReason, setRejectReason] = useState("");
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [confirmMessage, setConfirmMessage] = useState("");
  const [confirmAction, setConfirmAction] = useState(null);

  // Mock data - in real app, fetch based on params
  const productOrders = [
    {
      id: "ORD-003",
      customer: "Mike Brown",
      products: [
        {
          name: "Acrylic Charm",
          quantity: 100,
          size: "3x3cm",
          accessory: "Phone Strap",
          description:
            "Cute anime style acrylic charm with phone strap attachment",
          material: "Premium Acrylic",
          color: "Multi-color",
          printingMethod: "UV Printing",
          qualityNotes:
            "Check for scratches, color accuracy, and strap attachment strength",
        },
        {
          name: "Acrylic Keychain",
          quantity: 50,
          size: "5x5cm",
          accessory: "Key Ring",
          description: "Durable acrylic keychain with metal key ring",
          material: "Premium Acrylic",
          color: "Clear with colored design",
          printingMethod: "UV Printing",
          qualityNotes: "Verify key ring attachment and print quality",
        },
      ],
      staff: "Charlie Davis",
      dateManufactured: "2024-01-13",
      status: "pending_check",
      qrCode: "QR-ORD-003-2024",
      customerInfo: {
        name: "Mike Brown",
        phone: "+1234567892",
        email: "mike@example.com",
        address: "789 Pine St",
        city: "Chicago",
        state: "IL",
        zipcode: "60601",
        country: "USA",
      },
      productDetails: {
        activeTTS: true,
        note: "Cute anime style design",
        specialInstructions: "Pack individually, handle with care",
      },
      customerFiles: [
        { name: "reference.jpg", type: "image", url: "/acrylic-charm.jpg" },
        { name: "thanks-card.pdf", type: "pdf", url: "#" },
      ],
      designFiles: [
        {
          name: "charm-final-design.png",
          url: "/acrylic-charm.jpg",
          type: "image",
        },
        {
          name: "keychain-final-design.png",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
      ],
      productImages: [
        { name: "manufactured-charm.jpg", url: "/acrylic-charm.jpg" },
        { name: "manufactured-keychain.jpg", url: "/acrylic-keychain.jpg" },
      ],
    },
    {
      id: "ORD-004",
      customer: "Sarah Wilson",
      products: [
        {
          name: "Acrylic Keychain",
          quantity: 75,
          size: "4x6cm",
          accessory: "Key Ring",
          description: "Professional acrylic keychain with company branding",
          material: "Premium Acrylic",
          color: "Company colors",
          printingMethod: "UV Printing",
          qualityNotes: "Check logo clarity and color matching",
        },
        {
          name: "Acrylic Stand",
          quantity: 30,
          size: "10x15cm",
          accessory: "None",
          description: "Stable acrylic stand for desk display",
          material: "Premium Acrylic",
          color: "Clear",
          printingMethod: "Engraving",
          qualityNotes: "Verify stability and engraving depth",
        },
        {
          name: "Acrylic Charm",
          quantity: 200,
          size: "3x3cm",
          accessory: "Phone Strap",
          description: "Promotional acrylic charm with phone strap",
          material: "Premium Acrylic",
          color: "Multi-color",
          printingMethod: "UV Printing",
          qualityNotes: "Check for consistent color and strap quality",
        },
      ],
      staff: "David Lee",
      dateManufactured: "2024-01-12",
      status: "pending_check",
      qrCode: "QR-ORD-004-2024",
      customerInfo: {
        name: "Sarah Wilson",
        phone: "+1234567893",
        email: "sarah@example.com",
        address: "321 Elm St",
        city: "Miami",
        state: "FL",
        zipcode: "33101",
        country: "USA",
      },
      productDetails: {
        activeTTS: false,
        note: "Professional look with company branding",
        specialInstructions:
          "Ensure stable base for stand, vibrant colors for charm",
      },
      customerFiles: [
        {
          name: "logo-reference.jpg",
          type: "image",
          url: "/acrylic-keychain.jpg",
        },
        { name: "brand-guidelines.pdf", type: "pdf", url: "#" },
      ],
      designFiles: [
        {
          name: "keychain-final-design.png",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
        {
          name: "stand-final-design.png",
          url: "/acrylic-stand.jpg",
          type: "image",
        },
        {
          name: "charm-final-design.png",
          url: "/acrylic-charm.jpg",
          type: "image",
        },
      ],
      productImages: [
        { name: "manufactured-keychain.jpg", url: "/acrylic-keychain.jpg" },
        { name: "manufactured-stand.jpg", url: "/acrylic-stand.jpg" },
        { name: "manufactured-charm.jpg", url: "/acrylic-charm.jpg" },
      ],
    },
  ];

  const selectedOrder = productOrders.find(
    (order) => order.id === params.orderId
  );
  const productIndex = Number.parseInt(params.productIndex);
  const selectedProduct = selectedOrder?.products[productIndex];

  if (!selectedOrder || !selectedProduct) {
    return (
      <div className="flex h-screen bg-gray-50">
        <QcSidebar currentPage={currentPage} setCurrentPage={setCurrentPage} />
        <div className="flex-1 flex flex-col overflow-hidden">
          <QcHeader />
          <main className="flex-1 overflow-y-auto p-6">
            <div className="text-center py-12">
              <p className="text-gray-600">Product not found</p>
              <Button
                onClick={() => router.back()}
                className="mt-4"
                variant="outline"
              >
                <ArrowLeft className="h-4 w-4 mr-2" />
                Go Back
              </Button>
            </div>
          </main>
        </div>
      </div>
    );
  }

  const handleApprove = () => {
    setConfirmMessage(
      `Are you sure you want to approve ${selectedProduct.name}?`
    );
    setConfirmAction(() => () => {
      console.log(`Product approved: ${selectedProduct.name}`);
      router.back();
      setShowConfirmDialog(false);
    });
    setShowConfirmDialog(true);
  };

  const handleReject = () => {
    setShowRejectDialog(true);
  };

  const handleRejectConfirm = () => {
    if (rejectReason.trim()) {
      console.log(
        `Product rejected: ${selectedProduct.name}. Reason: ${rejectReason}`
      );
      router.back();
      setRejectReason("");
      setShowRejectDialog(false);
    }
  };

  const handleDownload = (fileUrl, fileName) => {
    console.log(`Downloading file: ${fileName} from ${fileUrl}`);
  };

  return (
    <div className="flex h-screen bg-gray-50">
      <QcSidebar currentPage={currentPage} setCurrentPage={setCurrentPage} />
      <div className="flex-1 flex flex-col overflow-hidden">
        <QcHeader />
        <main className="flex-1 overflow-y-auto p-6">
          <div className="space-y-6">
            {/* Header */}
            <div className="flex items-center gap-4">
              <Button variant="outline" size="sm" onClick={() => router.back()}>
                <ArrowLeft className="h-4 w-4 mr-2" />
                Back
              </Button>
              <div>
                <h1 className="text-2xl font-semibold text-gray-900">
                  Product Details - {selectedProduct.name}
                </h1>
                <p className="text-gray-600 mt-1">
                  Order {params.orderId} | {selectedOrder.customer}
                </p>
              </div>
            </div>

            {/* Main Content */}
            <div className="space-y-6">
              {/* Product Information */}
              <div className="bg-gray-50 p-4 rounded-lg">
                <h3 className="font-semibold text-lg mb-3 text-gray-900">
                  Product Information
                </h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Product Name
                    </Label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedProduct.name}
                    </p>
                  </div>
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Size
                    </Label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedProduct.size}
                    </p>
                  </div>
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Quantity
                    </Label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedProduct.quantity}
                    </p>
                  </div>
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Material
                    </Label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedProduct.material}
                    </p>
                  </div>
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Color
                    </Label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedProduct.color}
                    </p>
                  </div>
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Printing Method
                    </Label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedProduct.printingMethod}
                    </p>
                  </div>
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Accessory
                    </Label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedProduct.accessory}
                    </p>
                  </div>
                </div>
              </div>

              {/* Description */}
              <div className="bg-white border border-gray-200 p-4 rounded-lg">
                <h3 className="font-semibold text-lg mb-3 text-gray-900">
                  Description
                </h3>
                <p className="text-gray-700">{selectedProduct.description}</p>
              </div>

              {/* Quality Notes */}
              <div className="bg-blue-50 border border-blue-200 p-4 rounded-lg">
                <h3 className="font-semibold text-lg mb-3 text-blue-900">
                  Quality Check Points
                </h3>
                <p className="text-blue-800">{selectedProduct.qualityNotes}</p>
              </div>

              {/* Product Images */}
              <div>
                <h3 className="font-semibold text-lg mb-3 text-gray-900">
                  Manufactured Product Images
                </h3>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {selectedOrder.productImages.map((image, index) => (
                    <div
                      key={index}
                      className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
                    >
                      <img
                        src={image.url || "/placeholder.svg"}
                        alt={image.name}
                        className="w-full h-48 object-cover rounded border mb-2"
                      />
                      <p className="text-sm text-gray-600">{image.name}</p>
                      <Button
                        variant="outline"
                        size="sm"
                        className="mt-2 w-full bg-transparent hover:bg-gray-50"
                        onClick={() => handleDownload(image.url, image.name)}
                      >
                        <Download className="h-4 w-4 mr-2" />
                        Download
                      </Button>
                    </div>
                  ))}
                </div>
              </div>

              {/* Design Reference */}
              <div>
                <h3 className="font-semibold text-lg mb-3 text-gray-900">
                  Design Reference
                </h3>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {selectedOrder.designFiles.map((file, index) => (
                    <div
                      key={index}
                      className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
                    >
                      <img
                        src={file.url || "/placeholder.svg"}
                        alt="Design"
                        className="w-full h-48 object-cover rounded border mb-2"
                      />
                      <p className="text-sm text-gray-600">{file.name}</p>
                      <Button
                        variant="outline"
                        size="sm"
                        className="mt-2 w-full bg-transparent hover:bg-gray-50"
                        onClick={() => handleDownload(file.url, file.name)}
                      >
                        <Download className="h-4 w-4 mr-2" />
                        Download
                      </Button>
                    </div>
                  ))}
                </div>
              </div>

              {/* Actions */}
              <div className="flex justify-end gap-3 pt-4 border-t">
                <Button variant="outline" onClick={() => router.back()}>
                  Cancel
                </Button>
                <Button
                  variant="outline"
                  onClick={handleReject}
                  className="text-red-600 border-red-600 hover:bg-red-50 bg-transparent"
                >
                  <X className="h-4 w-4 mr-1" />
                  Reject
                </Button>
                <Button
                  onClick={handleApprove}
                  className="bg-green-600 hover:bg-green-700"
                >
                  <Check className="h-4 w-4 mr-1" />
                  Accept
                </Button>
              </div>
            </div>
          </div>
        </main>
      </div>

      {/* Reject Dialog */}
      {showRejectDialog && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-md w-full p-6">
            <h3 className="text-lg font-semibold mb-4">Reject Product</h3>
            <div className="space-y-4">
              <div>
                <Label htmlFor="reject-reason">Reason for rejection</Label>
                <Textarea
                  id="reject-reason"
                  placeholder="Please provide a detailed reason for rejecting this product..."
                  value={rejectReason}
                  onChange={(e) => setRejectReason(e.target.value)}
                  rows={4}
                />
              </div>
            </div>
            <div className="flex gap-3 justify-end mt-6">
              <Button
                variant="outline"
                onClick={() => {
                  setShowRejectDialog(false);
                  setRejectReason("");
                }}
              >
                Cancel
              </Button>
              <Button
                onClick={handleRejectConfirm}
                className="bg-red-600 hover:bg-red-700"
                disabled={!rejectReason.trim()}
              >
                Reject Product
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Confirmation Dialog */}
      {showConfirmDialog && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-md w-full p-6">
            <h3 className="text-lg font-semibold mb-4">Confirm Action</h3>
            <p className="text-gray-600 mb-6">{confirmMessage}</p>
            <div className="flex gap-3 justify-end">
              <Button
                variant="outline"
                onClick={() => setShowConfirmDialog(false)}
              >
                Cancel
              </Button>
              <Button
                onClick={confirmAction}
                className="bg-blue-600 hover:bg-blue-700"
              >
                Confirm
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
