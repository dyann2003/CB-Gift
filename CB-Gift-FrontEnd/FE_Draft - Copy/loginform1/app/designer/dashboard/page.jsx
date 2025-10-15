"use client";

import { useState, useEffect } from "react";
// Import các components UI cần thiết cho Dashboard, Filter và Dialog
import { Input } from "@/components/ui/input";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";

import DesignerHeader from "@/components/layout/designer/header";
import DesignerSidebar from "@/components/layout/designer/sidebar";
import { Badge } from "@/components/ui/badge";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Eye, Search, Download } from "lucide-react";

// Định nghĩa các trạng thái design cố định
const DESIGN_STATUSES = {
    "3": { name: "Cần Design", color: "bg-red-500", code: "NEEDDESIGN" },
    "4": { name: "Đang làm Design", color: "bg-yellow-500", code: "DESIGNING" },
    "5": { name: "Cần Check Design", color: "bg-blue-500", code: "CHECKDESIGN" },
    "6": { name: "Thiết kế Lại (Design Lỗi)", color: "bg-purple-500", code: "DESIGN_REDO" },
};

export default function DesignerDashboard() {
    const [currentPage, setCurrentPage] = useState("dashboard");
    const [assignedOrders, setAssignedOrders] = useState([]);
    const [loading, setLoading] = useState(true);
    const [counts, setCounts] = useState({});
    
    // State cho tính năng Filter và View Detail
    const [searchTerm, setSearchTerm] = useState("");
    const [statusFilter, setStatusFilter] = useState("all");
    const [selectedAssignment, setSelectedAssignment] = useState(null); 

    // Hàm gọi API và xử lý dữ liệu
    useEffect(() => {
        const fetchTasks = async () => {
            try {
                setLoading(true);
                
                const res = await fetch("https://localhost:7015/api/designer/tasks", { 
                    credentials: "include", 
                });
                
                if (!res.ok) {
                    throw new Error(`HTTP error! status: ${res.status}`);
                }
                
                const apiData = await res.json();

                const mappedData = (apiData || []).map((item) => ({
                    ...item,
                    id: item.orderDetailId.toString(), 
                    // <<< ĐÃ SỬA: LẤY TRỰC TIẾP TỪ orderStatus VÀ CHUYỂN THÀNH CHUỖI >>>
                    OrderStatus: String(item.orderStatus) || "3", 
                    customerName: `Customer for ${item.orderCode}`, 
                }));

                setAssignedOrders(mappedData);
                calculateCounts(mappedData);
            } catch (error) {
                console.error("Failed to fetch assigned tasks:", error);
                setAssignedOrders([]);
                calculateCounts([]);
            } finally {
                setLoading(false);
            }
        };

        fetchTasks();
    }, []);

    // Hàm tính toán số lượng cho Stats Cards và Filter Count
    const calculateCounts = (orders) => {
        const initialCounts = { "3": 0, "4": 0, "5": 0, "6": 0, total: orders.length };
        
        const newCounts = orders.reduce((acc, order) => {
            // <<< Dùng order.OrderStatus >>>
            const statusKey = String(order.OrderStatus || "3");
            
            if (acc.hasOwnProperty(statusKey)) {
                acc[statusKey] += 1;
            }
            return acc;
        }, initialCounts);

        setCounts(newCounts);
    };
    
    // Hàm tính toán số lượng hiển thị trong Filter Select
    const getFilterCounts = () => {
        const totalCounts = { all: assignedOrders.length };
        Object.keys(DESIGN_STATUSES).forEach(key => {
            totalCounts[key] = counts[key] || 0;
        });
        return totalCounts;
    };
    const filterCounts = getFilterCounts();


    // Hàm xử lý Click vào Badge/Stats Card để Filter
    const handleStatusBadgeClick = (statusKey) => {
        if (statusFilter === statusKey) {
            setStatusFilter("all");
        } else {
            setStatusFilter(statusKey);
        }
        setSearchTerm(""); 
    };

    // Hàm hiển thị Badge trạng thái (có thể click)
    const getStatusBadge = (statusKey) => {
        const statusInfo = DESIGN_STATUSES[String(statusKey)] || { name: "Không xác định", color: "bg-gray-500" };
        
        return (
            <span 
                onClick={() => handleStatusBadgeClick(statusKey)}
                className="cursor-pointer hover:opacity-80 transition-opacity" 
            >
                <Badge 
                    variant="default" 
                    className={
                        `${statusInfo.color} ${statusFilter === statusKey ? "ring-2 ring-offset-2 ring-black" : ""}`
                    }
                >
                    {statusInfo.name}
                </Badge>
            </span>
        );
    };

    // Hàm Filter cho Bảng
    const filteredAssignments = assignedOrders.filter((assignment) => {
        const matchesSearch =
            assignment.orderCode.toLowerCase().includes(searchTerm.toLowerCase()) ||
            assignment.productName.toLowerCase().includes(searchTerm.toLowerCase());

        // <<< Dùng assignment.OrderStatus >>>
        const orderStatusKey = String(assignment.OrderStatus || "3");

        const matchesStatus =
            statusFilter === "all" ||
            orderStatusKey === statusFilter;

        return matchesSearch && matchesStatus;
    });


    let previousOrderCode = null;


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
                        <div className="bg-white p-4 rounded-lg border-2 border-blue-200">
                            <h1 className="text-xl font-semibold text-gray-900">
                                Designer Dashboard
                            </h1>
                            <p className="text-gray-600 mt-1">
                                Chào mừng đến với không gian làm việc của Designer
                            </p>
                        </div>

                        {/* Stats Cards - 5 CỘT (CÓ THỂ CLICK) */}
                        {loading ? (
                            <div className="text-center text-gray-500">Loading stats...</div>
                        ) : (
                            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
                                
                                {/* 1. Cần Design (Status 3) */}
                                <div 
                                    className="bg-white p-6 rounded-lg shadow cursor-pointer hover:ring-2 hover:ring-red-300 transition-all"
                                    onClick={() => handleStatusBadgeClick("3")}
                                >
                                    <h3 className="text-sm font-medium text-gray-500">
                                        {DESIGN_STATUSES["3"].name}
                                    </h3>
                                    <p className="text-2xl font-bold text-gray-900 mt-2">{counts["3"] || 0}</p>
                                </div>
                                
                                {/* 2. Đang làm Design (Status 4) */}
                                <div 
                                    className="bg-white p-6 rounded-lg shadow cursor-pointer hover:ring-2 hover:ring-yellow-300 transition-all"
                                    onClick={() => handleStatusBadgeClick("4")}
                                >
                                    <h3 className="text-sm font-medium text-gray-500">
                                        {DESIGN_STATUSES["4"].name}
                                    </h3>
                                    <p className="text-2xl font-bold text-gray-900 mt-2">{counts["4"] || 0}</p>
                                </div>
                                
                                {/* 3. Cần Check Design (Status 5) */}
                                <div 
                                    className="bg-white p-6 rounded-lg shadow cursor-pointer hover:ring-2 hover:ring-blue-300 transition-all"
                                    onClick={() => handleStatusBadgeClick("5")}
                                >
                                    <h3 className="text-sm font-medium text-gray-500">
                                        {DESIGN_STATUSES["5"].name}
                                    </h3>
                                    <p className="text-2xl font-bold text-gray-900 mt-2">{counts["5"] || 0}</p>
                                </div>

                                {/* 4. Thiết kế Lại (Design Lỗi) (Status 6) */}
                                <div 
                                    className="bg-white p-6 rounded-lg shadow cursor-pointer hover:ring-2 hover:ring-purple-300 transition-all"
                                    onClick={() => handleStatusBadgeClick("6")}
                                >
                                    <h3 className="text-sm font-medium text-gray-500">
                                        {DESIGN_STATUSES["6"].name}
                                    </h3>
                                    <p className="text-2xl font-bold text-gray-900 mt-2">{counts["6"] || 0}</p>
                                </div>
                                
                                {/* 5. Tổng số Order Details (Tất cả) */}
                                <div 
                                    className="bg-white p-6 rounded-lg shadow cursor-pointer hover:ring-2 hover:ring-gray-300 transition-all"
                                    onClick={() => handleStatusBadgeClick("all")}
                                >
                                    <h3 className="text-sm font-medium text-gray-500">
                                        Tổng số Order Details
                                    </h3>
                                    <p className="text-2xl font-bold text-gray-900 mt-2">{counts.total || 0}</p>
                                </div>
                            </div>
                        )}
                    
                        {/* Search and Filter Section */}
                        {/* <div className="bg-white p-4 rounded-lg shadow mb-6">
                            <div className="flex gap-4 items-center">
                                <div className="flex-1">
                                    <div className="relative">
                                        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                                        <Input
                                            placeholder="Search by Order ID, Product Name..."
                                            value={searchTerm}
                                            onChange={(e) => setSearchTerm(e.target.value)}
                                            className="pl-10"
                                        />
                                    </div>
                                </div>
                                <div className="w-64">
                                    <Select value={statusFilter} onValueChange={setStatusFilter}>
                                        <SelectTrigger>
                                            <SelectValue placeholder="Filter by status">
                                                {statusFilter === "all" ? (
                                                    `Tất cả Status (${filterCounts.all})`
                                                ) : (
                                                    `${DESIGN_STATUSES[statusFilter]?.name} (${filterCounts[statusFilter] || 0})`
                                                )}
                                            </SelectValue>
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="all">
                                                Tất cả Status ({filterCounts.all})
                                            </SelectItem>
                                            
                                            {Object.entries(DESIGN_STATUSES).map(([key, value]) => (
                                                <SelectItem key={key} value={key}>
                                                    {value.name} ({filterCounts[key] || 0})
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                            </div>
                        </div> */}
                        
                        {/* Recent Assignments Table */}
                        <div className="bg-white rounded-lg shadow">
                            <div className="p-6 border-b border-gray-200">
                                <h2 className="text-lg font-semibold text-gray-900">
                                    Recent Assignments ({filteredAssignments.length} found)
                                </h2>
                                <p className="text-gray-600 mt-1">
                                    Danh sách chi tiết đơn hàng được giao gần nhất
                                </p>
                            </div>
                            
                            {loading ? (
                                <div className="p-6 text-center text-gray-500">Loading assignments...</div>
                            ) : filteredAssignments.length === 0 ? (
                                <div className="p-6 text-center text-gray-500">No matching assignments found.</div>
                            ) : (
                                <Table>
                                    <TableHeader>
                                        <TableRow>
                                            <TableHead>Detail ID</TableHead>
                                            <TableHead>Order Code</TableHead>
                                            <TableHead>Product</TableHead>
                                            <TableHead>Quantity</TableHead>
                                            <TableHead>Assigned Date</TableHead>
                                            <TableHead>Status</TableHead>
                                            <TableHead>Actions</TableHead>
                                        </TableRow>
                                    </TableHeader>
                                    <TableBody>
                                        {/* Khởi tạo biến theo dõi OrderCode trùng lặp */}
                                        {previousOrderCode = null} 
                                        {filteredAssignments.map((assignment) => {
                                            
                                            // 1. Logic kiểm tra trùng lặp
                                            const isDuplicateOrderCode = assignment.orderCode === previousOrderCode;
                                            previousOrderCode = assignment.orderCode;
                                            
                                            return (
                                                <TableRow key={assignment.id}>
                                                    <TableCell className="font-medium">
                                                        {assignment.orderDetailId}
                                                    </TableCell>
                                                    
                                                    <TableCell 
                                                        className={isDuplicateOrderCode ? "text-gray-400 font-normal" : "font-medium"}
                                                    >
                                                        {!isDuplicateOrderCode ? assignment.orderCode : "—"}
                                                    </TableCell>
                                                    
                                                    <TableCell>{assignment.productName}</TableCell>
                                                    <TableCell>{assignment.quantity}</TableCell>
                                                    <TableCell>{new Date(assignment.assignedAt).toLocaleDateString()}</TableCell>
                                                    
                                                    {/* Ô Status */}
                                                    <TableCell>{getStatusBadge(assignment.OrderStatus)}</TableCell>
                                                    
                                                    <TableCell>
                                                        {/* Nút View chi tiết */}
                                                        <Dialog>
                                                            <DialogTrigger asChild>
                                                                <Button 
                                                                    variant="outline" 
                                                                    size="sm"
                                                                    onClick={() => setSelectedAssignment(assignment)}
                                                                >
                                                                    <Eye className="h-4 w-4 mr-1" />
                                                                    View
                                                                </Button>
                                                            </DialogTrigger>

                                                            {/* Dialog Content */}
                                                            {selectedAssignment && selectedAssignment.id === assignment.id && (
                                                                <DialogContent className="max-w-xl max-h-[80vh] overflow-y-auto">
                                                                    <DialogHeader>
                                                                        <DialogTitle>
                                                                            Assignment Detail - {selectedAssignment.orderDetailId}
                                                                        </DialogTitle>
                                                                    </DialogHeader>
                                                                    <div className="space-y-4">
                                                                        <div>
                                                                            <Label className="text-sm text-gray-500">Order Code</Label>
                                                                            <p className="font-medium">{selectedAssignment.orderCode}</p>
                                                                        </div>
                                                                        <div>
                                                                            <Label className="text-sm text-gray-500">Product Name</Label>
                                                                            <p className="font-medium">{selectedAssignment.productName}</p>
                                                                        </div>
                                                                        // MỚI (Khắc phục lỗi)
                                                                        <div>
                                                                            <Label className="text-sm text-gray-500">Status</Label>
                                                                            {/* Dùng <div> thay cho <p> để chứa Badge Status phức tạp */}
                                                                            <div className="font-medium">
                                                                                {getStatusBadge(selectedAssignment.OrderStatus)}
                                                                            </div>
                                                                        </div>
                                                                        <div>
                                                                            <Label className="text-sm text-gray-500">Assigned Date</Label>
                                                                            <p className="font-medium">{new Date(selectedAssignment.assignedAt).toLocaleDateString()}</p>
                                                                        </div>
                                                                        <div>
                                                                            <Label className="text-sm text-gray-500">Customer Note</Label>
                                                                            <p className="font-medium">{selectedAssignment.note || 'Không có ghi chú.'}</p>
                                                                        </div>
                                                                        {/* Thêm phần download/view file nếu cần */}
                                                                        {selectedAssignment.linkImg && (
                                                                            <div>
                                                                                <Label className="text-sm text-gray-500">Reference Image</Label>
                                                                                <a href={selectedAssignment.linkImg} target="_blank" rel="noopener noreferrer" className="flex items-center text-blue-500 hover:underline mt-1">
                                                                                    <Download className="h-4 w-4 mr-1" />
                                                                                    Download File
                                                                                </a>
                                                                            </div>
                                                                        )}
                                                                        {/* Thêm chi tiết size */}
                                                                        {selectedAssignment.productDetails && (
                                                                            <div>
                                                                                <Label className="text-sm text-gray-500">Product Size</Label>
                                                                                <p className="font-medium">
                                                                                    {selectedAssignment.productDetails.lengthCm}x
                                                                                    {selectedAssignment.productDetails.heightCm}x
                                                                                    {selectedAssignment.productDetails.widthCm}cm
                                                                                </p>
                                                                            </div>
                                                                        )}
                                                                    </div>
                                                                </DialogContent>
                                                            )}
                                                        </Dialog>
                                                    </TableCell>
                                                </TableRow>
                                            );
                                        })}
                                    </TableBody>
                                </Table>
                            )}
                        </div>
                    </div>
                </main>
            </div>
        </div>
    );
}