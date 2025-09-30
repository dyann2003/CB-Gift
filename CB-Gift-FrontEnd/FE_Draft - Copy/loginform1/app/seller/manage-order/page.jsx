"use client";

import { useState } from "react";
import SellerSidebar from "@/components/layout/seller/sidebar";
import SellerHeader from "@/components/layout/seller/header";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import MakeManualModal from "@/components/modals/make-manual-modal";
import {
  Search,
  Upload,
  FileText,
  Package,
  Eye,
  Download,
  QrCode,
  Edit,
  Save,
} from "lucide-react";

export default function ManageOrder() {
  const [currentPage, setCurrentPage] = useState("manage-order");
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [selectedOrders, setSelectedOrders] = useState([]);
  const [selectAll, setSelectAll] = useState(false);
  const [showMakeManualModal, setShowMakeManualModal] = useState(false);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [isEditMode, setIsEditMode] = useState(false);
  const [editedOrder, setEditedOrder] = useState(null);

  const orders = [
    {
      id: 1,
      orderId: "ORD-S001",
      orderDate: "2024-01-20",
      customerName: "Emma Thompson",
      phone: "+1-555-0101",
      email: "emma.thompson@email.com",
      products: [
        {
          name: "Custom Acrylic Keychain",
          quantity: 25,
          price: 87.5,
          size: "5x5cm",
          accessory: "Key Ring",
        },
      ],
      address: "123 Oak Street, Portland, OR 97201",
      shipTo: "Same as billing",
      status: "Pending Design",
      totalAmount: "$87.50",
      timeCreated: "2 hours ago",
      selected: false,
      customerInfo: {
        name: "Emma Thompson",
        phone: "+1-555-0101",
        email: "emma.thompson@email.com",
        address: "123 Oak Street",
        city: "Portland",
        state: "OR",
        zipcode: "97201",
        country: "USA",
      },
      orderNotes: "Colorful anime design preferred",
      uploadedFiles: {
        linkImg: {
          name: "keychain-reference.jpg",
          url: "/acrylic-keychain.jpg",
        },
        linkThanksCard: { name: "thanks-card.pdf", url: "#" },
        linkFileDesign: { name: "design-file.ai", url: "#" },
      },
    },
    {
      id: 2,
      orderId: "ORD-S002",
      orderDate: "2024-01-21",
      customerName: "James Wilson",
      phone: "+1-555-0102",
      email: "james.wilson@email.com",
      products: [
        {
          name: "Acrylic Phone Stand",
          quantity: 15,
          price: 75.0,
          size: "8x10cm",
          accessory: "None",
        },
        {
          name: "Custom Charm Set",
          quantity: 30,
          price: 60.0,
          size: "3x3cm",
          accessory: "Phone Strap",
        },
      ],
      address: "456 Pine Avenue, Seattle, WA 98101",
      shipTo: "Office: 789 Business Blvd, Seattle, WA 98102",
      status: "In Progress",
      totalAmount: "$135.00",
      timeCreated: "5 hours ago",
      selected: false,
      customerInfo: {
        name: "James Wilson",
        phone: "+1-555-0102",
        email: "james.wilson@email.com",
        address: "456 Pine Avenue",
        city: "Seattle",
        state: "WA",
        zipcode: "98101",
        country: "USA",
      },
      orderNotes: "Minimalist design with matching theme for both items",
      uploadedFiles: {
        linkImg: { name: "stand-reference.png", url: "/acrylic-stand.jpg" },
        linkThanksCard: { name: "thank-you-card.jpg", url: "#" },
        linkFileDesign: { name: "base-design.psd", url: "#" },
      },
    },
    {
      id: 3,
      orderId: "ORD-S003",
      orderDate: "2024-01-22",
      customerName: "Lisa Chen",
      phone: "+1-555-0103",
      email: "lisa.chen@email.com",
      products: [
        {
          name: "Acrylic Display Case",
          quantity: 10,
          price: 125.0,
          size: "15x20cm",
          accessory: "LED Base",
        },
      ],
      address: "789 Maple Drive, San Francisco, CA 94102",
      shipTo: "Same as billing",
      status: "Completed",
      totalAmount: "$125.00",
      timeCreated: "1 day ago",
      selected: false,
      customerInfo: {
        name: "Lisa Chen",
        phone: "+1-555-0103",
        email: "lisa.chen@email.com",
        address: "789 Maple Drive",
        city: "San Francisco",
        state: "CA",
        zipcode: "94102",
        country: "USA",
      },
      orderNotes: "Professional showcase quality required",
      uploadedFiles: {
        linkImg: { name: "display-reference.png", url: "/acrylic-display.jpg" },
        linkThanksCard: { name: "premium-thanks.jpg", url: "#" },
        linkFileDesign: { name: "display-design.psd", url: "#" },
      },
    },
    {
      id: 4,
      orderId: "ORD-S004",
      orderDate: "2024-01-23",
      customerName: "Robert Davis",
      phone: "+1-555-0104",
      email: "robert.davis@email.com",
      products: [
        {
          name: "Custom Keychain",
          quantity: 50,
          price: 125.0,
          size: "5x5cm",
          accessory: "Key Ring",
        },
        {
          name: "Acrylic Stand",
          quantity: 20,
          price: 60.0,
          size: "10x12cm",
          accessory: "Stand Base",
        },
        {
          name: "Phone Charm",
          quantity: 35,
          price: 52.5,
          size: "4x4cm",
          accessory: "Phone Strap",
        },
      ],
      address: "321 Cedar Lane, Austin, TX 73301",
      shipTo: "Corporate HQ: 654 Business Park, Austin, TX 73302",
      status: "Pending Design",
      totalAmount: "$237.50",
      timeCreated: "6 hours ago",
      selected: false,
      customerInfo: {
        name: "Robert Davis",
        phone: "+1-555-0104",
        email: "robert.davis@email.com",
        address: "321 Cedar Lane",
        city: "Austin",
        state: "TX",
        zipcode: "73301",
        country: "USA",
      },
      orderNotes: "Corporate branding theme with company colors for all items",
      uploadedFiles: {
        linkImg: {
          name: "corporate-design.jpg",
          url: "/corporate-keychain.jpg",
        },
        linkThanksCard: { name: "corporate-thanks.pdf", url: "#" },
        linkFileDesign: { name: "corporate-design.ai", url: "#" },
      },
    },
    {
      id: 5,
      orderId: "ORD-S005",
      orderDate: "2024-01-24",
      customerName: "Maria Garcia",
      phone: "+1-555-0105",
      email: "maria.garcia@email.com",
      products: [
        {
          name: "Custom Acrylic Art",
          quantity: 5,
          price: 200.0,
          size: "20x30cm",
          accessory: "Wall Mount",
        },
      ],
      address: "987 Birch Street, Denver, CO 80201",
      shipTo: "Gallery: 147 Art District, Denver, CO 80202",
      status: "In Progress",
      totalAmount: "$200.00",
      timeCreated: "3 hours ago",
      selected: false,
      customerInfo: {
        name: "Maria Garcia",
        phone: "+1-555-0105",
        email: "maria.garcia@email.com",
        address: "987 Birch Street",
        city: "Denver",
        state: "CO",
        zipcode: "80201",
        country: "USA",
      },
      orderNotes: "High-quality finish for gallery display",
      uploadedFiles: {
        linkImg: { name: "art-reference.jpg", url: "/acrylic-art.jpg" },
        linkThanksCard: { name: "gallery-thanks.pdf", url: "#" },
        linkFileDesign: { name: "art-design.ai", url: "#" },
      },
    },
    {
      id: 6,
      orderId: "ORD-S006",
      orderDate: "2024-01-25",
      customerName: "David Kim",
      phone: "+1-555-0106",
      email: "david.kim@email.com",
      products: [
        {
          name: "Gaming Keychain Set",
          quantity: 100,
          price: 200.0,
          size: "4x4cm",
          accessory: "Key Ring",
        },
        {
          name: "LED Acrylic Stand",
          quantity: 25,
          price: 125.0,
          size: "12x15cm",
          accessory: "LED Base",
        },
        {
          name: "Custom Phone Case",
          quantity: 50,
          price: 75.0,
          size: "Various",
          accessory: "None",
        },
        {
          name: "Acrylic Coasters",
          quantity: 200,
          price: 50.0,
          size: "10x10cm",
          accessory: "Cork Base",
        },
      ],
      address: "555 Tech Boulevard, San Jose, CA 95101",
      shipTo: "Same as billing",
      status: "Pending Design",
      totalAmount: "$450.00",
      timeCreated: "1 hour ago",
      selected: false,
      customerInfo: {
        name: "David Kim",
        phone: "+1-555-0106",
        email: "david.kim@email.com",
        address: "555 Tech Boulevard",
        city: "San Jose",
        state: "CA",
        zipcode: "95101",
        country: "USA",
      },
      orderNotes: "Gaming theme with RGB lighting for all items",
      uploadedFiles: {
        linkImg: { name: "gaming-reference.jpg", url: "/gaming-keychain.jpg" },
        linkThanksCard: { name: "gaming-thanks.pdf", url: "#" },
        linkFileDesign: { name: "gaming-design.ai", url: "#" },
      },
    },
  ];

  const filteredOrders = orders.filter((order) => {
    const matchesSearch =
      order.orderId.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.customerName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.products.some((product) =>
        product.name.toLowerCase().includes(searchTerm.toLowerCase())
      );

    const matchesStatus =
      statusFilter === "all" ||
      order.status.toLowerCase().includes(statusFilter.toLowerCase());

    return matchesSearch && matchesStatus;
  });

  const handleSelectAll = () => {
    const newSelectAll = !selectAll;
    setSelectAll(newSelectAll);
    if (newSelectAll) {
      setSelectedOrders(filteredOrders.map((order) => order.id));
    } else {
      setSelectedOrders([]);
    }
  };

  const handleOrderSelect = (orderId) => {
    if (selectedOrders.includes(orderId)) {
      setSelectedOrders(selectedOrders.filter((id) => id !== orderId));
    } else {
      setSelectedOrders([...selectedOrders, orderId]);
    }
  };

  const handleFileImport = (event) => {
    const file = event.target.files[0];
    if (file) {
      console.log("File selected:", file.name);
      // Handle file import logic here
    }
  };

  const handleAssignToDesigner = () => {
    console.log("Assigning orders to designer:", selectedOrders);
  };

  const handleDownload = (file) => {
    console.log(`Downloading file: ${file.name}`);
    const link = document.createElement("a");
    link.href = file.url;
    link.download = file.name;
    link.click();
  };

  const handleViewDetails = (order) => {
    setSelectedOrder(order);
    setEditedOrder({ ...order });
    setIsEditMode(false);
  };

  const handleEditMode = () => {
    setIsEditMode(true);
  };

  const handleSaveUpdate = () => {
    console.log("Saving updated order:", editedOrder);
    // Here you would typically make an API call to save the changes
    setIsEditMode(false);
    setSelectedOrder(editedOrder);
    // Update the order in the orders array (in a real app, this would be done via API)
  };

  const handleCancelEdit = () => {
    setEditedOrder({ ...selectedOrder });
    setIsEditMode(false);
  };

  const handleFieldChange = (field, value) => {
    setEditedOrder((prev) => ({
      ...prev,
      [field]: value,
    }));
  };

  const handleCustomerInfoChange = (field, value) => {
    setEditedOrder((prev) => ({
      ...prev,
      customerInfo: {
        ...prev.customerInfo,
        [field]: value,
      },
    }));
  };

  const handleProductChange = (index, field, value) => {
    setEditedOrder((prev) => ({
      ...prev,
      products: prev.products.map((product, i) =>
        i === index ? { ...product, [field]: value } : product
      ),
    }));
  };

  const getStatusBadge = (status) => {
    switch (status) {
      case "Pending Design":
        return (
          <Badge className="bg-yellow-500 hover:bg-yellow-600 text-white">
            Pending Design
          </Badge>
        );
      case "In Progress":
        return (
          <Badge className="bg-blue-500 hover:bg-blue-600 text-white">
            In Progress
          </Badge>
        );
      case "Completed":
        return (
          <Badge className="bg-green-500 hover:bg-green-600 text-white">
            Completed
          </Badge>
        );
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  };

  return (
    <div className="flex h-screen bg-gray-50">
      <SellerSidebar
        currentPage={currentPage}
        setCurrentPage={setCurrentPage}
      />
      <div className="flex-1 flex flex-col overflow-hidden">
        <SellerHeader />
        <main className="flex-1 overflow-y-auto p-4 sm:p-6">
          <div className="space-y-6">
            {/* Welcome Header */}
            <div className="bg-white p-4 sm:p-6 rounded-lg border-2 border-blue-200 shadow-sm">
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <h1 className="text-lg sm:text-xl font-semibold text-blue-800">
                    Welcome back, Seller!
                  </h1>
                  <p className="text-sm sm:text-base text-blue-600 mt-1">
                    Manage your customer orders
                  </p>
                </div>
                <div className="mt-3 sm:mt-0">
                  <div className="flex items-center gap-2 text-sm text-blue-700">
                    <Package className="h-4 w-4" />
                    <span>{filteredOrders.length} orders</span>
                  </div>
                </div>
              </div>
            </div>

            {/* Controls */}
            <div className="bg-white p-4 sm:p-6 rounded-lg shadow-sm border border-gray-200">
              <div className="flex flex-col lg:flex-row gap-4 items-start lg:items-end justify-between">
                <div className="flex flex-col sm:flex-row gap-4 flex-1 w-full">
                  <div className="flex-1 max-w-xs">
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Search Orders
                    </label>
                    <div className="relative">
                      <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                      <Input
                        placeholder="Order ID, Customer, Product..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        className="pl-10 bg-white"
                      />
                    </div>
                  </div>
                  <div className="flex-1 max-w-xs">
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Filter by Status
                    </label>
                    <Select
                      value={statusFilter}
                      onValueChange={setStatusFilter}
                    >
                      <SelectTrigger className="bg-white">
                        <SelectValue placeholder="All Status" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="all">All Status</SelectItem>
                        <SelectItem value="pending">Pending Design</SelectItem>
                        <SelectItem value="progress">In Progress</SelectItem>
                        <SelectItem value="completed">Completed</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="flex flex-wrap gap-2 w-full lg:w-auto">
                  <Button
                    variant="outline"
                    onClick={() =>
                      document.getElementById("file-input").click()
                    }
                  >
                    <Upload className="h-4 w-4 mr-2" />
                    <span className="hidden sm:inline">Import file</span>
                    <span className="sm:hidden">Import</span>
                  </Button>
                  <input
                    id="file-input"
                    type="file"
                    className="hidden"
                    onChange={handleFileImport}
                    accept=".csv,.xlsx,.xls"
                  />
                  <Button
                    variant="outline"
                    onClick={() => setShowMakeManualModal(true)}
                  >
                    <FileText className="h-4 w-4 mr-2" />
                    <span className="hidden sm:inline">Make manual</span>
                    <span className="sm:hidden">Manual</span>
                  </Button>
                </div>
              </div>
            </div>

            {/* Action Buttons */}
            <div className="flex flex-wrap gap-2">
              <Button
                variant="outline"
                onClick={handleSelectAll}
                className="bg-white"
              >
                {selectAll ? "Deselect All" : "Select All"}
              </Button>

              <AlertDialog>
                <AlertDialogTrigger asChild>
                  <Button
                    variant="outline"
                    disabled={selectedOrders.length === 0}
                    className="bg-white"
                  >
                    Assign to Designer ({selectedOrders.length})
                  </Button>
                </AlertDialogTrigger>
                <AlertDialogContent>
                  <AlertDialogHeader>
                    <AlertDialogTitle>
                      Assign Orders to Designer
                    </AlertDialogTitle>
                    <AlertDialogDescription>
                      Are you sure you want to send {selectedOrders.length}{" "}
                      order(s) to the designer?
                    </AlertDialogDescription>
                  </AlertDialogHeader>
                  <AlertDialogFooter>
                    <AlertDialogCancel>No</AlertDialogCancel>
                    <AlertDialogAction onClick={handleAssignToDesigner}>
                      Yes
                    </AlertDialogAction>
                  </AlertDialogFooter>
                </AlertDialogContent>
              </AlertDialog>
            </div>

            <div className="bg-white rounded-lg shadow-sm border border-gray-200">
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow className="bg-gray-50">
                      <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                        Select
                      </TableHead>
                      <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                        Order ID
                      </TableHead>
                      <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                        Order Date
                      </TableHead>
                      <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                        Customer
                      </TableHead>
                      <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                        Phone
                      </TableHead>
                      <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                        Products
                      </TableHead>
                      <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                        Address
                      </TableHead>
                      <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                        Ship to
                      </TableHead>
                      <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                        Status
                      </TableHead>
                      <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                        Amount
                      </TableHead>
                      <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                        Time
                      </TableHead>
                      <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                        Actions
                      </TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {filteredOrders.map((order) => (
                      <TableRow
                        key={order.id}
                        className="hover:bg-gray-50 transition-colors"
                      >
                        <TableCell>
                          <Checkbox
                            checked={selectedOrders.includes(order.id)}
                            onCheckedChange={() => handleOrderSelect(order.id)}
                          />
                        </TableCell>
                        <TableCell className="font-medium text-gray-900 whitespace-nowrap">
                          {order.orderId}
                        </TableCell>
                        <TableCell className="text-gray-600 whitespace-nowrap">
                          {order.orderDate}
                        </TableCell>
                        <TableCell className="min-w-[200px]">
                          <div>
                            <div className="font-medium text-gray-900">
                              {order.customerName}
                            </div>
                            <div className="text-sm text-gray-500">
                              {order.email}
                            </div>
                          </div>
                        </TableCell>
                        <TableCell className="text-gray-600 whitespace-nowrap">
                          {order.phone}
                        </TableCell>
                        <TableCell className="text-gray-600 min-w-[200px]">
                          <div className="space-y-1">
                            {order.products
                              .slice(0, 2)
                              .map((product, index) => (
                                <div key={index} className="text-sm">
                                  {product.name} (x{product.quantity})
                                </div>
                              ))}
                            {order.products.length > 2 && (
                              <div className="text-xs text-gray-500">
                                +{order.products.length - 2} more items
                              </div>
                            )}
                          </div>
                        </TableCell>
                        <TableCell className="text-gray-600 max-w-[200px]">
                          <div className="truncate" title={order.address}>
                            {order.address}
                          </div>
                        </TableCell>
                        <TableCell className="text-gray-600 max-w-[200px]">
                          <div className="truncate" title={order.shipTo}>
                            {order.shipTo}
                          </div>
                        </TableCell>
                        <TableCell className="whitespace-nowrap">
                          {getStatusBadge(order.status)}
                        </TableCell>
                        <TableCell className="font-medium text-gray-900 whitespace-nowrap">
                          {order.totalAmount}
                        </TableCell>
                        <TableCell className="text-gray-600 whitespace-nowrap">
                          {order.timeCreated}
                        </TableCell>
                        <TableCell className="whitespace-nowrap">
                          <Dialog>
                            <DialogTrigger asChild>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => handleViewDetails(order)}
                                className="bg-transparent hover:bg-gray-50"
                              >
                                <Eye className="h-4 w-4 mr-1" />
                                <span className="hidden xl:inline">View</span>
                              </Button>
                            </DialogTrigger>
                            <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
                              <DialogHeader>
                                <DialogTitle className="flex items-center justify-between">
                                  <div className="flex items-center gap-2">
                                    <Package className="h-5 w-5" />
                                    Order Details - {editedOrder?.orderId}
                                  </div>
                                  {!isEditMode && (
                                    <Button
                                      variant="outline"
                                      size="sm"
                                      onClick={handleEditMode}
                                      className="bg-transparent hover:bg-gray-50"
                                    >
                                      <Edit className="h-4 w-4 mr-2" />
                                      Edit Order
                                    </Button>
                                  )}
                                </DialogTitle>
                              </DialogHeader>
                              {editedOrder && (
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
                                        {isEditMode ? (
                                          <Input
                                            value={
                                              editedOrder.customerInfo.name
                                            }
                                            onChange={(e) =>
                                              handleCustomerInfoChange(
                                                "name",
                                                e.target.value
                                              )
                                            }
                                            className="mt-1"
                                          />
                                        ) : (
                                          <p className="font-medium text-gray-900 mt-1">
                                            {editedOrder.customerInfo.name}
                                          </p>
                                        )}
                                      </div>
                                      <div>
                                        <Label className="text-sm text-gray-500 font-medium">
                                          Phone
                                        </Label>
                                        {isEditMode ? (
                                          <Input
                                            value={
                                              editedOrder.customerInfo.phone
                                            }
                                            onChange={(e) =>
                                              handleCustomerInfoChange(
                                                "phone",
                                                e.target.value
                                              )
                                            }
                                            className="mt-1"
                                          />
                                        ) : (
                                          <p className="font-medium text-gray-900 mt-1">
                                            {editedOrder.customerInfo.phone}
                                          </p>
                                        )}
                                      </div>
                                      <div>
                                        <Label className="text-sm text-gray-500 font-medium">
                                          Email
                                        </Label>
                                        {isEditMode ? (
                                          <Input
                                            value={
                                              editedOrder.customerInfo.email
                                            }
                                            onChange={(e) =>
                                              handleCustomerInfoChange(
                                                "email",
                                                e.target.value
                                              )
                                            }
                                            className="mt-1"
                                          />
                                        ) : (
                                          <p className="font-medium text-gray-900 mt-1">
                                            {editedOrder.customerInfo.email}
                                          </p>
                                        )}
                                      </div>
                                      <div>
                                        <Label className="text-sm text-gray-500 font-medium">
                                          Address
                                        </Label>
                                        {isEditMode ? (
                                          <Input
                                            value={`${editedOrder.customerInfo.address}, ${editedOrder.customerInfo.city}, ${editedOrder.customerInfo.state} ${editedOrder.customerInfo.zipcode}`}
                                            onChange={(e) =>
                                              handleCustomerInfoChange(
                                                "address",
                                                e.target.value
                                              )
                                            }
                                            className="mt-1"
                                          />
                                        ) : (
                                          <p className="font-medium text-gray-900 mt-1">
                                            {editedOrder.customerInfo.address},{" "}
                                            {editedOrder.customerInfo.city},{" "}
                                            {editedOrder.customerInfo.state}{" "}
                                            {editedOrder.customerInfo.zipcode}
                                          </p>
                                        )}
                                      </div>
                                    </div>
                                  </div>

                                  {/* Product Details */}
                                  <div>
                                    <h3 className="font-semibold text-lg mb-3 text-gray-900">
                                      Product Details
                                    </h3>
                                    <div className="space-y-4">
                                      {editedOrder.products.map(
                                        (product, index) => (
                                          <div
                                            key={index}
                                            className="border border-gray-200 rounded-lg p-4"
                                          >
                                            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                                              <div>
                                                <Label className="text-sm text-gray-500 font-medium">
                                                  Product
                                                </Label>
                                                {isEditMode ? (
                                                  <Input
                                                    value={product.name}
                                                    onChange={(e) =>
                                                      handleProductChange(
                                                        index,
                                                        "name",
                                                        e.target.value
                                                      )
                                                    }
                                                    className="mt-1"
                                                  />
                                                ) : (
                                                  <p className="font-medium text-gray-900 mt-1">
                                                    {product.name}
                                                  </p>
                                                )}
                                              </div>
                                              <div>
                                                <Label className="text-sm text-gray-500 font-medium">
                                                  Size
                                                </Label>
                                                {isEditMode ? (
                                                  <Input
                                                    value={product.size}
                                                    onChange={(e) =>
                                                      handleProductChange(
                                                        index,
                                                        "size",
                                                        e.target.value
                                                      )
                                                    }
                                                    className="mt-1"
                                                  />
                                                ) : (
                                                  <p className="font-medium text-gray-900 mt-1">
                                                    {product.size}
                                                  </p>
                                                )}
                                              </div>
                                              <div>
                                                <Label className="text-sm text-gray-500 font-medium">
                                                  Quantity
                                                </Label>
                                                {isEditMode ? (
                                                  <Input
                                                    type="number"
                                                    value={product.quantity}
                                                    onChange={(e) =>
                                                      handleProductChange(
                                                        index,
                                                        "quantity",
                                                        Number.parseInt(
                                                          e.target.value
                                                        )
                                                      )
                                                    }
                                                    className="mt-1"
                                                  />
                                                ) : (
                                                  <p className="font-medium text-gray-900 mt-1">
                                                    {product.quantity}
                                                  </p>
                                                )}
                                              </div>
                                              <div>
                                                <Label className="text-sm text-gray-500 font-medium">
                                                  Accessory
                                                </Label>
                                                {isEditMode ? (
                                                  <Input
                                                    value={product.accessory}
                                                    onChange={(e) =>
                                                      handleProductChange(
                                                        index,
                                                        "accessory",
                                                        e.target.value
                                                      )
                                                    }
                                                    className="mt-1"
                                                  />
                                                ) : (
                                                  <p className="font-medium text-gray-900 mt-1">
                                                    {product.accessory}
                                                  </p>
                                                )}
                                              </div>
                                            </div>
                                          </div>
                                        )
                                      )}
                                    </div>
                                    {editedOrder.orderNotes && (
                                      <div className="mt-4 p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                                        <Label className="text-sm text-yellow-700 font-medium">
                                          Order Notes
                                        </Label>
                                        {isEditMode ? (
                                          <Textarea
                                            value={editedOrder.orderNotes}
                                            onChange={(e) =>
                                              handleFieldChange(
                                                "orderNotes",
                                                e.target.value
                                              )
                                            }
                                            className="mt-1 bg-white"
                                            rows={3}
                                          />
                                        ) : (
                                          <p className="text-yellow-800 mt-1">
                                            {editedOrder.orderNotes}
                                          </p>
                                        )}
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
                                        {isEditMode ? (
                                          <Select
                                            value={editedOrder.status}
                                            onValueChange={(value) =>
                                              handleFieldChange("status", value)
                                            }
                                          >
                                            <SelectTrigger className="mt-1 bg-white">
                                              <SelectValue />
                                            </SelectTrigger>
                                            <SelectContent>
                                              <SelectItem value="Pending Design">
                                                Pending Design
                                              </SelectItem>
                                              <SelectItem value="In Progress">
                                                In Progress
                                              </SelectItem>
                                              <SelectItem value="Completed">
                                                Completed
                                              </SelectItem>
                                            </SelectContent>
                                          </Select>
                                        ) : (
                                          <div className="mt-1">
                                            {getStatusBadge(editedOrder.status)}
                                          </div>
                                        )}
                                      </div>
                                      <div>
                                        <Label className="text-sm text-gray-500 font-medium">
                                          Order Date
                                        </Label>
                                        <p className="font-medium text-gray-900 mt-1">
                                          {editedOrder.orderDate}
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
                                      <div className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow">
                                        <Label className="text-sm text-gray-500 font-medium">
                                          Reference Image
                                        </Label>
                                        <div className="mt-2">
                                          <img
                                            src={
                                              editedOrder.uploadedFiles.linkImg
                                                .url || "/placeholder.svg"
                                            }
                                            alt="Reference"
                                            className="w-full h-32 object-cover rounded border"
                                          />
                                          <p className="text-sm mt-2 text-gray-600">
                                            {
                                              editedOrder.uploadedFiles.linkImg
                                                .name
                                            }
                                          </p>
                                          <Button
                                            variant="outline"
                                            size="sm"
                                            className="mt-2 w-full bg-transparent hover:bg-gray-50"
                                            onClick={() =>
                                              handleDownload(
                                                editedOrder.uploadedFiles
                                                  .linkImg
                                              )
                                            }
                                          >
                                            <Download className="h-4 w-4 mr-2" />
                                            Download
                                          </Button>
                                        </div>
                                      </div>

                                      <div className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow">
                                        <Label className="text-sm text-gray-500 font-medium">
                                          Thanks Card
                                        </Label>
                                        <div className="mt-2">
                                          <div className="w-full h-32 bg-gray-100 rounded border flex items-center justify-center">
                                            <QrCode className="h-8 w-8 text-gray-400" />
                                          </div>
                                          <p className="text-sm mt-2 text-gray-600">
                                            {
                                              editedOrder.uploadedFiles
                                                .linkThanksCard.name
                                            }
                                          </p>
                                          <Button
                                            variant="outline"
                                            size="sm"
                                            className="mt-2 w-full bg-transparent hover:bg-gray-50"
                                            onClick={() =>
                                              handleDownload(
                                                editedOrder.uploadedFiles
                                                  .linkThanksCard
                                              )
                                            }
                                          >
                                            <Download className="h-4 w-4 mr-2" />
                                            Download
                                          </Button>
                                        </div>
                                      </div>

                                      <div className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow">
                                        <Label className="text-sm text-gray-500 font-medium">
                                          Design File
                                        </Label>
                                        <div className="mt-2">
                                          <div className="w-full h-32 bg-gray-100 rounded border flex items-center justify-center">
                                            <QrCode className="h-8 w-8 text-gray-400" />
                                          </div>
                                          <p className="text-sm mt-2 text-gray-600">
                                            {
                                              editedOrder.uploadedFiles
                                                .linkFileDesign.name
                                            }
                                          </p>
                                          <Button
                                            variant="outline"
                                            size="sm"
                                            className="mt-2 w-full bg-transparent hover:bg-gray-50"
                                            onClick={() =>
                                              handleDownload(
                                                editedOrder.uploadedFiles
                                                  .linkFileDesign
                                              )
                                            }
                                          >
                                            <Download className="h-4 w-4 mr-2" />
                                            Download
                                          </Button>
                                        </div>
                                      </div>
                                    </div>
                                  </div>

                                  {/* QR Code Section */}
                                  <div className="bg-gray-50 p-4 rounded-lg">
                                    <h4 className="font-medium mb-2 text-gray-900">
                                      QR Code for Order {editedOrder.orderId}
                                    </h4>
                                    <div className="w-32 h-32 bg-white border-2 border-gray-300 rounded flex items-center justify-center">
                                      <QrCode className="h-16 w-16 text-gray-400" />
                                    </div>
                                  </div>
                                </div>
                              )}
                              {isEditMode && (
                                <DialogFooter className="flex gap-2">
                                  <Button
                                    variant="outline"
                                    onClick={handleCancelEdit}
                                  >
                                    Cancel
                                  </Button>
                                  <Button
                                    onClick={handleSaveUpdate}
                                    className="bg-blue-600 hover:bg-blue-700"
                                  >
                                    <Save className="h-4 w-4 mr-2" />
                                    Save Changes
                                  </Button>
                                </DialogFooter>
                              )}
                            </DialogContent>
                          </Dialog>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </div>
          </div>
        </main>
      </div>

      <MakeManualModal
        isOpen={showMakeManualModal}
        onClose={() => setShowMakeManualModal(false)}
      />
    </div>
  );
}
