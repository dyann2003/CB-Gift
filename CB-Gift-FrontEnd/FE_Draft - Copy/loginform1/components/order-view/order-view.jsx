"use client";

import OrderHeader from "./order-header";
import ProductItem from "./product-item";
import CustomerDetails from "./customer-details";
import ShippingAddress from "./shipping-address";
import BillingSummary from "./billing-summary";
import OrderActivity from "./order-activity";

export default function OrderView({
  order,
  onCancel,
  onBack,
}) {
  if (!order) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-500">Order not found</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <OrderHeader
          orderId={order.id}
          status={order.status}
          createdAt={order.createdAt}
          orderDate={order.orderDate}
          onCancel={onCancel}
          onBack={onBack}
        />

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main Content */}
          <div className="lg:col-span-2">
            {/* Products */}
            <div className="mb-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">
                Products
              </h2>
              {order.products && order.products.length > 0 ? (
                order.products.map((product, idx) => (
                  <ProductItem key={idx} product={product} />
                ))
              ) : (
                <p className="text-gray-500 py-4">No products in order</p>
              )}
            </div>

            {/* Activity Timeline */}
            {order.activities && (
              <OrderActivity activities={order.activities} />
            )}
          </div>

          {/* Sidebar */}
          <div className="lg:col-span-1">
            <div className="space-y-4">
              {/* Customer Details */}
              {order.customer && (
                <CustomerDetails customer={order.customer} />
              )}

              {/* Shipping Address */}
              {order.shippingAddress && (
                <ShippingAddress address={order.shippingAddress} />
              )}

              {/* Billing Summary */}
              {order.billing && (
                <BillingSummary billing={order.billing} />
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
