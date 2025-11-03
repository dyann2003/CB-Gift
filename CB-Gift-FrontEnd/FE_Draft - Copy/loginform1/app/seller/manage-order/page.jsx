"use client";

import { useState, useEffect, useMemo } from "react"; // ✅ Đảm bảo useMemo được import
import React from "react";
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
  DialogDescription,
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
  ChevronDown,
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
  // This seems unused, consider removing
  const [isEditMode, setIsEditMode] = useState(false);
  const [editedOrder, setEditedOrder] = useState(null);

  // ✅ Cập nhật state Page và ItemsPerPage
  const [page, setPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(10);
  const [dateRange, setDateRange] = useState({ from: null, to: null });

  // ✅ Cập nhật state Sắp xếp
  const [sortColumn, setSortColumn] = useState("orderDate"); // Mặc định sắp xếp theo ngày
  const [sortDirection, setSortDirection] = useState("desc"); // Mặc định giảm dần

  const [showAssignDialog, setShowAssignDialog] = useState(false);
  const [showCannotAssignDialog, setShowCannotAssignDialog] = useState(false);
  const [cannotAssignMessage, setCannotAssignMessage] = useState("");
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  // Added for dialog state management
  const [showResultDialog, setShowResultDialog] = useState(false);
  const [resultMessage, setResultMessage] = useState("");
  const [isAssignPopupOpen, setIsAssignPopupOpen] = useState(false);
  const [designers, setDesigners] = useState([]);
  const [selectedDesignerId, setSelectedDesignerId] = useState("");
  const [showDesignCheckDialog, setShowDesignCheckDialog] = useState(false);
  const [designCheckAction, setDesignCheckAction] = useState(null); // 'approve' or 'reject'
  const [designCheckOrderId, setDesignCheckOrderId] = useState(null);

  const [expandedOrderId, setExpandedOrderId] = useState(null);
  const [successMessage, setSuccessMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [showSuccessDialog, setShowSuccessDialog] = useState(false);
  const [showErrorDialog, setShowErrorDialog] = useState(false);

  // orders: chỉ chứa dữ liệu trang hiện tại
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // ✅ STATE MỚI: Lưu tổng số lượng đơn hàng (từ BE)
  const [totalOrdersCount, setTotalOrdersCount] = useState(0);

  const fetchOrders = async () => {
    try {
      setLoading(true);
      setError(null);

      // 1. Xác định Status Filter (BE cần tên Status đầy đủ)
      const selectedStatConfig = stats.find(
        (stat) => stat.title === selectedStat
      );
      const statusFilter = selectedStatConfig?.statusFilter;

      // 2. Xây dựng Query Parameters
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: itemsPerPage.toString(),
        searchTerm: searchTerm,
        sortColumn: sortColumn || "orderDate", // Mặc định sortColumn
        sortDirection: sortDirection,
      });

      if (statusFilter && selectedStat !== "Total Order") {
        params.append("status", statusFilter);
      }

      const url = `https://localhost:7015/api/Seller?${params.toString()}`;

      // 3. Gọi API với URL có tham số
      const response = await fetch(url, {
        credentials: "include",
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      // 4. Nhận response có { total, orders }
      const { total, orders: data } = await response.json();
      console.log("📦 Orders fetched:", data);

      setTotalOrdersCount(total);

      // 5. Logic map data (Ánh xạ từ BE DTO sang FE model)
      const mappedOrders = data.map((order) => ({
        id: order.orderId,
        orderId: order.orderCode,
        orderDate: new Date(order.orderDate).toISOString().split("T")[0],
        customerName: order.customerName,
        phone: order.phone || "",
        email: order.email || "",
        products: order.details.map((detail) => ({
          name: detail.productName || `Product ${detail.productVariantID}`,
          quantity: detail.quantity,
          price: detail.price,
          size: detail.size || "",
          accessory: detail.accessory || "",
          activeTTS: order.activeTts || false,
          linkFileDesign: detail.linkFileDesign,
          linkThanksCard: detail.linkThanksCard,
          linkImg: detail.linkImg,
        })),
        address: order.address || "",
        shipTo: "",
        status: order.statusOderName,
        totalAmount: `$${order.totalCost.toFixed(2)}`,
        timeCreated: new Date(order.creationDate).toLocaleString(),
        selected: false,
        customerInfo: {
          name: order.customerName,
          phone: order.phone,
          email: order.email,
          address: order.address,
          city: order.city,
          state: order.state,
          zipcode: order.zipcode,
          country: order.country,
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

  // ✅ Dependency Array: Gọi lại fetchOrders khi bất kỳ tham số filter/pagination/sort nào thay đổi
  useEffect(() => {
    fetchOrders();
  }, [page, itemsPerPage, searchTerm, selectedStat, sortColumn, sortDirection]);

  const stats = [
    {
      title: "Total Order",
      color: "bg-blue-50 border-blue-200",
      icon: Package,
      iconColor: "text-blue-500",
      statusFilter: null,
    },
    {
      title: "Draft",
      color: "bg-gray-50 border-gray-200",
      icon: FileEdit,
      iconColor: "text-gray-500",
      statusFilter: "Draft (Nháp)",
    },
    {
      title: "Need Design",
      color: "bg-yellow-50 border-yellow-200",
      icon: Handshake,
      iconColor: "text-yellow-500",
      statusFilter: "Cần Design",
    },
    {
      title: "Designing",
      color: "bg-purple-50 border-purple-200",
      icon: Clock,
      iconColor: "text-purple-500",
      statusFilter: "Đang làm Design",
    },
    {
      title: "Need Check Design",

      color: "bg-green-50 border-green-200",
      icon: ListTodo,
      iconColor: "text-green-500",
      statusFilter: "Cần Check Design",
    },
    {
      title: "Close Order (Lock Seller)",
      color: "bg-orange-50 border-orange-200",
      icon: CheckCircle,
      iconColor: "text-orange-500",
      statusFilter: "Chốt Đơn (Khóa Seller)",
    },
    {
      title: "Redesign (Design Error)",

      color: "bg-red-50 border-red-200",
      icon: AlertTriangle,
      iconColor: "text-red-500",
      statusFilter: "Thiết kế Lại (Design Lỗi)",
    },
    {
      title: "Production Ready",
      color: "bg-cyan-50 border-cyan-200",
      icon: Package,
      iconColor: "text-cyan-500",
      statusFilter: "Sẵn sàng Sản xuất",
    },
    {
      title: "Production Done",
      color: "bg-teal-50 border-teal-200",
      icon: CheckCircle,
      iconColor: "text-teal-500",
      statusFilter: "Sản xuất Xong",
    },
    {
      title: "Manufacturing Defect (Requires Rework)",
      color: "bg-red-50 border-red-200",
      icon: AlertTriangle,
      iconColor: "text-red-600",
      statusFilter: "Sản xuất Lại",
    },
    {
      title: "Quality Checked",
      color: "bg-emerald-50 border-emerald-200",

      icon: CheckCircle,
      iconColor: "text-emerald-500",
      statusFilter: "Đã Kiểm tra Chất lượng",
    },
    {
      title: "Shipped",
      color: "bg-blue-50 border-blue-200",
      icon: Package,
      iconColor: "text-blue-600",
      statusFilter: "Đã Ship",
    },
    {
      title: "Cancel",
      color: "bg-gray-50 border-gray-300",
      icon: AlertTriangle,

      iconColor: "text-gray-600",
      statusFilter: "Cancel",
    },
    {
      title: "Return the product",
      color: "bg-amber-50 border-amber-200",
      icon: Package,
      iconColor: "text-amber-600",
      statusFilter: "Hoàn Hàng",
    },
    // </CHANGE>
  ];

  // ✅ Sửa lỗi: Đảm bảo orders là một mảng trước khi dùng filter
  const statsWithCounts = useMemo(() => {
    // ❌ Lỗi Orders.filter is not a function thường do orders không phải là mảng
    const safeOrders = Array.isArray(orders) ? orders : [];

    let newStats = stats.map((stat) => ({
      ...stat,
      // Sử dụng safeOrders
      value: stat.statusFilter
        ? safeOrders.filter((o) => o.status === stat.statusFilter).length
        : 0,
    }));

    // Cập nhật Total Order bằng totalOrdersCount từ BE
    newStats = newStats.map((stat, i) =>
      i === 0 ? { ...stat, value: totalOrdersCount } : stat
    );
    return newStats;
  }, [orders, stats, totalOrdersCount]);

  const totalPages = Math.ceil(totalOrdersCount / itemsPerPage);
  // orders đã là dữ liệu phân trang (paginatedOrders)
  const paginatedOrders = orders;

  const handleSort = (column) => {
    // Luôn setPage(1) khi đổi sắp xếp để BE trả về dữ liệu mới từ trang đầu
    if (sortColumn === column) {
      setSortDirection(sortDirection === "asc" ? "desc" : "asc");
    } else {
      setSortColumn(column);
      setSortDirection("asc");
    }
    setPage(1);
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
    setPage(1); // Quay về trang 1 khi tìm kiếm
  };

  const handleStatClick = (statTitle) => {
    setSelectedStat(statTitle);
    setPage(1); // Quay về trang 1 khi lọc status
  };

  const handleItemsPerPageChange = (value) => {
    setItemsPerPage(Number(value));
    setPage(1); // Quay về trang 1 khi đổi số lượng item
  };

  // Giữ lại handleDateSelect, nhưng nó không ảnh hưởng đến fetchOrders hiện tại
  const handleDateSelect = (range) => {
    setDateRange(range || { from: null, to: null });
    setPage(1); // Mặc dù BE chưa hỗ trợ lọc theo ngày, giữ lại để FE reset state
  };

  const handleOpenAssignPopup = async () => {
    // Lấy danh sách order đã chọn
    const selectedOrdersData = orders.filter((order) =>
      selectedOrders.includes(order.id)
    );
    // Kiểm tra có order nào KHÔNG phải là Draft (Nháp)
    const nonDraftOrders = selectedOrdersData.filter(
      (order) => order.status !== "Draft (Nháp)"
    );
    if (nonDraftOrders.length > 0) {
      setCannotAssignMessage(
        `Cannot assign ${nonDraftOrders.length} order(s) to designer. Only orders with "Draft (Nháp)" status can be assigned.`
      );
      setShowCannotAssignDialog(true);
      return;
    }

    // Nếu tất cả đều hợp lệ, fetch danh sách designer
    try {
      const res = await fetch("https://localhost:7015/api/Seller/my-designer", {
        credentials: "include",
      });
      if (!res.ok) throw new Error("Failed to fetch designers");
      const data = await res.json();
      setDesigners(data);
      setIsAssignPopupOpen(true);
    } catch (err) {
      console.error("Fetch designers failed:", err);
      alert("❌ Failed to load designers");
    }
  };

  const handleConfirmAssignDesigner = async () => {
    // ✅ Kiểm tra nếu chưa chọn designer hoặc chưa chọn order
    if (!selectedDesignerId || selectedOrders.length === 0) {
      setErrorMessage("⚠️ Please select a designer and at least one order.");
      setShowErrorDialog(true);
      return;
    }

    try {
      // ✅ Gọi API assign cho từng order
      for (const orderId of selectedOrders) {
        const res = await fetch(
          `https://localhost:7015/api/Seller/orders/${orderId}/assign-designer`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },

            credentials: "include",
            body: JSON.stringify({ designerUserId: selectedDesignerId }),
          }
        );
        if (!res.ok) throw new Error(`Failed to assign for order ${orderId}`);
      }

      // ✅ Hiển thị popup thành công
      setSuccessMessage(
        `✅ Successfully assigned designer to ${selectedOrders.length} order(s).`
      );
      setShowSuccessDialog(true);

      // ✅ Đóng popup chọn designer
      setIsAssignPopupOpen(false);
      setSelectedDesignerId("");

      // ✅ Reload lại dữ liệu sau 1.5s để cập nhật dữ liệu
      setTimeout(() => {
        fetchOrders(); // ✅ GỌI LẠI fetchOrders THAY CHO window.location.reload()
        setSelectedOrders([]);
        setSelectAll(false);
      }, 1500);
    } catch (err) {
      console.error("❌ Assign designer failed:", err);
      setErrorMessage(`❌ Failed to assign: ${err.message}`);
      setShowErrorDialog(true);
    }
  };

  const handleExport = () => {
    // Vì FE chỉ có dữ liệu 1 trang, cần gọi 1 API BE không phân trang để export toàn bộ
    alert(
      "Chức năng export đang sử dụng dữ liệu lọc hiện tại. Nếu muốn export toàn bộ, cần có API riêng!"
    );

    // Logic export hiện tại đang dùng orders (chỉ 1 trang) - cần sửa nếu muốn export full
    if (!paginatedOrders || paginatedOrders.length === 0) {
      alert("❌ Không có đơn hàng nào để export!");
      return;
    }

    const exportData = [];

    paginatedOrders.forEach((order) => {
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
      `✅ Đã export ${exportData.length} dòng dữ liệu từ ${paginatedOrders.length} đơn hàng!`
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

  const handleDelete = async (orderId) => {
    if (!orderId) return;
    try {
      // 🔍 Tìm đơn hàng trong danh sách hiện tại
      const orderToDelete = orders.find((o) => o.id === orderId);
      if (!orderToDelete) {
        setResultMessage("⚠️ Không tìm thấy đơn hàng để xóa.");
        setShowResultDialog(true);
        return;
      }

      // ❌ Nếu không phải trạng thái Draft (Nháp), chặn xóa
      if (orderToDelete.status !== "Draft (Nháp)") {
        setResultMessage(
          `Can't delete the order have status "${orderToDelete.status}". Only can delete the order have status Draft".`
        );
        setShowResultDialog(true);
        return;
      }

      // ✅ Gọi API xóa

      const response = await fetch(
        `https://localhost:7015/api/Order/${orderId}`,
        {
          method: "DELETE",
          credentials: "include",
        }
      );

      const result = await response.json();

      if (response.ok) {
        setOrders((prev) => prev.filter((o) => o.id !== orderId));

        setResultMessage(result.message || "Delete Successfully");
        fetchOrders(); // ✅ Gọi lại fetchOrders để cập nhật danh sách và count
      } else {
        setResultMessage(result.message || "Can't delete this order");
      }
    } catch (error) {
      console.error("❌ Delete failed:", error);
      setResultMessage("Have some error");
    } finally {
      setShowResultDialog(true);
    }
  };

  // const handleSelectAll = () => {
  //   const newSelectAll = !selectAll;
  //   setSelectAll(newSelectAll);
  //   if (newSelectAll) {
  //     setSelectedOrders(paginatedOrders.map((order) => order.id));
  //   } else {
  //     setSelectedOrders([]);
  //   }
  // };

  // const handleOrderSelect = (orderId) => {
  //   if (selectedOrders.includes(orderId)) {
  //     setSelectedOrders(selectedOrders.filter((id) => id !== orderId));
  //   } else {
  //     setSelectedOrders([...selectedOrders, orderId]);
  //   }
  // };

  // const handleFileImport = (event) => {
  //   const file = event.target.files[0];
  //   if (file) {
  //     console.log("File selected:", file.name);
  //   }
  // };

  const handleAssignClick = () => {
    const selectedOrdersData = orders.filter((order) =>
      selectedOrders.includes(order.id)
    );
    const nonDraftOrders = selectedOrdersData.filter(
      (order) => order.status !== "Draft (Nháp)"
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

  // ✅ Cập nhật: Gọi API GET /api/Seller/{id} để lấy chi tiết 1 đơn hàng
  const handleViewDetails = async (order) => {
    try {
      console.log("🧾 Selected order (before fetch):", order);

      // ✅ Gọi API chi tiết đơn hàng
      const res = await fetch(`https://localhost:7015/api/Seller/${order.id}`, {
        credentials: "include",
      });

      if (!res.ok) throw new Error(`HTTP ${res.status}`);

      const fullOrder = await res.json();
      console.log("✅ Full order fetched:", fullOrder);
      console.log("🧩 Details inside order:", fullOrder?.details);

      if (!fullOrder) {
        alert("Không tìm thấy chi tiết đơn hàng này!");
        return;
      }

      // Map data (giữ nguyên logic, đảm bảo lấy trường từ fullOrder)
      const mappedOrder = {
        id: fullOrder.orderId,
        orderId: fullOrder.orderCode,
        orderDate: new Date(fullOrder.orderDate).toISOString().split("T")[0],
        customerName: fullOrder.customerName,
        phone: fullOrder.phone || "",
        email: fullOrder.email || "",
        address: fullOrder.address || "",
        shipTo: `${fullOrder.shipCity || ""}, ${fullOrder.shipState || ""}, ${
          fullOrder.shipCountry || ""
        }`,
        status: fullOrder.statusOderName,
        totalAmount: `$${fullOrder.totalCost?.toFixed(2) || 0}`,
        timeCreated: new Date(fullOrder.creationDate).toLocaleString(),
        customerInfo: {
          name: fullOrder.customerName,
          phone: fullOrder.phone || "",
          email: fullOrder.email || "",
          address: fullOrder.address || "",
          address1: fullOrder.address1 || "",
          city: fullOrder.shipCity || "",
          state: fullOrder.shipState || "",
          zipcode: fullOrder.zipcode || "",
          country: fullOrder.shipCountry || "",
        },
        orderNotes: fullOrder.details[0]?.note || "",
        uploadedFiles: {
          linkImg: {
            name: "image.jpg",
            url: fullOrder.details[0]?.linkImg || "/placeholder.svg",
          },
          linkThanksCard: {
            name: "thanks-card.jpg",
            url: fullOrder.details[0]?.linkThanksCard || "#",
          },
          linkFileDesign: {
            name: "design-file.psd",
            url: fullOrder.details[0]?.linkFileDesign || "#",
          },
        },
        products: fullOrder.details.map((detail) => ({
          name: detail.productName || `Product ${detail.productVariantID}`,
          quantity: detail.quantity,
          price: detail.price,
          size: detail.size || "",
          accessory: detail.accessory || "",

          activeTTS: fullOrder.activeTts || false,
          linkFileDesign: detail.linkFileDesign,
          linkThanksCard: detail.linkThanksCard,
          linkImg: detail.linkImg,
        })),
      };

      console.log("🎯 Mapped order for modal:", mappedOrder);

      setEditedOrder(mappedOrder);
      setIsDialogOpen(true);
    } catch (err) {
      console.error("❌ Failed to fetch order details:", err);
      alert("Failed to load order details. Please try again.");
    }
  };

  const handleEditMode = () => {
    setIsEditMode(true);
  };

  const handleSaveUpdate = () => {
    console.log("Saving updated order:", editedOrder);
    setIsEditMode(false);
    setSelectedOrder(editedOrder);
    setIsDialogOpen(false);
  };

  const handleCancelEdit = () => {
    // Sửa: Dùng editedOrder hiện tại để reset nếu selectedOrder chưa được set
    setEditedOrder(editedOrder);
    setIsEditMode(false);
    setIsDialogOpen(false);
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

  const handleApproveDesign = async (orderId) => {
    try {
      if (!orderId) {
        console.error("[v0] Invalid orderId:", orderId);
        throw new Error("Order ID is missing");
      }

      console.log(
        "[v0] Approving design for order:",
        orderId,
        "Type:",
        typeof orderId
      );
      const res = await fetch(
        `https://localhost:7015/api/Seller/orders/${orderId}/approve-or-reject-design`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ productionStatus: 6 }), // 6 = READY_PROD (Approve)
        }
      );
      console.log("[v0] API Response status:", res.status);
      console.log("[v0] API Response headers:", res.headers);
      if (!res.ok) {
        const errorText = await res.text();
        console.error("[v0] API Error response text:", errorText);

        let errorData = {};
        try {
          errorData = JSON.parse(errorText);
        } catch (e) {
          console.error("[v0] Failed to parse error response as JSON");
        }

        throw new Error(
          errorData.message || `HTTP ${res.status}: Failed to approve design`
        );
      }

      setSuccessMessage("✅ Design approved successfully!");
      setShowSuccessDialog(true);
      setIsDialogOpen(false);

      // ✅ GỌI LẠI fetchOrders THAY CHO window.location.reload()
      setTimeout(() => {
        fetchOrders();
      }, 1500);
    } catch (err) {
      console.error("[v0] Approve design failed:", err);
      setErrorMessage(`❌ Failed to approve: ${err.message}`);
      setShowErrorDialog(true);
    }
  };

  const handleRejectDesign = async (orderId) => {
    try {
      if (!orderId) {
        console.error("[v0] Invalid orderId:", orderId);
        throw new Error("Order ID is missing");
      }

      console.log(
        "[v0] Rejecting design for order:",
        orderId,
        "Type:",
        typeof orderId
      );
      const res = await fetch(
        `https://localhost:7015/api/Seller/orders/${orderId}/approve-or-reject-design`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ productionStatus: 5 }), // 5 = DESIGN_REDO (Reject)
        }
      );
      console.log("[v0] API Response status:", res.status);
      console.log("[v0] API Response headers:", res.headers);
      if (!res.ok) {
        const errorText = await res.text();
        console.error("[v0] API Error response text:", errorText);

        let errorData = {};
        try {
          errorData = JSON.parse(errorText);
        } catch (e) {
          console.error("[v0] Failed to parse error response as JSON");
        }

        throw new Error(
          errorData.message || `HTTP ${res.status}: Failed to reject design`
        );
      }

      setSuccessMessage("✅ Design rejected successfully!");
      setShowSuccessDialog(true);
      setIsDialogOpen(false);

      // ✅ GỌI LẠI fetchOrders THAY CHO window.location.reload()
      setTimeout(() => {
        fetchOrders();
      }, 1500);
    } catch (err) {
      console.error("[v0] Reject design failed:", err);
      setErrorMessage(`❌ Failed to reject: ${err.message}`);
      setShowErrorDialog(true);
    }
  };

  const getStatusBadge = (status) => {
    switch (status) {
      case "Pending Design":
        return <Badge variant="secondary">Pending Design</Badge>;
      case "In Progress":
        return <Badge variant="secondary">In Progress</Badge>;
      case "Completed":
        return <Badge variant="secondary">Completed</Badge>;
      case "Assigned Designer":
        return <Badge variant="secondary">Đã Kiểm tra Chất lượng</Badge>;
      case "Đã Ship":
        return <Badge variant="secondary">Đã Ship</Badge>;
      case "Cancel":
        return <Badge variant="secondary">Cancel</Badge>;
      case "Hoàn Hàng":
        return <Badge variant="secondary">Hoàn Hàng</Badge>;
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  };

  const handleAssignStaff = async () => {
    if (selectedOrders.length === 0) {
      setCannotAssignMessage(
        "Please select at least one order to assign to staff."
      );
      setShowCannotAssignDialog(true);
      return;
    }

    try {
      const failedOrders = [];
      for (const orderId of selectedOrders) {
        try {
          const res = await fetch(
            `https://localhost:7015/api/Seller/orders/${orderId}/send-to-staff`,
            {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              credentials: "include", // Added credentials to send authentication token
            }
          );
          if (!res.ok) {
            const errorData = await res.json().catch(() => ({}));
            console.error("[v0] Assign staff error:", errorData);
            failedOrders.push(orderId);
          }
        } catch (err) {
          console.error("[v0] Assign staff failed for order:", orderId, err);
          failedOrders.push(orderId);
        }
      }

      if (failedOrders.length === 0) {
        setSuccessMessage(
          `✅ Successfully assigned ${selectedOrders.length} order(s) to staff.`
        );
        setShowSuccessDialog(true);
        setSelectedOrders([]);
        setSelectAll(false);
        fetchOrders();
      } else {
        setCannotAssignMessage(
          `Failed to assign ${failedOrders.length} order(s) to staff. Please try again.`
        );
        setShowCannotAssignDialog(true);
      }
    } catch (err) {
      console.error("[v0] Assign staff error:", err);
      setCannotAssignMessage(
        "An error occurred while assigning orders to staff."
      );
      setShowCannotAssignDialog(true);
    }
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

  return (
    <div className="flex h-screen bg-blue-50">
      {" "}
      {/* changed from bg-slate-50 to bg-blue-50 */}
      <SellerSidebar
        currentPage={currentPage}
        setCurrentPage={setCurrentPage}
      />
      <div className="flex-1 flex flex-col overflow-hidden">
        <SellerHeader />
        <main className="flex-1 overflow-y-auto p-4 sm:p-6">
          <div className="space-y-6">
            {/* Welcome Header */}
            <div className="bg-blue-50 p-4 sm:p-6 rounded-lg border-2 border-blue-200 shadow-sm">
              {" "}
              {/* unified to blue-50 and blue-200 */}
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <h1 className="text-lg sm:text-xl font-semibold text-slate-900">
                    Welcome back, Seller!
                  </h1>
                  <p className="text-sm sm:text-base text-slate-600 mt-1">
                    Manage your customer orders
                  </p>
                </div>
                <div className="mt-3 sm:mt-0">
                  <div className="flex items-center gap-2 text-sm text-slate-700">
                    <Package className="h-4 w-4" />
                    <span>{totalOrdersCount} orders</span>{" "}
                    {/* ✅ Sửa: Dùng totalOrdersCount */}
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
                      isActive
                        ? "ring-2 ring-indigo-500 shadow-lg scale-105"
                        : ""
                    }`}
                  >
                    <div className="flex flex-col items-center text-center gap-2">
                      <IconComponent className={`h-5 w-5 ${stat.iconColor}`} />
                      <div>
                        <p className="text-lg font-bold text-slate-900">
                          {stat.value}
                        </p>

                        <h3 className="text-[10px] sm:text-xs font-medium text-slate-600 uppercase tracking-wide">
                          {stat.title}
                        </h3>
                      </div>
                    </div>
                    {isActive && (
                      <div className="mt-1 text-[10px] font-medium text-indigo-600 text-center">
                        ✓ Active
                      </div>
                    )}
                  </div>
                );
              })}
            </div>

            <div className="bg-blue-50 p-4 sm:p-6 rounded-lg shadow-sm border border-blue-100">
              {" "}
              {/* changed to bg-blue-50 */}
              <div className="flex flex-col lg:flex-row gap-4 items-start lg:items-end justify-between">
                <div className="flex flex-col sm:flex-row gap-4 flex-1 w-full">
                  <div className="flex-1 max-w-xs">
                    <label className="block text-sm font-medium text-slate-700 mb-1">
                      Search Orders
                    </label>

                    <div className="relative">
                      <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-slate-400 h-4 w-4" />
                      <Input
                        placeholder="Order ID, Customer, Product..."
                        value={searchTerm}
                        onChange={(e) => {
                          handleSearchChange(e.target.value);
                        }}
                        className="pl-10 bg-white border-blue-100 focus:border-blue-300"
                      />
                    </div>
                  </div>

                  <div className="flex-1 max-w-xs">
                    <label className="block text-sm font-medium text-slate-700 mb-1">
                      Filter by Date
                    </label>
                    <Popover>
                      <PopoverTrigger asChild>
                        <Button
                          variant="outline"
                          className="w-full justify-start text-left font-normal 
                            bg-white border-blue-100 hover:border-blue-300 hover:bg-blue-50"
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
                              className="w-full bg-transparent border-blue-100 hover:bg-blue-50"
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
                  <Button
                    variant="outline"
                    onClick={handleExport}
                    className="border-blue-100 hover:bg-blue-50 bg-transparent"
                  >
                    <FileDown className="h-4 w-4 mr-2" />
                    <span className="hidden sm:inline">Export file</span>
                    <span className="sm:hidden">Export</span>
                  </Button>
                  <Button
                    variant="outline"
                    onClick={() =>
                      document.getElementById("file-input").click()
                    }
                    className="border-blue-100 hover:bg-blue-50"
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
                    className="border-blue-100 hover:bg-blue-50"
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
                className="bg-white border-blue-100 hover:bg-blue-50"
              >
                {selectAll ? "Deselect All" : "Select All"}
              </Button>

              <Button
                className="bg-indigo-600 hover:bg-indigo-700 text-white"
                onClick={handleOpenAssignPopup}
                disabled={selectedOrders.length === 0}
              >
                Assign Designer ({selectedOrders.length})
              </Button>

              <Button
                className="bg-blue-600 hover:bg-blue-700 text-white"
                onClick={handleAssignStaff}
                disabled={selectedOrders.length === 0}
              >
                Assign Staff ({selectedOrders.length})
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
                  <AlertDialogAction className="bg-indigo-600 hover:bg-indigo-700">
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
                  <AlertDialogAction className="bg-indigo-600 hover:bg-indigo-700">
                    OK
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>

            <div className="bg-blue-50 rounded-lg shadow-sm border border-blue-100">
              {" "}
              {/* unified bg color */}
              {loading ? (
                <div className="flex items-center justify-center p-12">
                  <div className="text-center">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 mx-auto"></div>
                    <p className="mt-4 text-slate-600">Loading orders...</p>
                  </div>
                </div>
              ) : error ? (
                <div className="flex items-center justify-center p-12">
                  <div className="text-center">
                    <AlertTriangle className="h-12 w-12 text-red-500 mx-auto" />
                    <p className="mt-4 text-red-600 font-medium">
                      Error loading orders
                    </p>
                    <p className="mt-2 text-slate-600 text-sm">{error}</p>
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
                        <TableRow className="bg-blue-100 hover:bg-blue-100">
                          {" "}
                          {/* updated header bg */}
                          <TableHead className="font-medium text-slate-700 uppercase text-xs tracking-wide whitespace-nowrap">
                            <Checkbox
                              checked={selectAll}
                              onCheckedChange={handleSelectAll}
                            />
                          </TableHead>
                          <TableHead
                            className="font-medium text-slate-700 uppercase text-xs tracking-wide whitespace-nowrap cursor-pointer hover:bg-blue-200 transition-colors"
                            onClick={() => handleSort("orderId")}
                          >
                            Order ID {renderSortIcon("orderId")}
                          </TableHead>
                          <TableHead
                            className="font-medium text-slate-700 uppercase text-xs tracking-wide whitespace-nowrap cursor-pointer hover:bg-blue-200 transition-colors"
                            onClick={() => handleSort("orderDate")}
                          >
                            Order Date {renderSortIcon("orderDate")}
                          </TableHead>
                          <TableHead
                            className="font-medium text-slate-700 uppercase text-xs tracking-wide whitespace-nowrap cursor-pointer hover:bg-blue-200 transition-colors"
                            onClick={() => handleSort("customerName")}
                          >
                            Customer {renderSortIcon("customerName")}
                          </TableHead>
                          <TableHead className="font-medium text-slate-700 uppercase text-xs tracking-wide whitespace-nowrap">
                            Status
                          </TableHead>
                          <TableHead
                            className="font-medium text-slate-700 uppercase text-xs tracking-wide whitespace-nowrap cursor-pointer hover:bg-blue-200 transition-colors"
                            onClick={() => handleSort("totalAmount")}
                          >
                            Amount {renderSortIcon("totalAmount")}
                          </TableHead>
                          <TableHead
                            className="font-medium text-slate-700 uppercase text-xs tracking-wide 
whitespace-nowrap"
                          >
                            Actions
                          </TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {paginatedOrders.length === 0 ? (
                          <TableRow>
                            <TableCell
                              colSpan={8}
                              className="text-center py-8 text-slate-500"
                            >
                              No orders found
                            </TableCell>
                          </TableRow>
                        ) : (
                          paginatedOrders.map((order) => (
                            <>
                              <TableRow
                                key={order.id}
                                className="hover:bg-blue-50 transition-colors"
                              >
                                {" "}
                                {/* updated hover color */}
                                <TableCell>
                                  <Checkbox
                                    checked={selectedOrders.includes(order.id)}
                                    onCheckedChange={() =>
                                      handleOrderSelect(order.id)
                                    }
                                  />
                                </TableCell>
                                <TableCell className="font-medium text-slate-900 whitespace-nowrap">
                                  {order.orderId}
                                </TableCell>
                                <TableCell className="text-slate-600 whitespace-nowrap">
                                  {order.orderDate}
                                </TableCell>
                                <TableCell className="min-w-[200px]">
                                  <div>
                                    <div className="font-medium text-slate-900">
                                      {order.customerName}
                                    </div>

                                    {order.email && (
                                      <div className="text-sm text-slate-500">
                                        {order.email}
                                      </div>
                                    )}
                                  </div>
                                </TableCell>
                                <TableCell className="whitespace-nowrap">
                                  {getStatusBadge(order.status)}
                                </TableCell>
                                <TableCell className="font-medium text-slate-900 whitespace-nowrap">
                                  {order.totalAmount}
                                </TableCell>
                                <TableCell className="whitespace-nowrap">
                                  <div className="flex items-center gap-2">
                                    <Button
                                      variant="outline"
                                      size="sm"
                                      className={`bg-transparent hover:bg-blue-100 border-blue-200 transition-colors ${
                                        expandedOrderId === order.id
                                          ? "bg-blue-100"
                                          : ""
                                      }`}
                                      onClick={() =>
                                        setExpandedOrderId(
                                          expandedOrderId === order.id
                                            ? null
                                            : order.id
                                        )
                                      }
                                      title="Expand Details"
                                    >
                                      <ChevronDown
                                        className={`h-4 w-4 transition-transform ${
                                          expandedOrderId === order.id
                                            ? "rotate-180"
                                            : ""
                                        }`}
                                      />
                                    </Button>

                                    {order.status === "Cần Check Design" && (
                                      <>
                                        <Button
                                          variant="outline"
                                          size="sm"
                                          className="bg-transparent hover:bg-green-50 text-green-600 hover:text-green-700 border-green-200"
                                          onClick={() =>
                                            handleApproveDesign(order.id)
                                          }
                                          title="Approve Design"
                                        >
                                          ✓
                                        </Button>
                                        <Button
                                          variant="outline"
                                          size="sm"
                                          className="bg-transparent hover:bg-red-50 text-red-600 hover:text-red-700 border-red-200"
                                          onClick={() =>
                                            handleRejectDesign(order.id)
                                          }
                                          title="Reject Design"
                                        >
                                          ✕
                                        </Button>
                                      </>
                                    )}
                                    <Dialog
                                      open={isDialogOpen}
                                      onOpenChange={setIsDialogOpen}
                                    >
                                      <DialogTrigger asChild>
                                        <Button
                                          variant="outline"
                                          size="sm"
                                          className="bg-transparent hover:bg-blue-100 border-blue-200"
                                          onClick={() =>
                                            handleViewDetails(order)
                                          }
                                          title="View Details"
                                        >
                                          <Eye className="h-4 w-4 mr-1" />
                                        </Button>
                                      </DialogTrigger>
                                      <DialogContent className="max-w-5xl max-h-[90vh] overflow-y-auto">
                                        <DialogHeader>
                                          <DialogTitle className="flex items-center justify-between">
                                            <div className="flex items-center gap-2">
                                              <Package className="h-5 w-5" />
                                              Order Details -
                                              {editedOrder?.orderId}
                                            </div>
                                          </DialogTitle>
                                        </DialogHeader>

                                        {editedOrder && (
                                          <div className="space-y-6">
                                            <div className="bg-blue-50 p-4 rounded-lg border border-blue-100">
                                              <h3 className="font-semibold text-lg mb-3 text-slate-900">
                                                Customer Information
                                              </h3>

                                              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                                                <div>
                                                  <Label className="text-sm text-slate-500 font-medium">
                                                    Name *
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
                                                      className="mt-1 border-blue-100 focus:border-blue-300"
                                                    />
                                                  ) : (
                                                    <p className="font-medium text-slate-900 mt-1">
                                                      {editedOrder.customerInfo
                                                        .name || "N/A"}
                                                    </p>
                                                  )}
                                                </div>
                                                <div>
                                                  <Label className="text-sm text-slate-500 font-medium">
                                                    Phone *
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
                                                      className="mt-1 border-blue-100 focus:border-blue-300"
                                                    />
                                                  ) : (
                                                    <p className="font-medium text-slate-900 mt-1">
                                                      {editedOrder.customerInfo
                                                        .phone || "N/A"}
                                                    </p>
                                                  )}
                                                </div>
                                                <div>
                                                  <Label className="text-sm text-slate-500 font-medium">
                                                    Email *
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
                                                      className="mt-1 border-blue-100 focus:border-blue-300"
                                                    />
                                                  ) : (
                                                    <p className="font-medium text-slate-900 mt-1">
                                                      {editedOrder.customerInfo
                                                        .email || "N/A"}
                                                    </p>
                                                  )}
                                                </div>
                                                <div>
                                                  <Label className="text-sm text-slate-500 font-medium">
                                                    Address *
                                                  </Label>

                                                  {isEditMode ? (
                                                    <Input
                                                      value={
                                                        editedOrder.customerInfo
                                                          .address
                                                      }
                                                      onChange={(e) =>
                                                        handleCustomerInfoChange(
                                                          "address",

                                                          e.target.value
                                                        )
                                                      }
                                                      className="mt-1 border-blue-100 focus:border-blue-300"
                                                    />
                                                  ) : (
                                                    <p className="font-medium text-slate-900 mt-1">
                                                      {editedOrder.customerInfo
                                                        .address || "N/A"}
                                                    </p>
                                                  )}
                                                </div>
                                                <div>
                                                  <Label className="text-sm text-slate-500 font-medium">
                                                    Address Line 2
                                                  </Label>

                                                  {isEditMode ? (
                                                    <Input
                                                      value={
                                                        editedOrder.customerInfo
                                                          .address1 || ""
                                                      }
                                                      onChange={(e) =>
                                                        handleCustomerInfoChange(
                                                          "address1",

                                                          e.target.value
                                                        )
                                                      }
                                                      className="mt-1 border-blue-100 focus:border-blue-300"
                                                    />
                                                  ) : (
                                                    <p className="font-medium text-slate-900 mt-1">
                                                      {editedOrder.customerInfo
                                                        .address1 || "N/A"}
                                                    </p>
                                                  )}
                                                </div>
                                                <div>
                                                  <Label className="text-sm text-slate-500 font-medium">
                                                    Zipcode *
                                                  </Label>

                                                  {isEditMode ? (
                                                    <Input
                                                      value={
                                                        editedOrder.customerInfo
                                                          .zipcode
                                                      }
                                                      onChange={(e) =>
                                                        handleCustomerInfoChange(
                                                          "zipcode",

                                                          e.target.value
                                                        )
                                                      }
                                                      className="mt-1 border-blue-100 focus:border-blue-300"
                                                    />
                                                  ) : (
                                                    <p className="font-medium text-slate-900 mt-1">
                                                      {editedOrder.customerInfo
                                                        .zipcode || "N/A"}
                                                    </p>
                                                  )}
                                                </div>
                                                <div>
                                                  <Label className="text-sm text-slate-500 font-medium">
                                                    Ship City *
                                                  </Label>

                                                  {isEditMode ? (
                                                    <Input
                                                      value={
                                                        editedOrder.customerInfo
                                                          .city
                                                      }
                                                      onChange={(e) =>
                                                        handleCustomerInfoChange(
                                                          "city",

                                                          e.target.value
                                                        )
                                                      }
                                                      className="mt-1 border-blue-100 focus:border-blue-300"
                                                    />
                                                  ) : (
                                                    <p className="font-medium text-slate-900 mt-1">
                                                      {editedOrder.customerInfo
                                                        .city || "N/A"}
                                                    </p>
                                                  )}
                                                </div>
                                                <div>
                                                  <Label className="text-sm text-slate-500 font-medium">
                                                    Ship State *
                                                  </Label>

                                                  {isEditMode ? (
                                                    <Input
                                                      value={
                                                        editedOrder.customerInfo
                                                          .state
                                                      }
                                                      onChange={(e) =>
                                                        handleCustomerInfoChange(
                                                          "state",

                                                          e.target.value
                                                        )
                                                      }
                                                      className="mt-1 border-blue-100 focus:border-blue-300"
                                                    />
                                                  ) : (
                                                    <p className="font-medium text-slate-900 mt-1">
                                                      {editedOrder.customerInfo
                                                        .state || "N/A"}
                                                    </p>
                                                  )}
                                                </div>
                                                <div>
                                                  <Label className="text-sm text-slate-500 font-medium">
                                                    Ship Country *
                                                  </Label>

                                                  {isEditMode ? (
                                                    <Input
                                                      value={
                                                        editedOrder.customerInfo
                                                          .country
                                                      }
                                                      onChange={(e) =>
                                                        handleCustomerInfoChange(
                                                          "country",

                                                          e.target.value
                                                        )
                                                      }
                                                      className="mt-1 border-blue-100 focus:border-blue-300"
                                                    />
                                                  ) : (
                                                    <p className="font-medium text-slate-900 mt-1">
                                                      {editedOrder.customerInfo
                                                        .country || "N/A"}
                                                    </p>
                                                  )}
                                                </div>
                                              </div>
                                            </div>

                                            <div>
                                              <h3 className="font-semibold text-lg mb-3 text-slate-900">
                                                Product Details
                                              </h3>
                                              <div className="space-y-4">
                                                {editedOrder.products.map(
                                                  (product, index) => (
                                                    <div
                                                      key={index}
                                                      className="border border-blue-100 rounded-lg p-4 bg-white hover:shadow-md transition-shadow"
                                                    >
                                                      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
                                                        <div>
                                                          <Label className="text-sm text-slate-500 font-medium">
                                                            Product Name
                                                          </Label>

                                                          {isEditMode ? (
                                                            <Input
                                                              value={
                                                                product.name
                                                              }
                                                              onChange={(e) =>
                                                                handleProductChange(
                                                                  index,

                                                                  "name",
                                                                  e.target.value
                                                                )
                                                              }
                                                              className="mt-1 border-blue-100 focus:border-blue-300"
                                                            />
                                                          ) : (
                                                            <p className="font-medium text-slate-900 mt-1">
                                                              {product.name}
                                                            </p>
                                                          )}
                                                        </div>

                                                        <div>
                                                          <Label className="text-sm text-slate-500 font-medium">
                                                            Size/Variant
                                                          </Label>
                                                          {isEditMode ? (
                                                            <Input
                                                              value={
                                                                product.size
                                                              }
                                                              onChange={(e) =>
                                                                handleProductChange(
                                                                  index,

                                                                  "size",
                                                                  e.target.value
                                                                )
                                                              }
                                                              className="mt-1 border-blue-100 focus:border-blue-300"
                                                            />
                                                          ) : (
                                                            <p className="font-medium text-slate-900 mt-1">
                                                              {product.size ||
                                                                "N/A"}
                                                            </p>
                                                          )}
                                                        </div>

                                                        <div>
                                                          <Label className="text-sm text-slate-500 font-medium">
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
                                                                    e.target
                                                                      .value
                                                                  )
                                                                )
                                                              }
                                                              className="mt-1 border-blue-100 focus:border-blue-300"
                                                            />
                                                          ) : (
                                                            <p className="font-medium text-slate-900 mt-1">
                                                              {product.quantity}
                                                            </p>
                                                          )}
                                                        </div>

                                                        <div>
                                                          <Label className="text-sm text-slate-500 font-medium">
                                                            Unit Price
                                                          </Label>

                                                          <p className="font-medium text-slate-900 mt-1">
                                                            $
                                                            {product.price?.toFixed(
                                                              2
                                                            ) || "0.00"}
                                                          </p>
                                                        </div>
                                                        <div>
                                                          <Label className="text-sm text-slate-500 font-medium">
                                                            Total Price
                                                          </Label>

                                                          <p className="font-bold text-indigo-600 mt-1">
                                                            $
                                                            {(
                                                              (product.price ||
                                                                0) *
                                                              (product.quantity ||
                                                                1)
                                                            ).toFixed(2)}
                                                          </p>
                                                        </div>

                                                        <div>
                                                          <Label className="text-sm text-slate-500 font-medium">
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
                                                              className="mt-1 border-blue-100 focus:border-blue-300"
                                                            />
                                                          ) : (
                                                            <p className="font-medium text-slate-900 mt-1">
                                                              {product.accessory ||
                                                                "None"}
                                                            </p>
                                                          )}
                                                        </div>

                                                        <div className="flex items-center space-x-2 lg:col-span-2">
                                                          <Checkbox
                                                            id={`activeTTS-${index}`}
                                                            checked={
                                                              product.activeTTS ||
                                                              false
                                                            }
                                                            disabled={
                                                              !isEditMode
                                                            }
                                                            onCheckedChange={(
                                                              checked
                                                            ) =>
                                                              handleProductChange(
                                                                index,

                                                                "activeTTS",
                                                                checked
                                                              )
                                                            }
                                                          />

                                                          <Label
                                                            htmlFor={`activeTTS-${index}`}
                                                            className="text-sm"
                                                          >
                                                            Active TTS (+$1.00)
                                                          </Label>
                                                        </div>
                                                      </div>

                                                      <div className="mt-4 pt-4 border-t border-blue-100">
                                                        <h4 className="font-medium text-sm text-slate-700 mb-3">
                                                          Order Files for this
                                                          Product
                                                        </h4>
                                                        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                                                          {/* Design File */}
                                                          <div
                                                            className="border border-blue-100 
rounded-lg p-3 hover:shadow-md transition-shadow bg-blue-50"
                                                          >
                                                            <Label className="text-xs text-slate-500 font-medium">
                                                              Design File
                                                            </Label>

                                                            <div className="mt-2">
                                                              {product.linkFileDesign &&
                                                              product.linkFileDesign !==
                                                                "#" ? (
                                                                <>
                                                                  <img
                                                                    src={
                                                                      product.linkFileDesign ||
                                                                      "/placeholder.svg" ||
                                                                      "/placeholder.svg"
                                                                    }
                                                                    alt="Design File"
                                                                    className="w-full h-32 object-cover rounded border border-blue-200"
                                                                    onError={(
                                                                      e
                                                                    ) => {
                                                                      console.log(
                                                                        "[v0] Design file image failed to load:",

                                                                        e.target
                                                                          .src
                                                                      );
                                                                      e.target.style.display =
                                                                        "none";
                                                                      e.target.nextElementSibling.style.display =
                                                                        "flex";
                                                                    }}
                                                                  />

                                                                  <div
                                                                    className="w-full h-32 bg-blue-100 rounded border border-blue-200 flex items-center justify-center"
                                                                    style={{
                                                                      display:
                                                                        "none",
                                                                    }}
                                                                  >
                                                                    <QrCode className="h-8 w-8 text-indigo-400" />
                                                                  </div>
                                                                </>
                                                              ) : (
                                                                <div className="w-full h-32 bg-blue-100 rounded border border-blue-200 flex items-center justify-center">
                                                                  <QrCode className="h-8 w-8 text-indigo-400" />
                                                                </div>
                                                              )}

                                                              <p className="text-xs mt-2 text-slate-600 truncate">
                                                                design-file-
                                                                {index + 1}.psd
                                                              </p>

                                                              {product.linkFileDesign &&
                                                                product.linkFileDesign !==
                                                                  "#" && (
                                                                  <a
                                                                    href={
                                                                      product.linkFileDesign
                                                                    }
                                                                    target="_blank"
                                                                    rel="noopener noreferrer"
                                                                    className="text-xs text-indigo-600 hover:underline block mt-1 truncate"
                                                                  >
                                                                    View file
                                                                  </a>
                                                                )}

                                                              <Button
                                                                variant="outline"
                                                                size="sm"
                                                                className="mt-2 w-full bg-transparent hover:bg-blue-100 border-blue-200"
                                                                onClick={() =>
                                                                  handleDownload(
                                                                    {
                                                                      name: `design-file-${
                                                                        index +
                                                                        1
                                                                      }.psd`,
                                                                      url: product.linkFileDesign,
                                                                    }
                                                                  )
                                                                }
                                                                disabled={
                                                                  !product.linkFileDesign ||
                                                                  product.linkFileDesign ===
                                                                    "#"
                                                                }
                                                              >
                                                                <Download className="h-4 w-4 mr-2" />
                                                                Download
                                                              </Button>
                                                            </div>
                                                          </div>

                                                          {/* Thanks Card */}

                                                          <div className="border border-blue-100 rounded-lg p-3 hover:shadow-md transition-shadow bg-blue-50">
                                                            <Label className="text-xs text-slate-500 font-medium">
                                                              Thanks Card
                                                            </Label>
                                                            <div className="mt-2">
                                                              {product.linkThanksCard &&
                                                              product.linkThanksCard !==
                                                                "#" ? (
                                                                <>
                                                                  <img
                                                                    src={
                                                                      product.linkThanksCard ||
                                                                      "/placeholder.svg" ||
                                                                      "/placeholder.svg"
                                                                    }
                                                                    alt="Thanks Card"
                                                                    className="w-full h-32 object-cover rounded border border-blue-200"
                                                                    onError={(
                                                                      e
                                                                    ) => {
                                                                      console.log(
                                                                        "[v0] Thanks card image failed to load:",

                                                                        e.target
                                                                          .src
                                                                      );
                                                                      e.target.style.display =
                                                                        "none";
                                                                      e.target.nextElementSibling.style.display =
                                                                        "flex";
                                                                    }}
                                                                  />

                                                                  <div
                                                                    className="w-full h-32 bg-blue-100 rounded border border-blue-200 flex items-center justify-center"
                                                                    style={{
                                                                      display:
                                                                        "none",
                                                                    }}
                                                                  >
                                                                    <QrCode className="h-8 w-8 text-indigo-400" />
                                                                  </div>
                                                                </>
                                                              ) : (
                                                                <div className="w-full h-32 bg-blue-100 rounded border border-blue-200 flex items-center justify-center">
                                                                  <QrCode className="h-8 w-8 text-indigo-400" />
                                                                </div>
                                                              )}

                                                              <p className="text-xs mt-2 text-slate-600 truncate">
                                                                thanks-card-
                                                                {index + 1}.jpg
                                                              </p>

                                                              {product.linkThanksCard &&
                                                                product.linkThanksCard !==
                                                                  "#" && (
                                                                  <a
                                                                    href={
                                                                      product.linkThanksCard
                                                                    }
                                                                    target="_blank"
                                                                    rel="noopener noreferrer"
                                                                    className="text-xs text-indigo-600 hover:underline block mt-1 truncate"
                                                                  >
                                                                    View file
                                                                  </a>
                                                                )}

                                                              <Button
                                                                variant="outline"
                                                                size="sm"
                                                                className="mt-2 w-full bg-transparent hover:bg-blue-100 border-blue-200"
                                                                onClick={() =>
                                                                  handleDownload(
                                                                    {
                                                                      name: `thanks-card-${
                                                                        index +
                                                                        1
                                                                      }.jpg`,
                                                                      url: product.linkThanksCard,
                                                                    }
                                                                  )
                                                                }
                                                                disabled={
                                                                  !product.linkThanksCard ||
                                                                  product.linkThanksCard ===
                                                                    "#"
                                                                }
                                                              >
                                                                <Download className="h-4 w-4 mr-2" />
                                                                Download
                                                              </Button>
                                                            </div>
                                                          </div>
                                                          {/* Product Image */}

                                                          <div className="border border-blue-100 rounded-lg p-3 hover:shadow-md transition-shadow bg-blue-50">
                                                            <Label className="text-xs text-slate-500 font-medium">
                                                              Product Image
                                                            </Label>
                                                            <div className="mt-2">
                                                              {product.linkImg &&
                                                              product.linkImg !==
                                                                "#" ? (
                                                                <>
                                                                  <img
                                                                    src={
                                                                      product.linkImg ||
                                                                      "/placeholder.svg" ||
                                                                      "/placeholder.svg"
                                                                    }
                                                                    alt="Product Image"
                                                                    className="w-full h-32 object-cover rounded border border-blue-200"
                                                                    onError={(
                                                                      e
                                                                    ) => {
                                                                      console.log(
                                                                        "[v0] Product image failed to load:",

                                                                        e.target
                                                                          .src
                                                                      );
                                                                      e.target.style.display =
                                                                        "none";
                                                                      e.target.nextElementSibling.style.display =
                                                                        "flex";
                                                                    }}
                                                                  />

                                                                  <div className="w-full h-32 bg-blue-100 rounded border border-blue-200 flex items-center justify-center">
                                                                    <QrCode className="h-8 w-8 text-indigo-400" />
                                                                  </div>
                                                                </>
                                                              ) : (
                                                                <div className="w-full h-32 bg-blue-100 rounded border border-blue-200 flex items-center justify-center">
                                                                  <QrCode className="h-8 w-8 text-indigo-400" />
                                                                </div>
                                                              )}

                                                              <p className="text-xs mt-2 text-slate-600 truncate">
                                                                product-image-
                                                                {index + 1}.jpg
                                                              </p>

                                                              {product.linkImg &&
                                                                product.linkImg !==
                                                                  "#" && (
                                                                  <a
                                                                    href={
                                                                      product.linkImg
                                                                    }
                                                                    target="_blank"
                                                                    rel="noopener noreferrer"
                                                                    className="text-xs text-indigo-600 hover:underline block mt-1 truncate"
                                                                  >
                                                                    View file
                                                                  </a>
                                                                )}

                                                              <Button
                                                                variant="outline"
                                                                size="sm"
                                                                className="mt-2 w-full bg-transparent hover:bg-blue-100 border-blue-200"
                                                                onClick={() =>
                                                                  handleDownload(
                                                                    {
                                                                      name: `product-image-${
                                                                        index +
                                                                        1
                                                                      }.jpg`,
                                                                      url: product.linkImg,
                                                                    }
                                                                  )
                                                                }
                                                                disabled={
                                                                  !product.linkImg ||
                                                                  product.linkImg ===
                                                                    "#"
                                                                }
                                                              >
                                                                <Download className="h-4 w-4 mr-2" />
                                                                Download
                                                              </Button>
                                                            </div>
                                                          </div>
                                                        </div>
                                                      </div>
                                                    </div>
                                                  )
                                                )}
                                              </div>
                                              {editedOrder.orderNotes && (
                                                <div className="mt-4 p-4 bg-amber-50 border border-amber-200 rounded-lg">
                                                  <Label className="text-sm text-amber-800 font-medium">
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
                                                      className="mt-1 bg-white border-amber-200 focus:border-amber-300"
                                                      rows={3}
                                                    />
                                                  ) : (
                                                    <p className="text-amber-800 mt-1">
                                                      {editedOrder.orderNotes}
                                                    </p>
                                                  )}
                                                </div>
                                              )}
                                            </div>

                                            <div className="bg-blue-50 p-4 rounded-lg border border-blue-100">
                                              <h3 className="font-semibold text-lg mb-3 text-slate-900">
                                                Order Status & Timeline
                                              </h3>
                                              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                                                <div>
                                                  <Label className="text-sm text-slate-500 font-medium">
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
                                                      <SelectTrigger className="mt-1 bg-white border-blue-100 focus:border-blue-300">
                                                        <SelectValue />
                                                      </SelectTrigger>

                                                      <SelectContent>
                                                        <SelectItem value="Draft">
                                                          Draft
                                                        </SelectItem>

                                                        <SelectItem value="Pending Design">
                                                          Pending Design
                                                        </SelectItem>

                                                        <SelectItem value="Assigned Designer">
                                                          Assigned Designer
                                                        </SelectItem>
                                                        <SelectItem value="Designing">
                                                          Designing
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
                                                      {getStatusBadge(
                                                        editedOrder.status
                                                      )}
                                                    </div>
                                                  )}
                                                </div>
                                                <div>
                                                  <Label
                                                    className="text-sm 
text-slate-500 font-medium"
                                                  >
                                                    Order Date
                                                  </Label>
                                                  <p className="font-medium text-slate-900 mt-1">
                                                    {editedOrder.orderDate}
                                                  </p>
                                                </div>
                                                <div>
                                                  <Label className="text-sm text-slate-500 font-medium">
                                                    Created Time
                                                  </Label>
                                                  <p className="font-medium text-slate-900 mt-1">
                                                    {editedOrder.timeCreated ||
                                                      "N/A"}
                                                  </p>
                                                </div>

                                                <div>
                                                  <Label className="text-sm text-slate-500 font-medium">
                                                    Order ID
                                                  </Label>
                                                  <p className="font-medium text-slate-900 mt-1">
                                                    {editedOrder.orderId}
                                                  </p>
                                                </div>
                                                <div>
                                                  <Label className="text-sm text-slate-500 font-medium">
                                                    Total Amount
                                                  </Label>
                                                  <p className="font-bold text-indigo-600 text-lg mt-1">
                                                    {editedOrder.totalAmount}
                                                  </p>
                                                </div>
                                              </div>
                                            </div>

                                            {/* QR Code Section */}
                                            <div
                                              className="bg-blue-50 p-4 rounded-lg border 
border-blue-100"
                                            >
                                              <h4 className="font-medium mb-2 text-slate-900">
                                                QR Code for Order{" "}
                                                {editedOrder.orderId}
                                              </h4>

                                              <div className="w-32 h-32 bg-white border-2 border-blue-300 rounded flex items-center justify-center">
                                                <QrCode className="h-16 w-16 text-indigo-400" />
                                              </div>
                                            </div>
                                          </div>
                                        )}

                                        {editedOrder?.status ===
                                          "Cần Check Design" &&
                                          !isEditMode && (
                                            <DialogFooter className="flex gap-2">
                                              <Button
                                                variant="outline"
                                                onClick={() =>
                                                  setIsDialogOpen(false)
                                                }
                                              >
                                                Close
                                              </Button>
                                              <Button
                                                onClick={() =>
                                                  handleRejectDesign(
                                                    editedOrder.id
                                                  )
                                                }
                                                className="bg-red-600 hover:bg-red-700"
                                              >
                                                ✕ Reject Design
                                              </Button>
                                              <Button
                                                onClick={() =>
                                                  handleApproveDesign(
                                                    editedOrder.id
                                                  )
                                                }
                                                className="bg-green-600 hover:bg-green-700"
                                              >
                                                ✓ Approve Design
                                              </Button>
                                            </DialogFooter>
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
                                              className="bg-indigo-600 hover:bg-indigo-700"
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
                                          className="bg-transparent hover:bg-red-50 text-red-600 hover:text-red-700 border-red-200"
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
                                            Are you sure you want to delete
                                            order {order.orderId}? This action
                                            cannot be undone.
                                          </AlertDialogDescription>
                                        </AlertDialogHeader>
                                        <AlertDialogFooter>
                                          <AlertDialogCancel>
                                            Cancel
                                          </AlertDialogCancel>
                                          <AlertDialogAction
                                            onClick={() =>
                                              handleDelete(order.id)
                                            }
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

                              {expandedOrderId === order.id &&
                                order.products &&
                                order.products.length > 0 && (
                                  <TableRow className="bg-blue-100">
                                    <TableCell colSpan={8} className="p-4">
                                      <div className="space-y-3">
                                        <h4 className="font-semibold text-slate-900 text-sm">
                                          Order Items:
                                        </h4>
                                        <div className="space-y-2">
                                          {order.products.map((item, idx) => (
                                            <div
                                              key={idx}
                                              className="flex items-start gap-3 p-3 bg-white rounded border border-blue-200 hover:shadow-sm transition-shadow"
                                            >
                                              {/* Product Image */}
                                              {item.linkImg ? (
                                                <img
                                                  src={
                                                    item.linkImg ||
                                                    "/placeholder.svg"
                                                  }
                                                  alt={item.name || "Product"}
                                                  className="w-16 h-16 rounded object-cover border border-blue-200"
                                                />
                                              ) : (
                                                <div className="w-16 h-16 rounded bg-blue-100 border border-blue-200 flex items-center justify-center text-slate-400 text-xs">
                                                  No Image
                                                </div>
                                              )}

                                              {/* Item Details */}
                                              <div className="flex-1">
                                                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                                                  <div>
                                                    <span className="text-slate-600 font-medium">
                                                      Price:
                                                    </span>
                                                    <p className="text-slate-900 font-semibold">
                                                      ${item.price || "0"}
                                                    </p>
                                                  </div>
                                                  <div>
                                                    <span className="text-slate-600 font-medium">
                                                      Quantity:
                                                    </span>
                                                    <p className="text-slate-900 font-semibold">
                                                      {item.quantity || "0"}
                                                    </p>
                                                  </div>
                                                  <div>
                                                    <span className="text-slate-600 font-medium">
                                                      Total:
                                                    </span>
                                                    <p className="text-indigo-600 font-bold">
                                                      $
                                                      {(
                                                        Number.parseFloat(
                                                          item.price || 0
                                                        ) * (item.quantity || 0)
                                                      ).toFixed(2)}
                                                    </p>
                                                  </div>
                                                  <div>
                                                    <span className="text-slate-600 font-medium">
                                                      Name:
                                                    </span>
                                                    <p className="text-slate-900">
                                                      {item.name || "N/A"}
                                                    </p>
                                                  </div>
                                                </div>
                                              </div>
                                            </div>
                                          ))}
                                        </div>
                                      </div>
                                    </TableCell>
                                  </TableRow>
                                )}
                            </>
                          ))
                        )}
                      </TableBody>
                    </Table>
                  </div>

                  {/* Pagination Footer */}
                  <div className="bg-blue-100 px-4 py-3 border-t border-blue-200 sm:px-6">
                    <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
                      {/* Thêm Items Per Page Selector */}
                      <div className="flex items-center gap-2">
                        <span className="text-sm text-slate-600">
                          Items per page:
                        </span>
                        <Select
                          value={itemsPerPage.toString()}
                          onValueChange={handleItemsPerPageChange}
                        >
                          <SelectTrigger className="w-[70px] bg-white border-blue-200 hover:bg-blue-50">
                            <SelectValue placeholder={itemsPerPage} />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="5">5</SelectItem>
                            <SelectItem value="10">10</SelectItem>
                            <SelectItem value="15">15</SelectItem>
                            <SelectItem value="20">20</SelectItem>
                            <SelectItem value="50">50</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>

                      <div className="text-sm text-slate-600">
                        Showing{" "}
                        {paginatedOrders.length > 0
                          ? (page - 1) * itemsPerPage + 1
                          : 0}{" "}
                        to {Math.min(page * itemsPerPage, totalOrdersCount)} of{" "}
                        {totalOrdersCount}
                      </div>
                      <div className="flex items-center gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          disabled={page === 1}
                          onClick={() => setPage(page - 1)}
                          className="disabled:opacity-50 border-blue-200 hover:bg-blue-50"
                        >
                          <ChevronLeft className="h-4 w-4" />
                        </Button>

                        {/* Sửa lỗi: Thay thế React.Fragment bằng cú pháp rút gọn <> */}
                        {Array.from({ length: totalPages }, (_, i) => i + 1)
                          .filter(
                            (pageNum) =>
                              pageNum === 1 ||
                              pageNum === totalPages ||
                              (pageNum >= page - 2 && pageNum <= page + 2)
                          )
                          .map((pageNum, index, arr) => (
                            <React.Fragment key={pageNum}>
                              {/* Thêm dấu ... nếu cần */}
                              {index > 0 && pageNum > arr[index - 1] + 1 && (
                                <span className="text-slate-600 mx-1">...</span>
                              )}

                              <Button
                                key={pageNum}
                                variant={
                                  page === pageNum ? "default" : "outline"
                                }
                                size="sm"
                                onClick={() => setPage(pageNum)}
                                className={`w-8 h-8 p-0 ${
                                  page === pageNum
                                    ? "bg-indigo-600 hover:bg-indigo-700"
                                    : "border-blue-200 hover:bg-blue-50"
                                }`}
                              >
                                {pageNum}
                              </Button>
                            </React.Fragment>
                          ))}
                        <Button
                          variant="outline"
                          size="sm"
                          disabled={page === totalPages}
                          onClick={() => setPage(page + 1)}
                          className="disabled:opacity-50 border-blue-200 hover:bg-blue-50"
                        >
                          <ChevronRight className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  </div>
                </>
              )}
            </div>
          </div>
          {/* Result Popup */}
          <AlertDialog
            open={showResultDialog}
            onOpenChange={setShowResultDialog}
          >
            <AlertDialogContent className="max-w-sm">
              <AlertDialogHeader>
                <AlertDialogTitle>Result Action</AlertDialogTitle>
                <AlertDialogDescription>{resultMessage}</AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogAction className="bg-indigo-600 hover:bg-indigo-700">
                  OK
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </main>
      </div>
      <MakeManualModal
        isOpen={showMakeManualModal}
        onClose={() => setShowMakeManualModal(false)}
      />
      <Dialog open={isAssignPopupOpen} onOpenChange={setIsAssignPopupOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Assign Designer</DialogTitle>
            <DialogDescription>
              Choose a designer to assign to {selectedOrders.length} selected
              order(s).
            </DialogDescription>
          </DialogHeader>

          <div className="mt-4 space-y-4">
            <Label htmlFor="designer">Select Designer</Label>
            <Select
              value={selectedDesignerId}
              onValueChange={(value) => setSelectedDesignerId(value)}
            >
              <SelectTrigger className="border-blue-100 focus:border-blue-300">
                <SelectValue placeholder="Choose designer" />
              </SelectTrigger>
              <SelectContent>
                {designers.map((d) => (
                  <SelectItem key={d.designerUserId} value={d.designerUserId}>
                    {d.designerName || "Unnamed Designer"}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <div className="flex justify-end gap-2 mt-4">
              <Button
                variant="outline"
                onClick={() => setIsAssignPopupOpen(false)}
                className="border-blue-100 hover:bg-blue-50"
              >
                Cancel
              </Button>

              <Button
                onClick={handleConfirmAssignDesigner}
                className="bg-indigo-600 hover:bg-indigo-700"
              >
                OK
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
      {/* ✅ Success Dialog */}
      <Dialog open={showSuccessDialog} onOpenChange={setShowSuccessDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="text-green-600">Success</DialogTitle>
          </DialogHeader>
          <p className="text-gray-700 py-4">{successMessage}</p>

          <DialogFooter>
            <Button
              onClick={() => setShowSuccessDialog(false)}
              className="bg-green-600 hover:bg-green-700"
            >
              OK
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      {/* ❌ Error Dialog */}
      <Dialog open={showErrorDialog} onOpenChange={setShowErrorDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="text-red-600">Error</DialogTitle>
          </DialogHeader>
          <p className="text-gray-700 py-4">{errorMessage}</p>
          <DialogFooter>
            <Button
              onClick={() => setShowErrorDialog(false)}
              className="bg-red-600 hover:bg-red-700"
            >
              Close
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
