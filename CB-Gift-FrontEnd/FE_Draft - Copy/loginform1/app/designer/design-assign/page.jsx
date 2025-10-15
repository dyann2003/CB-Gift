"use client";

import { useState } from "react";
import RoleSidebar from "@/components/layout/shared/role-sidebar";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Eye,
  Upload,
  Check,
  Download,
  ImageIcon,
  Search,
  Send,
} from "lucide-react";
import DesignerHeader from "@/components/layout/designer/header";
import DesignerSidebar from "@/components/layout/designer/sidebar";

export default function DesignAssignPage() {
  const [currentPage, setCurrentPage] = useState("design-assign");
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [designFile, setDesignFile] = useState(null);
  const [designNotes, setDesignNotes] = useState("");
  const [acceptedOrders, setAcceptedOrders] = useState(new Set());
  const [uploadedDesigns, setUploadedDesigns] = useState(new Set());
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [selectedOrders, setSelectedOrders] = useState(new Set());
  const [selectAll, setSelectAll] = useState(false);
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [confirmAction, setConfirmAction] = useState(null);
  const [confirmMessage, setConfirmMessage] = useState("");

  const assignedOrders = [
    {
      id: "ORD-001",
      customerName: "John Doe",
      productName: "Acrylic Keychain",
      quantity: 50,
      size: "5x5cm",
      accessory: "Key Ring",
      assignedDate: "2024-01-15",
      status: "pending",
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
        size: "5x5cm",
        accessory: "Key Ring",
        quantity: 50,
        activeTTS: true,
        note: "Please make it colorful and vibrant",
      },
      uploadedFiles: {
        linkImg: { name: "design-reference.jpg", url: "/acrylic-keychain.jpg" },
        linkThanksCard: { name: "thanks-card.pdf", url: "#" },
        linkFileDesign: { name: "original-design.ai", url: "#" },
      },
    },
    {
      id: "ORD-002",
      customerName: "Jane Smith",
      productName: "Acrylic Stand",
      quantity: 25,
      size: "10x15cm",
      accessory: "None",
      assignedDate: "2024-01-16",
      status: "pending",
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
        size: "10x15cm",
        accessory: "None",
        quantity: 25,
        activeTTS: false,
        note: "Minimalist design preferred",
      },
      uploadedFiles: {
        linkImg: { name: "stand-reference.png", url: "/acrylic-stand.jpg" },
        linkThanksCard: { name: "thank-you-card.jpg", url: "#" },
        linkFileDesign: { name: "base-design.psd", url: "#" },
      },
    },
    {
      id: "ORD-003",
      customerName: "Mike Johnson",
      productName: "Acrylic Charm",
      quantity: 100,
      size: "3x3cm",
      accessory: "Phone Strap",
      assignedDate: "2024-01-17",
      status: "pending",
      customerInfo: {
        name: "Mike Johnson",
        phone: "+1234567892",
        email: "mike@example.com",
        address: "789 Pine St",
        city: "Chicago",
        state: "IL",
        zipcode: "60601",
        country: "USA",
      },
      productDetails: {
        size: "3x3cm",
        accessory: "Phone Strap",
        quantity: 100,
        activeTTS: true,
        note: "Cute anime style design",
      },
      uploadedFiles: {
        linkImg: { name: "charm-reference.jpg", url: "/acrylic-charm.jpg" },
        linkThanksCard: { name: "thanks-note.pdf", url: "#" },
        linkFileDesign: { name: "charm-design.ai", url: "#" },
      },
    },
  ];

  const filteredOrders = assignedOrders.filter((order) => {
    const matchesSearch =
      order.id.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.customerName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.productName.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesStatus =
      statusFilter === "all" ||
      (statusFilter === "pending" && !acceptedOrders.has(order.id)) ||
      (statusFilter === "accepted" && acceptedOrders.has(order.id)) ||
      (statusFilter === "uploaded" && uploadedDesigns.has(order.id));

    return matchesSearch && matchesStatus;
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

  const handleBulkAccept = () => {
    const newAccepted = new Set([...acceptedOrders, ...selectedOrders]);
    setAcceptedOrders(newAccepted);
    setSelectedOrders(new Set());
    setSelectAll(false);
    console.log(
      `Bulk accepting orders: ${Array.from(selectedOrders).join(", ")}`
    );
  };

  const handleBulkSendToQA = () => {
    const eligibleOrders = Array.from(selectedOrders).filter((orderId) =>
      uploadedDesigns.has(orderId)
    );
    if (eligibleOrders.length > 0) {
      setConfirmMessage(
        `Are you sure you want to send ${eligibleOrders.length} order(s) to QA?`
      );
      setConfirmAction(() => () => {
        console.log(`Bulk sending to QA: ${eligibleOrders.join(", ")}`);
        setSelectedOrders(new Set());
        setSelectAll(false);
        setShowConfirmDialog(false);
      });
      setShowConfirmDialog(true);
    }
  };

  const handleAcceptDesign = (orderId) => {
    console.log(`Accepting design for order: ${orderId}`);
    setAcceptedOrders((prev) => new Set([...prev, orderId]));
  };

  const handleUploadDesign = () => {
    if (designFile && selectedOrder) {
      console.log(`Uploading design for order: ${selectedOrder.id}`);
      setUploadedDesigns((prev) => new Set([...prev, selectedOrder.id]));
      setDesignFile(null);
      setDesignNotes("");
      setSelectedOrder(null);
    }
  };

  const handleSendToQA = (orderId) => {
    setConfirmMessage(`Are you sure you want to send order ${orderId} to QA?`);
    setConfirmAction(() => () => {
      console.log(`Sending order ${orderId} to QA`);
      setShowConfirmDialog(false);
    });
    setShowConfirmDialog(true);
  };

  const getOrderStatus = (order) => {
    if (uploadedDesigns.has(order.id)) {
      return (
        <Badge variant="default" className="bg-blue-500">
          Design Uploaded
        </Badge>
      );
    }
    if (acceptedOrders.has(order.id)) {
      return (
        <Badge variant="default" className="bg-green-500">
          Accepted
        </Badge>
      );
    }
    return <Badge variant="secondary">Pending</Badge>;
  };

  return (
    <div className="flex h-screen bg-gray-50">
      <DesignerSidebar
        currentPage={currentPage}
        setCurrentPage={setCurrentPage}
      />

      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Header */}
        <DesignerHeader />

        <header className="bg-white shadow-sm border-b border-gray-200 px-6 py-4">
          <h1 className="text-2xl font-semibold text-gray-900">
            Design Assign by Seller
          </h1>
          <p className="text-gray-600 mt-1">
            Orders assigned to you for design work
          </p>
        </header>

        {/* Main Content */}
        <main className="flex-1 overflow-y-auto p-6">
          <div className="bg-white p-4 rounded-lg shadow mb-6">
            <div className="flex gap-4 items-center">
              <div className="flex-1">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                  <Input
                    placeholder="Search by Order ID, Customer Name, or Product..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-10"
                  />
                </div>
              </div>
              <div className="w-48">
                <Select value={statusFilter} onValueChange={setStatusFilter}>
                  <SelectTrigger>
                    <SelectValue placeholder="Filter by status" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Status</SelectItem>
                    <SelectItem value="pending">Pending</SelectItem>
                    <SelectItem value="accepted">Accepted</SelectItem>
                    <SelectItem value="uploaded">Design Uploaded</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </div>

          {selectedOrders.size > 0 && (
            <div className="bg-blue-50 p-4 rounded-lg shadow mb-6 border border-blue-200">
              <div className="flex items-center justify-between">
                <span className="text-blue-800 font-medium">
                  {selectedOrders.size} order
                  {selectedOrders.size > 1 ? "s" : ""} selected
                </span>
                <div className="flex gap-2">
                  <Button
                    onClick={handleBulkAccept}
                    className="bg-green-600 hover:bg-green-700"
                    size="sm"
                  >
                    <Check className="h-4 w-4 mr-1" />
                    Accept Selected ({selectedOrders.size})
                  </Button>
                  <Button
                    onClick={handleBulkSendToQA}
                    className="bg-blue-600 hover:bg-blue-700"
                    size="sm"
                    disabled={
                      !Array.from(selectedOrders).some((orderId) =>
                        uploadedDesigns.has(orderId)
                      )
                    }
                  >
                    <Send className="h-4 w-4 mr-1" />
                    Send to QA (
                    {
                      Array.from(selectedOrders).filter((orderId) =>
                        uploadedDesigns.has(orderId)
                      ).length
                    }
                    )
                  </Button>
                </div>
              </div>
            </div>
          )}

          <div className="bg-white rounded-lg shadow">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-12">
                    <Checkbox
                      checked={selectAll}
                      onCheckedChange={handleSelectAll}
                    />
                  </TableHead>
                  <TableHead>Order ID</TableHead>
                  <TableHead>Customer</TableHead>
                  <TableHead>Product</TableHead>
                  <TableHead>Quantity</TableHead>
                  <TableHead>Size</TableHead>
                  <TableHead>Assigned Date</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredOrders.map((order) => (
                  <TableRow key={order.id}>
                    <TableCell>
                      <Checkbox
                        checked={selectedOrders.has(order.id)}
                        onCheckedChange={(checked) =>
                          handleSelectOrder(order.id, checked)
                        }
                      />
                    </TableCell>
                    <TableCell className="font-medium">{order.id}</TableCell>
                    <TableCell>{order.customerName}</TableCell>
                    <TableCell>{order.productName}</TableCell>
                    <TableCell>{order.quantity}</TableCell>
                    <TableCell>{order.size}</TableCell>
                    <TableCell>{order.assignedDate}</TableCell>
                    <TableCell>{getOrderStatus(order)}</TableCell>
                    <TableCell>
                      <div className="flex gap-2">
                        <Dialog>
                          <DialogTrigger asChild>
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => setSelectedOrder(order)}
                            >
                              <Eye className="h-4 w-4 mr-1" />
                              View
                            </Button>
                          </DialogTrigger>
                          <DialogContent className="max-w-4xl max-h-[80vh] overflow-y-auto">
                            <DialogHeader>
                              <DialogTitle>
                                Order Details - {order.id}
                              </DialogTitle>
                            </DialogHeader>
                            <div className="space-y-6">
                              <div>
                                <h3 className="font-semibold text-lg mb-3">
                                  Customer Information
                                </h3>
                                <div className="grid grid-cols-2 gap-4">
                                  <div>
                                    <Label className="text-sm text-gray-500">
                                      Name
                                    </Label>
                                    <p className="font-medium">
                                      {order.customerInfo.name}
                                    </p>
                                  </div>
                                  <div>
                                    <Label className="text-sm text-gray-500">
                                      Phone
                                    </Label>
                                    <p className="font-medium">
                                      {order.customerInfo.phone}
                                    </p>
                                  </div>
                                  <div>
                                    <Label className="text-sm text-gray-500">
                                      Email
                                    </Label>
                                    <p className="font-medium">
                                      {order.customerInfo.email}
                                    </p>
                                  </div>
                                  <div>
                                    <Label className="text-sm text-gray-500">
                                      Address
                                    </Label>
                                    <p className="font-medium">
                                      {order.customerInfo.address}
                                    </p>
                                  </div>
                                  <div>
                                    <Label className="text-sm text-gray-500">
                                      City
                                    </Label>
                                    <p className="font-medium">
                                      {order.customerInfo.city}
                                    </p>
                                  </div>
                                  <div>
                                    <Label className="text-sm text-gray-500">
                                      State
                                    </Label>
                                    <p className="font-medium">
                                      {order.customerInfo.state}
                                    </p>
                                  </div>
                                </div>
                              </div>

                              <div>
                                <h3 className="font-semibold text-lg mb-3">
                                  Product Details
                                </h3>
                                <div className="grid grid-cols-2 gap-4">
                                  <div>
                                    <Label className="text-sm text-gray-500">
                                      Product
                                    </Label>
                                    <p className="font-medium">
                                      {order.productName}
                                    </p>
                                  </div>
                                  <div>
                                    <Label className="text-sm text-gray-500">
                                      Size
                                    </Label>
                                    <p className="font-medium">
                                      {order.productDetails.size}
                                    </p>
                                  </div>
                                  <div>
                                    <Label className="text-sm text-gray-500">
                                      Accessory
                                    </Label>
                                    <p className="font-medium">
                                      {order.productDetails.accessory}
                                    </p>
                                  </div>
                                  <div>
                                    <Label className="text-sm text-gray-500">
                                      Quantity
                                    </Label>
                                    <p className="font-medium">
                                      {order.productDetails.quantity}
                                    </p>
                                  </div>
                                  <div>
                                    <Label className="text-sm text-gray-500">
                                      Active TTS
                                    </Label>
                                    <p className="font-medium">
                                      {order.productDetails.activeTTS
                                        ? "Yes"
                                        : "No"}
                                    </p>
                                  </div>
                                </div>
                                {order.productDetails.note && (
                                  <div className="mt-4">
                                    <Label className="text-sm text-gray-500">
                                      Note
                                    </Label>
                                    <p className="font-medium">
                                      {order.productDetails.note}
                                    </p>
                                  </div>
                                )}
                              </div>

                              <div>
                                <h3 className="font-semibold text-lg mb-3">
                                  Files from Seller
                                </h3>
                                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                  <div className="border rounded-lg p-4">
                                    <Label className="text-sm text-gray-500">
                                      Reference Image
                                    </Label>
                                    <div className="mt-2">
                                      <img
                                        src={
                                          order.uploadedFiles.linkImg.url ||
                                          "/placeholder.svg"
                                        }
                                        alt="Reference"
                                        className="w-full h-32 object-cover rounded border"
                                      />
                                      <p className="text-sm mt-2">
                                        {order.uploadedFiles.linkImg.name}
                                      </p>
                                      <Button
                                        variant="outline"
                                        size="sm"
                                        className="mt-2 w-full bg-transparent"
                                      >
                                        <Download className="h-4 w-4 mr-2" />
                                        Download
                                      </Button>
                                    </div>
                                  </div>

                                  <div className="border rounded-lg p-4">
                                    <Label className="text-sm text-gray-500">
                                      Thanks Card
                                    </Label>
                                    <div className="mt-2">
                                      <div className="w-full h-32 bg-gray-100 rounded border flex items-center justify-center">
                                        <ImageIcon className="h-8 w-8 text-gray-400" />
                                      </div>
                                      <p className="text-sm mt-2">
                                        {
                                          order.uploadedFiles.linkThanksCard
                                            .name
                                        }
                                      </p>
                                      <Button
                                        variant="outline"
                                        size="sm"
                                        className="mt-2 w-full bg-transparent"
                                      >
                                        <Download className="h-4 w-4 mr-2" />
                                        Download
                                      </Button>
                                    </div>
                                  </div>

                                  <div className="border rounded-lg p-4">
                                    <Label className="text-sm text-gray-500">
                                      Design File
                                    </Label>
                                    <div className="mt-2">
                                      <div className="w-full h-32 bg-gray-100 rounded border flex items-center justify-center">
                                        <ImageIcon className="h-8 w-8 text-gray-400" />
                                      </div>
                                      <p className="text-sm mt-2">
                                        {
                                          order.uploadedFiles.linkFileDesign
                                            .name
                                        }
                                      </p>
                                      <Button
                                        variant="outline"
                                        size="sm"
                                        className="mt-2 w-full bg-transparent"
                                      >
                                        <Download className="h-4 w-4 mr-2" />
                                        Download
                                      </Button>
                                    </div>
                                  </div>
                                </div>
                              </div>

                              {acceptedOrders.has(order.id) && (
                                <div>
                                  <h3 className="font-semibold text-lg mb-3">
                                    Upload Design
                                  </h3>
                                  <div className="space-y-4">
                                    <div>
                                      <Label htmlFor="design-file">
                                        Design File
                                      </Label>
                                      <Input
                                        id="design-file"
                                        type="file"
                                        accept=".jpg,.jpeg,.png,.pdf,.ai,.psd"
                                        onChange={(e) =>
                                          setDesignFile(e.target.files[0])
                                        }
                                      />
                                    </div>
                                    <div>
                                      <Label htmlFor="design-notes">
                                        Design Notes
                                      </Label>
                                      <Textarea
                                        id="design-notes"
                                        placeholder="Add any notes about the design..."
                                        value={designNotes}
                                        onChange={(e) =>
                                          setDesignNotes(e.target.value)
                                        }
                                      />
                                    </div>
                                    <div className="flex gap-2">
                                      <Button
                                        onClick={handleUploadDesign}
                                        className="flex-1"
                                      >
                                        <Upload className="h-4 w-4 mr-2" />
                                        Upload Design
                                      </Button>
                                      {uploadedDesigns.has(order.id) && (
                                        <Button
                                          onClick={() =>
                                            handleSendToQA(order.id)
                                          }
                                          className="flex-1 bg-blue-600 hover:bg-blue-700"
                                        >
                                          <Send className="h-4 w-4 mr-1" />
                                          Send to QA
                                        </Button>
                                      )}
                                    </div>
                                  </div>
                                </div>
                              )}
                            </div>
                          </DialogContent>
                        </Dialog>

                        {!acceptedOrders.has(order.id) && (
                          <Button
                            size="sm"
                            onClick={() => handleAcceptDesign(order.id)}
                          >
                            <Check className="h-4 w-4 mr-1" />
                            Accept
                          </Button>
                        )}

                        {uploadedDesigns.has(order.id) && (
                          <Button
                            size="sm"
                            onClick={() => handleSendToQA(order.id)}
                            className="bg-blue-600 hover:bg-blue-700"
                          >
                            <Send className="h-4 w-4 mr-1" />
                            Send to QA
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </main>
      </div>

      {showConfirmDialog && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-[9999]">
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
