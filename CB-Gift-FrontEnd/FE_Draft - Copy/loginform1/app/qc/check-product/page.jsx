"use client";

import { useState, useEffect } from "react";
import apiClient from "../../../lib/apiClient";
// --- UI Components ---
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
// --- Icons ---
import { Search, Eye, Check, X, Download } from "lucide-react";
// --- Layout Components ---
import QcSidebar from "@/components/layout/qc/sidebar";
import QcHeader from "@/components/layout/qc/header";
// --- Navigation ---
import { useRouter } from "next/navigation";

// --- ADDED: Helper function to map status ID to string ---
const mapProductionStatusToString = (statusId) => {
  switch (statusId) {
    case 0:
      return "DRAFT";
    case 1:
      return "CREATED";
    case 2:
      return "NEED_DESIGN";
    case 3:
      return "DESIGNING";
    case 4:
      return "CHECK_DESIGN";
    case 5:
      return "DESIGN_REDO";
    case 6:
      return "READY_PROD";
    case 7:
      return "IN_PROD";
    case 8:
      return "FINISHED";
    case 9:
      return "QC_DONE";
    case 10:
      return "QC_FAIL";
    case 11:
      return "PROD_REWORK";
    case 12:
      return "PACKING";
    case 13:
      return "HOLD";
    case 14:
      return "CANCELLED";
    default:
      return "UNKNOWN"; // Fallback for unexpected values
  }
};
// --- Optional: Helper function for badge colors ---
const getStatusBadgeVariant = (statusString) => {
  switch (statusString) {
    case "QC_FAIL":
    case "DESIGN_REDO":
    case "PROD_REWORK":
    case "CANCELLED":
    case "HOLD":
      return "destructive";
    case "QC_DONE":
    case "FINISHED":
    case "PACKING":
      return "success"; // Use "default" or define "success" in your theme
    case "CHECK_DESIGN":
    case "IN_PROD":
    case "DESIGNING":
      return "secondary";
    case "READY_PROD":
      return "default";
    default:
      return "outline";
  }
};
// -------------------------------------------------------------

