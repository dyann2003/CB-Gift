"use client";

import { useState, useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ArrowLeft, Edit2, Download } from "lucide-react";
import apiClient from "@/lib/apiClient";
import OrderEditModal from "@/components/modals/order-edit-modal";

export default function OrderViewDetailsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const orderId = searchParams.get("id");

  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);

  // Fetch order details
  useEffect(() => {
    if (!orderId) {
      setError("Order ID not found");
      setLoading(false);
      return;
    }

    const fetchOrderDetails = async () => {
      try {
        setLoading(true);
        const res = await fetch(
          `${apiClient.defaults.baseURL}/api/Seller/${orderId}`,
          {
            credentials: "include",
          }
        );
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const fullOrder = await res.json();

        const mappedOrder = {
          id: fullOrder.orderId,
          orderId: fullOrder.orderCode,
          orderDate: new Date(fullOrder.orderDate).toISOString().split("T")[0],
          customerName: fullOrder.customerName,
          phone: fullOrder.phone || "",
          email: fullOrder.email || "",
          address: fullOrder.address || "",
          address1: fullOrder.address1 || "",
          provinceId: fullOrder.provinceId || "",
          provinceName: fullOrder.provinceName || "",
          districtId: fullOrder.districtId || "",
          districtName: fullOrder.districtName || "",
          wardId: fullOrder.wardId || "",
          wardName: fullOrder.wardName || "",
          zipcode: fullOrder.zipcode || "",
          status: fullOrder.statusOderName,
          totalCost: fullOrder.totalCost || 0,
          creationDate: new Date(fullOrder.creationDate).toLocaleString(),
          products: fullOrder.details.map((detail) => ({
            orderDetailId: detail.orderDetailID,
            productName: detail.productName || "Unknown Product",
            variantId: detail.productVariantID,
            quantity: detail.quantity,
            price: detail.price,
            size: detail.size || "",
            accessory: detail.accessory || "",
            activeTTS: detail.activeTts || false,
            linkImg: detail.linkImg || "",
            linkThanksCard: detail.linkThanksCard || "",
            linkFileDesign: detail.linkFileDesign || "",
            note: detail.note || "",
            productionStatus: detail.productionStatus,
          })),
        };

        setOrder(mappedOrder);
        setError(null);
      } catch (err) {
        console.error("Error fetching order:", err);
        setError("Failed to load order details");
      } finally {
        setLoading(false);
      }
    };

    fetchOrderDetails();
  }, [orderId]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600" />
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="flex items-center justify-center h-screen">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle>Error</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-red-600">{error || "Order not found"}</p>
            <Button onClick={() => router.back()} variant="outline">
              <ArrowLeft className="w-4 h-4 mr-2" />
              Go Back
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  const getStatusColor = (status) => {
    const statusLower = status?.toLowerCase() || "";
    if (statusLower.includes("pending")) return "bg-yellow-100 text-yellow-800";
    if (statusLower.includes("completed")) return "bg-green-100 text-green-800";
    if (statusLower.includes("cancelled")) return "bg-red-100 text-red-800";
    if (statusLower.includes("processing")) return "bg-blue-100 text-blue-800";
    return "bg-gray-100 text-gray-800";
  };

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-6xl mx-auto">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center gap-4">
            <Button variant="outline" size="icon" onClick={() => router.back()}>
              <ArrowLeft className="w-4 h-4" />
            </Button>
            <div>
              <h1 className="text-3xl font-bold">Order #{order.orderId}</h1>
              <p className="text-gray-600">{order.creationDate}</p>
            </div>
          </div>
          <div className="flex gap-2">
            <Badge className={getStatusColor(order.status)}>
              {order.status}
            </Badge>
            <Button onClick={() => setIsEditModalOpen(true)} className="gap-2">
              <Edit2 className="w-4 h-4" />
              Edit Order
            </Button>
          </div>
        </div>

        {/* Main Content */}
        <Tabs defaultValue="customer" className="space-y-4">
          <TabsList>
            <TabsTrigger value="customer">Customer Info</TabsTrigger>
            <TabsTrigger value="products">Products</TabsTrigger>
            <TabsTrigger value="summary">Summary</TabsTrigger>
          </TabsList>

          {/* Customer Information Tab */}
          <TabsContent value="customer">
            <Card>
              <CardHeader>
                <CardTitle>Customer Information</CardTitle>
              </CardHeader>
              <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Name */}
                <div>
                  <p className="text-sm font-medium text-gray-600">Full Name</p>
                  <p className="text-lg text-gray-900">{order.customerName}</p>
                </div>

                {/* Phone */}
                <div>
                  <p className="text-sm font-medium text-gray-600">Phone</p>
                  <p className="text-lg text-gray-900">{order.phone}</p>
                </div>

                {/* Email */}
                <div>
                  <p className="text-sm font-medium text-gray-600">Email</p>
                  <p className="text-lg text-gray-900">{order.email}</p>
                </div>

                {/* Address */}
                <div>
                  <p className="text-sm font-medium text-gray-600">
                    Address Line 1
                  </p>
                  <p className="text-lg text-gray-900">{order.address}</p>
                </div>

                {/* Address 2 */}
                <div>
                  <p className="text-sm font-medium text-gray-600">
                    Address Line 2
                  </p>
                  <p className="text-lg text-gray-900">
                    {order.address1 || "N/A"}
                  </p>
                </div>

                {/* Zipcode */}
                <div>
                  <p className="text-sm font-medium text-gray-600">Zipcode</p>
                  <p className="text-lg text-gray-900">
                    {order.zipcode || "N/A"}
                  </p>
                </div>

                {/* Province */}
                <div>
                  <p className="text-sm font-medium text-gray-600">Province</p>
                  <p className="text-lg text-gray-900">{order.provinceName}</p>
                </div>

                {/* District */}
                <div>
                  <p className="text-sm font-medium text-gray-600">District</p>
                  <p className="text-lg text-gray-900">{order.districtName}</p>
                </div>

                {/* Ward */}
                <div>
                  <p className="text-sm font-medium text-gray-600">Ward</p>
                  <p className="text-lg text-gray-900">{order.wardName}</p>
                </div>
              </CardContent>
            </Card>
          </TabsContent>

          {/* Products Tab */}
          <TabsContent value="products">
            <Card>
              <CardHeader>
                <CardTitle>Products in Order</CardTitle>
                <CardDescription>
                  {order.products.length} product(s)
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                {order.products.map((product, idx) => (
                  <div
                    key={product.orderDetailId || idx}
                    className="border rounded-lg p-4 space-y-4"
                  >
                    {/* Product Header */}
                    <div className="flex items-start justify-between">
                      <div>
                        <h3 className="font-semibold text-lg">
                          {product.productName}
                        </h3>
                        <p className="text-sm text-gray-600">
                          Size: {product.size || "N/A"} • Qty:{" "}
                          {product.quantity}
                        </p>
                      </div>
                      <div className="text-right">
                        <p className="text-2xl font-bold text-blue-600">
                          ${product.price?.toFixed(2) || "0.00"}
                        </p>
                        {product.activeTTS && (
                          <Badge className="mt-2 bg-green-100 text-green-800">
                            TTS Service Active
                          </Badge>
                        )}
                      </div>
                    </div>

                    {/* Product Details Grid */}
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4 pt-4 border-t">
                      {/* Accessory */}
                      <div>
                        <p className="text-sm font-medium text-gray-600">
                          Accessory
                        </p>
                        <p className="text-gray-900">
                          {product.accessory || "None"}
                        </p>
                      </div>

                      {/* Production Status */}
                      <div>
                        <p className="text-sm font-medium text-gray-600">
                          Status
                        </p>
                        <Badge variant="outline">
                          {product.productionStatus}
                        </Badge>
                      </div>

                      {/* Notes */}
                      <div>
                        <p className="text-sm font-medium text-gray-600">
                          Notes
                        </p>
                        <p className="text-gray-900 text-sm">
                          {product.note || "No notes"}
                        </p>
                      </div>
                    </div>

                    {/* File Links */}
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4 pt-4 border-t bg-gray-50 p-4 rounded">
                      {/* Link Image */}
                      <div>
                        <p className="text-sm font-medium text-gray-600 mb-2">
                          Product Image
                        </p>
                        {product.linkImg ? (
                          <div className="space-y-2">
                            <img
                              src={product.linkImg || "/placeholder.svg"}
                              alt="Product"
                              className="w-20 h-20 object-cover rounded border"
                            />
                            <a
                              href={product.linkImg}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="text-xs text-blue-600 hover:underline"
                            >
                              View Full
                            </a>
                          </div>
                        ) : (
                          <p className="text-sm text-gray-500">No image</p>
                        )}
                      </div>

                      {/* Link Thanks Card */}
                      <div>
                        <p className="text-sm font-medium text-gray-600 mb-2">
                          Thanks Card
                        </p>
                        {product.linkThanksCard ? (
                          <div className="space-y-2">
                            <img
                              src={product.linkThanksCard || "/placeholder.svg"}
                              alt="Thanks Card"
                              className="w-20 h-20 object-cover rounded border"
                            />
                            <a
                              href={product.linkThanksCard}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="text-xs text-blue-600 hover:underline"
                            >
                              View Full
                            </a>
                          </div>
                        ) : (
                          <p className="text-sm text-gray-500">
                            No thanks card
                          </p>
                        )}
                      </div>

                      {/* Link File Design */}
                      <div>
                        <p className="text-sm font-medium text-gray-600 mb-2">
                          Design File
                        </p>
                        {product.linkFileDesign ? (
                          <a
                            href={product.linkFileDesign}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex items-center gap-2 text-blue-600 hover:underline"
                          >
                            <Download className="w-4 h-4" />
                            Download
                          </a>
                        ) : (
                          <p className="text-sm text-gray-500">
                            No design file
                          </p>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </CardContent>
            </Card>
          </TabsContent>

          {/* Summary Tab */}
          <TabsContent value="summary">
            <Card>
              <CardHeader>
                <CardTitle>Order Summary</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="border-l-4 border-blue-500 pl-4">
                    <p className="text-sm text-gray-600">Order ID</p>
                    <p className="text-xl font-semibold">{order.orderId}</p>
                  </div>
                  <div className="border-l-4 border-green-500 pl-4">
                    <p className="text-sm text-gray-600">Order Date</p>
                    <p className="text-xl font-semibold">{order.orderDate}</p>
                  </div>
                  <div className="border-l-4 border-purple-500 pl-4">
                    <p className="text-sm text-gray-600">Total Products</p>
                    <p className="text-xl font-semibold">
                      {order.products.length}
                    </p>
                  </div>
                  <div className="border-l-4 border-yellow-500 pl-4">
                    <p className="text-sm text-gray-600">Status</p>
                    <p className="text-xl font-semibold">{order.status}</p>
                  </div>
                </div>

                {/* Total Cost */}
                <div className="bg-gradient-to-r from-blue-50 to-blue-100 p-6 rounded-lg border border-blue-200">
                  <p className="text-sm text-gray-600 mb-2">Total Cost</p>
                  <p className="text-4xl font-bold text-blue-600">
                    ${order.totalCost?.toFixed(2) || "0.00"}
                  </p>
                </div>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>
      </div>

      {/* Edit Modal */}
      {order && (
        <OrderEditModal
          isOpen={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
          order={order}
          onSave={() => {
            setIsEditModalOpen(false);
            // Refresh order details
            window.location.reload();
          }}
        />
      )}
    </div>
  );
}
