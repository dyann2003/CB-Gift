"use client";

import { useState, useEffect } from "react";
import apiClient from "../../../lib/apiClient";
// Import các components UI
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
// --- THÊM IMPORT CHO SELECT ---
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
// --- KẾT THÚC IMPORT ---
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
  Search,
  ChevronLeft,
  ChevronRight,
  Package,
} from "lucide-react";
import DesignerHeader from "@/components/layout/designer/header";
import DesignerSidebar from "@/components/layout/designer/sidebar";
import { toast } from "@/components/ui/use-toast";

const COMPLETED_STATUS = { name: "Completed", color: "bg-green-600" };

export default function DesignHistoryPage() {
  const [currentPage, setCurrentPage] = useState("design-history");
  const [selectedOrder, setSelectedOrder] = useState(null);

  // --- STATE PHÂN TRANG (Server-side) ---
  const [page, setPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

  // --- STATE FILTER ---
  const [searchTerm, setSearchTerm] = useState("");
  const [sellerIdFilter, setSellerIdFilter] = useState("");
  const [startDateFilter, setStartDateFilter] = useState(""); // Dùng string YYYY-MM-DD
  const [endDateFilter, setEndDateFilter] = useState("");   // Dùng string YYYY-MM-DD
  const [productFilter, setProductFilter] = useState(""); // THÊM MỚI (lưu ProductId)

  // --- STATE DATA ---
  const [loading, setLoading] = useState(true);
  const [allOrders, setAllOrders] = useState([]);
  const [availableProducts, setAvailableProducts] = useState([]); // THÊM MỚI
  const [availableSellers, setAvailableSellers] = useState([]);
  // --- STATE DIALOG ---
  const [detailLoading, setDetailLoading] = useState(false);
  const [taskLogs, setTaskLogs] = useState([]);

  // --- HÀM FETCHTASKS ĐÃ CẬP NHẬT ---
  const fetchTasks = async (fetchPage = page, fetchPageSize = itemsPerPage) => {
    try {
      setLoading(true);
      
      const params = new URLSearchParams();
      params.append("page", fetchPage.toString());
      params.append("pageSize", fetchPageSize.toString());
      
      if (productFilter) { // THÊM MỚI
        params.append("productId", productFilter);
      }
      if (sellerIdFilter) {
        params.append("sellerId", sellerIdFilter);
      }
      if (searchTerm) {
        params.append("searchTerm", searchTerm);
      }
      if (startDateFilter) {
        params.append("startDate", startDateFilter);
      }
      if (endDateFilter) {
        params.append("endDate", endDateFilter);
      }

      // 2. Gọi API
      const res = await fetch(`${apiClient.defaults.baseURL}/api/designer/tasks/history?${params.toString()}`, {
        credentials: "include",
      });

      if (!res.ok) throw new Error(`HTTP error! ${res.status}`);

      const data = await res.json(); // Data giờ là PaginatedResult
      
      // 3. Map dữ liệu
      const mapped = (data.items || []).map((item) => ({
        ...item,
        id: item.orderDetailId.toString(),
        // Status luôn là Completed (đã bị ghi đè ở API)
        ProductionStatus: item.productionStatus, 
      }));

      setAllOrders(mapped); // Gán trực tiếp
      
      // 4. Cập nhật state phân trang
      setTotalCount(data.total);
      setPage(data.page); // Cập nhật page từ server (phòng trường hợp invalid)
      setItemsPerPage(data.pageSize);
      
      const calculatedTotalPages = Math.ceil(data.total / data.pageSize);
      setTotalPages(calculatedTotalPages);

    } catch (err) {
      console.error("❌ Failed to fetch history:", err);
      setAllOrders([]);
    } finally {
      setLoading(false);
    }
  };

  // --- HÀM MỚI: Lấy danh sách sản phẩm ---
  const fetchProducts = async () => {
    try {
      const res = await fetch(`${apiClient.defaults.baseURL}/api/designer/tasks/products`, {
        credentials: "include",
      });
      if (!res.ok) throw new Error("Failed to fetch products");
      const data = await res.json();
      setAvailableProducts(data || []);
    } catch (error) {
      console.error(error);
    }
  };
  // lấy danh sách seller
   const fetchSellers = async () => {
    try {
      const res = await fetch(`${apiClient.defaults.baseURL}/api/designer/tasks/sellers`, {
        credentials: "include",
      });
      if (!res.ok) throw new Error("Failed to fetch sellers");
      const data = await res.json();
      setAvailableSellers(data || []);
    } catch (error) {
      console.error(error);
    }
  };
  
  // Nút search (Đã cập nhật)
  const handleSearch = () => {
    // Nếu đang ở trang 1, tự kích hoạt fetch
    if (page === 1) {
      fetchTasks(1, itemsPerPage);
    } else {
      // Nếu ở trang khác, setPage(1) sẽ kích hoạt useEffect
      setPage(1); 
    }
  };
  
  // Clear filter (Đã cập nhật)
  const handleClearFilters = () => {
    // Set state về rỗng
    setSearchTerm("");
    setSellerIdFilter("");
    setStartDateFilter("");
    setEndDateFilter("");
    setProductFilter(""); // THÊM MỚI

    // Nếu đang ở trang 1, tự kích hoạt fetch
    if (page === 1) {
      // Cần gọi fetchTasks với các giá trị đã clear
      // (Dùng 1 trick nhỏ là `fetchTasks` sẽ chạy sau khi render)
      setTimeout(() => fetchTasks(1, itemsPerPage), 0);
    } else {
      // Nếu ở trang khác, setPage(1) sẽ kích hoạt useEffect
      setPage(1);
    }
  };

  // --- CẬP NHẬT useEffect ---
  useEffect(() => {
    // Gọi cả 2 khi page/itemsPerPage thay đổi
    fetchTasks(page, itemsPerPage);
  }, [page, itemsPerPage]);

  useEffect(() => {
    // Gọi 1 lần duy nhất khi component load
    fetchProducts();
     fetchSellers();
  }, []); // <-- Chạy 1 lần

  // --- HÀM LẤY CHI TIẾT TASK (GIỐNG BÊN ASSIGN) ---
  const fetchTaskDetail = async (orderDetailId) => {
    if (!orderDetailId) return;
    setDetailLoading(true);
    setTaskLogs([]);
    try {
      const res = await fetch(
        `${apiClient.defaults.baseURL}/api/designer/tasks/${orderDetailId}`,
        { credentials: "include" }
      );
      if (!res.ok) throw new Error(`Failed to fetch details: ${res.status}`);
      const data = await res.json();
      
      const updatedTaskInfo = {
        ...data.taskInfo,
        id: data.taskInfo.orderDetailId.toString(),
        ProductionStatus: "Completed", // Hiển thị Completed
      };
      setSelectedOrder(updatedTaskInfo);
      setTaskLogs(data.logs || []);
    } catch (error) {
      console.error("Error fetching task detail:", error);
      toast({
        title: "Lỗi",
        description: "Không thể tải chi tiết công việc hoặc lịch sử.",
        variant: "destructive",
      });
    } finally {
      setDetailLoading(false);
    }
  };

  // Hàm trả về Badge "Completed"
  const getOrderStatus = (order) => {
    return (
      <Badge variant="default" className={COMPLETED_STATUS.color}>
        {COMPLETED_STATUS.name}
      </Badge>
    );
  };

  let previousOrderCode = null;

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
            Design History
          </h1>
          <p className="text-gray-600 mt-1">
            Browse all design tasks you have completed
          </p>
        </header>

        <main className="flex-1 overflow-y-auto p-6">
          {/* Search and Filter Section MỚI */}
          <div className="bg-white p-4 rounded-lg shadow mb-6 space-y-4">
            {/* Hàng 1: Search Term */}
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
              <Input
                placeholder="Search by Order ID, Product Name..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
              />
            </div>

            {/* Hàng 2: Filters (CẬP NHẬT) */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              {/* --- DROPDOWN SẢN PHẨM (MỚI) --- */}
              <Select 
                value={productFilter} 
                onValueChange={(value) => {
                  // Nếu user chọn "all", set state thành "", ngược lại set bằng value
                  setProductFilter(value === "all" ? "" : value);
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Filter by Product..." />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Products</SelectItem> {/* <-- SỬA Ở ĐÂY */}
                  {availableProducts.map((product) => (
                    <SelectItem key={product.productId} value={product.productId.toString()}>
                      {product.productName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {/* --- KẾT THÚC DROPDOWN --- */}

             {/* === THAY THẾ INPUT BẰNG SELECT === */}
            <Select
              value={sellerIdFilter}
              onValueChange={(value) => {
                setSellerIdFilter(value === "all" ? "" : value);
              }}
            >
              <SelectTrigger>
                <SelectValue placeholder="Filter by Seller..." />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Sellers</SelectItem>
                {availableSellers.map((seller) => (
                  <SelectItem key={seller.sellerId} value={seller.sellerId}>
                    {seller.sellerName}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {/* === KẾT THÚC THAY THẾ === */}
              <Input
                type="date"
                value={startDateFilter}
                onChange={(e) => setStartDateFilter(e.target.value)}
                placeholder="Start Date"
              />
              <Input
                type="date"
                value={endDateFilter}
                onChange={(e) => setEndDateFilter(e.target.value)}
                placeholder="End Date"
              />
            </div>
            
            {/* Hàng 3: Nút Bấm */}
            <div className="flex justify-end gap-2">
              <Button
                variant="outline"
                onClick={handleClearFilters}
              >
                Clear Filters
              </Button>
              <Button onClick={handleSearch}>
                <Search className="h-4 w-4 mr-2" /> Search
              </Button>
            </div>
          </div>
          

          {/* Table Section */}
          <div className="bg-white rounded-lg shadow">
            {loading && (
              <div className="p-4 text-center text-gray-500">
                Đang tải lịch sử thiết kế...
              </div>
            )}
            {!loading && allOrders.length === 0 && (
              <div className="p-4 text-center text-gray-500">
                Không tìm thấy dữ liệu nào.
              </div>
            )}
            {!loading && allOrders.length > 0 && (
              <Table>
                <TableHeader>
                  <TableRow>
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
                  {(previousOrderCode = null)}
                  {allOrders.map((order) => {
                    const isDuplicateOrderCode =
                      order.orderCode === previousOrderCode;
                    previousOrderCode = order.orderCode;

                    return (
                      <TableRow key={order.id}>
                        <TableCell className="font-medium">
                          {order.orderDetailId}
                        </TableCell>

                        <TableCell
                          className="font-medium">
                          { order.orderCode }
                        </TableCell>

                        <TableCell>{order.productName}</TableCell>
                        <TableCell>{order.productDescribe || "N/A"}</TableCell>
                        <TableCell>{order.quantity}</TableCell>
                        <TableCell>
                          {order.productDetails?.lengthCm}x
                          {order.productDetails?.heightCm}x
                          {order.productDetails?.widthCm}cm
                        </TableCell>
                        <TableCell>
                          {new Date(order.assignedAt).toLocaleString()}
                        </TableCell>
                        <TableCell>{getOrderStatus(order)}</TableCell>
                        <TableCell>
                          <div className="flex gap-2">
                            <Dialog
                              onOpenChange={(open) => {
                                if (open) {
                                  setSelectedOrder(order);
                                  fetchTaskDetail(order.id);
                                } else {
                                  setSelectedOrder(null);
                                  setTaskLogs([]);
                                }
                              }}
                            >
                              <DialogTrigger asChild>
                                <Button variant="outline" size="sm">
                                  <Eye className="h-4 w-4 mr-1" /> View
                                </Button>
                              </DialogTrigger>

                              {/* DIALOG CONTENT */}
                              {selectedOrder &&
                                selectedOrder.id === order.id && (
                                  <DialogContent className="max-w-5xl max-h-[90vh] overflow-y-auto">
                                    <DialogHeader>
                                      <DialogTitle className="flex items-center gap-2">
                                        <Package className="h-5 w-5" />
                                        Order Details -{" "}
                                        {selectedOrder.orderDetailId} (
                                        {selectedOrder.orderCode})
                                      </DialogTitle>
                                    </DialogHeader>
                                    
                                    <div className="space-y-6">
                                      {/* Order Info */}
                                      <div className="bg-gray-50 p-4 rounded-lg">
                                        {/* ... (Code hiển thị Order Info, giống trang Assign) ... */}
                                        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                                            <div>
                                              <Label className="text-sm text-gray-500 font-medium">Order Code</Label>
                                              <p className="font-medium text-gray-900 mt-1">{selectedOrder.orderCode}</p>
                                            </div>
                                            <div>
                                              <Label className="text-sm text-gray-500 font-medium">Order Detail ID</Label>
                                              <p className="font-medium text-gray-900 mt-1">{selectedOrder.orderDetailId}</p>
                                            </div>
                                            <div>
                                              <Label className="text-sm text-gray-500 font-medium">Assigned At</Label>
                                              <p className="font-medium text-gray-900 mt-1">{new Date(selectedOrder.assignedAt).toLocaleString()}</p>
                                            </div>
                                            <div>
                                              <Label className="text-sm text-gray-500 font-medium">Status</Label>
                                              <div className="mt-1">{getOrderStatus(selectedOrder)}</div>
                                            </div>
                                        </div>
                                      </div>

                                      {/* Lịch sử Logs */}
                                      {detailLoading && ( <div className="text-center p-6"><p>Đang tải lịch sử...</p></div> )}
                                      {!detailLoading && taskLogs.length > 0 && (
                                        <div className="space-y-4">
                                          <h3 className="font-semibold text-lg text-gray-900">Lịch sử Duyệt Thiết Kế</h3>
                                          <ul className="space-y-3">
                                            {taskLogs.map((log) => {
                                              const isRejection = log.eventType === 'DESIGN_REJECTED' || log.eventType === 'QC_FAIL';
                                              const isApproval = log.eventType === 'DESIGN_APPROVED';
                                              let containerClass = "bg-gray-50 border-gray-200 p-4 rounded-lg border";
                                              let titleClass = "font-medium text-gray-700";
                                              let title = log.eventType;

                                              if (isRejection) {
                                                containerClass = "bg-red-50 border-red-200 p-4 rounded-lg border";
                                                titleClass = "font-medium text-red-700";
                                                title = log.eventType === 'DESIGN_REJECTED' ? 'Design Bị Từ chối' : 'QC Báo lỗi';
                                              } else if (isApproval) {
                                                containerClass = "bg-green-50 border-green-200 p-4 rounded-lg border";
                                                titleClass = "font-medium text-green-700";
                                                title = 'Design Được Duyệt';
                                              }

                                              return (
                                                <li key={log.orderDetailLogId} className={containerClass}>
                                                  <div className="flex justify-between items-center mb-1">
                                                    <span className={titleClass}>{title}</span>
                                                    <span className="text-xs text-gray-600">{new Date(log.createdAt).toLocaleString()}</span>
                                                  </div>
                                                  <p className="text-sm text-gray-800"><strong>Thực hiện bởi:</strong> {log.userName || 'System'}</p>
                                                  {log.reason && (<p className="text-sm text-gray-800 mt-1"><strong>Lý do:</strong> {log.reason}</p>)}
                                                  {log.comment && (<p className="text-sm text-gray-900 bg-white p-2 rounded mt-1 border border-gray-200"><strong>Ghi chú:</strong> {log.comment}</p>)}
                                                </li>
                                              );
                                            })}
                                          </ul>
                                        </div>
                                      )}

                                      {/* Product Details */}
                                      <div>
                                        {/* ... (Code hiển thị Product Details, giống trang Assign) ... */}
                                        <h3 className="font-semibold text-lg mb-3 text-gray-900">Product Details</h3>
                                        <div className="border border-gray-200 rounded-lg p-4 bg-white space-y-4">
                                          {/* ... (Nội dung chi tiết sản phẩm) ... */}
                                          {selectedOrder.linkFileDesign && (
                                            <div>
                                              <Label className="text-sm text-gray-500 font-medium">Final Design File</Label>
                                              <div className="mt-2 w-full max-w-md">
                                                <img
                                                  src={selectedOrder.linkFileDesign || "/placeholder.svg"}
                                                  alt="Design File"
                                                  className="w-full h-auto rounded-lg border border-gray-200 object-cover"
                                                />
                                              </div>
                                            </div>
                                          )}
                                        </div>
                                      </div>
                                    </div>
                                    
                                  </DialogContent>
                                )}
                              {/* END DIALOG CONTENT */}
                            </Dialog>
                          </div>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            )}

            {/* Pagination (Server-side) */}
            {!loading && allOrders.length > 0 && (
              <div className="border-t px-6 py-4 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="text-sm text-gray-700">Show</span>
                  <select
                    value={itemsPerPage}
                    onChange={(e) => {
                      setItemsPerPage(Number(e.target.value));
                      setPage(1); // Reset về trang 1 khi đổi size
                    }}
                    className="px-2 py-1 border border-gray-300 rounded-md text-sm"
                  >
                    <option value="10">10</option>
                    <option value="20">20</option>
                    <option value="50">50</option>
                  </select>
                  <span className="text-sm text-gray-700">per page</span>
                </div>

                <div className="flex items-center gap-2">
                  <span className="text-sm text-gray-700">
                    Page {page} of {totalPages} ({totalCount} total)
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
            )}
          </div>
        </main>
      </div>
    </div>
  );
}