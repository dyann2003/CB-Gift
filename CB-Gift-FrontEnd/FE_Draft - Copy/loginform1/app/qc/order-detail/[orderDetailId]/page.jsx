"use client"

import { useEffect, useState } from "react"
import { useParams, useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"

// --- Helper function to map status ID to string ---
const mapProductionStatusToString = (statusId) => {
  switch (statusId) {
    case 0: return "DRAFT";
    case 1: return "CREATED";
    case 2: return "NEED_DESIGN";
    case 3: return "DESIGNING";
    case 4: return "CHECK_DESIGN";
    case 5: return "DESIGN_REDO";
    case 6: return "READY_PROD";
    case 7: return "IN_PROD";
    case 8: return "FINISHED";
    case 9: return "QC_DONE";
    case 10: return "QC_FAIL";
    case 11: return "PROD_REWORK";
    case 12: return "PACKING";
    case 13: return "HOLD";
    case 14: return "CANCELLED";
    default: return "UNKNOWN"; // Fallback for unexpected values
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

export default function OrderDetailPage() {
  const params = useParams()
  const router = useRouter()
  const orderDetailId = params.orderDetailId

  const [orderDetail, setOrderDetail] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (!orderDetailId) return

    const fetchOrderDetail = async () => {
      try {
        const response = await fetch(`https://localhost:7015/api/OrderDetail/${orderDetailId}`)

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`)
        }

        const data = await response.json()
        setOrderDetail(data)
        setError(null)
      } catch (err) {
        console.error("Error fetching order detail:", err)
        setError(err instanceof Error ? err.message : "Failed to fetch order detail")
      } finally {
        setLoading(false)
      }
    }

    fetchOrderDetail()
  }, [orderDetailId])

  const handleAccept = async () => {
    try {
      const response = await fetch(`https://localhost:7015/api/OrderDetail/${orderDetailId}/accept`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      alert(`Order Detail #${orderDetailId} has been accepted`)

      const updatedResponse = await fetch(`https://localhost:7015/api/OrderDetail/${orderDetailId}`)
      if (updatedResponse.ok) {
        const updatedData = await updatedResponse.json()
        setOrderDetail(updatedData)
      }
    } catch (err) {
      console.error("Error accepting order:", err)
      alert("Failed to accept order")
    }
  }

  const handleReject = async () => {
    try {
      const response = await fetch(`https://localhost:7015/api/OrderDetail/${orderDetailId}/reject`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
      })

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }

      alert(`Order Detail #${orderDetailId} has been rejected`)

      const updatedResponse = await fetch(`https://localhost:7015/api/OrderDetail/${orderDetailId}`)
      if (updatedResponse.ok) {
        const updatedData = await updatedResponse.json()
        setOrderDetail(updatedData)
      }
    } catch (err) {
      console.error("Error rejecting order:", err)
      alert("Failed to reject order")
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-lg text-gray-600">Loading order details...</div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-lg text-red-600">Error: {error}</div>
      </div>
    )
  }

  if (!orderDetail) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-lg text-gray-600">Order detail not found</div>
      </div>
    )
  }

  const product = orderDetail.productVariant?.product
  const productImageUrl = orderDetail.linkImg
    ? orderDetail.linkImg.startsWith("http")
      ? orderDetail.linkImgd
      : `https://localhost:7015/${orderDetail.linkImg}`
    : null

  const statusString = mapProductionStatusToString(orderDetail.productionStatus);
  const statusVariant = getStatusBadgeVariant(statusString);

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between mb-6 gap-4">
          <div className="flex-grow">
            <h1 className="text-3xl font-bold text-gray-900">Order Detail #{orderDetail.orderDetailId}</h1>
            <p className="text-gray-600 mt-1 text-sm sm:text-base"> 
              Order ID: {orderDetail.orderId} | Product: {product?.productName || "N/A"}
            </p>
            <div className="mt-2">
                <Badge variant={statusVariant} className="text-sm px-3 py-1">
                    Status: {statusString}
                </Badge>
            </div>
          </div>
          <Button onClick={() => router.back()} variant="outline" className="gap-2 flex-shrink-0"> {/* Prevent button shrinking */}
            ← Back
          </Button>
        </div>

        {/* Images Section */}
        {(productImageUrl || orderDetail.linkFileDesign) && (
          <Card className="p-6 mb-6">
            <h2 className="text-xl font-semibold mb-4">Images</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {productImageUrl && (
                <div>
                  <p className="font-medium mb-2 text-gray-700">Product Image</p>
                  <img
                    src={productImageUrl || "/placeholder.svg"}
                    alt="Product"
                    className="w-full h-80 object-cover rounded-lg border border-gray-200"
                  />
                </div>
              )}

              {orderDetail.linkFileDesign && (
                <div>
                  <p className="font-medium mb-2 text-gray-700">Design File</p>
                  <a href={orderDetail.linkFileDesign} target="_blank" rel="noopener noreferrer" className="block">
                    <img
                      src={orderDetail.linkFileDesign || "/placeholder.svg"}
                      alt="Design file"
                      className="w-full h-80 object-cover rounded-lg border border-gray-200 hover:opacity-90 transition-opacity cursor-pointer"
                    />
                  </a>
                  <p className="text-sm text-gray-500 mt-2">Click to view full size</p>
                </div>
              )}
            </div>
          </Card>
        )}

        {/* Order Information */}
        <Card className="p-6 mb-6">
          <h2 className="text-xl font-semibold mb-4">Order Information</h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <InfoItem label="Order Detail ID" value={orderDetail.orderDetailId} />
            <InfoItem label="Order ID" value={orderDetail.orderId} />
            <InfoItem label="Quantity" value={orderDetail.quantity} />
            <InfoItem label="Price" value={`$${orderDetail.price.toFixed(2)}`} />
            <InfoItem label="Created Date" value={new Date(orderDetail.createdDate).toLocaleString()} />
            <InfoItem label="Production Status" value={orderDetail.productionStatus || "Pending"} />
            <InfoItem label="Need Design" value={orderDetail.needDesign ? "Yes" : "No"} />
            <InfoItem
              label="Assigned At"
              value={orderDetail.assignedAt ? new Date(orderDetail.assignedAt).toLocaleString() : "Not assigned"}
            />
            <InfoItem label="Designer ID" value={orderDetail.assignedDesignerUserId || "Not assigned"} />
          </div>
        </Card>

        {/* Product Information */}
        <Card className="p-6 mb-6">
          <h2 className="text-xl font-semibold mb-4">Product Information</h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <InfoItem label="Product Name" value={product?.productName} />
            <InfoItem label="Product Code" value={product?.productCode} />
            <InfoItem label="Category ID" value={product?.categoryId} />
            <InfoItem label="Status" value={product?.status} />
            <InfoItem label="SKU" value={orderDetail.productVariant?.sku} />
            <InfoItem label="Accessory" value={orderDetail.accessory} />
          </div>

          {product?.describe && (
            <div className="mt-4">
              <p className="font-medium text-gray-700 mb-1">Description</p>
              <p className="text-gray-600">{product.describe}</p>
            </div>
          )}
        </Card>

        {/* Product Variant Details */}
        <Card className="p-6 mb-6">
          <h2 className="text-xl font-semibold mb-4">Product Variant Details</h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <InfoItem label="Size (inch)" value={orderDetail.productVariant?.sizeInch} />
            <InfoItem label="Thickness (mm)" value={orderDetail.productVariant?.thicknessMm} />
            <InfoItem label="Layer" value={orderDetail.productVariant?.layer} />
            <InfoItem label="Custom Shape" value={orderDetail.productVariant?.customShape} />
            <InfoItem label="Length (cm)" value={orderDetail.productVariant?.lengthCm} />
            <InfoItem label="Height (cm)" value={orderDetail.productVariant?.heightCm} />
            <InfoItem label="Width (cm)" value={orderDetail.productVariant?.widthCm} />
            <InfoItem label="Weight (gram)" value={orderDetail.productVariant?.weightGram} />
          </div>
        </Card>

        {/* Cost Breakdown */}
        <Card className="p-6 mb-6">
          <h2 className="text-xl font-semibold mb-4">Cost Breakdown</h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <InfoItem label="Base Cost" value={`$${orderDetail.productVariant?.baseCost.toFixed(2)}`} />
            <InfoItem label="Ship Cost" value={`$${orderDetail.productVariant?.shipCost.toFixed(2)}`} />
            <InfoItem label="Extra Shipping" value={`$${orderDetail.productVariant?.extraShipping.toFixed(2)}`} />
            <InfoItem
              label="Total Cost"
              value={`$${orderDetail.productVariant?.totalCost.toFixed(2)}`}
              className="font-semibold text-lg"
            />
          </div>
        </Card>

        {/* Notes */}
        {orderDetail.note && (
          <Card className="p-6 mb-6">
            <h2 className="text-xl font-semibold mb-4">Notes</h2>
            <p className="text-gray-700">{orderDetail.note}</p>
          </Card>
        )}

        <div className="flex justify-end gap-4">
          {orderDetail.productionStatus === 9 || orderDetail.productionStatus > 11 ? (
            <div className="px-8 py-3 bg-green-100 text-green-700 rounded-md font-medium">Already Accept</div>
          ) : orderDetail.productionStatus === 10 || orderDetail.productionStatus === 11 ? (
            <div className="px-8 py-3 bg-red-100 text-red-700 rounded-md font-medium">Already Reject</div>
          ) : orderDetail.productionStatus === 8 ? (
            <>
              <Button onClick={handleReject} variant="destructive" size="lg" className="px-8">
                Reject Order
              </Button>
              <Button onClick={handleAccept} size="lg" className="px-8 bg-green-600 hover:bg-green-700">
                Accept Order
              </Button>
            </>
          ) : null}
        </div>
      </div>
    </div>
  )
}

function InfoItem({ label, value, className = "" }) {
  return (
    <div className={className}>
      <p className="text-sm text-gray-500 mb-1">{label}</p>
      <p className="font-medium text-gray-900">{value ?? "N/A"}</p>
    </div>
  )
}
