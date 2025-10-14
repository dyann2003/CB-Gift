"use client";

import { useState, useEffect } from "react";
import SellerSidebar from "@/components/layout/seller/sidebar";
import SellerHeader from "@/components/layout/seller/header";
import * as XLSX from "xlsx";
import { saveAs } from "file-saver";

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
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Calendar } from "@/components/ui/calendar";
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
  Clock,
  CheckCircle,
  AlertTriangle,
  Handshake,
  ListTodo,
  ChevronLeft,
  ChevronRight,
  CalendarIcon,
  FileDown,
  Trash2,
  FileEdit,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
} from "lucide-react";
import { format } from "date-fns";

export default function ManageOrder() {
  const [currentPage, setCurrentPage] = useState("manage-order");
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedStat, setSelectedStat] = useState("Total Order");
  const [selectedOrders, setSelectedOrders] = useState([]);
  const [selectAll, setSelectAll] = useState(false);
  const [showMakeManualModal, setShowMakeManualModal] = useState(false);
  const [selectedOrder, setSelectedOrder] = useState(null);
  const [isEditMode, setIsEditMode] = useState(false);
  const [editedOrder, setEditedOrder] = useState(null);
  const [page, setPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(10);
  const [dateRange, setDateRange] = useState({ from: null, to: null });
  const [sortColumn, setSortColumn] = useState(null);
  const [sortDirection, setSortDirection] = useState("asc");
  const [showAssignDialog, setShowAssignDialog] = useState(false);
  const [showCannotAssignDialog, setShowCannotAssignDialog] = useState(false);
  const [cannotAssignMessage, setCannotAssignMessage] = useState("");
  const [showPopup, setShowPopup] = useState(false);
  const [popupMessage, setPopupMessage] = useState("");
  const [popupTitle, setPopupTitle] = useState("");

  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchOrders = async () => {
      try {
        setLoading(true);
        setError(null);

        const response = await fetch("https://localhost:7015/api/seller", {
          credentials: "include",
        });

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();

        // Map API data to match table structure
        const mappedOrders = data.map((order) => ({
          id: order.orderId,
          orderId: order.orderCode,
          orderDate: new Date(order.orderDate).toISOString().split("T")[0], // Format: YYYY-MM-DD
          customerName: order.customerName,
          phone: "", // Not provided in API
          email: "", // Not provided in API
          products: order.details.map((detail) => ({
            name: `Product ${detail.productVariantID}`,
            quantity: detail.quantity,
            price: detail.price,
            size: "",
            accessory: detail.accessory || "",
          })),
          address: "", // Not provided in API
          shipTo: "", // Not provided in API
          status: order.statusOderName,
          totalAmount: `$${order.totalCost.toFixed(2)}`,
          timeCreated: new Date(order.creationDate).toLocaleString(),
          selected: false,
          customerInfo: {
            name: order.customerName,
            phone: "",
            email: "",
            address: "",
            city: "",
            state: "",
            zipcode: "",
            country: "",
          },
          orderNotes: order.details[0]?.note || "",
          uploadedFiles: {
            linkImg: {
              name: "image.jpg",
              url: order.details[0]?.linkImg || "/placeholder.svg",
            },
            linkThanksCard: {
              name: "thanks-card.jpg",
              url: order.details[0]?.linkThanksCard || "#",
            },
            linkFileDesign: {
              name: "design-file.psd",
              url: order.details[0]?.linkFileDesign || "#",
            },
          },
        }));

        setOrders(mappedOrders);
      } catch (err) {
        console.error("[v0] Error fetching orders:", err);
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };

    fetchOrders();
  }, []);

  const stats = [
    {
      title: "Total Order",
      color: "bg-blue-50 border-blue-200",
      icon: Package,
      iconColor: "text-blue-500",
      statusFilter: null,
    },
    {
      title: "Draft (Nháp)",
      color: "bg-gray-50 border-gray-200",
      icon: FileEdit,
      iconColor: "text-gray-500",
      statusFilter: "Draft (Nháp)",
    },
    {
      title: "Cần Design",
      color: "bg-yellow-50 border-yellow-200",
      icon: Handshake,
      iconColor: "text-yellow-500",
      statusFilter: "Cần Design",
    },
    {
      title: "Đang làm Design",
      color: "bg-purple-50 border-purple-200",
      icon: Clock,
      iconColor: "text-purple-500",
      statusFilter: "Đang làm Design",
    },
    {
      title: "Cần Check Design",
      color: "bg-green-50 border-green-200",
      icon: ListTodo,
      iconColor: "text-green-500",
      statusFilter: "Cần Check Design",
    },
    {
      title: "Chốt Đơn (Khóa Seller)",
      color: "bg-orange-50 border-orange-200",
      icon: CheckCircle,
      iconColor: "text-orange-500",
      statusFilter: "Chốt Đơn (Khóa Seller)",
    },
    {
      title: "Thiết kế Lại (Design Lỗi)",
      color: "bg-red-50 border-red-200",
      icon: AlertTriangle,
      iconColor: "text-red-500",
      statusFilter: "Thiết kế Lại (Design Lỗi)",
    },
  ];

  const filteredOrders = orders.filter((order) => {
    const matchesSearch =
      order.orderId.toString().includes(searchTerm.toLowerCase()) ||
      order.customerName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.details?.some((detail) =>
        detail.note?.toLowerCase().includes(searchTerm.toLowerCase())
      );

    const selectedStatConfig = stats.find(
      (stat) => stat.title === selectedStat
    );

    const matchesStatus =
      !selectedStatConfig?.statusFilter ||
      order.status === selectedStatConfig.statusFilter;

    let matchesDateRange = true;

    if (dateRange.from && dateRange.to) {
      const orderDate = new Date(order.orderDate);
      const fromDate = new Date(dateRange.from);
      const toDate = new Date(dateRange.to);

      // 🧠 Fix: set giờ cuối ngày để bao gồm cả ngày to
      toDate.setHours(23, 59, 59, 999);

      matchesDateRange = orderDate >= fromDate && orderDate <= toDate;
    } else if (dateRange.from) {
      const orderDate = new Date(order.orderDate);
      const fromDate = new Date(dateRange.from);

      // Bao gồm toàn bộ ngày from
      const fromEndOfDay = new Date(fromDate);
      fromEndOfDay.setHours(23, 59, 59, 999);

      matchesDateRange = orderDate >= fromDate && orderDate <= fromEndOfDay;
    }

    return matchesSearch && matchesStatus && matchesDateRange;
  });

  // Cập nhật dynamic count cho từng status
  const statsWithCounts = stats.map((stat) => {
    const count = stat.statusFilter
      ? orders.filter((order) => order.status === stat.statusFilter).length
      : orders.length;
    return { ...stat, value: count };
  });

  const sortedOrders = [...filteredOrders].sort((a, b) => {
    if (!sortColumn) return 0;

    let aValue, bValue;

    if (sortColumn === "orderDate") {
      aValue = new Date(a.orderDate).getTime();
      bValue = new Date(b.orderDate).getTime();
    } else if (sortColumn === "customerName") {
      aValue = a.customerName.toLowerCase();
      bValue = b.customerName.toLowerCase();
    } else if (sortColumn === "totalAmount") {
      aValue = Number.parseFloat(a.totalAmount.replace("$", ""));
      bValue = Number.parseFloat(b.totalAmount.replace("$", ""));
    }

    if (sortDirection === "asc") {
      return aValue > bValue ? 1 : -1;
    } else {
      return aValue < bValue ? 1 : -1;
    }
  });

  const totalPages = Math.ceil(sortedOrders.length / itemsPerPage);
  const startIndex = (page - 1) * itemsPerPage;
  const endIndex = startIndex + itemsPerPage;
  const paginatedOrders = sortedOrders.slice(startIndex, endIndex);

  const handleSort = (column) => {
    if (sortColumn === column) {
      setSortDirection(sortDirection === "asc" ? "desc" : "asc");
    } else {
      setSortColumn(column);
      setSortDirection("asc");
    }
  };

  const renderSortIcon = (column) => {
    if (sortColumn !== column) {
      return <ArrowUpDown className="h-4 w-4 ml-1 inline" />;
    }
    return sortDirection === "asc" ? (
      <ArrowUp className="h-4 w-4 ml-1 inline" />
    ) : (
      <ArrowDown className="h-4 w-4 ml-1 inline" />
    );
  };

  const handleSearchChange = (value) => {
    setSearchTerm(value);
    setPage(1);
  };

  const handleStatClick = (statTitle) => {
    setSelectedStat(statTitle);
    setPage(1);
  };

  const handleItemsPerPageChange = (value) => {
    setItemsPerPage(Number(value));
    setPage(1);
  };

  const handleDateSelect = (range) => {
    setDateRange(range || { from: null, to: null });
    setPage(1);
  };

  const handleExport = () => {
    if (!filteredOrders || filteredOrders.length === 0) {
      alert("❌ Không có đơn hàng nào để export!");
      return;
    }

    const exportData = [];

    filteredOrders.forEach((order) => {
      const products = order.products || [];

      products.forEach((p) => {
        exportData.push({
          OrderID: order.id,
          OrderCode: order.orderId,
          OrderDate: formatMySQLDate(order.orderDate),
          CustomerName: order.customerName || order.customerInfo?.name || "",
          Phone: order.phone || order.customerInfo?.phone || "",
          Email: order.email || order.customerInfo?.email || "",
          Address: order.address || order.customerInfo?.address || "",
          Size: p.size || "",
          ProductName: p.name || "",
          Quantity: p.quantity || 0,
          Price: p.price || 0,
          Accessory: p.accessory || "",
          Note: order.orderNotes || "",
          LinkImg: order.uploadedFiles?.linkImg?.url || "",
          LinkThanksCard: order.uploadedFiles?.linkThanksCard?.url || "",
          LinkFileDesign: order.uploadedFiles?.linkFileDesign?.url || "",
          Status: order.status || "",
          TotalAmount: order.totalAmount || "",
          OrderNotes: order.orderNotes || "",
          TimeCreated: order.timeCreated || "",
        });
      });

      // Nếu order không có product nào, vẫn export 1 dòng tổng
      if (products.length === 0) {
        exportData.push({
          OrderID: order.id,
          OrderCode: order.orderId,
          OrderDate: formatMySQLDate(order.orderDate),
          CustomerName: order.customerName || order.customerInfo?.name || "",
          Phone: order.phone || order.customerInfo?.phone || "",
          Email: order.email || order.customerInfo?.email || "",
          Address: order.address || order.customerInfo?.address || "",
          ProductName: "",
          Quantity: "",
          Price: "",
          Size: "",
          Accessory: "",
          Note: order.orderNotes || "",
          LinkImg: order.uploadedFiles?.linkImg?.url || "",
          LinkThanksCard: order.uploadedFiles?.linkThanksCard?.url || "",
          LinkFileDesign: order.uploadedFiles?.linkFileDesign?.url || "",
          Status: order.status || "",
          TotalAmount: order.totalAmount || "",
          OrderNotes: order.orderNotes || "",
          TimeCreated: order.timeCreated || "",
        });
      }
    });

    console.log("📦 Export Data:", exportData);

    const worksheet = XLSX.utils.json_to_sheet(exportData);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, "Orders");

    const excelBuffer = XLSX.write(workbook, {
      bookType: "xlsx",
      type: "array",
    });
    const blob = new Blob([excelBuffer], {
      type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    });

    const fileName = `Orders_${new Date().toISOString().slice(0, 10)}.xlsx`;
    saveAs(blob, fileName);

    alert(
      `✅ Đã export ${exportData.length} dòng dữ liệu từ ${filteredOrders.length} đơn hàng!`
    );
  };

  // Helper: định dạng MySQL
  function formatMySQLDate(dateStr) {
    if (!dateStr) return "";
    const date = new Date(dateStr);
    const pad = (n) => n.toString().padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(
      date.getDate()
    )} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(
      date.getSeconds()
    )}`;
  }

  function formatMySQLDate(dateStr) {
    if (!dateStr) return "";
    const date = new Date(dateStr);
    const pad = (n) => n.toString().padStart(2, "0");
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(
      date.getDate()
    )} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(
      date.getSeconds()
    )}`;
  }

  const handleDelete = (orderId) => {
    console.log("Deleting order:", orderId);
  };

  const handleSelectAll = () => {
    const newSelectAll = !selectAll;
    setSelectAll(newSelectAll);
    if (newSelectAll) {
      setSelectedOrders(paginatedOrders.map((order) => order.id));
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
    }
  };

  const handleAssignClick = () => {
    const selectedOrdersData = orders.filter((order) =>
      selectedOrders.includes(order.id)
    );
    const nonDraftOrders = selectedOrdersData.filter(
      (order) => order.status !== "Draft"
    );

    if (nonDraftOrders.length > 0) {
      setCannotAssignMessage(
        `Cannot assign ${nonDraftOrders.length} order(s) to designer. Only orders with "Draft Seller Order" status can be assigned.`
      );
      setShowCannotAssignDialog(true);
    } else {
      setShowAssignDialog(true);
    }
  };

  const handleAssignToDesigner = () => {
    console.log("Assigning orders to designer:", selectedOrders);
    setShowAssignDialog(false);
    setSelectedOrders([]);
    setSelectAll(false);
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
    setIsEditMode(false);
    setSelectedOrder(editedOrder);
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
      case "Assigned Designer":
        return (
          <Badge className="bg-purple-500 hover:bg-purple-600 text-white">
            Assigned Designer
          </Badge>
        );
      case "Check File Design":
        return (
          <Badge className="bg-indigo-500 hover:bg-indigo-600 text-white">
            Check File Design
          </Badge>
        );
      case "Seller Approved Design":
        return (
          <Badge className="bg-orange-500 hover:bg-orange-600 text-white">
            Seller Approved Design
          </Badge>
        );
      case "Seller Reject Design":
        return (
          <Badge className="bg-red-500 hover:bg-red-600 text-white">
            Seller Reject Design
          </Badge>
        );
      case "Draft":
        return (
          <Badge className="bg-gray-500 hover:bg-gray-600 text-white">
            Draft
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

            <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-7 gap-2 sm:gap-3">
              {statsWithCounts.map((stat, index) => {
                const IconComponent = stat.icon;
                const isActive = selectedStat === stat.title;
                return (
                  <div
                    key={index}
                    onClick={() => handleStatClick(stat.title)}
                    className={`p-3 rounded-lg border-2 ${
                      stat.color
                    } hover:shadow-lg transition-all cursor-pointer ${
                      isActive ? "ring-2 ring-blue-400 shadow-lg scale-105" : ""
                    }`}
                  >
                    <div className="flex flex-col items-center text-center gap-2">
                      <IconComponent className={`h-5 w-5 ${stat.iconColor}`} />
                      <div>
                        <p className="text-lg font-bold text-gray-900">
                          {stat.value}
                        </p>
                        <h3 className="text-[10px] sm:text-xs font-medium text-gray-600 uppercase tracking-wide">
                          {stat.title}
                        </h3>
                      </div>
                    </div>
                    {isActive && (
                      <div className="mt-1 text-[10px] font-medium text-blue-600 text-center">
                        ✓ Active
                      </div>
                    )}
                  </div>
                );
              })}
            </div>

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
                        onChange={(e) => handleSearchChange(e.target.value)}
                        className="pl-10 bg-white"
                      />
                    </div>
                  </div>

                  <div className="flex-1 max-w-xs">
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Filter by Date
                    </label>
                    <Popover>
                      <PopoverTrigger asChild>
                        <Button
                          variant="outline"
                          className="w-full justify-start text-left font-normal bg-white"
                        >
                          <CalendarIcon className="mr-2 h-4 w-4" />
                          {dateRange.from ? (
                            dateRange.to ? (
                              <>
                                {format(dateRange.from, "MMM dd, yyyy")} -{" "}
                                {format(dateRange.to, "MMM dd, yyyy")}
                              </>
                            ) : (
                              format(dateRange.from, "MMM dd, yyyy")
                            )
                          ) : (
                            <span>Pick a date range</span>
                          )}
                        </Button>
                      </PopoverTrigger>
                      <PopoverContent className="w-auto p-0" align="start">
                        <Calendar
                          mode="range"
                          selected={dateRange}
                          onSelect={handleDateSelect}
                          numberOfMonths={2}
                          initialFocus
                        />
                        {(dateRange.from || dateRange.to) && (
                          <div className="p-3 border-t">
                            <Button
                              variant="outline"
                              size="sm"
                              className="w-full bg-transparent"
                              onClick={() =>
                                handleDateSelect({ from: null, to: null })
                              }
                            >
                              Clear Filter
                            </Button>
                          </div>
                        )}
                      </PopoverContent>
                    </Popover>
                  </div>
                </div>

                <div className="flex flex-wrap gap-2 w-full lg:w-auto">
                  <Button variant="outline" onClick={handleExport}>
                    <FileDown className="h-4 w-4 mr-2" />
                    <span className="hidden sm:inline">Export file</span>
                    <span className="sm:hidden">Export</span>
                  </Button>
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

              <Button
                variant="outline"
                disabled={selectedOrders.length === 0}
                onClick={handleAssignClick}
                className="bg-white"
              >
                Assign to Designer ({selectedOrders.length})
              </Button>
            </div>

            <AlertDialog
              open={showAssignDialog}
              onOpenChange={setShowAssignDialog}
            >
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>Assign Orders to Designer</AlertDialogTitle>
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

            <AlertDialog
              open={showCannotAssignDialog}
              onOpenChange={setShowCannotAssignDialog}
            >
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>Cannot Assign Orders</AlertDialogTitle>
                  <AlertDialogDescription>
                    {cannotAssignMessage}
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogAction>OK</AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>

            <div className="bg-white rounded-lg shadow-sm border border-gray-200">
              {loading ? (
                <div className="flex items-center justify-center p-12">
                  <div className="text-center">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
                    <p className="mt-4 text-gray-600">Loading orders...</p>
                  </div>
                </div>
              ) : error ? (
                <div className="flex items-center justify-center p-12">
                  <div className="text-center">
                    <AlertTriangle className="h-12 w-12 text-red-500 mx-auto" />
                    <p className="mt-4 text-red-600 font-medium">
                      Error loading orders
                    </p>
                    <p className="mt-2 text-gray-600 text-sm">{error}</p>
                    <Button
                      onClick={() => window.location.reload()}
                      className="mt-4"
                      variant="outline"
                    >
                      Retry
                    </Button>
                  </div>
                </div>
              ) : (
                <>
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
                          <TableHead
                            className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap cursor-pointer hover:bg-gray-100"
                            onClick={() => handleSort("orderDate")}
                          >
                            Order Date {renderSortIcon("orderDate")}
                          </TableHead>
                          <TableHead
                            className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap cursor-pointer hover:bg-gray-100"
                            onClick={() => handleSort("customerName")}
                          >
                            Customer {renderSortIcon("customerName")}
                          </TableHead>
                          <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                            Address
                          </TableHead>
                          <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                            Status
                          </TableHead>
                          <TableHead
                            className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap cursor-pointer hover:bg-gray-100"
                            onClick={() => handleSort("totalAmount")}
                          >
                            Amount {renderSortIcon("totalAmount")}
                          </TableHead>
                          <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                            Actions
                          </TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {paginatedOrders.length === 0 ? (
                          <TableRow>
                            <TableCell
                              colSpan={8}
                              className="text-center py-8 text-gray-500"
                            >
                              No orders found
                            </TableCell>
                          </TableRow>
                        ) : (
                          paginatedOrders.map((order) => (
                            <TableRow
                              key={order.id}
                              className="hover:bg-gray-50 transition-colors"
                            >
                              <TableCell>
                                <Checkbox
                                  checked={selectedOrders.includes(order.id)}
                                  onCheckedChange={() =>
                                    handleOrderSelect(order.id)
                                  }
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
                                  {order.email && (
                                    <div className="text-sm text-gray-500">
                                      {order.email}
                                    </div>
                                  )}
                                </div>
                              </TableCell>
                              <TableCell className="text-gray-600 max-w-[200px]">
                                <div className="truncate" title={order.address}>
                                  {order.address || "N/A"}
                                </div>
                              </TableCell>
                              <TableCell className="whitespace-nowrap">
                                {getStatusBadge(order.status)}
                              </TableCell>
                              <TableCell className="font-medium text-gray-900 whitespace-nowrap">
                                {order.totalAmount}
                              </TableCell>
                              <TableCell className="whitespace-nowrap">
                                <div className="flex items-center gap-2">
                                  <Dialog>
                                    <DialogTrigger asChild>
                                      <Button
                                        variant="outline"
                                        size="sm"
                                        onClick={() => handleViewDetails(order)}
                                        className="bg-transparent hover:bg-gray-50"
                                      >
                                        <Eye className="h-4 w-4 mr-1" />
                                        <span className="hidden xl:inline">
                                          View
                                        </span>
                                      </Button>
                                    </DialogTrigger>
                                    <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
                                      <DialogHeader>
                                        <DialogTitle className="flex items-center justify-between">
                                          <div className="flex items-center gap-2">
                                            <Package className="h-5 w-5" />
                                            Order Details -{" "}
                                            {editedOrder?.orderId}
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
                                                      editedOrder.customerInfo
                                                        .name
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
                                                    {
                                                      editedOrder.customerInfo
                                                        .name
                                                    }
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
                                                      editedOrder.customerInfo
                                                        .phone
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
                                                    {
                                                      editedOrder.customerInfo
                                                        .phone
                                                    }
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
                                                      editedOrder.customerInfo
                                                        .email
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
                                                    {
                                                      editedOrder.customerInfo
                                                        .email
                                                    }
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
                                                    {
                                                      editedOrder.customerInfo
                                                        .address
                                                    }
                                                    ,{" "}
                                                    {
                                                      editedOrder.customerInfo
                                                        .city
                                                    }
                                                    ,{" "}
                                                    {
                                                      editedOrder.customerInfo
                                                        .state
                                                    }{" "}
                                                    {
                                                      editedOrder.customerInfo
                                                        .zipcode
                                                    }
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
                                                            value={
                                                              product.quantity
                                                            }
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
                                                            value={
                                                              product.accessory
                                                            }
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
                                                    value={
                                                      editedOrder.orderNotes
                                                    }
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
                                                      handleFieldChange(
                                                        "status",
                                                        value
                                                      )
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
                                                      <SelectItem value="Assigned Designer">
                                                        Assigned Designer
                                                      </SelectItem>
                                                      <SelectItem value="Check File Design">
                                                        Check File Design
                                                      </SelectItem>
                                                      <SelectItem value="Seller Approved Design">
                                                        Seller Approved Design
                                                      </SelectItem>
                                                      <SelectItem value="Seller Reject Design">
                                                        Seller Reject Design
                                                      </SelectItem>
                                                      <SelectItem value="Draft">
                                                        Draft
                                                      </SelectItem>
                                                    </SelectContent>
                                                  </Select>
                                                ) : (
                                                  <div className="mt-1">
                                                    {getStatusBadge(
                                                      editedOrder.status
                                                    )}
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
                                                      editedOrder.uploadedFiles
                                                        .linkImg.url ||
                                                      "/placeholder.svg"
                                                    }
                                                    alt="Reference"
                                                    className="w-full h-32 object-cover rounded border"
                                                  />
                                                  <p className="text-sm mt-2 text-gray-600">
                                                    {
                                                      editedOrder.uploadedFiles
                                                        .linkImg.name
                                                    }
                                                  </p>
                                                  <Button
                                                    variant="outline"
                                                    size="sm"
                                                    className="mt-2 w-full bg-transparent hover:bg-gray-50"
                                                    onClick={() =>
                                                      handleDownload(
                                                        editedOrder
                                                          .uploadedFiles.linkImg
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
                                                        editedOrder
                                                          .uploadedFiles
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
                                                        editedOrder
                                                          .uploadedFiles
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
                                              QR Code for Order{" "}
                                              {editedOrder.orderId}
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
                                  <AlertDialog>
                                    <AlertDialogTrigger asChild>
                                      <Button
                                        variant="outline"
                                        size="sm"
                                        className="bg-transparent hover:bg-red-50 text-red-600 hover:text-red-700"
                                      >
                                        <Trash2 className="h-4 w-4" />
                                      </Button>
                                    </AlertDialogTrigger>
                                    <AlertDialogContent>
                                      <AlertDialogHeader>
                                        <AlertDialogTitle>
                                          Delete Order
                                        </AlertDialogTitle>
                                        <AlertDialogDescription>
                                          Are you sure you want to delete order{" "}
                                          {order.orderId}? This action cannot be
                                          undone.
                                        </AlertDialogDescription>
                                      </AlertDialogHeader>
                                      <AlertDialogFooter>
                                        <AlertDialogCancel>
                                          Cancel
                                        </AlertDialogCancel>
                                        <AlertDialogAction
                                          onClick={() => handleDelete(order.id)}
                                          className="bg-red-600 hover:bg-red-700"
                                        >
                                          Delete
                                        </AlertDialogAction>
                                      </AlertDialogFooter>
                                    </AlertDialogContent>
                                  </AlertDialog>
                                </div>
                              </TableCell>
                            </TableRow>
                          ))
                        )}
                      </TableBody>
                    </Table>
                  </div>

                  <div className="bg-gray-50 px-4 py-3 border-t border-gray-200 sm:px-6">
                    <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
                      <div className="flex items-center gap-2">
                        <span className="text-sm text-gray-700">Show</span>
                        <select
                          value={itemsPerPage}
                          onChange={(e) =>
                            handleItemsPerPageChange(e.target.value)
                          }
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
                          {Math.min(endIndex, sortedOrders.length)} of{" "}
                          {sortedOrders.length} results
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
                          <span className="hidden sm:inline ml-1">
                            Previous
                          </span>
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
                                  variant={
                                    page === pageNum ? "default" : "outline"
                                  }
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
                </>
              )}
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
