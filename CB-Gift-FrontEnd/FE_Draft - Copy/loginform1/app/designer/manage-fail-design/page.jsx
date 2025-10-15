"use client";

import { useState } from "react";
import RoleSidebar from "@/components/layout/shared/role-sidebar";
import DesignerHeader from "@/components/layout/designer/header";
import DesignerSidebar from "@/components/layout/designer/sidebar";
import SellerHeader from "@/components/layout/seller/header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Search, Eye, Upload, X, CheckCircle, Send } from "lucide-react";

export default function ManageFailDesign() {
  const [currentPage, setCurrentPage] = useState("manage-fail-design");
  const [searchTerm, setSearchTerm] = useState("");
  const [filterStatus, setFilterStatus] = useState("all");
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [uploadedFile, setUploadedFile] = useState(null);
  const [selectedOrders, setSelectedOrders] = useState(new Set());
  const [selectAll, setSelectAll] = useState(false);
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [confirmAction, setConfirmAction] = useState(null);

  // Mock data for failed design orders
  const failedOrders = [
    {
      id: "ORD-001",
      customer: "John Doe",
      product: "Acrylic Keychain",
      failReason: "Design doesn't match customer requirements",
      dateReturned: "2024-01-15",
      status: "pending_design", // Changed from pending_redesign to pending_design
      originalFiles: [
        { name: "reference.jpg", type: "image", url: "/acrylic-keychain.jpg" },
        { name: "thanks-card.pdf", type: "pdf", url: "#" },
      ],
      previousDesign: { name: "design-v1.png", url: "/acrylic-keychain.jpg" },
      qcNotes:
        "Colors don't match the reference image. Please adjust the color scheme.",
    },
    {
      id: "ORD-002",
      customer: "Jane Smith",
      product: "Acrylic Stand",
      failReason: "Size specifications incorrect",
      dateReturned: "2024-01-14",
      status: "design_uploaded", // Added status for design uploaded
      originalFiles: [
        { name: "reference.jpg", type: "image", url: "/acrylic-stand.jpg" },
      ],
      previousDesign: { name: "design-v1.png", url: "/acrylic-stand.jpg" },
      qcNotes: "Stand dimensions are too small. Please increase size by 20%.",
      newDesign: { name: "design-v2.png", url: "/acrylic-stand.jpg" }, // Added new design
    },
    {
      id: "ORD-003",
      customer: "Mike Brown",
      product: "Acrylic Charm",
      failReason: "Wrong color scheme",
      dateReturned: "2024-01-13",
      status: "pending_design",
      originalFiles: [
        { name: "reference.jpg", type: "image", url: "/acrylic-keychain.jpg" },
      ],
      previousDesign: { name: "design-v1.png", url: "/acrylic-keychain.jpg" },
      qcNotes: "Please use brighter colors as requested by customer.",
    },
  ];

  const filteredOrders = failedOrders.filter((order) => {
    const matchesSearch =
      order.id.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.customer.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.product.toLowerCase().includes(searchTerm.toLowerCase());
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

  const handleBulkAcceptRedesign = () => {
    const selectedOrdersData = failedOrders.filter((order) =>
      selectedOrders.has(order.id)
    );
    const eligibleOrders = selectedOrdersData.filter(
      (order) => order.status === "pending_design"
    );

    if (eligibleOrders.length === 0) {
      setConfirmAction({
        type: "error",
        title: "No Eligible Orders",
        message:
          "No orders with 'Pending Design' status selected. Only pending design orders can be accepted for redesign.",
      });
      setShowConfirmDialog(true);
      return;
    }

    setConfirmAction({
      type: "accept_redesign",
      title: "Accept Redesign",
      message: `Accept redesign for ${eligibleOrders.length} order${
        eligibleOrders.length > 1 ? "s" : ""
      }?`,
      count: eligibleOrders.length,
    });
    setShowConfirmDialog(true);
  };

  const handleBulkSendToQA = () => {
    const selectedOrdersData = failedOrders.filter((order) =>
      selectedOrders.has(order.id)
    );
    const eligibleOrders = selectedOrdersData.filter(
      (order) => order.status === "design_uploaded"
    );

    if (eligibleOrders.length === 0) {
      setConfirmAction({
        type: "error",
        title: "No Eligible Orders",
        message:
          "No orders with 'Design Uploaded' status selected. Only orders with uploaded designs can be sent to QA.",
      });
      setShowConfirmDialog(true);
      return;
    }

    setConfirmAction({
      type: "send_to_qa",
      title: "Send to QA",
      message: `Send ${eligibleOrders.length} order${
        eligibleOrders.length > 1 ? "s" : ""
      } to QA for review?`,
      count: eligibleOrders.length,
    });
    setShowConfirmDialog(true);
  };

  const handleConfirmAction = () => {
    if (confirmAction?.type === "accept_redesign") {
      console.log(`Accepted redesign for ${confirmAction.count} orders`);
      // Here you would update the order status to accepted
    } else if (confirmAction?.type === "send_to_qa") {
      console.log(`Sent ${confirmAction.count} orders to QA`);
      // Here you would update the order status to sent to QA
    }

    setSelectedOrders(new Set());
    setSelectAll(false);
    setShowConfirmDialog(false);
    setConfirmAction(null);
  };

  const handleFileUpload = (event) => {
    const file = event.target.files[0];
    if (file) {
      setUploadedFile(file);
    }
  };

  const handleResubmit = () => {
    if (uploadedFile) {
      setConfirmAction({
        type: "resubmit",
        title: "Resubmit Design",
        message: "Are you sure you want to resubmit this design?",
      });
      setShowConfirmDialog(true);
    }
  };

  const getStatusBadge = (status) => {
    switch (status) {
      case "pending_design":
        return <Badge variant="destructive">Pending Design</Badge>;
      case "design_uploaded":
        return (
          <Badge className="bg-blue-100 text-blue-800">Design Uploaded</Badge>
        );
      case "redesigned":
        return (
          <Badge className="bg-green-100 text-green-800">Redesigned</Badge>
        );
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  };

  const pendingDesignCount = Array.from(selectedOrders).filter(
    (orderId) =>
      failedOrders.find((order) => order.id === orderId)?.status ===
      "pending_design"
  ).length;

  const designUploadedCount = Array.from(selectedOrders).filter(
    (orderId) =>
      failedOrders.find((order) => order.id === orderId)?.status ===
      "design_uploaded"
  ).length;

  return (
    <div className="flex h-screen bg-gray-50">
      <DesignerSidebar
        currentPage={currentPage}
        setCurrentPage={setCurrentPage}
      />
      <div className="flex-1 flex flex-col overflow-hidden">
        <DesignerHeader />
        <main className="flex-1 overflow-y-auto p-6">
          <div className="space-y-6">
            {/* Header */}
            <div className="bg-white p-4 rounded-lg border-2 border-red-200">
              <h1 className="text-xl font-semibold text-gray-900">
                Manager Order Fail Design
              </h1>
              <p className="text-gray-600 mt-1">
                Redesign orders returned by QC
              </p>
            </div>

            {/* Search and Filter */}
            <div className="bg-white p-4 rounded-lg shadow">
              <div className="flex gap-4 items-center">
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
                  <option value="pending_design">Pending Design</option>
                  <option value="design_uploaded">Design Uploaded</option>
                  <option value="redesigned">Redesigned</option>
                </select>
              </div>
            </div>

            {selectedOrders.size > 0 && (
              <div className="bg-red-50 p-4 rounded-lg shadow border border-red-200">
                <div className="flex items-center justify-between">
                  <span className="text-red-800 font-medium">
                    {selectedOrders.size} order
                    {selectedOrders.size > 1 ? "s" : ""} selected
                  </span>
                  <div className="flex gap-2">
                    {pendingDesignCount > 0 && (
                      <Button
                        onClick={handleBulkAcceptRedesign}
                        className="bg-orange-600 hover:bg-orange-700"
                        size="sm"
                      >
                        <CheckCircle className="h-4 w-4 mr-1" />
                        Accept Redesign ({pendingDesignCount})
                      </Button>
                    )}
                    {designUploadedCount > 0 && (
                      <Button
                        onClick={handleBulkSendToQA}
                        className="bg-blue-600 hover:bg-blue-700"
                        size="sm"
                      >
                        <Send className="h-4 w-4 mr-1" />
                        Send to QA ({designUploadedCount})
                      </Button>
                    )}
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
                      Product
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Date Returned
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
                        {order.product}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {order.dateReturned}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        {getStatusBadge(order.status)}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setSelectedOrder(order)}
                        >
                          <Eye className="h-4 w-4 mr-1" />
                          View Details
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

      {/* Order Details Modal */}
      {selectedOrder && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-4xl w-full max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <div className="flex justify-between items-center mb-4">
                <h2 className="text-xl font-semibold">
                  Order Details - {selectedOrder.id}
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
                {/* QC Feedback */}
                <div className="bg-red-50 p-4 rounded-lg">
                  <h3 className="font-medium text-red-800 mb-2">QC Feedback</h3>
                  <p className="text-red-700">{selectedOrder.qcNotes}</p>
                  <p className="text-sm text-red-600 mt-1">
                    Reason: {selectedOrder.failReason}
                  </p>
                </div>

                {/* Original Files */}
                <div>
                  <h3 className="font-medium mb-3">Original Customer Files</h3>
                  <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                    {selectedOrder.originalFiles.map((file, index) => (
                      <div key={index} className="border rounded-lg p-3">
                        {file.type === "image" ? (
                          <img
                            src={file.url || "/placeholder.svg"}
                            alt={file.name}
                            className="w-full h-32 object-cover rounded mb-2"
                          />
                        ) : (
                          <div className="w-full h-32 bg-gray-100 rounded mb-2 flex items-center justify-center">
                            <span className="text-gray-500">PDF File</span>
                          </div>
                        )}
                        <p className="text-sm text-gray-600">{file.name}</p>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Previous Design */}
                <div>
                  <h3 className="font-medium mb-3">
                    Previous Design (Rejected)
                  </h3>
                  <div className="border rounded-lg p-3 max-w-xs">
                    <img
                      src={
                        selectedOrder.previousDesign.url || "/placeholder.svg"
                      }
                      alt="Previous design"
                      className="w-full h-32 object-cover rounded mb-2"
                    />
                    <p className="text-sm text-gray-600">
                      {selectedOrder.previousDesign.name}
                    </p>
                  </div>
                </div>

                {selectedOrder.newDesign && (
                  <div>
                    <h3 className="font-medium mb-3">New Design (Uploaded)</h3>
                    <div className="border rounded-lg p-3 max-w-xs">
                      <img
                        src={selectedOrder.newDesign.url || "/placeholder.svg"}
                        alt="New design"
                        className="w-full h-32 object-cover rounded mb-2"
                      />
                      <p className="text-sm text-gray-600">
                        {selectedOrder.newDesign.name}
                      </p>
                    </div>
                  </div>
                )}

                {/* Upload New Design - only show if status is pending_design or no new design uploaded */}
                {(selectedOrder.status === "pending_design" ||
                  !selectedOrder.newDesign) && (
                  <div>
                    <h3 className="font-medium mb-3">Upload New Design</h3>
                    <div className="border-2 border-dashed border-gray-300 rounded-lg p-6">
                      <input
                        type="file"
                        accept="image/*"
                        onChange={handleFileUpload}
                        className="hidden"
                        id="design-upload"
                      />
                      <label htmlFor="design-upload" className="cursor-pointer">
                        <div className="text-center">
                          <Upload className="mx-auto h-12 w-12 text-gray-400" />
                          <p className="mt-2 text-sm text-gray-600">
                            Click to upload new design
                          </p>
                        </div>
                      </label>
                      {uploadedFile && (
                        <div className="mt-4 p-3 bg-green-50 rounded-lg">
                          <p className="text-sm text-green-700">
                            File uploaded: {uploadedFile.name}
                          </p>
                        </div>
                      )}
                    </div>
                  </div>
                )}

                {/* Actions */}
                <div className="flex justify-end gap-3">
                  <Button
                    variant="outline"
                    onClick={() => setSelectedOrder(null)}
                  >
                    Cancel
                  </Button>
                  {selectedOrder.status === "pending_design" &&
                    uploadedFile && (
                      <Button
                        onClick={handleResubmit}
                        className="bg-blue-600 hover:bg-blue-700"
                      >
                        Resubmit Design
                      </Button>
                    )}
                  {selectedOrder.status === "design_uploaded" && (
                    <Button
                      onClick={() => {
                        setConfirmAction({
                          type: "send_to_qa",
                          title: "Send to QA",
                          message: "Send this order to QA for review?",
                          count: 1,
                        });
                        setShowConfirmDialog(true);
                      }}
                      className="bg-blue-600 hover:bg-blue-700"
                    >
                      <Send className="h-4 w-4 mr-1" />
                      Send to QA
                    </Button>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      <Dialog open={showConfirmDialog} onOpenChange={setShowConfirmDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{confirmAction?.title}</DialogTitle>
          </DialogHeader>
          <div className="py-4">
            <p className="text-gray-600">{confirmAction?.message}</p>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setShowConfirmDialog(false)}
            >
              Cancel
            </Button>
            {confirmAction?.type !== "error" && (
              <Button
                onClick={handleConfirmAction}
                className={
                  confirmAction?.type === "accept_redesign"
                    ? "bg-orange-600 hover:bg-orange-700"
                    : confirmAction?.type === "send_to_qa"
                    ? "bg-blue-600 hover:bg-blue-700"
                    : "bg-green-600 hover:bg-green-700"
                }
              >
                {confirmAction?.type === "accept_redesign"
                  ? "Accept"
                  : confirmAction?.type === "send_to_qa"
                  ? "Send to QA"
                  : "Confirm"}
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
