"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import QcHeader from "@/components/layout/qc/header";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Check, X, QrCode, Download, ImageIcon, ArrowLeft } from "lucide-react";
import QcSidebar from "@/components/layout/qc/sidebar";

export default function InspectProductDetail({ params }) {
  const router = useRouter();
  const [currentPage, setCurrentPage] = useState("check-product");
  const [showRejectDialog, setShowRejectDialog] = useState(false);
  const [rejectReason, setRejectReason] = useState("");
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [confirmMessage, setConfirmMessage] = useState("");
  const [confirmAction, setConfirmAction] = useState(null);

  // Mock data - in real app, fetch based on params.id
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
        },
        {
          name: "Acrylic Keychain",
          quantity: 50,
          size: "5x5cm",
          accessory: "Key Ring",
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
        },
        {
          name: "Acrylic Stand",
          quantity: 30,
          size: "10x15cm",
          accessory: "None",
        },
        {
          name: "Acrylic Charm",
          quantity: 200,
          size: "3x3cm",
          accessory: "Phone Strap",
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

  const selectedOrder = productOrders.find((order) => order.id === params.id);

  if (!selectedOrder) {
    return (
      <div className="flex h-screen bg-gray-50">
        <QcSidebar currentPage={currentPage} setCurrentPage={setCurrentPage} />
        <div className="flex-1 flex flex-col overflow-hidden">
          <QcHeader />
          <main className="flex-1 overflow-y-auto p-6">
            <div className="text-center py-12">
              <p className="text-gray-600">Order not found</p>
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

  const handleApprove = (orderId) => {
    setConfirmMessage(
      `Are you sure you want to approve product for order ${orderId}?`
    );
    setConfirmAction(() => () => {
      console.log(
        `Product approved for order ${orderId}. Sending for packaging and shipping.`
      );
      router.back();
      setShowConfirmDialog(false);
    });
    setShowConfirmDialog(true);
  };

  const handleReject = (orderId) => {
    setShowRejectDialog(true);
  };

  const handleRejectConfirm = () => {
    if (rejectReason.trim()) {
      console.log(
        `Product rejected for order ${selectedOrder.id}. Sending back to staff for reproduction. Reason: ${rejectReason}`
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
                  Product Inspection - {selectedOrder.id}
                </h1>
                <p className="text-gray-600 mt-1">
                  Quality check details for {selectedOrder.customer}
                </p>
              </div>
            </div>

            {/* Main Content */}
            <div className="space-y-6">
              {/* Customer Information */}
              <div className="bg-gray-50 p-4 rounded-lg">
                <h3 className="font-semibold text-lg mb-3 text-gray-900">
                  Customer Information
                </h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Name
                    </Label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedOrder.customerInfo.name}
                    </p>
                  </div>
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Phone
                    </Label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedOrder.customerInfo.phone}
                    </p>
                  </div>
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Email
                    </Label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedOrder.customerInfo.email}
                    </p>
                  </div>
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Address
                    </Label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedOrder.customerInfo.address},{" "}
                      {selectedOrder.customerInfo.city},{" "}
                      {selectedOrder.customerInfo.state}{" "}
                      {selectedOrder.customerInfo.zipcode}
                    </p>
                  </div>
                </div>
              </div>

              {/* Product Details */}
              <div>
                <h3 className="font-semibold text-lg mb-3 text-gray-900">
                  Product Details
                </h3>
                <div className="space-y-4">
                  {selectedOrder.products.map((product, index) => (
                    <div
                      key={index}
                      className="border border-gray-200 rounded-lg p-4"
                    >
                      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                        <div>
                          <Label className="text-sm text-gray-500 font-medium">
                            Product
                          </Label>
                          <p className="font-medium text-gray-900 mt-1">
                            {product.name}
                          </p>
                        </div>
                        <div>
                          <Label className="text-sm text-gray-500 font-medium">
                            Size
                          </Label>
                          <p className="font-medium text-gray-900 mt-1">
                            {product.size}
                          </p>
                        </div>
                        <div>
                          <Label className="text-sm text-gray-500 font-medium">
                            Quantity
                          </Label>
                          <p className="font-medium text-gray-900 mt-1">
                            {product.quantity}
                          </p>
                        </div>
                        <div>
                          <Label className="text-sm text-gray-500 font-medium">
                            Accessory
                          </Label>
                          <p className="font-medium text-gray-900 mt-1">
                            {product.accessory}
                          </p>
                        </div>
                      </div>
                      <div className="mt-4 flex justify-end">
                        <Button
                          onClick={() =>
                            router.push(
                              `/qc/product-details/${selectedOrder.id}/${index}`
                            )
                          }
                          className="bg-blue-600 hover:bg-blue-700"
                        >
                          View Details
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
                {selectedOrder.productDetails.specialInstructions && (
                  <div className="mt-4 p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                    <Label className="text-sm text-yellow-700 font-medium">
                      Special Instructions
                    </Label>
                    <p className="text-yellow-800 mt-1">
                      {selectedOrder.productDetails.specialInstructions}
                    </p>
                  </div>
                )}
              </div>

              {/* Order Status */}
              <div className="bg-gray-50 p-4 rounded-lg">
                <h3 className="font-semibold text-lg mb-3 text-gray-900">
                  Order Status
                </h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Current Status
                    </Label>
                    <div className="mt-1">
                      <Badge variant="secondary">Pending Check</Badge>
                    </div>
                  </div>
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Date Manufactured
                    </Label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedOrder.dateManufactured}
                    </p>
                  </div>
                  <div>
                    <Label className="text-sm text-gray-500 font-medium">
                      Staff
                    </Label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedOrder.staff}
                    </p>
                  </div>
                </div>
              </div>

              {/* Order Files */}
              <div>
                <h3 className="font-semibold text-lg mb-3 text-gray-900">
                  Order Files
                </h3>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  {/* Customer Files */}
                  <div>
                    <h4 className="font-medium text-sm mb-3 text-gray-700">
                      Customer Requirements
                    </h4>
                    <div className="space-y-3">
                      {selectedOrder.customerFiles.map((file, index) => (
                        <div
                          key={index}
                          className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
                        >
                          {file.type === "image" ? (
                            <img
                              src={file.url || "/placeholder.svg"}
                              alt={file.name}
                              className="w-full h-32 object-cover rounded border mb-2"
                            />
                          ) : (
                            <div className="w-full h-32 bg-gray-100 rounded border flex items-center justify-center mb-2">
                              <ImageIcon className="h-8 w-8 text-gray-400" />
                            </div>
                          )}
                          <p className="text-sm mt-2 text-gray-600">
                            {file.name}
                          </p>
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

                  {/* Design Files */}
                  <div>
                    <h4 className="font-medium text-sm mb-3 text-gray-700">
                      Designer's Design
                    </h4>
                    <div className="space-y-3">
                      {selectedOrder.designFiles.map((file, index) => (
                        <div
                          key={index}
                          className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
                        >
                          <img
                            src={file.url || "/placeholder.svg"}
                            alt="Design"
                            className="w-full h-32 object-cover rounded border mb-2"
                          />
                          <p className="text-sm mt-2 text-gray-600">
                            {file.name}
                          </p>
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

                  {/* Manufactured Products */}
                  <div>
                    <h4 className="font-medium text-sm mb-3 text-gray-700">
                      Manufactured Products
                    </h4>
                    <div className="space-y-3">
                      {selectedOrder.productImages.map((file, index) => (
                        <div
                          key={index}
                          className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
                        >
                          <img
                            src={file.url || "/placeholder.svg"}
                            alt="Manufactured Product"
                            className="w-full h-32 object-cover rounded border mb-2"
                          />
                          <p className="text-sm mt-2 text-gray-600">
                            {file.name}
                          </p>
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
                </div>
              </div>

              {/* QR Code Section */}
              <div className="bg-gray-50 p-4 rounded-lg">
                <h4 className="font-medium mb-2 text-gray-900">
                  QR Code for Order {selectedOrder.id}
                </h4>
                <div className="w-32 h-32 bg-white border-2 border-gray-300 rounded flex items-center justify-center">
                  <QrCode className="h-16 w-16 text-gray-400" />
                </div>
              </div>

              {/* Actions */}
              <div className="flex justify-end gap-3 pt-4 border-t">
                <Button variant="outline" onClick={() => router.back()}>
                  Cancel
                </Button>
                <Button
                  variant="outline"
                  onClick={() => handleReject(selectedOrder.id)}
                  className="text-red-600 border-red-600 hover:bg-red-50"
                >
                  <X className="h-4 w-4 mr-1" />
                  Reject Product
                </Button>
                <Button
                  onClick={() => handleApprove(selectedOrder.id)}
                  className="bg-green-600 hover:bg-green-700"
                >
                  <Check className="h-4 w-4 mr-1" />
                  Approve for Shipping
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
