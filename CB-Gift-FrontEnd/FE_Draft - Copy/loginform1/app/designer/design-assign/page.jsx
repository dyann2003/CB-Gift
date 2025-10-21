"use client";

import { useState, useEffect } from "react";
// Import các components UI
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
    Search,
    Send,
    ImageIcon,
} from "lucide-react";
import DesignerHeader from "@/components/layout/designer/header";
import DesignerSidebar from "@/components/layout/designer/sidebar";

// CẬP NHẬT CONSTANTS DỰA TRÊN ENUM ProductionStatus MỚI
const DESIGN_STATUSES = {
    "NEED_DESIGN": { name: "NEED DESIGN", color: "bg-red-500", code: 2 },
    "DESIGNING": { name: "DESIGNING", color: "bg-yellow-500", code: 3 },
    "CHECK_DESIGN": { name: "CHECK DESIGN", color: "bg-blue-500", code:4 },
    "DESIGN_REDO": { name: "DESIGN REDO", color: "bg-purple-500", code: 5 },
};

export default function DesignAssignPage() {
    const [currentPage, setCurrentPage] = useState("design-assign");
    const [selectedOrder, setSelectedOrder] = useState(null);
    const [designFile, setDesignFile] = useState(null);
    const [designNotes, setDesignNotes] = useState("");
    
    // State cho tính năng chọn hàng loạt
    const [selectedOrderDetails, setSelectedOrderDetails] = useState(new Set()); 
    const [selectAll, setSelectAll] = useState(false);

    const [searchTerm, setSearchTerm] = useState("");
    // Cập nhật trạng thái filter mặc định
    const [statusFilter, setStatusFilter] = useState("all"); 
    const [showConfirmDialog, setShowConfirmDialog] = useState(false);
    const [confirmAction, setConfirmAction] = useState(null);
    const [confirmMessage, setConfirmMessage] = useState("");

    const [loading, setLoading] = useState(true);
    const [assignedOrders, setAssignedOrders] = useState([]); 

    // Hàm lấy giá trị Code (Int) từ tên Status (String)
    const getStatusCode = (statusKey) => DESIGN_STATUSES[statusKey]?.code;


    // --- HÀM GỌI API CẬP NHẬT TRẠNG THÁI (Sử dụng ProductionStatus) ---
    const updateDesignStatusApi = async (orderDetailId, newStatusKey) => {
        const newStatusCode = getStatusCode(newStatusKey); 
    if (!newStatusCode && newStatusCode !== 0) { // Kiểm tra cả trường hợp code = 0 (DRAFT)
        console.error("Invalid status key:", newStatusKey);
        alert(`Lỗi: Trạng thái ${newStatusKey} không hợp lệ.`);
        return false;
    }

        const url = `https://localhost:7015/api/designer/tasks/status/${orderDetailId}`; 
        
        try {
            const response = await fetch(url, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ 
                    productionStatus: newStatusCode, // Gửi ProductionStatus Code
                }),
                credentials: 'include',
            });

            if (!response.ok) {
                const contentType = response.headers.get("content-type");
                
                if (!contentType || !contentType.includes("application/json")) {
                    const errorText = await response.text();
                    throw new Error(`Server returned status ${response.status}. Response: ${errorText.substring(0, 50)}...`);
                }
                
                const errorData = await response.json();
                throw new Error(errorData.message || `Failed to update status: ${response.status}`);
            }

            return true;

        } catch (error) {
            console.error("API Error during status update:", error);
            alert(`Lỗi khi cập nhật trạng thái: ${error.message}`);
            return false;
        }
    };
    
    // Hàm cập nhật trạng thái trên frontend (chỉ khi API thành công)
    // Cập nhật để sử dụng ProductionStatus
    const handleUpdateStatusLocal = (orderId, newStatusKey) => {
        setAssignedOrders(prevOrders => 
            prevOrders.map(order => 
                order.id === orderId ? { ...order, ProductionStatus: newStatusKey } : order
            )
        );
        console.log(`Cập nhật trạng thái Order Detail ${orderId} thành ${newStatusKey} (Local)`);
    };

    // Hàm chấp nhận thiết kế (NEED_DESIGN -> DESIGNING)
    const handleAcceptDesign = async (orderId) => {
        const success = await updateDesignStatusApi(orderId, "DESIGNING");
        
        if (success) {
            handleUpdateStatusLocal(orderId, "DESIGNING");
        }
    };

    // Hàm Gửi QA (DESIGNING/DESIGN_REDO -> CHECK_DESIGN)
    // Dùng cho nút nhanh bên trong Dialog
    const handleSendToQA = (orderId) => {
        setConfirmMessage(`Bạn có chắc muốn gửi Order Detail ${orderId} sang CHECK DESIGN?`);
        setConfirmAction(() => async () => {
            const success = await updateDesignStatusApi(orderId, "CHECK_DESIGN");
            if (success) {
                handleUpdateStatusLocal(orderId, "CHECK_DESIGN");
            }
            setShowConfirmDialog(false);
        });
        setShowConfirmDialog(true);
    };
    
    // Hàm Upload Design (và chuyển trạng thái)