export default function CheckProduct() {
  const [currentPage, setCurrentPage] = useState("check-product");
  const [searchTerm, setSearchTerm] = useState("");
  const [filterStatus, setFilterStatus] = useState("all");
  const [selectedOrder, setSelectedOrder] = useState(null); // For main inspection modal
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [showRejectDialog, setShowRejectDialog] = useState(false);
  const [rejectReason, setRejectReason] = useState("");
  const [confirmAction, setConfirmAction] = useState(null); // Stores the function to run on confirm
  const [confirmMessage, setConfirmMessage] = useState("");
  const [manualOrderId, setManualOrderId] = useState("");
  const router = useRouter();

  // --- Data States ---
  const [allOrders, setAllOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // --- State for Product Detail Modal ---
  const [viewingProduct, setViewingProduct] = useState(null); // Holds the product being viewed in detail

  // --- REFACTORED: Reusable function to load orders ---
  const loadOrders = async () => {
    try {
      setLoading(true);
      setError(null);

      const response = await fetch(
        `${apiClient.defaults.baseURL}/api/Order/GetAllOrders`
      );

      if (!response.ok) {
        throw new Error(
          `HTTP Error: ${response.status} ${response.statusText}`
        );
      }

      const data = await response.json();

      // ✅ API trả về { total, orders: [...] } → lấy mảng thực tế
      const ordersArray = data.orders || [];

      const transformedData = ordersArray.map((order) => {
        // Nếu API mới không còn trường "details", tránh lỗi .map undefined
        const products = (order.details || []).map((detail) => ({
          orderDetailID: detail.orderDetailID,
          name: detail.productName,
          quantity: detail.quantity,
          size: detail.size,
          accessory: detail.accessory,
          note: detail.note,
          status: mapProductionStatusToString(detail.productionStatus),
        }));

        const specialInstructions = (order.details || [])
          .map((d) => d.note)
          .filter(Boolean)
          .join("; ");

        const customerFiles = (order.details || []).map((detail) => ({}));
        const designFiles = (order.details || [])
          .filter((detail) => detail.linkFileDesign)
          .map((detail) => ({}));

        return {
          id: order.orderId,
          code: order.orderCode,
          customer: order.customerName,
          products: products,
          staff: order.sellerName,
          dateManufactured: new Date(order.orderDate).toLocaleDateString(
            "vi-VN"
          ),
          status: order.statusOderName,
          statusOrderId: order.statusOrder,
          customerInfo: {
            name: order.customerName,
            phone: order.phone,
            email: order.email,
            address: order.address,
            city: order.shipCity,
            state: order.shipState,
            zipcode: order.zipcode,
            country: order.shipCountry,
          },
          productDetails: {
            activeTTS: order.activeTTS,
            specialInstructions: specialInstructions,
          },
          customerFiles: customerFiles,
          designFiles: designFiles,
          productImages: [],
        };
      });

      // ✅ cập nhật danh sách
      setAllOrders(transformedData);
    } catch (err) {
      setError(
        "Could not load orders. Please check the API connection and try again."
      );
      console.error("Error fetching orders:", err);
    } finally {
      setLoading(false);
    }
  };

  // --- END REFACTORED FUNCTION ---

  // --- Initial Data Load ---
  useEffect(() => {
    loadOrders(); // Call the reusable function on mount
  }, []); // Empty dependency array means this runs once

  // --- Filtering Logic ---
  const filteredOrders = allOrders.filter((order) => {
    const lowerSearchTerm = searchTerm.toLowerCase();
    const orderIdMatch = order.code?.toLowerCase().includes(lowerSearchTerm);
    const customerMatch = order.customer
      ?.toLowerCase()
      .includes(lowerSearchTerm);
    const productMatch = order.products?.some((product) =>
      product.name?.toLowerCase().includes(lowerSearchTerm)
    );
    const orderStatusString = order.status; // Use the string status name for filtering dropdown
    const matchesSearch = orderIdMatch || customerMatch || productMatch;
    // Compare filterStatus against the string name from API (order.status)
    const matchesFilter =
      filterStatus === "all" || orderStatusString === filterStatus;
    return matchesSearch && matchesFilter;
  });

  // --- Handlers ---
  const handleManualOrderSearch = () => {
    if (!manualOrderId.trim()) return;
    const foundOrder = allOrders.find(
      (order) =>
        order.id?.toString().toLowerCase() ===
          manualOrderId.trim().toLowerCase() ||
        order.code?.toLowerCase() === manualOrderId.trim().toLowerCase()
    );
    if (foundOrder) {
      setSelectedOrder(foundOrder);
      setManualOrderId("");
    } else {
      alert(`Order with ID or Code "${manualOrderId}" not found.`);
    }
  };

  // --- UPDATED: handleApprove with API call ---
  const handleApprove = (orderId) => {
    setConfirmMessage(
      `Are you sure you want to approve order ${
        selectedOrder?.code || orderId
      } for shipping?`
    );

    setConfirmAction(() => async () => {
      console.log(`Attempting to approve order ${orderId}...`);
      try {
        setLoading(true); // Indicate loading state during API call
        const response = await fetch(
          `${apiClient.defaults.baseURL}/api/Order/${orderId}/approve-shipping`,
          {
            method: "PUT",
            headers: {
              "Content-Type": "application/json",
              // Add Authorization header if required:
              // 'Authorization': `Bearer ${your_token}`
            },
          }
        );

        if (!response.ok) {
          let errorMsg = `API Error: ${response.status} ${response.statusText}`;
          try {
            const errorData = await response.json();
            errorMsg = errorData.message || errorData.title || errorMsg;
          } catch (e) {
            /* Ignore if body is not JSON */
          }
          throw new Error(errorMsg);
        }

        console.log(`Order ${orderId} approved successfully.`);
        setShowConfirmDialog(false);
        setSelectedOrder(null);

        await loadOrders(); // Refresh the order list

        alert(`Order ${selectedOrder?.code || orderId} approved for shipping!`);
      } catch (error) {
        console.error("Failed to approve order:", error);
        alert(`Failed to approve order: ${error.message}`);
        setShowConfirmDialog(false); // Close confirm dialog on error
      } finally {
        setLoading(false); // Ensure loading is turned off
      }
    });

    setShowConfirmDialog(true);
  };
  // --- END UPDATED handleApprove ---

  const handleReject = (orderId) => {
    const orderToReject = selectedOrder;
    if (orderToReject) {
      setShowRejectDialog(true);
    } else {
      console.error("Cannot reject: No order selected.");
    }
  };

  const handleRejectConfirm = async () => {
    // Make async for potential API call
    if (rejectReason.trim() && selectedOrder) {
      const orderId = selectedOrder.id;
      console.log(
        `Attempting to reject order ${orderId}. Reason: ${rejectReason}...`
      );
      try {
        setLoading(true);
        // TODO: Implement API call for rejection (Example)
        /*
         const response = await fetch(`https://localhost:7015/api/Orders/${orderId}/reject`, {
           method: 'PUT', // Or POST, depending on your API design
           headers: { 'Content-Type': 'application/json' },
           body: JSON.stringify({ reason: rejectReason })
         });
         if (!response.ok) {
           // Handle API error
           throw new Error('Failed to reject order via API');
         }
         */
        console.log(`Order ${orderId} rejected successfully (simulation).`);
        setShowRejectDialog(false);
        setRejectReason("");
        setSelectedOrder(null);
        await loadOrders(); // Refresh data after rejection
        alert(`Order ${selectedOrder?.code || orderId} rejected.`);
      } catch (error) {
        console.error("Failed to reject order:", error);
        alert(`Failed to reject order: ${error.message}`);
        // Optionally keep reject dialog open on error
      } finally {
        setLoading(false);
      }
    }
  };

  const handleDownload = (fileUrl, fileName) => {
    // ... (handleDownload function remains the same) ...
    if (!fileUrl || fileUrl === "#") {
      console.warn(`Invalid URL for download: ${fileName}`);
      alert(`Cannot download file "${fileName}" - Invalid URL provided.`);
      return;
    }
    console.log(`Attempting to download: ${fileName} from ${fileUrl}`);
    const link = document.createElement("a");
    link.href = fileUrl;
    link.target = "_blank";
    link.download = fileName || "download";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  // --- JSX ---
  return (
    <div className="flex h-screen bg-gray-100">
      <QcSidebar currentPage={currentPage} setCurrentPage={setCurrentPage} />
      <div className="flex-1 flex flex-col overflow-hidden">
        <QcHeader />
        <main className="flex-1 overflow-y-auto p-6">
          <div className="space-y-6">
            {/* Header */}
            <div className="bg-white p-4 rounded-lg shadow-sm border border-gray-200">
              <h1 className="text-xl font-semibold text-gray-800">
                Check Product
              </h1>
              <p className="text-gray-600 mt-1 text-sm">
                Perform quality checks on manufactured products.
              </p>
            </div>

            {/* Search and Filter */}
            <div className="bg-white p-4 rounded-lg shadow-sm border border-gray-200">
              <div className="flex flex-col md:flex-row gap-4 items-center mb-4">
                <div className="flex-1 relative w-full">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                  <Input
                    placeholder="Search Order Code, Customer, Product..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-10 w-full"
                  />
                </div>
                {/* --- Filter Dropdown: Values should match API string status names --- */}
                <select
                  value={filterStatus}
                  onChange={(e) => setFilterStatus(e.target.value)}
                  className="px-3 py-2 border border-gray-300 rounded-md text-sm w-full md:w-auto"
                >
                  <option value="all">All Status</option>
                  {/* Example values - adjust to match your API's statusOderName strings */}
                  <option value="Cần Check Design">Cần Check Design</option>
                  <option value="Sản xuất Xong">Sản xuất Xong</option>
                  <option value="Packing">Packing</option>
                  <option value="Cancelled">Cancelled</option>
                  {/* Add all relevant statuses returned by API's statusOderName */}
                </select>
                {/* ---------------------------------------------------------------- */}
              </div>
              <div className="flex gap-2 items-center">
                <Input
                  placeholder="Enter Order ID or Code manually..."
                  value={manualOrderId}
                  onChange={(e) => setManualOrderId(e.target.value)}
                  className="flex-1"
                />
                <Button
                  onClick={handleManualOrderSearch}
                  variant="outline"
                  size="sm"
                >
                  <Search className="h-4 w-4 mr-1" />
                  Find Order
                </Button>
              </div>
            </div>

            {/* Orders Table */}
            <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Code
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Customer
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Products
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Staff
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Date
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Action
                    </th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {loading && (
                    <tr>
                      <td
                        colSpan="6"
                        className="text-center px-6 py-4 text-gray-500"
                      >
                        Loading orders...
                      </td>
                    </tr>
                  )}
                  {error && (
                    <tr>
                      <td
                        colSpan="6"
                        className="text-center px-6 py-4 text-red-600"
                      >
                        {error}
                      </td>
                    </tr>
                  )}
                  {!loading && !error && filteredOrders.length === 0 && (
                    <tr>
                      <td
                        colSpan="6"
                        className="text-center px-6 py-4 text-gray-500"
                      >
                        No orders found matching your criteria.
                      </td>
                    </tr>
                  )}
                  {!loading &&
                    !error &&
                    filteredOrders.map((order) => (
                      <tr
                        key={order.id}
                        className="hover:bg-gray-50 transition-colors"
                      >
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                          {order.code}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                          {order.customer}
                        </td>
                        <td
                          className="px-6 py-4 text-sm text-gray-600 max-w-xs truncate"
                          title={order.products?.map((p) => p.name).join(", ")}
                        >
                          {order.products?.map((p) => p.name).join(", ") ||
                            "N/A"}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                          {order.staff}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                          {order.dateManufactured}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => setSelectedOrder(order)}
                          >
                            <Eye className="h-4 w-4 mr-1" /> Inspect
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

      {/* Product Inspection Modal (Vertical Layout with Approve Button Logic) */}
      {selectedOrder && (
        <div className="fixed inset-0 bg-black/60 flex items-center justify-center p-4 z-50 transition-opacity duration-300">
          <div className="bg-white rounded-lg shadow-xl max-w-3xl w-full max-h-[90vh] transform transition-all duration-300 scale-95 opacity-0 animate-fade-in-scale flex flex-col">
            <style jsx global>{`
              @keyframes fade-in-scale {
                from {
                  opacity: 0;
                  transform: scale(0.95);
                }
                to {
                  opacity: 1;
                  transform: scale(1);
                }
              }
              .animate-fade-in-scale {
                animation: fade-in-scale 0.2s ease-out forwards;
              }
            `}</style>
            <div className="sticky top-0 bg-white px-6 py-4 border-b border-gray-200 flex justify-between items-center z-10 flex-shrink-0">
              <h2 className="text-lg font-semibold text-gray-800">
                Product Inspection - Order {selectedOrder.code}
              </h2>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setSelectedOrder(null)}
                className="rounded-full text-gray-500 hover:bg-gray-100 -mr-2"
              >
                <X className="h-5 w-5" />
              </Button>
            </div>
            <div className="p-6 space-y-6 overflow-y-auto">
              <div className="bg-gray-50 p-4 rounded-lg border border-gray-200">
                <h3 className="font-semibold text-base mb-3 text-gray-700">
                  Customer Information
                </h3>
                <div className="grid grid-cols-2 gap-x-4 gap-y-2 text-sm">
                  <div>
                    <Label className="text-xs text-gray-500">Name</Label>
                    <p className="font-medium text-gray-800">
                      {selectedOrder.customerInfo?.name || "N/A"}
                    </p>
                  </div>
                  <div>
                    <Label className="text-xs text-gray-500">Phone</Label>
                    <p className="font-medium text-gray-800">
                      {selectedOrder.customerInfo?.phone || "N/A"}
                    </p>
                  </div>
                  <div className="col-span-2">
                    <Label className="text-xs text-gray-500">Email</Label>
                    <p className="font-medium text-gray-800 truncate">
                      {selectedOrder.customerInfo?.email || "N/A"}
                    </p>
                  </div>
                  <div className="col-span-2">
                    <Label className="text-xs text-gray-500">Address</Label>
                    <p className="font-medium text-gray-800">
                      {`${selectedOrder.customerInfo?.address || ""}${
                        selectedOrder.customerInfo?.city
                          ? `, ${selectedOrder.customerInfo.city}`
                          : ""
                      }${
                        selectedOrder.customerInfo?.state
                          ? `, ${selectedOrder.customerInfo.state}`
                          : ""
                      }${
                        selectedOrder.customerInfo?.zipcode
                          ? ` ${selectedOrder.customerInfo.zipcode}`
                          : ""
                      }${
                        selectedOrder.customerInfo?.country
                          ? `, ${selectedOrder.customerInfo.country}`
                          : ""
                      }` || "N/A"}
                    </p>
                  </div>
                </div>
              </div>
              <div className="bg-blue-50 p-4 rounded-lg border border-blue-200">
                <h3 className="font-semibold text-base mb-3 text-blue-800">
                  Order & Product Details
                </h3>
                <div className="space-y-3 mb-4 max-h-60 overflow-y-auto pr-2">
                  {selectedOrder.products?.map((product, index) => (
                    <div
                      key={index}
                      className="bg-white p-3 rounded border border-gray-200 shadow-sm"
                    >
                      <div className="flex justify-between items-center mb-2">
                        <p
                          className="font-medium text-sm text-gray-800 flex-1 mr-2 truncate"
                          title={product.name}
                        >
                          {product.name || "Unnamed Product"}
                        </p>
                        <Button
                          variant="outline"
                          size="sm"
                          className="text-xs h-7 px-2 flex-shrink-0"
                          onClick={() => {
                            if (product.orderDetailID) {
                              // !!! IMPORTANT: Adjust path if needed !!!
                              router.push(
                                `/qc/order-detail/${product.orderDetailID}`
                              );
                            } else {
                              console.error("Order Detail ID missing");
                              alert("Cannot navigate: ID missing.");
                            }
                          }}
                        >
                          <Eye className="h-3 w-3 mr-1" /> Detail
                        </Button>
                      </div>
                      <div className="grid grid-cols-4 gap-x-2 text-xs">
                        <div>
                          <Label className="text-gray-500">Qty:</Label>
                          <p className="font-medium text-gray-700">
                            {product.quantity ?? "?"}
                          </p>
                        </div>
                        <div>
                          <Label className="text-gray-500">Size:</Label>
                          <p className="font-medium text-gray-700">
                            {product.size || "-"}
                          </p>
                        </div>
                        <div>
                          <Label className="text-gray-500">Accessory:</Label>
                          <p className="font-medium text-gray-700">
                            {product.accessory || "-"}
                          </p>
                        </div>
                        <div>
                          <Label className="text-gray-500">Status:</Label>
                          <Badge
                            variant={getStatusBadgeVariant(product.status)}
                            className="text-xs px-1.5 py-0.5"
                          >
                            {product.status || "Unknown"}
                          </Badge>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
                <div className="grid grid-cols-2 gap-x-4 gap-y-2 text-sm pt-3 border-t border-blue-100">
                  <div>
                    <Label className="text-xs text-gray-500">
                      Staff Assigned
                    </Label>
                    <p className="font-medium text-gray-800">
                      {selectedOrder.staff || "N/A"}
                    </p>
                  </div>
                  <div>
                    <Label className="text-xs text-gray-500">Order Date</Label>
                    <p className="font-medium text-gray-800">
                      {selectedOrder.dateManufactured || "N/A"}
                    </p>
                  </div>
                  {selectedOrder.productDetails?.specialInstructions && (
                    <div className="col-span-2 mt-1">
                      <Label className="text-xs text-gray-500">Notes</Label>
                      <p className="font-medium text-gray-800 text-sm bg-yellow-100 border border-yellow-200 p-2 rounded mt-1">
                        {selectedOrder.productDetails.specialInstructions}
                      </p>
                    </div>
                  )}
                </div>
              </div>
            </div>{" "}
            {/* End Modal Body */}
            {/* Footer / Actions Row */}
            <div className="flex justify-end gap-3 px-6 py-4 border-t border-gray-200 bg-gray-50 rounded-b-lg flex-shrink-0">
              {(() => {
                // Check if the order status is already PACKING (12) or beyond
                const isAlreadyApprovedOrBeyond =
                  selectedOrder.statusOrderId >= 12;

                if (isAlreadyApprovedOrBeyond) {
                  return (
                    <>
                      {/* Reject button might still be needed depending on workflow */}
                      {/* <Button variant="outline" onClick={() => handleReject(selectedOrder.id)} className="text-red-600 border-red-300 hover:bg-red-50 ...">...</Button> */}
                      <Button
                        className="bg-gray-400 text-white px-5 cursor-not-allowed"
                        disabled={true}
                        title="This order has already been approved or shipped."
                      >
                        <Check className="h-4 w-4 mr-1" /> Already Approved
                      </Button>
                    </>
                  );
                } else {
                  // Check if all products are QC_DONE
                  const canApprove =
                    selectedOrder.products?.length > 0 &&
                    selectedOrder.products.every((p) => p.status === "QC_DONE");
                  return (
                    <>
                      {/* <Button variant="outline" onClick={() => handleReject(selectedOrder.id)} className="text-red-600 border-red-300 hover:bg-red-50 hover:border-red-500 hover:text-red-700">
                            <X className="h-4 w-4 mr-1" /> Reject Product
                          </Button> */}
                      <Button
                        onClick={() => handleApprove(selectedOrder.id)}
                        className="bg-green-600 hover:bg-green-700 text-white px-5 disabled:opacity-50 disabled:cursor-not-allowed"
                        disabled={!canApprove || loading} // Also disable while loading
                        title={
                          !canApprove
                            ? "All products must have status QC_DONE to approve"
                            : "Approve order for shipping"
                        }
                      >
                        <Check className="h-4 w-4 mr-1" /> Approve for Shipping
                      </Button>
                    </>
                  );
                }
              })()}
            </div>
          </div>
        </div>
      )}

      {/* Reject Reason Dialog */}
      {showRejectDialog && (
        <div className="fixed inset-0 bg-black/60 flex items-center justify-center p-4 z-[60] transition-opacity duration-300">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full p-6 transform transition-all duration-300 scale-95 opacity-0 animate-fade-in-scale">
            <style jsx global>{`
              @keyframes fade-in-scale {
                from {
                  opacity: 0;
                  transform: scale(0.95);
                }
                to {
                  opacity: 1;
                  transform: scale(1);
                }
              }
              .animate-fade-in-scale {
                animation: fade-in-scale 0.2s ease-out forwards;
              }
            `}</style>
            <h3 className="text-lg font-semibold mb-4 text-gray-800">
              Reject Product
            </h3>
            <div className="space-y-4">
              <div>
                <Label
                  htmlFor="reject-reason"
                  className="text-sm font-medium text-gray-700"
                >
                  Reason for rejection <span className="text-red-500">*</span>
                </Label>
                <Textarea
                  id="reject-reason"
                  placeholder="Please provide a detailed reason..."
                  value={rejectReason}
                  onChange={(e) => setRejectReason(e.target.value)}
                  rows={4}
                  className="mt-1 w-full"
                />
                {!rejectReason.trim() && (
                  <p className="text-xs text-red-500 mt-1">
                    Reason cannot be empty.
                  </p>
                )}
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
                onClick={handleRejectConfirm}
                className="bg-red-600 hover:bg-red-700 text-white disabled:opacity-50"
                disabled={!rejectReason.trim() || loading}
              >
                {" "}
                {/* Disable while loading */}
                Confirm Rejection
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* General Confirmation Dialog */}
      {showConfirmDialog && (
        <div className="fixed inset-0 bg-black/60 flex items-center justify-center p-4 z-[60] transition-opacity duration-300">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full p-6 transform transition-all duration-300 scale-95 opacity-0 animate-fade-in-scale">
            <style jsx global>{`
              @keyframes fade-in-scale {
                from {
                  opacity: 0;
                  transform: scale(0.95);
                }
                to {
                  opacity: 1;
                  transform: scale(1);
                }
              }
              .animate-fade-in-scale {
                animation: fade-in-scale 0.2s ease-out forwards;
              }
            `}</style>
            <h3 className="text-lg font-semibold mb-4 text-gray-800">
              Confirm Action
            </h3>
            <p className="text-gray-600 mb-6 text-sm">{confirmMessage}</p>
            <div className="flex gap-3 justify-end">
              <Button
                variant="outline"
                onClick={() => setShowConfirmDialog(false)}
                disabled={loading}
              >
                Cancel
              </Button>{" "}
              {/* Disable while loading */}
              <Button
                onClick={() => {
                  if (confirmAction) confirmAction();
                }}
                className="bg-blue-600 hover:bg-blue-700 text-white"
                disabled={loading}
              >
                Confirm
              </Button>{" "}
              {/* Disable while loading */}
            </div>
          </div>
        </div>
      )}

      {/* Product Detail Modal */}
      {viewingProduct && (
        <div className="fixed inset-0 bg-black/60 flex items-center justify-center p-4 z-[60] transition-opacity duration-300">
          <div className="bg-white rounded-lg shadow-xl max-w-lg w-full transform transition-all duration-300 scale-95 opacity-0 animate-fade-in-scale flex flex-col max-h-[85vh]">
            <style jsx global>{`
              @keyframes fade-in-scale {
                from {
                  opacity: 0;
                  transform: scale(0.95);
                }
                to {
                  opacity: 1;
                  transform: scale(1);
                }
              }
              .animate-fade-in-scale {
                animation: fade-in-scale 0.2s ease-out forwards;
              }
            `}</style>
            <div className="flex justify-between items-center px-6 py-4 border-b border-gray-200 flex-shrink-0">
              <h3 className="text-lg font-medium text-gray-800 text-center flex-grow">
                Product Details
              </h3>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setViewingProduct(null)}
                className="rounded-full text-gray-500 hover:bg-gray-100 -mr-2"
              >
                <X className="h-5 w-5" />
              </Button>
            </div>
            <div className="p-6 space-y-5 overflow-y-auto">
              <div className="text-center mb-4">
                <p className="text-xl font-semibold text-gray-900">
                  {viewingProduct.name || "N/A"}
                </p>
              </div>
              <dl className="border-t border-gray-200 pt-4">
                <div className="grid grid-cols-3 gap-x-4 gap-y-4 text-sm">
                  <div className="border-b border-gray-100 pb-2">
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Quantity
                    </dt>
                    <dd className="mt-1 text-gray-900 font-medium">
                      {viewingProduct.quantity ?? "?"}
                    </dd>
                  </div>
                  <div className="border-b border-gray-100 pb-2">
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Size
                    </dt>
                    <dd className="mt-1 text-gray-900 font-medium">
                      {viewingProduct.size || "-"}
                    </dd>
                  </div>
                  <div className="border-b border-gray-100 pb-2">
                    <dt className="text-xs font-medium text-gray-500 uppercase tracking-wider">
                      Accessory
                    </dt>
                    <dd className="mt-1 text-gray-900 font-medium">
                      {viewingProduct.accessory || "-"}
                    </dd>
                  </div>
                </div>
              </dl>
              <div className="pt-4">
                <h4 className="text-sm font-semibold text-gray-700 mb-2">
                  Note / Instructions
                </h4>
                {viewingProduct.note ? (
                  <div className="text-gray-700 bg-amber-50 border border-amber-200 rounded-md p-3 text-sm shadow-sm leading-relaxed">
                    {viewingProduct.note}
                  </div>
                ) : (
                  <p className="text-gray-500 italic text-sm">
                    No specific note provided.
                  </p>
                )}
              </div>
            </div>
            <div className="flex justify-end px-6 py-3 bg-gray-50 border-t border-gray-200 rounded-b-lg flex-shrink-0">
              <Button variant="outline" onClick={() => setViewingProduct(null)}>
                Close
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
