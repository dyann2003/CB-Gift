"use client";

import { useState } from "react";
import RoleSidebar from "@/components/layout/shared/role-sidebar";
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
import {
  Search,
  Play,
  CheckCircle,
  Send,
  Eye,
  X,
  QrCode,
  Download,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";
import ImageIcon from "@/components/ui/image-icon";

export default function StaffManageOrder() {
  const [currentPage, setCurrentPage] = useState("manage-order");
  const [searchTerm, setSearchTerm] = useState("");
  const [filterStatus, setFilterStatus] = useState("all");
  const [selectedOrders, setSelectedOrders] = useState([]);
  const [selectAll, setSelectAll] = useState(false);
  const [selectedOrderDetails, setSelectedOrderDetails] = useState(null);
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [confirmAction, setConfirmAction] = useState(null);
  const [dateFilter, setDateFilter] = useState("");
  const [page, setPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(10);

  const orders = [
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
      designer: "Alice Smith",
      dateApproved: "2024-01-15",
      status: "qc_approved",
      priority: "high",
      qrCode: "QR-ORD-001-2024",
      customerEmail: "john.doe@email.com",
      customerPhone: "+1234567890",
      specifications:
        "5cm x 3cm keychain with transparent acrylic and full color print, 10x15cm stand with clear acrylic",
      specialInstructions:
        "Please ensure colors are vibrant and stand has stable base",
      deliveryDate: "2024-01-25",
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
        { name: "design-specs.pdf", url: "#", type: "pdf" },
      ],
      originalFiles: [
        {
          name: "customer-reference.jpg",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
        { name: "logo-file.png", url: "/acrylic-stand.jpg", type: "image" },
      ],
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
      ],
      designer: "Bob Johnson",
      dateApproved: "2024-01-14",
      status: "in_production",
      priority: "medium",
      qrCode: "QR-ORD-002-2024",
      customerEmail: "jane.smith@email.com",
      customerPhone: "+1234567891",
      specifications:
        "10cm x 15cm clear acrylic stand with logo, 3cm diameter double-sided charm",
      specialInstructions:
        "Ensure stable base for stand, cute design for charm",
      deliveryDate: "2024-01-28",
      designFiles: [
        { name: "stand-design.png", url: "/acrylic-stand.jpg", type: "image" },
        { name: "charm-design.png", url: "/acrylic-charm.jpg", type: "image" },
      ],
      originalFiles: [
        { name: "logo-file.png", url: "/acrylic-stand.jpg", type: "image" },
        {
          name: "charm-reference.jpg",
          url: "/acrylic-charm.jpg",
          type: "image",
        },
      ],
    },
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
          quantity: 75,
          size: "4x6cm",
          accessory: "Key Ring",
        },
        {
          name: "Acrylic Stand",
          quantity: 50,
          size: "8x12cm",
          accessory: "None",
        },
      ],
      designer: "Alice Smith",
      dateApproved: "2024-01-13",
      status: "production_complete",
      priority: "low",
      qrCode: "QR-ORD-003-2024",
      customerEmail: "mike.brown@email.com",
      customerPhone: "+1234567892",
      specifications:
        "3cm diameter double-sided charm, 4x6cm keychain with UV print, 8x12cm display stand",
      specialInstructions:
        "Pack individually, handle with care - delicate designs",
      deliveryDate: "2024-01-30",
      designFiles: [
        {
          name: "charm-design-front.png",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
        {
          name: "charm-design-back.png",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
        {
          name: "keychain-design.png",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
        { name: "stand-design.png", url: "/acrylic-stand.jpg", type: "image" },
      ],
      originalFiles: [
        { name: "artwork.ai", url: "#", type: "file" },
        { name: "reference-images.zip", url: "#", type: "file" },
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
      ],
      designer: "Bob Johnson",
      dateApproved: "2024-01-12",
      status: "qc_approved",
      priority: "high",
      qrCode: "QR-ORD-004-2024",
      customerEmail: "sarah.wilson@email.com",
      customerPhone: "+1234567893",
      specifications: "4cm x 6cm, frosted acrylic with UV print",
      specialInstructions: "Handle with care - delicate design",
      deliveryDate: "2024-01-26",
      designFiles: [
        {
          name: "keychain-final.png",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
      ],
      originalFiles: [
        {
          name: "photo-reference.jpg",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
      ],
    },
    {
      id: "ORD-005",
      customer: "David Lee",
      products: [
        {
          name: "Acrylic Stand",
          quantity: 30,
          size: "12x18cm",
          accessory: "None",
        },
      ],
      designer: "Alice Smith",
      dateApproved: "2024-01-11",
      status: "in_production",
      priority: "medium",
      qrCode: "QR-ORD-005-2024",
      customerEmail: "david.lee@email.com",
      customerPhone: "+1234567894",
      specifications: "12cm x 18cm premium acrylic stand with UV print",
      specialInstructions: "Ensure high quality finish",
      deliveryDate: "2024-01-27",
      designFiles: [
        { name: "stand-premium.png", url: "/acrylic-stand.jpg", type: "image" },
      ],
      originalFiles: [
        { name: "artwork.png", url: "/acrylic-stand.jpg", type: "image" },
      ],
    },
    {
      id: "ORD-006",
      customer: "Emily Chen",
      products: [
        {
          name: "Acrylic Charm",
          quantity: 150,
          size: "4x4cm",
          accessory: "Phone Strap",
        },
        {
          name: "Acrylic Keychain",
          quantity: 100,
          size: "5x7cm",
          accessory: "Key Ring",
        },
      ],
      designer: "Bob Johnson",
      dateApproved: "2024-01-10",
      status: "production_complete",
      priority: "high",
      qrCode: "QR-ORD-006-2024",
      customerEmail: "emily.chen@email.com",
      customerPhone: "+1234567895",
      specifications:
        "4cm charm with double-sided print, 5x7cm keychain with holographic effect",
      specialInstructions: "Pack separately by product type",
      deliveryDate: "2024-01-24",
      designFiles: [
        { name: "charm-design.png", url: "/acrylic-charm.jpg", type: "image" },
        {
          name: "keychain-holo.png",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
      ],
      originalFiles: [
        { name: "reference.jpg", url: "/acrylic-charm.jpg", type: "image" },
        { name: "logo.png", url: "/acrylic-keychain.jpg", type: "image" },
      ],
    },
    {
      id: "ORD-007",
      customer: "Robert Taylor",
      products: [
        {
          name: "Acrylic Stand",
          quantity: 40,
          size: "15x20cm",
          accessory: "None",
        },
      ],
      designer: "Alice Smith",
      dateApproved: "2024-01-09",
      status: "qc_approved",
      priority: "low",
      qrCode: "QR-ORD-007-2024",
      customerEmail: "robert.taylor@email.com",
      customerPhone: "+1234567896",
      specifications: "15x20cm large display stand with clear acrylic",
      specialInstructions: "Extra padding for shipping",
      deliveryDate: "2024-01-29",
      designFiles: [
        { name: "large-stand.png", url: "/acrylic-stand.jpg", type: "image" },
      ],
      originalFiles: [{ name: "design-file.ai", url: "#", type: "file" }],
    },
    {
      id: "ORD-008",
      customer: "Lisa Anderson",
      products: [
        {
          name: "Acrylic Keychain",
          quantity: 200,
          size: "6x8cm",
          accessory: "Key Ring",
        },
        {
          name: "Acrylic Charm",
          quantity: 200,
          size: "3x3cm",
          accessory: "Phone Strap",
        },
        {
          name: "Acrylic Stand",
          quantity: 100,
          size: "10x15cm",
          accessory: "None",
        },
      ],
      designer: "Bob Johnson",
      dateApproved: "2024-01-08",
      status: "in_production",
      priority: "high",
      qrCode: "QR-ORD-008-2024",
      customerEmail: "lisa.anderson@email.com",
      customerPhone: "+1234567897",
      specifications: "Bulk order with custom designs for each product type",
      specialInstructions: "Quality check each piece individually",
      deliveryDate: "2024-01-23",
      designFiles: [
        {
          name: "keychain-bulk.png",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
        { name: "charm-bulk.png", url: "/acrylic-charm.jpg", type: "image" },
        { name: "stand-bulk.png", url: "/acrylic-stand.jpg", type: "image" },
      ],
      originalFiles: [
        { name: "designs.zip", url: "#", type: "file" },
        { name: "specifications.pdf", url: "#", type: "file" },
      ],
    },
    {
      id: "ORD-009",
      customer: "Michael White",
      products: [
        {
          name: "Acrylic Charm",
          quantity: 80,
          size: "3.5x3.5cm",
          accessory: "Phone Strap",
        },
      ],
      designer: "Alice Smith",
      dateApproved: "2024-01-07",
      status: "production_complete",
      priority: "medium",
      qrCode: "QR-ORD-009-2024",
      customerEmail: "michael.white@email.com",
      customerPhone: "+1234567898",
      specifications: "3.5cm square charm with gradient print",
      specialInstructions: "Ensure color accuracy",
      deliveryDate: "2024-01-26",
      designFiles: [
        {
          name: "gradient-charm.png",
          url: "/acrylic-charm.jpg",
          type: "image",
        },
      ],
      originalFiles: [
        {
          name: "color-reference.jpg",
          url: "/acrylic-charm.jpg",
          type: "image",
        },
      ],
    },
    {
      id: "ORD-010",
      customer: "Jennifer Martinez",
      products: [
        {
          name: "Acrylic Keychain",
          quantity: 120,
          size: "5x5cm",
          accessory: "Key Ring",
        },
        {
          name: "Acrylic Stand",
          quantity: 60,
          size: "8x12cm",
          accessory: "None",
        },
      ],
      designer: "Bob Johnson",
      dateApproved: "2024-01-06",
      status: "qc_approved",
      priority: "high",
      qrCode: "QR-ORD-010-2024",
      customerEmail: "jennifer.martinez@email.com",
      customerPhone: "+1234567899",
      specifications:
        "5cm keychain with metallic finish, 8x12cm stand with matte coating",
      specialInstructions: "Test metallic finish quality",
      deliveryDate: "2024-01-25",
      designFiles: [
        {
          name: "metallic-keychain.png",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
        { name: "matte-stand.png", url: "/acrylic-stand.jpg", type: "image" },
      ],
      originalFiles: [
        { name: "design-v1.png", url: "/acrylic-keychain.jpg", type: "image" },
        { name: "design-v2.png", url: "/acrylic-stand.jpg", type: "image" },
      ],
    },
    {
      id: "ORD-011",
      customer: "Christopher Garcia",
      products: [
        {
          name: "Acrylic Stand",
          quantity: 45,
          size: "10x15cm",
          accessory: "None",
        },
      ],
      designer: "Alice Smith",
      dateApproved: "2024-01-05",
      status: "in_production",
      priority: "low",
      qrCode: "QR-ORD-011-2024",
      customerEmail: "christopher.garcia@email.com",
      customerPhone: "+1234567800",
      specifications: "Standard 10x15cm stand with glossy finish",
      specialInstructions: "Standard packaging",
      deliveryDate: "2024-01-28",
      designFiles: [
        {
          name: "standard-stand.png",
          url: "/acrylic-stand.jpg",
          type: "image",
        },
      ],
      originalFiles: [
        { name: "artwork.png", url: "/acrylic-stand.jpg", type: "image" },
      ],
    },
    {
      id: "ORD-012",
      customer: "Amanda Rodriguez",
      products: [
        {
          name: "Acrylic Charm",
          quantity: 180,
          size: "4x4cm",
          accessory: "Phone Strap",
        },
        {
          name: "Acrylic Keychain",
          quantity: 90,
          size: "6x6cm",
          accessory: "Key Ring",
        },
      ],
      designer: "Bob Johnson",
      dateApproved: "2024-01-04",
      status: "production_complete",
      priority: "medium",
      qrCode: "QR-ORD-012-2024",
      customerEmail: "amanda.rodriguez@email.com",
      customerPhone: "+1234567801",
      specifications:
        "4cm charm with glitter effect, 6cm keychain with UV coating",
      specialInstructions: "Handle glitter pieces with care",
      deliveryDate: "2024-01-27",
      designFiles: [
        { name: "glitter-charm.png", url: "/acrylic-charm.jpg", type: "image" },
        {
          name: "uv-keychain.png",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
      ],
      originalFiles: [
        {
          name: "reference-charm.jpg",
          url: "/acrylic-charm.jpg",
          type: "image",
        },
        {
          name: "reference-keychain.jpg",
          url: "/acrylic-keychain.jpg",
          type: "image",
        },
      ],
    },
  ];

  const filteredOrders = orders.filter((order) => {
    const matchesSearch =
      order.id.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.customer.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.products.some((product) =>
        product.name.toLowerCase().includes(searchTerm.toLowerCase())
      );
    const matchesFilter =
      filterStatus === "all" || order.status === filterStatus;
    const matchesDate = !dateFilter || order.dateApproved === dateFilter;
    return matchesSearch && matchesFilter && matchesDate;
  });

  const totalPages = Math.ceil(filteredOrders.length / itemsPerPage);
  const startIndex = (page - 1) * itemsPerPage;
  const endIndex = startIndex + itemsPerPage;
  const paginatedOrders = filteredOrders.slice(startIndex, endIndex);

  const handleSearchChange = (value) => {
    setSearchTerm(value);
    setPage(1);
  };

  const handleFilterChange = (value) => {
    setFilterStatus(value);
    setPage(1);
  };

  const handleDateFilterChange = (value) => {
    setDateFilter(value);
    setPage(1);
  };

  const handleItemsPerPageChange = (value) => {
    setItemsPerPage(Number(value));
    setPage(1);
  };

  const getStatusBadge = (status) => {
    switch (status) {
      case "qc_approved":
        return (
          <Badge className="bg-green-100 text-green-800">QC Approved</Badge>
        );
      case "in_production":
        return (
          <Badge className="bg-blue-100 text-blue-800">In Production</Badge>
        );
      case "production_complete":
        return (
          <Badge className="bg-purple-100 text-purple-800">
            Production Complete
          </Badge>
        );
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  };

  const getPriorityBadge = (priority) => {
    switch (priority) {
      case "high":
        return <Badge variant="destructive">High</Badge>;
      case "medium":
        return <Badge className="bg-yellow-100 text-yellow-800">Medium</Badge>;
      case "low":
        return <Badge className="bg-gray-100 text-gray-800">Low</Badge>;
      default:
        return <Badge variant="secondary">{priority}</Badge>;
    }
  };

  const handleSelectOrder = (orderId) => {
    setSelectedOrders((prev) =>
      prev.includes(orderId)
        ? prev.filter((id) => id !== orderId)
        : [...prev, orderId]
    );
  };

  const handleSelectAll = () => {
    if (selectAll) {
      setSelectedOrders([]);
    } else {
      setSelectedOrders(paginatedOrders.map((order) => order.id));
    }
    setSelectAll(!selectAll);
  };

  const handleStartProduction = () => {
    if (selectedOrders.length === 0) {
      setConfirmAction({
        type: "error",
        title: "No Orders Selected",
        message: "Please select orders to start production",
      });
      setShowConfirmDialog(true);
      return;
    }
    const selectedOrdersData = orders.filter((order) =>
      selectedOrders.includes(order.id)
    );
    const eligibleOrders = selectedOrdersData.filter(
      (order) => order.status === "qc_approved"
    );

    if (eligibleOrders.length === 0) {
      setConfirmAction({
        type: "error",
        title: "No Eligible Orders",
        message:
          "No eligible orders selected. Only QC Approved orders can be moved to production.",
      });
      setShowConfirmDialog(true);
      return;
    }

    setConfirmAction({
      type: "start_production",
      title: "Start Production",
      message: `Start production for ${eligibleOrders.length} order${
        eligibleOrders.length > 1 ? "s" : ""
      }?`,
      count: eligibleOrders.length,
    });
    setShowConfirmDialog(true);
  };

  const handleMarkDone = () => {
    if (selectedOrders.length === 0) {
      setConfirmAction({
        type: "error",
        title: "No Orders Selected",
        message: "Please select orders to mark as done",
      });
      setShowConfirmDialog(true);
      return;
    }
    const selectedOrdersData = orders.filter((order) =>
      selectedOrders.includes(order.id)
    );
    const eligibleOrders = selectedOrdersData.filter(
      (order) => order.status === "in_production"
    );

    if (eligibleOrders.length === 0) {
      setConfirmAction({
        type: "error",
        title: "No Eligible Orders",
        message:
          "No eligible orders selected. Only In Production orders can be marked as done.",
      });
      setShowConfirmDialog(true);
      return;
    }

    setConfirmAction({
      type: "mark_done",
      title: "Mark as Done",
      message: `Mark ${eligibleOrders.length} order${
        eligibleOrders.length > 1 ? "s" : ""
      } as production complete?`,
      count: eligibleOrders.length,
    });
    setShowConfirmDialog(true);
  };

  const handleAssignToQC = () => {
    if (selectedOrders.length === 0) {
      setConfirmAction({
        type: "error",
        title: "No Orders Selected",
        message: "Please select orders to assign to QC",
      });
      setShowConfirmDialog(true);
      return;
    }
    const selectedOrdersData = orders.filter((order) =>
      selectedOrders.includes(order.id)
    );
    const eligibleOrders = selectedOrdersData.filter(
      (order) => order.status === "production_complete"
    );

    if (eligibleOrders.length === 0) {
      setConfirmAction({
        type: "error",
        title: "No Eligible Orders",
        message:
          "No eligible orders selected. Only Production Complete orders can be assigned to QC.",
      });
      setShowConfirmDialog(true);
      return;
    }

    setConfirmAction({
      type: "assign_qc",
      title: "Assign to QC",
      message: `Assign ${eligibleOrders.length} order${
        eligibleOrders.length > 1 ? "s" : ""
      } to QC for final inspection?`,
      count: eligibleOrders.length,
    });
    setShowConfirmDialog(true);
  };

  const handleConfirmAction = () => {
    if (confirmAction?.type === "start_production") {
      console.log(`Starting production for ${confirmAction.count} orders`);
    } else if (confirmAction?.type === "mark_done") {
      console.log(
        `Marking ${confirmAction.count} orders as production complete`
      );
    } else if (confirmAction?.type === "assign_qc") {
      console.log(`Assigning ${confirmAction.count} orders to QC`);
    }

    setSelectedOrders([]);
    setSelectAll(false);
    setShowConfirmDialog(false);
    setConfirmAction(null);
  };

  const handleDownload = (fileUrl, fileName) => {
    console.log(`Downloading file: ${fileName} from ${fileUrl}`);
    const link = document.createElement("a");
    link.href = fileUrl;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  return (
    <div className="flex h-screen bg-gray-50">
      <RoleSidebar
        currentRole="staff"
        currentPage={currentPage}
        setCurrentPage={setCurrentPage}
      />
      <div className="flex-1 flex flex-col overflow-hidden">
        <SellerHeader />
        <main className="flex-1 overflow-y-auto p-4 md:p-6">
          <div className="space-y-6">
            {/* Header */}
            <div className="bg-white p-4 rounded-lg border-2 border-blue-200">
              <h1 className="text-xl font-semibold text-gray-900">
                Manage Order
              </h1>
              <p className="text-gray-600 mt-1">
                Manage production orders and status updates
              </p>
            </div>

            {/* Search and Filter */}
            <div className="bg-white p-4 rounded-lg shadow">
              <div className="flex flex-col md:flex-row gap-4 items-stretch md:items-center">
                <div className="flex-1 relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                  <Input
                    placeholder="Search by Order ID, Customer, or Product..."
                    value={searchTerm}
                    onChange={(e) => handleSearchChange(e.target.value)}
                    className="pl-10"
                  />
                </div>
                <select
                  value={filterStatus}
                  onChange={(e) => handleFilterChange(e.target.value)}
                  className="px-3 py-2 border border-gray-300 rounded-md w-full md:w-auto"
                >
                  <option value="all">All Status</option>
                  <option value="qc_approved">QC Approved</option>
                  <option value="in_production">In Production</option>
                  <option value="production_complete">
                    Production Complete
                  </option>
                </select>
                <Input
                  type="date"
                  value={dateFilter}
                  onChange={(e) => handleDateFilterChange(e.target.value)}
                  className="w-full md:w-48"
                  placeholder="Filter by date"
                />
              </div>
            </div>

            {/* Action Buttons */}
            {selectedOrders.length > 0 && (
              <div className="bg-white p-4 rounded-lg shadow">
                <div className="flex flex-col sm:flex-row gap-3 items-stretch sm:items-center">
                  <span className="text-sm text-gray-600">
                    {selectedOrders.length} orders selected
                  </span>
                  <Button
                    onClick={handleStartProduction}
                    className="bg-blue-600 hover:bg-blue-700 w-full sm:w-auto"
                  >
                    <Play className="h-4 w-4 mr-1" />
                    Start Production
                  </Button>
                  <Button
                    onClick={handleMarkDone}
                    className="bg-green-600 hover:bg-green-700 w-full sm:w-auto"
                  >
                    <CheckCircle className="h-4 w-4 mr-1" />
                    Mark Done
                  </Button>
                  <Button
                    onClick={handleAssignToQC}
                    className="bg-purple-600 hover:bg-purple-700 w-full sm:w-auto"
                  >
                    <Send className="h-4 w-4 mr-1" />
                    Assign to QC
                  </Button>
                </div>
              </div>
            )}

            {/* Orders Table */}
            <div className="bg-white rounded-lg shadow overflow-hidden">
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-4 md:px-6 py-3 text-left">
                        <Checkbox
                          checked={selectAll}
                          onCheckedChange={handleSelectAll}
                          aria-label="Select all orders"
                        />
                      </th>
                      <th className="px-4 md:px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">
                        Order ID
                      </th>
                      <th className="px-4 md:px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">
                        Customer
                      </th>
                      <th className="px-4 md:px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">
                        Products
                      </th>
                      <th className="px-4 md:px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">
                        Designer
                      </th>
                      <th className="px-4 md:px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">
                        Date Approved
                      </th>
                      <th className="px-4 md:px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">
                        Priority
                      </th>
                      <th className="px-4 md:px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">
                        Status
                      </th>
                      <th className="px-4 md:px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider whitespace-nowrap">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {paginatedOrders.map((order) => (
                      <tr key={order.id} className="hover:bg-gray-50">
                        <td className="px-4 md:px-6 py-4 whitespace-nowrap">
                          <Checkbox
                            checked={selectedOrders.includes(order.id)}
                            onCheckedChange={() => handleSelectOrder(order.id)}
                            aria-label={`Select order ${order.id}`}
                          />
                        </td>
                        <td className="px-4 md:px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                          {order.id}
                        </td>
                        <td className="px-4 md:px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {order.customer}
                        </td>
                        <td className="px-4 md:px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          <div className="max-w-xs truncate">
                            {order.products
                              .map((product) => product.name)
                              .join(", ")}
                          </div>
                        </td>
                        <td className="px-4 md:px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {order.designer}
                        </td>
                        <td className="px-4 md:px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {order.dateApproved}
                        </td>
                        <td className="px-4 md:px-6 py-4 whitespace-nowrap">
                          {getPriorityBadge(order.priority)}
                        </td>
                        <td className="px-4 md:px-6 py-4 whitespace-nowrap">
                          {getStatusBadge(order.status)}
                        </td>
                        <td className="px-4 md:px-6 py-4 whitespace-nowrap text-sm font-medium">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => setSelectedOrderDetails(order)}
                          >
                            <Eye className="h-4 w-4 mr-1" />
                            <span className="hidden sm:inline">
                              View Details
                            </span>
                            <span className="sm:hidden">View</span>
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="bg-gray-50 px-4 py-3 border-t border-gray-200 sm:px-6">
                <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
                  <div className="flex items-center gap-2">
                    <span className="text-sm text-gray-700">Show</span>
                    <select
                      value={itemsPerPage}
                      onChange={(e) => handleItemsPerPageChange(e.target.value)}
                      className="px-2 py-1 border border-gray-300 rounded-md text-sm"
                    >
                      <option value="5">5</option>
                      <option value="10">10</option>
                      <option value="20">20</option>
                      <option value="50">50</option>
                    </select>
                    <span className="text-sm text-gray-700">per page</span>
                  </div>

                  <div className="flex items-center gap-2">
                    <span className="text-sm text-gray-700">
                      Showing {startIndex + 1} to{" "}
                      {Math.min(endIndex, filteredOrders.length)} of{" "}
                      {filteredOrders.length} results
                    </span>
                  </div>

                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setPage(page - 1)}
                      disabled={page === 1}
                      className="disabled:opacity-50"
                    >
                      <ChevronLeft className="h-4 w-4" />
                      <span className="hidden sm:inline ml-1">Previous</span>
                    </Button>

                    <div className="flex items-center gap-1">
                      {Array.from(
                        { length: Math.min(5, totalPages) },
                        (_, i) => {
                          let pageNum;
                          if (totalPages <= 5) {
                            pageNum = i + 1;
                          } else if (page <= 3) {
                            pageNum = i + 1;
                          } else if (page >= totalPages - 2) {
                            pageNum = totalPages - 4 + i;
                          } else {
                            pageNum = page - 2 + i;
                          }

                          return (
                            <Button
                              key={pageNum}
                              variant={page === pageNum ? "default" : "outline"}
                              size="sm"
                              onClick={() => setPage(pageNum)}
                              className="w-8 h-8 p-0"
                            >
                              {pageNum}
                            </Button>
                          );
                        }
                      )}
                    </div>

                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setPage(page + 1)}
                      disabled={page === totalPages}
                      className="disabled:opacity-50"
                    >
                      <span className="hidden sm:inline mr-1">Next</span>
                      <ChevronRight className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </main>
      </div>

      {/* Order Details Modal */}
      {selectedOrderDetails && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg max-w-6xl w-full max-h-[90vh] overflow-y-auto">
            <div className="p-4 md:p-6">
              <div className="flex justify-between items-center mb-6">
                <h2 className="text-xl md:text-2xl font-semibold">
                  Order Details - {selectedOrderDetails.id}
                </h2>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setSelectedOrderDetails(null)}
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>

              <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 md:gap-8">
                {/* Left Column - Order Information */}
                <div className="space-y-6">
                  {/* Customer Information */}
                  <div className="bg-gray-50 p-4 rounded-lg">
                    <h3 className="font-semibold text-lg mb-3">
                      Customer Information
                    </h3>
                    <div className="space-y-2">
                      <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                        <span className="text-gray-600">Name:</span>
                        <span className="font-medium">
                          {selectedOrderDetails.customer}
                        </span>
                      </div>
                      <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                        <span className="text-gray-600">Email:</span>
                        <span className="font-medium break-all">
                          {selectedOrderDetails.customerEmail}
                        </span>
                      </div>
                      <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                        <span className="text-gray-600">Phone:</span>
                        <span className="font-medium">
                          {selectedOrderDetails.customerPhone}
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* Order Details */}
                  <div className="bg-blue-50 p-4 rounded-lg">
                    <h3 className="font-semibold text-lg mb-3">
                      Order Details
                    </h3>
                    <div className="space-y-2">
                      <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                        <span className="text-gray-600">Products:</span>
                        <div className="sm:text-right">
                          {selectedOrderDetails.products.map(
                            (product, index) => (
                              <div key={index} className="font-medium">
                                {product.name} (x{product.quantity})
                              </div>
                            )
                          )}
                        </div>
                      </div>
                      <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                        <span className="text-gray-600">Designer:</span>
                        <span className="font-medium">
                          {selectedOrderDetails.designer}
                        </span>
                      </div>
                      <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                        <span className="text-gray-600">Priority:</span>
                        <span>
                          {getPriorityBadge(selectedOrderDetails.priority)}
                        </span>
                      </div>
                      <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                        <span className="text-gray-600">Status:</span>
                        <span>
                          {getStatusBadge(selectedOrderDetails.status)}
                        </span>
                      </div>
                      <div className="flex flex-col sm:flex-row sm:justify-between gap-1">
                        <span className="text-gray-600">Delivery Date:</span>
                        <span className="font-medium">
                          {selectedOrderDetails.deliveryDate}
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* Specifications */}
                  <div className="bg-green-50 p-4 rounded-lg">
                    <h3 className="font-semibold text-lg mb-3">
                      Specifications
                    </h3>
                    <p className="text-gray-700">
                      {selectedOrderDetails.specifications}
                    </p>
                    {selectedOrderDetails.specialInstructions && (
                      <div className="mt-3">
                        <h4 className="font-medium text-gray-800">
                          Special Instructions:
                        </h4>
                        <p className="text-gray-700 italic">
                          {selectedOrderDetails.specialInstructions}
                        </p>
                      </div>
                    )}
                  </div>

                  {/* QR Code section */}
                  <div className="bg-purple-50 p-4 rounded-lg">
                    <h3 className="font-semibold text-lg mb-3 flex items-center">
                      <QrCode className="h-5 w-5 mr-2" />
                      QR Code for QC
                    </h3>
                    <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
                      <div className="flex-1">
                        <p className="text-sm text-gray-600">QR Code:</p>
                        <p className="font-mono text-base md:text-lg break-all">
                          {selectedOrderDetails.qrCode}
                        </p>
                        <p className="text-xs text-gray-500 mt-1">
                          Scan this code during QC product check
                        </p>
                      </div>
                      <div className="w-20 h-20 md:w-24 md:h-24 bg-white border-2 border-gray-300 rounded-lg flex items-center justify-center flex-shrink-0">
                        <QrCode className="h-10 w-10 md:h-12 md:w-12 text-gray-400" />
                      </div>
                    </div>
                  </div>
                </div>

                {/* Right Column - Files */}
                <div className="space-y-6">
                  {/* Design Files */}
                  <div>
                    <h3 className="font-semibold text-lg mb-3">Design Files</h3>
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                      {selectedOrderDetails.designFiles.map((file, index) => (
                        <div key={index} className="border rounded-lg p-3">
                          {file.type === "image" ? (
                            <img
                              src={file.url || "/placeholder.svg"}
                              alt={file.name}
                              className="w-full h-32 object-cover rounded mb-2"
                            />
                          ) : (
                            <div className="w-full h-32 bg-gray-100 rounded mb-2 flex items-center justify-center">
                              <ImageIcon className="h-8 w-8 text-gray-400" />
                            </div>
                          )}
                          <p className="text-sm text-gray-600 truncate">
                            {file.name}
                          </p>
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

                  {/* Original Customer Files */}
                  <div>
                    <h3 className="font-semibold text-lg mb-3">
                      Original Customer Files
                    </h3>
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                      {selectedOrderDetails.originalFiles.map((file, index) => (
                        <div key={index} className="border rounded-lg p-3">
                          {file.type === "image" ? (
                            <img
                              src={file.url || "/placeholder.svg"}
                              alt={file.name}
                              className="w-full h-32 object-cover rounded mb-2"
                            />
                          ) : (
                            <div className="w-full h-32 bg-gray-100 rounded mb-2 flex items-center justify-center">
                              <ImageIcon className="h-8 w-8 text-gray-400" />
                            </div>
                          )}
                          <p className="text-sm text-gray-600 truncate">
                            {file.name}
                          </p>
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
              </div>

              <div className="flex justify-end mt-6">
                <Button
                  variant="outline"
                  onClick={() => setSelectedOrderDetails(null)}
                >
                  Close
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Confirmation Dialog */}
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
                  confirmAction?.type === "start_production"
                    ? "bg-blue-600 hover:bg-blue-700"
                    : confirmAction?.type === "mark_done"
                    ? "bg-green-600 hover:bg-green-700"
                    : confirmAction?.type === "assign_qc"
                    ? "bg-purple-600 hover:bg-purple-700"
                    : "bg-gray-600 hover:bg-gray-700"
                }
              >
                {confirmAction?.type === "start_production"
                  ? "Start Production"
                  : confirmAction?.type === "mark_done"
                  ? "Mark as Done"
                  : confirmAction?.type === "assign_qc"
                  ? "Assign to QC"
                  : "Confirm"}
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