const handleUploadDesign = async () => {
        if (!designFile || !selectedOrder) {
        alert("Vui lòng chọn file thiết kế.");
        return;
        }

    const orderDetailId = selectedOrder.id;
    const url = `https://localhost:7015/api/designer/tasks/${orderDetailId}/upload`; 
    
    setLoading(true); // Bật loading khi bắt đầu upload

    try {
        const formData = new FormData();
        formData.append('DesignFile', designFile); 
        // FIX ĐƠN GIẢN VÀ CHẮC CHẮN NHẤT:
        // Đảm bảo designNotes luôn là chuỗi rỗng nếu nó là null/undefined, sau đó gửi nó đi.
        // Nếu designNotes không được khởi tạo (undefined), ta dùng chuỗi rỗng.
       const noteToSend = designNotes || "";
       formData.append('Note', noteToSend);
        // --- 1. Thực hiện Upload File ---
        const response = await fetch(url, {
            method: 'POST',
            // Quan trọng: Không set Content-Type 'application/json' khi dùng FormData,
            // trình duyệt sẽ tự động thiết lập Content-Type: multipart/form-data.
            body: formData,
            credentials: 'include',
        });

        const contentType = response.headers.get("content-type");
        let responseData = {};
        
        // Cố gắng đọc JSON nếu có
        if (contentType && contentType.includes("application/json")) {
            responseData = await response.json();
        }

        if (!response.ok) {
            let errorDetails = response.statusText || `Status ${response.status}`;
            
            // Cố gắng đọc JSON (Problem Details)
            const contentType = response.headers.get("content-type");
            if (contentType && contentType.includes("application/json")) {
                const errorData = await response.json();
                
                // ASP.NET Core Model Binding Errors nằm trong đối tượng 'errors'
                if (errorData.errors) {
                    const modelErrors = Object.values(errorData.errors)
                        .flat()
                        .join('; ');
                    errorDetails = errorData.title || "Model Binding Error";
                    errorDetails += ` [Details: ${modelErrors}]`;
                } else {
                    // Lỗi generic (ví dụ: từ Controller)
                    errorDetails = errorData.message || errorDetails;
                }
            } else {
                // Nếu không phải JSON, chỉ lấy Status Text
                errorDetails = await response.text();
            }
            
            throw new Error(errorDetails);
        }
        
        // --- 2. Cập nhật trạng thái trên Frontend (API backend đã xử lý chuyển trạng thái thành CHECK_DESIGN) ---
        
        // API backend của bạn đã xử lý cập nhật trạng thái (Order.StatusOrder = 5) và lưu Record
        // Tuy nhiên, để đồng bộ ProductionStatus trong OrderDetail, 
        // chúng ta cần cập nhật trạng thái local thành "CHECK_DESIGN"
        handleUpdateStatusLocal(orderDetailId, "CHECK_DESIGN"); 
        
        // Reset state và đóng dialog
        setDesignFile(null);
        setDesignNotes("");
        // Đặt selectedOrder về null để đóng Dialog (nếu Dialog được điều khiển bằng state này)
        setSelectedOrder(null); 
        alert("Upload file thiết kế thành công và đã gửi đi kiểm duyệt!");

        return true; 
        
    } catch (error) {
        console.error("Lỗi khi upload file thiết kế:", error);
        alert(`Lỗi khi upload file thiết kế: ${error.message}`);
        return false;
    } finally {
        setLoading(false); // Tắt loading
    }
};

    // Hàm Bắt đầu Redo (DESIGN_REDO -> DESIGNING)
    const handleStartRedo = async (orderId) => {
        // Cần xác nhận API call không cần body nếu chỉ chuyển trạng thái
        const success = await updateDesignStatusApi(orderId, "DESIGNING");
        
        if (success) {
            handleUpdateStatusLocal(orderId, "DESIGNING");
        }
    };


    // *** LOGIC GỌI API BAN ĐẦU ***
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
                    // LẤY ProductionStatus TỪ DTO
                    ProductionStatus: item.productionStatus || "NEED_DESIGN", 
                    customerName: `Customer for ${item.orderCode}`, 
                }));

                setAssignedOrders(mappedData);
            } catch (error) {
                console.error("Failed to fetch assigned tasks:", error);
                setAssignedOrders([]);
            } finally {
                setLoading(false);
            }
        };

        fetchTasks();
    }, []); 

    // --- LOGIC FILTER ---
    const filteredOrders = assignedOrders.filter((order) => {
        const matchesSearch =
            order.orderCode.toLowerCase().includes(searchTerm.toLowerCase()) ||
            order.productName.toLowerCase().includes(searchTerm.toLowerCase());

        const orderStatusKey = order.ProductionStatus || "NEED_DESIGN";

        const matchesStatus =
            statusFilter === "all" ||
            orderStatusKey === statusFilter;

        return matchesSearch && matchesStatus;
    });

    // --- LOGIC COUNT ---
    const getFilterCounts = () => {
        const counts = { all: assignedOrders.length };
        Object.keys(DESIGN_STATUSES).forEach(key => {
            counts[key] = 0;
        });

        assignedOrders.forEach(order => {
            const statusKey = order.ProductionStatus || "NEED_DESIGN";
            if (counts.hasOwnProperty(statusKey)) {
                counts[statusKey] += 1;
            }
        });
        return counts;
    };
    const filterCounts = getFilterCounts();

    // --- LOGIC CHECKBOX/BULK ACTIONS (Giữ nguyên, cần thêm logic bulk API) ---
    const handleSelectAll = (checked) => {
        setSelectAll(checked);
        if (checked) {
          setSelectedOrderDetails(new Set(filteredOrders.map((order) => order.id)));
        } else {
          setSelectedOrderDetails(new Set());
        }
    };
    
    const handleSelectOrder = (orderId, checked) => {
        const newSelected = new Set(selectedOrderDetails);
        if (checked) {
          newSelected.add(orderId);
        } else {
          newSelected.delete(orderId);
        }
        setSelectedOrderDetails(newSelected);
        setSelectAll(newSelected.size === filteredOrders.length && filteredOrders.length > 0);
    };

    // Hàm hiển thị Badge trạng thái (Sử dụng ProductionStatus)
    const getOrderStatus = (order) => {
        const statusKey = order.ProductionStatus || "NEED_DESIGN";
        const statusInfo = DESIGN_STATUSES[statusKey] || { name: "UNKNOWN", color: "bg-gray-500" };

        return (
            <Badge variant="default" className={statusInfo.color}>
                {statusInfo.name}
            </Badge>
        );
    };
    
    // Biến theo dõi OrderCode trùng lặp
    //let previousOrderCode = null;


    return (
        <div className="flex h-screen bg-gray-50">
            <DesignerSidebar
                currentPage={currentPage}
                setCurrentPage={setCurrentPage}
            />

            <div className="flex-1 flex flex-col overflow-hidden">
                <DesignerHeader />

                <header className="bg-white shadow-sm border-b border-gray-200 px-6 py-4">
                    <h1 className="text-2xl font-semibold text-gray-900">
                        Design Assigned
                    </h1>
                    <p className="text-gray-600 mt-1">
                        Orders assigned to you for design work
                    </p>
                </header>

                <main className="flex-1 overflow-y-auto p-6">
                    {/* Search and Filter Section */}
                    <div className="bg-white p-4 rounded-lg shadow mb-6">
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
                            <div className="w-48">
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
                    </div>
                    {/* Bulk Actions (Chưa có logic cập nhật trạng thái bulk) */}
                    {selectedOrderDetails.size > 0 && (
                        <div className="bg-blue-50 p-4 rounded-lg shadow mb-6 border border-blue-200">
                            <div className="flex items-center justify-between">
                                <span className="text-blue-800 font-medium">
                                    {selectedOrderDetails.size} order detail
                                    {selectedOrderDetails.size > 1 ? "s" : ""} selected
                                </span>
                                <div className="flex gap-2">
                                    <Button
                                        onClick={() => { /* Bulk accept logic */ }}
                                        className="bg-green-600 hover:bg-green-700"
                                        size="sm"
                                        disabled={true} // Tạm thời disable bulk actions
                                    >
                                        <Check className="h-4 w-4 mr-1" />
                                        Accept Selected ({selectedOrderDetails.size})
                                    </Button>
                                    <Button
                                        onClick={() => { /* Bulk send to QA logic */ }}
                                        className="bg-blue-600 hover:bg-blue-700"
                                        size="sm"
                                        disabled={true} 
                                    >
                                        <Send className="h-4 w-4 mr-1" />
                                        Send to QA (0)
                                    </Button>
                                </div>
                            </div>
                        </div>
                    )}
                    
                    {/* Table Section */}
                    <div className="bg-white rounded-lg shadow">
                        {loading && (
                            <div className="p-4 text-center text-gray-500">
                                Đang tải danh sách công việc được giao...
                            </div>
                        )}
                        {!loading && filteredOrders.length === 0 && (
                            <div className="p-4 text-center text-gray-500">
                                Không tìm thấy công việc thiết kế nào được giao.
                            </div>
                        )}
                        {!loading && filteredOrders.length > 0 && (
                            <Table>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead className="w-12"><Checkbox checked={selectAll} onCheckedChange={handleSelectAll} /></TableHead>
                                        <TableHead>Detail ID</TableHead> 
                                        <TableHead>Order Code</TableHead>
                                        <TableHead>Product Name</TableHead>
                                        <TableHead>Product Description</TableHead> 
                                        <TableHead>Quantity</TableHead>
                                        <TableHead>Size (L x H x W)</TableHead>
                                        <TableHead>Assigned At</TableHead>
                                        <TableHead>Status</TableHead>
                                        <TableHead>Actions</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                   
                                    {filteredOrders.map((order) => {
                                       
                                        
                                        const currentStatus = order.ProductionStatus || "NEED_DESIGN";

                                        return (
                                            <TableRow key={order.id}> 
                                                <TableCell>
                                                    <Checkbox
                                                        checked={selectedOrderDetails.has(order.id)}
                                                        onCheckedChange={(checked) => handleSelectOrder(order.id, checked)}
                                                    />
                                                </TableCell>
                                                <TableCell className="font-medium">{order.orderDetailId}</TableCell>
                                                <TableCell className="font-medium">{order.orderCode }</TableCell>
                                                <TableCell>{order.productName}</TableCell>
                                                <TableCell>{order.productDescribe || 'N/A'}</TableCell>
                                                <TableCell>{order.quantity}</TableCell>
                                                <TableCell>
                                                    {order.productDetails?.lengthCm}x{order.productDetails?.heightCm}x{order.productDetails?.widthCm}cm
                                                </TableCell>
                                                <TableCell>{new Date(order.assignedAt).toLocaleString()}</TableCell>
                                                <TableCell>{getOrderStatus(order)}</TableCell>
                                                <TableCell>
                                                    <div className="flex gap-2">
                                                        <Dialog>
                                                            <DialogTrigger asChild>
                                                                <Button
                                                                    variant="outline"
                                                                    size="sm"
                                                                    onClick={() => {
                                                                        setSelectedOrder(order);
                                                                        // Reset upload state khi mở dialog
                                                                        setDesignFile(null); 
                                                                        setDesignNotes("");
                                                                    }}
                                                                >
                                                                    <Eye className="h-4 w-4 mr-1" /> View
                                                                </Button>
                                                            </DialogTrigger>
                                                            {/* DIALOG CONTENT */}
                                                            {selectedOrder && selectedOrder.id === order.id && ( 
                                                                <DialogContent className="max-w-4xl max-h-[80vh] overflow-y-auto">
                                                                    <DialogHeader>
                                                                        <DialogTitle>Order Detail - {selectedOrder.orderDetailId} ({selectedOrder.orderCode})</DialogTitle>
                                                                    </DialogHeader>
                                                                    <div className="space-y-6">
                                                                        {/* PRODUCT DETAILS (Đã tối giản) */}
                                                                        <div>
                                                                            <h3 className="font-semibold text-lg mb-3">Product Details</h3>
                                                                            <div className="grid grid-cols-2 gap-4">
                                                                                <div><Label className="text-sm text-gray-500">Product</Label><p className="font-medium">{selectedOrder.productName}</p></div>
                                                                                <div><Label className="text-sm text-gray-500">Size</Label><p className="font-medium">{selectedOrder.productDetails?.lengthCm}x{selectedOrder.productDetails?.heightCm}x{selectedOrder.productDetails?.widthCm}cm</p></div>
                                                                                <div><Label className="text-sm text-gray-500">Quantity</Label><p className="font-medium">{selectedOrder.quantity}</p></div>
                                                                                <div><Label className="text-sm text-gray-500">SKU</Label><p className="font-medium">{selectedOrder.productDetails?.sku || 'N/A'}</p></div>
                                                                            </div>
                                                                            <div className="mt-4"><Label className="text-sm text-gray-500">Note</Label><p className="font-medium">{selectedOrder.note || 'No notes.'}</p></div>
                                                                        </div>

                                                                        {/* UPLOAD DESIGN SECTION */}
                                                                        {/* HIỂN THỊ KHI ĐANG DESIGNING HOẶC DESIGN_REDO */}
                                                                        {(currentStatus === "DESIGNING" || currentStatus === "DESIGN_REDO") && (
                                                                            <div className="border-t pt-4">
                                                                                <h3 className="font-semibold text-lg mb-3">Upload Design</h3>
                                                                                <div className="space-y-4">
                                                                                    <div><Label htmlFor="design-file">Design File</Label><Input id="design-file" type="file" accept=".zip,.rar,.7z,.pdf,.ai,.psd,.jpg,.jpeg,.png" onChange={(e) => setDesignFile(e.target.files[0])}/></div>
                                                                                    <div><Label htmlFor="design-notes">Design Notes</Label><Textarea id="design-notes" placeholder="Add notes..." value={designNotes} onChange={(e) => setDesignNotes(e.target.value)} /></div>
                                                                                    <div className="flex gap-2">
                                                                                        <Button onClick={handleUploadDesign} className="flex-1" disabled={!designFile}>
                                                                                            <Upload className="h-4 w-4 mr-2" /> Upload & Send to Check
                                                                                        </Button>
                                                                                    </div>
                                                                                </div>
                                                                            </div>
                                                                        )}
                                                                        
                                                                        {/* FILES FROM SELLER (Tạm thời giữ nguyên logic file cũ) */}
                                                                        <div className="border-t pt-4">
                                                                            <h3 className="font-semibold text-lg mb-3">Files from Seller</h3>
                                                                            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                                                                <div className="border rounded-lg p-4 opacity-50">
                                                                                    <Label className="text-sm text-gray-500">Reference Image</Label>
                                                                                    <div className="mt-2 w-full h-32 bg-gray-100 rounded border flex items-center justify-center">
                                                                                        <ImageIcon className="h-8 w-8 text-gray-400" />
                                                                                    </div>
                                                                                    <p className="text-sm mt-2">N/A</p>
                                                                                </div>
                                                                                {/* Thêm các file khác... */}
                                                                            </div>
                                                                        </div>
                                                                    </div>
                                                                </DialogContent>
                                                            )}
                                                            {/* END DIALOG CONTENT */}
                                                        </Dialog>

                                                        {/* ACCEPT BUTTON (Chỉ hiển thị khi NEED_DESIGN) */}
                                                        {currentStatus === "NEED_DESIGN" && (
                                                            <Button
                                                                size="sm"
                                                                onClick={() => handleAcceptDesign(order.id)}
                                                            >
                                                                <Check className="h-4 w-4 mr-1" />
                                                                Accept
                                                            </Button>
                                                        )}

                                                        {/* Nút START REDO (Cho Status DESIGN_REDO) */}
                                                        {currentStatus === "DESIGN_REDO" && (
                                                            <Button
                                                                size="sm"
                                                                onClick={() => handleStartRedo(order.id)} // Chuyển về DESIGNING để bắt đầu lại
                                                                className="bg-purple-600 hover:bg-purple-700"
                                                            >
                                                                <Check className="h-4 w-4 mr-1" />
                                                                Start Redo
                                                            </Button>
                                                        )}
                                                        
                                                        {/* NÚT UPLOAD/GỬI QA NHANH (Chỉ khi đang DESIGNING) */}
                                                        {/* Dùng DialogTrigger như trên để mở form upload */}
                                                        {currentStatus === "DESIGNING" && (
                                                            <Dialog>
                                                                <DialogTrigger asChild>
                                                                    <Button
                                                                        size="sm"
                                                                        onClick={() => {
                                                                            setSelectedOrder(order);
                                                                            setDesignFile(null); 
                                                                            setDesignNotes("");
                                                                        }}
                                                                        className="bg-blue-600 hover:bg-blue-700"
                                                                    >
                                                                        <Upload className="h-4 w-4 mr-1" />
                                                                        Upload Design
                                                                    </Button>
                                                                </DialogTrigger>
                                                                {selectedOrder && selectedOrder.id === order.id && ( 
                                                                    <DialogContent className="max-w-4xl max-h-[80vh] overflow-y-auto">
                                                                        <DialogHeader><DialogTitle>Order Detail - {selectedOrder.orderDetailId} ({selectedOrder.orderCode})</DialogTitle></DialogHeader>
                                                                        <div className="space-y-6">
                                                                            {/* PRODUCT DETAILS (tối giản) */}
                                                                            <div>
                                                                                <h3 className="font-semibold text-lg mb-3">Product Details</h3>
                                                                                <div className="grid grid-cols-2 gap-4">
                                                                                    <div><Label className="text-sm text-gray-500">Product</Label><p className="font-medium">{selectedOrder.productName}</p></div>
                                                                                    <div><Label className="text-sm text-gray-500">Size</Label><p className="font-medium">{selectedOrder.productDetails?.lengthCm}x{selectedOrder.productDetails?.heightCm}x{selectedOrder.productDetails?.widthCm}cm</p></div>
                                                                                </div>
                                                                            </div>
                                                                            {/* UPLOAD DESIGN SECTION */}
                                                                            <div className="border-t pt-4">
                                                                                <h3 className="font-semibold text-lg mb-3">Upload Design</h3>
                                                                                <div className="space-y-4">
                                                                                    <div><Label htmlFor="design-file-quick">Design File</Label><Input id="design-file-quick" type="file" accept=".zip,.rar,.7z,.pdf,.ai,.psd,.jpg,.jpeg,.png" onChange={(e) => setDesignFile(e.target.files[0])}/></div>
                                                                                    <div><Label htmlFor="design-notes-quick">Design Notes</Label><Textarea id="design-notes-quick" placeholder="Add notes..." value={designNotes} onChange={(e) => setDesignNotes(e.target.value)} /></div>
                                                                                    <div className="flex gap-2">
                                                                                        <Button onClick={handleUploadDesign} className="flex-1" disabled={!designFile}>
                                                                                            <Upload className="h-4 w-4 mr-2" /> Upload & Send to Check
                                                                                        </Button>
                                                                                    </div>
                                                                                </div>
                                                                            </div>
                                                                        </div>
                                                                    </DialogContent>
                                                                )}
                                                            </Dialog>
                                                        )}
                                                        
                                                        {/* NÚT SEND QA (Chỉ khi CHECK_DESIGN) - Có thể là bước kiểm tra nội bộ đã hoàn tất */}
                                                        {currentStatus === "CHECK_DESIGN" && (
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
                                        );
                                    })}
                                </TableBody>
                            </Table>
                        )}
                    </div>
                </main>
            </div>
            
            {/* Confirmation Dialog (giữ nguyên) */}
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