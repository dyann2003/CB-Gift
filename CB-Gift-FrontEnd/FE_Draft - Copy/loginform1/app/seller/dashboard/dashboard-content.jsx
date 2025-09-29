"use client"

import { Package, Clock, CheckCircle, Truck, DollarSign, AlertTriangle } from "lucide-react"

export default function DashboardContent() {
  const stats = [
    {
      title: "Total Order",
      value: "24",
      color: "bg-blue-50 border-blue-200",
      icon: Package,
      iconColor: "text-blue-500",
    },
    {
      title: "Wait Accepted",
      value: "8",
      color: "bg-yellow-50 border-yellow-200",
      icon: Clock,
      iconColor: "text-yellow-500",
    },
    {
      title: "Manufacturing",
      value: "12",
      color: "bg-purple-50 border-purple-200",
      icon: Package,
      iconColor: "text-purple-500",
    },
    {
      title: "Complete",
      value: "18",
      color: "bg-green-50 border-green-200",
      icon: CheckCircle,
      iconColor: "text-green-500",
    },
    {
      title: "Delivery",
      value: "15",
      color: "bg-orange-50 border-orange-200",
      icon: Truck,
      iconColor: "text-orange-500",
    },
    {
      title: "Refund",
      value: "2",
      color: "bg-red-50 border-red-200",
      icon: AlertTriangle,
      iconColor: "text-red-500",
    },
  ]

  const recentOrders = [
    {
      orderDate: "2024-01-25",
      customerName: "Emma Thompson",
      phone: "+1-555-0101",
      products: ["Custom Keychain (x25)"],
      address: "123 Oak Street, Portland, OR",
      shipTo: "Same as billing",
      status: "Pending",
      amount: "$87.50",
    },
    {
      orderDate: "2024-01-24",
      customerName: "James Wilson",
      phone: "+1-555-0102",
      products: ["Phone Stand (x15)", "Charm Set (x30)"],
      address: "456 Pine Avenue, Seattle, WA",
      shipTo: "Office Address",
      status: "In Progress",
      amount: "$135.00",
    },
    {
      orderDate: "2024-01-23",
      customerName: "Lisa Chen",
      phone: "+1-555-0103",
      products: ["Display Case (x10)"],
      address: "789 Maple Drive, San Francisco, CA",
      shipTo: "Same as billing",
      status: "Completed",
      amount: "$125.00",
    },
  ]

  return (
    <div className="space-y-6">
      {/* Welcome Card */}
      <div className="bg-white p-4 sm:p-6 rounded-lg border-2 border-blue-200 shadow-sm">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-lg sm:text-xl font-semibold text-blue-800">Seller Dashboard</h2>
            <p className="text-sm sm:text-base text-blue-600 mt-1">Track your orders and performance</p>
          </div>
          <div className="mt-3 sm:mt-0">
            <div className="flex items-center gap-2 text-sm text-blue-700">
              <DollarSign className="h-4 w-4" />
              <span>Total Revenue: $2,847.50</span>
            </div>
          </div>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {stats.map((stat, index) => {
          const IconComponent = stat.icon
          return (
            <div
              key={index}
              className={`p-4 sm:p-6 rounded-lg border-2 ${stat.color} hover:shadow-md transition-shadow`}
            >
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-xs sm:text-sm font-medium text-gray-600 uppercase tracking-wide mb-2">
                    {stat.title}
                  </h3>
                  <p className="text-xl sm:text-2xl font-bold text-gray-900">{stat.value}</p>
                </div>
                <IconComponent className={`h-6 w-6 sm:h-8 sm:w-8 ${stat.iconColor}`} />
              </div>
            </div>
          )
        })}
      </div>

      {/* Total Money Card */}
      <div className="bg-gradient-to-r from-blue-500 to-purple-600 p-4 sm:p-6 rounded-lg text-white shadow-lg">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h3 className="text-base sm:text-lg font-medium mb-2">Total Money Earned</h3>
            <p className="text-2xl sm:text-3xl font-bold">$2,847.50</p>
            <p className="text-sm text-blue-100 mt-1">+12.5% from last month</p>
          </div>
          <div className="mt-4 sm:mt-0">
            <DollarSign className="h-12 w-12 sm:h-16 sm:w-16 text-blue-200" />
          </div>
        </div>
      </div>

      {/* Orders Table */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200">
        <div className="px-4 sm:px-6 py-4 border-b border-gray-200">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h3 className="text-lg font-semibold text-gray-900">Recent Orders</h3>
              <p className="text-sm text-gray-600 mt-1">Latest customer orders</p>
            </div>
            <div className="mt-3 sm:mt-0">
              <button className="text-sm text-blue-600 hover:text-blue-800 font-medium">View All Orders →</button>
            </div>
          </div>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="text-left py-3 px-4 font-medium text-gray-600 uppercase text-xs tracking-wide">
                  Order Date
                </th>
                <th className="text-left py-3 px-4 font-medium text-gray-600 uppercase text-xs tracking-wide">
                  Customer
                </th>
                <th className="text-left py-3 px-4 font-medium text-gray-600 uppercase text-xs tracking-wide">Phone</th>
                <th className="text-left py-3 px-4 font-medium text-gray-600 uppercase text-xs tracking-wide">
                  Products
                </th>
                <th className="text-left py-3 px-4 font-medium text-gray-600 uppercase text-xs tracking-wide">
                  Address
                </th>
                <th className="text-left py-3 px-4 font-medium text-gray-600 uppercase text-xs tracking-wide">
                  Ship To
                </th>
                <th className="text-left py-3 px-4 font-medium text-gray-600 uppercase text-xs tracking-wide">
                  Amount
                </th>
              </tr>
            </thead>
            <tbody>
              {recentOrders.map((order, index) => (
                <tr key={index} className="border-b border-gray-200 hover:bg-gray-50 transition-colors">
                  <td className="py-3 px-4 text-gray-600">{order.orderDate}</td>
                  <td className="py-3 px-4">
                    <div className="font-medium text-gray-900">{order.customerName}</div>
                  </td>
                  <td className="py-3 px-4 text-gray-600">{order.phone}</td>
                  <td className="py-3 px-4 text-gray-600">
                    <div className="space-y-1">
                      {order.products.map((product, idx) => (
                        <div key={idx} className="text-sm">
                          {product}
                        </div>
                      ))}
                    </div>
                  </td>
                  <td className="py-3 px-4 text-gray-600 max-w-xs">
                    <div className="truncate" title={order.address}>
                      {order.address}
                    </div>
                  </td>
                  <td className="py-3 px-4 text-gray-600">{order.shipTo}</td>
                  <td className="py-3 px-4 font-medium text-gray-900">{order.amount}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
