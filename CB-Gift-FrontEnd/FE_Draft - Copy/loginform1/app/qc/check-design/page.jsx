"use client";

import { useState } from "react";
import RoleSidebar from "@/components/layout/shared/role-sidebar";
import SellerHeader from "@/components/layout/seller/header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import {
  Search,
  Eye,
  Check,
  X,
  Download,
  QrCode,
  ImageIcon,
} from "lucide-react";
import { Checkbox } from "@/components/ui/checkbox";
import QcSidebar from "@/components/layout/qc/sidebar";
import QcHeader from "@/components/layout/qc/header";

export default function CheckDesign() {
  const [currentPage, setCurrentPage] = useState("check-design");
  const [searchTerm, setSearchTerm] = useState("");
  const [filterStatus, setFilterStatus] = useState("all");
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [selectedOrders, setSelectedOrders] = useState(new Set());
  const [selectAll, setSelectAll] = useState(false);
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [showRejectDialog, setShowRejectDialog] = useState(false);
  const [rejectReason, setRejectReason] = useState("");
  const [confirmAction, setConfirmAction] = useState(null);
  const [confirmMessage, setConfirmMessage] = useState("");
  const [showQRScanner, setShowQRScanner] = useState(false);
  const [manualOrderId, setManualOrderId] = useState("");

  const designOrders = [
    {
      id: "ORD-001",
      customer: "John Doe",
      products: [
        {
          name: "Acrylic Keychain",
          quantity: 50,
          size: "5x5cm",
          accessory: "Key Ring",
        },
        {
          name: "Acrylic Stand",
          quantity: 25,
          size: "10x15cm",
          accessory: "None",
        },
      ],
      designer: "Alice Johnson",
      dateSubmitted: "2024-01-15",
      status: "pending_review",
      qrCode: "QR-ORD-001-2024",
      customerInfo: {
        name: "John Doe",
        phone: "+1234567890",
        email: "john@example.com",
        address: "123 Main St",
        city: "New York",
        state: "NY",
        zipcode: "10001",
        country: "USA",
      },
      productDetails: {
        activeTTS: true,
        note: "Customer wants bright colors with cartoon style",
        specialInstructions: "Please ensure colors are vibrant and durable",
      },
      customerFiles: [
        { name: "reference.jpg", type: "image", url: "/acrylic-keychain.jpg" },
        { name: "thanks-card.pdf", type: "pdf", url: "#" },
        { name: "original-design.ai", type: "file", url: "#" },
      ],
      designFiles: [
        {
          name: "keychain-design.png",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
        { name: "stand-design.png", url: "/acrylic-stand.jpg", type: "image" },
      ],
      notes: "Customer wants bright colors with cartoon style",
    },
    {
      id: "ORD-002",
      customer: "Jane Smith",
      products: [
        {
          name: "Acrylic Stand",
          quantity: 25,
          size: "10x15cm",
          accessory: "None",
        },
        {
          name: "Acrylic Charm",
          quantity: 100,
          size: "3x3cm",
          accessory: "Phone Strap",
        },
        {
          name: "Acrylic Keychain",
          quantity: 30,
          size: "4x6cm",
          accessory: "Key Ring",
        },
      ],
      designer: "Bob Wilson",
      dateSubmitted: "2024-01-14",
      status: "pending_review",
      qrCode: "QR-ORD-002-2024",
      customerInfo: {
        name: "Jane Smith",
        phone: "+1234567891",
        email: "jane@example.com",
        address: "456 Oak Ave",
        city: "Los Angeles",
        state: "CA",
        zipcode: "90001",
        country: "USA",
      },
      productDetails: {
        activeTTS: false,
        note: "Minimalist design with clean lines",
        specialInstructions:
          "Ensure stable base for stand, cute design for charm",
      },
      customerFiles: [
        { name: "reference.jpg", type: "image", url: "/acrylic-stand.jpg" },
        { name: "logo-file.png", type: "image", url: "/acrylic-charm.jpg" },
      ],
      designFiles: [
        { name: "stand-design.png", url: "/acrylic-stand.jpg", type: "image" },
        { name: "charm-design.png", url: "/acrylic-charm.jpg", type: "image" },
        {
          name: "keychain-design.png",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
      ],
      notes: "Minimalist design with clean lines",
    },
  ];

  const filteredOrders = designOrders.filter((order) => {
    const matchesSearch =
      order.id.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.customer.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.products.some((product) =>
        product.name.toLowerCase().includes(searchTerm.toLowerCase())
      );
    const matchesFilter =
      filterStatus === "all" || order.status === filterStatus;
    return matchesSearch && matchesFilter;
  });

  const handleSelectAll = (checked) => {
    setSelectAll(checked);
    if (checked) {
      setSelectedOrders(new Set(filteredOrders.map((order) => order.id)));
    } else {
      setSelectedOrders(new Set());
    }
  };

  const handleSelectOrder = (orderId, checked) => {
    const newSelected = new Set(selectedOrders);
    if (checked) {
      newSelected.add(orderId);
    } else {
      newSelected.delete(orderId);
    }
    setSelectedOrders(newSelected);
    setSelectAll(newSelected.size === filteredOrders.length);
  };

  const handleBulkApprove = () => {
    setConfirmMessage(
      `Are you sure you want to approve ${selectedOrders.size} design(s)?`
    );
    setConfirmAction(() => () => {
      console.log(
        `Bulk approving designs: ${Array.from(selectedOrders).join(", ")}`
      );
      setSelectedOrders(new Set());
      setSelectAll(false);
      setShowConfirmDialog(false);
    });
    setShowConfirmDialog(true);
  };

  const handleBulkReject = () => {
    setShowRejectDialog(true);
  };

  const handleBulkRejectConfirm = () => {
    if (rejectReason.trim()) {
      console.log(
        `Bulk rejecting designs: ${Array.from(selectedOrders).join(
          ", "
        )} - Reason: ${rejectReason}`
      );
      setSelectedOrders(new Set());
      setSelectAll(false);
      setRejectReason("");
      setShowRejectDialog(false);
    }
  };

  const handleApprove = (orderId) => {
    setConfirmMessage(
      `Are you sure you want to approve design for order ${orderId}?`
    );
    setConfirmAction(() => () => {
      console.log(
        `Design approved for order ${orderId}. Sending to production.`
      );
      setSelectedOrder(null);
      setShowConfirmDialog(false);
    });
    setShowConfirmDialog(true);
  };

  const handleReject = (orderId) => {
    setSelectedOrder({ ...selectedOrder, rejectOrderId: orderId });
    setShowRejectDialog(true);
  };

  const handleRejectConfirm = () => {
    if (rejectReason.trim()) {
      const orderId = selectedOrder?.rejectOrderId || selectedOrder?.id;
      console.log(
        `Design rejected for order ${orderId}. Sending back to designer. Reason: ${rejectReason}`
      );
      setSelectedOrder(null);
      setRejectReason("");
      setShowRejectDialog(false);
    }
  };

  const handleQRScan = () => {
    const mockQRResult = "QR-ORD-001-2024";
    const foundOrder = designOrders.find(
      (order) => order.qrCode === mockQRResult
    );
    if (foundOrder) {
      setSelectedOrder(foundOrder);
      setShowQRScanner(false);
    } else {
      alert("Order not found for this QR code");
    }
  };

  const handleManualOrderSearch = () => {
    if (manualOrderId.trim()) {
      const foundOrder = designOrders.find(
        (order) => order.id.toLowerCase() === manualOrderId.toLowerCase()
      );
      if (foundOrder) {
        setSelectedOrder(foundOrder);
        setManualOrderId("");
      } else {
        alert("Order not found");
      }
    }
  };

  const handleDownload = (fileUrl, fileName) => {
    // Simulate file download
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
            <div className="bg-white p-4 rounded-lg border-2 border-blue-200">
              <h1 className="text-xl font-semibold text-gray-900">
                Check Design
              </h1>
              <p className="text-gray-600 mt-1">
                Review designs submitted by designers
              </p>
            </div>

            {/* Search, Filter and QR Scanner */}
            <div className="bg-white p-4 rounded-lg shadow">
              <div className="flex gap-4 items-center mb-4">
                <div className="flex-1 relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                  <Input
                    placeholder="Search by Order ID, Customer, or Product..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-10"
                  />
                </div>
                <select
                  value={filterStatus}
                  onChange={(e) => setFilterStatus(e.target.value)}
                  className="px-3 py-2 border border-gray-300 rounded-md"
                >
                  <option value="all">All Status</option>
                  <option value="pending_review">Pending Review</option>
                  <option value="approved">Approved</option>
                  <option value="rejected">Rejected</option>
                </select>
                <Button
                  onClick={() => setShowQRScanner(true)}
                  className="bg-blue-600 hover:bg-blue-700"
                >
                  <QrCode className="h-4 w-4 mr-2" />
                  Scan QR
                </Button>
              </div>

              <div className="flex gap-2 items-center">
                <Input
                  placeholder="Enter Order ID manually..."
                  value={manualOrderId}
                  onChange={(e) => setManualOrderId(e.target.value)}
                  className="flex-1"
                />
                <Button onClick={handleManualOrderSearch} variant="outline">
                  <Search className="h-4 w-4 mr-1" />
                  Search Order
                </Button>
              </div>
            </div>

            {/* Bulk Action Bar */}
            {selectedOrders.size > 0 && (
              <div className="bg-blue-50 p-4 rounded-lg shadow mb-6 border border-blue-200">
                <div className="flex items-center justify-between">
                  <span className="text-blue-800 font-medium">
                    {selectedOrders.size} design
                    {selectedOrders.size > 1 ? "s" : ""} selected
                  </span>
                  <div className="flex gap-2">
                    <Button
                      onClick={handleBulkApprove}
                      className="bg-green-600 hover:bg-green-700"
                      size="sm"
                    >
                      <Check className="h-4 w-4 mr-1" />
                      Accept Selected ({selectedOrders.size})
                    </Button>
                    <Button
                      onClick={handleBulkReject}
                      variant="outline"
                      className="text-red-600 border-red-600 hover:bg-red-50 bg-transparent"
                      size="sm"
                    >
                      <X className="h-4 w-4 mr-1" />
                      Reject Selected ({selectedOrders.size})
                    </Button>
                  </div>
                </div>
              </div>
            )}

            {/* Orders Table */}
            <div className="bg-white rounded-lg shadow overflow-hidden">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider w-12">
                      <Checkbox
                        checked={selectAll}
                        onCheckedChange={handleSelectAll}
                      />
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Order ID
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Customer
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Products
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Designer
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Date Submitted
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Status
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {filteredOrders.map((order) => (
                    <tr key={order.id}>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <Checkbox
                          checked={selectedOrders.has(order.id)}
                          onCheckedChange={(checked) =>
                            handleSelectOrder(order.id, checked)
                          }
                        />
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                        {order.id}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {order.customer}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {order.products
                          .map((product) => product.name)
                          .join(", ")}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {order.designer}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {order.dateSubmitted}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <Badge variant="secondary">Pending Review</Badge>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setSelectedOrder(order)}
                        >
                          <Eye className="h-4 w-4 mr-1" />
                          Review
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </main>
      </div>

      {/* QR Scanner Modal */}
      {showQRScanner && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-md w-full p-6">
            <div className="text-center">
              <QrCode className="mx-auto h-16 w-16 text-blue-600 mb-4" />
              <h2 className="text-xl font-semibold mb-2">QR Code Scanner</h2>
              <p className="text-gray-600 mb-6">
                Scan the QR code to view order details
              </p>

              <div className="border-2 border-dashed border-gray-300 rounded-lg p-8 mb-4">
                <p className="text-gray-500">Camera view would appear here</p>
              </div>

              <div className="flex gap-3">
                <Button
                  variant="outline"
                  onClick={() => setShowQRScanner(false)}
                  className="flex-1"
                >
                  Cancel
                </Button>
                <Button
                  onClick={handleQRScan}
                  className="flex-1 bg-blue-600 hover:bg-blue-700"
                >
                  Simulate Scan
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Design Review Modal */}
      {selectedOrder && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-6xl w-full max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <div className="flex justify-between items-center mb-4">
                <h2 className="text-xl font-semibold">
                  Design Review - {selectedOrder.id}
                </h2>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setSelectedOrder(null)}
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>

              <div className="space-y-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  {/* Customer Information */}
                  <div className="bg-gray-50 p-4 rounded-lg">
                    <h3 className="font-semibold text-lg mb-3">
                      Customer Information
                    </h3>
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <Label className="text-sm text-gray-500">Name</Label>
                        <p className="font-medium">
                          {selectedOrder.customerInfo.name}
                        </p>
                      </div>
                      <div>
                        <Label className="text-sm text-gray-500">Phone</Label>
                        <p className="font-medium">
                          {selectedOrder.customerInfo.phone}
                        </p>
                      </div>
                      <div>
                        <Label className="text-sm text-gray-500">Email</Label>
                        <p className="font-medium">
                          {selectedOrder.customerInfo.email}
                        </p>
                      </div>
                      <div>
                        <Label className="text-sm text-gray-500">Address</Label>
                        <p className="font-medium">
                          {selectedOrder.customerInfo.address}
                        </p>
                      </div>
                      <div>
                        <Label className="text-sm text-gray-500">City</Label>
                        <p className="font-medium">
                          {selectedOrder.customerInfo.city}
                        </p>
                      </div>
                      <div>
                        <Label className="text-sm text-gray-500">State</Label>
                        <p className="font-medium">
                          {selectedOrder.customerInfo.state}
                        </p>
                      </div>
                    </div>
                  </div>

                  {/* Product Details */}
                  <div className="bg-blue-50 p-4 rounded-lg">
                    <h3 className="font-semibold text-lg mb-3">
                      Product Details
                    </h3>
                    <div className="space-y-3">
                      {selectedOrder.products.map((product, index) => (
                        <div
                          key={index}
                          className="bg-white p-3 rounded border"
                        >
                          <div className="grid grid-cols-2 gap-2 text-sm">
                            <div>
                              <Label className="text-xs text-gray-500">
                                Product
                              </Label>
                              <p className="font-medium">{product.name}</p>
                            </div>
                            <div>
                              <Label className="text-xs text-gray-500">
                                Quantity
                              </Label>
                              <p className="font-medium">{product.quantity}</p>
                            </div>
                            <div>
                              <Label className="text-xs text-gray-500">
                                Size
                              </Label>
                              <p className="font-medium">{product.size}</p>
                            </div>
                            <div>
                              <Label className="text-xs text-gray-500">
                                Accessory
                              </Label>
                              <p className="font-medium">{product.accessory}</p>
                            </div>
                          </div>
                        </div>
                      ))}
                      <div className="mt-4">
                        <Label className="text-sm text-gray-500">
                          Active TTS
                        </Label>
                        <p className="font-medium">
                          {selectedOrder.productDetails.activeTTS
                            ? "Yes"
                            : "No"}
                        </p>
                      </div>
                      {selectedOrder.productDetails.note && (
                        <div>
                          <Label className="text-sm text-gray-500">Note</Label>
                          <p className="font-medium">
                            {selectedOrder.productDetails.note}
                          </p>
                        </div>
                      )}
                      {selectedOrder.productDetails.specialInstructions && (
                        <div>
                          <Label className="text-sm text-gray-500">
                            Special Instructions
                          </Label>
                          <p className="font-medium">
                            {selectedOrder.productDetails.specialInstructions}
                          </p>
                        </div>
                      )}
                    </div>
                  </div>
                </div>

                {/* Files Comparison */}
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                  {/* Customer Files */}
                  <div>
                    <h3 className="font-medium mb-3">
                      Customer Reference Files
                    </h3>
                    <div className="space-y-3">
                      {selectedOrder.customerFiles.map((file, index) => (
                        <div key={index} className="border rounded-lg p-3">
                          {file.type === "image" ? (
                            <img
                              src={file.url || "/placeholder.svg"}
                              alt={file.name}
                              className="w-full h-48 object-cover rounded mb-2"
                            />
                          ) : (
                            <div className="w-full h-48 bg-gray-100 rounded mb-2 flex items-center justify-center">
                              <ImageIcon className="h-8 w-8 text-gray-400" />
                            </div>
                          )}
                          <p className="text-sm text-gray-600">{file.name}</p>
                          <Button
                            variant="outline"
                            size="sm"
                            className="mt-2 w-full bg-transparent"
                            onClick={() => handleDownload(file.url, file.name)}
                          >
                            <Download className="h-4 w-4 mr-2" />
                            Download
                          </Button>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Designer Files */}
                  <div>
                    <h3 className="font-medium mb-3">Designer's Design</h3>
                    <div className="space-y-3">
                      {selectedOrder.designFiles.map((file, index) => (
                        <div key={index} className="border rounded-lg p-3">
                          <img
                            src={file.url || "/placeholder.svg"}
                            alt="Designer's work"
                            className="w-full h-48 object-cover rounded mb-2"
                          />
                          <p className="text-sm text-gray-600">{file.name}</p>
                          <Button
                            variant="outline"
                            size="sm"
                            className="mt-2 w-full bg-transparent"
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

                {/* Actions */}
                <div className="flex justify-end gap-3">
                  <Button
                    variant="outline"
                    onClick={() => handleReject(selectedOrder.id)}
                    className="text-red-600 border-red-600 hover:bg-red-50"
                  >
                    <X className="h-4 w-4 mr-1" />
                    Reject Design
                  </Button>
                  <Button
                    onClick={() => handleApprove(selectedOrder.id)}
                    className="bg-green-600 hover:bg-green-700"
                  >
                    <Check className="h-4 w-4 mr-1" />
                    Approve Design
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {showRejectDialog && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-md w-full p-6">
            <h3 className="text-lg font-semibold mb-4">Reject Design</h3>
            <div className="space-y-4">
              <div>
                <Label htmlFor="reject-reason">Reason for rejection</Label>
                <Textarea
                  id="reject-reason"
                  placeholder="Please provide a detailed reason for rejecting this design..."
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
                onClick={
                  selectedOrders.size > 0
                    ? handleBulkRejectConfirm
                    : handleRejectConfirm
                }
                className="bg-red-600 hover:bg-red-700"
                disabled={!rejectReason.trim()}
              >
                Reject Design
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
