"use client";

import { useState } from "react";
import RoleSidebar from "@/components/layout/shared/role-sidebar";
import SellerHeader from "@/components/layout/seller/header";
import QcSidebar from "@/components/layout/qc/sidebar";
import QcHeader from "@/components/layout/qc/header";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Eye } from "lucide-react";

export default function QCDashboard() {
  const [currentPage, setCurrentPage] = useState("dashboard");

  // Mock data for design reviews
  const designReviews = [
    {
      id: "ORD-001",
      customer: "John Doe",
      product: "Acrylic Keychain",
      designer: "Alice Johnson",
      dateSubmitted: "2024-01-15",
      status: "pending_review",
    },
    {
      id: "ORD-002",
      customer: "Jane Smith",
      product: "Acrylic Stand",
      designer: "Bob Wilson",
      dateSubmitted: "2024-01-14",
      status: "pending_review",
    },
  ];

  // Mock data for product checks
  const productChecks = [
    {
      id: "ORD-003",
      customer: "Mike Brown",
      product: "Acrylic Charm",
      staff: "Charlie Davis",
      dateManufactured: "2024-01-13",
      status: "pending_check",
    },
  ];

  return (
    <div className="flex h-screen bg-gray-50">
      <QcSidebar currentPage={currentPage} setCurrentPage={setCurrentPage} />
      <div className="flex-1 flex flex-col overflow-hidden">
        <QcHeader />
        <main className="flex-1 overflow-y-auto p-6">
          <div className="space-y-6">
            {/* Header */}
            <div className="bg-white p-4 rounded-lg border-2 border-blue-200">
              <h1 className="text-xl font-semibold text-gray-900">
                QC Dashboard
              </h1>
              <p className="text-gray-600 mt-1">
                Quality Control workspace overview
              </p>
            </div>

            {/* Stats Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-sm font-medium text-gray-500">
                  Design Reviews
                </h3>
                <p className="text-2xl font-bold text-gray-900 mt-2">
                  {designReviews.length}
                </p>
              </div>
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-sm font-medium text-gray-500">
                  Product Checks
                </h3>
                <p className="text-2xl font-bold text-gray-900 mt-2">
                  {productChecks.length}
                </p>
              </div>
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-sm font-medium text-gray-500">
                  Approved Today
                </h3>
                <p className="text-2xl font-bold text-gray-900 mt-2">5</p>
              </div>
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-sm font-medium text-gray-500">
                  Quality Score
                </h3>
                <p className="text-2xl font-bold text-gray-900 mt-2">98%</p>
              </div>
            </div>

            {/* Design Reviews List */}
            <div className="bg-white rounded-lg shadow">
              <div className="p-4 border-b border-gray-200">
                <h2 className="text-lg font-medium text-gray-900">
                  Pending Design Reviews
                </h2>
                <p className="text-sm text-gray-600">
                  Orders from designers waiting for QC approval
                </p>
              </div>
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Order ID
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Customer
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Product
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Designer
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Date Submitted
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Status
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {designReviews.map((order) => (
                      <tr key={order.id}>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                          {order.id}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {order.customer}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {order.product}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {order.designer}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {order.dateSubmitted}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <Badge variant="secondary">Pending Review</Badge>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                          <Button variant="outline" size="sm">
                            <Eye className="h-4 w-4 mr-1" />
                            Review
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {/* Product Checks List */}
            <div className="bg-white rounded-lg shadow">
              <div className="p-4 border-b border-gray-200">
                <h2 className="text-lg font-medium text-gray-900">
                  Pending Product Checks
                </h2>
                <p className="text-sm text-gray-600">
                  Manufactured products waiting for quality inspection
                </p>
              </div>
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Order ID
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Customer
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Product
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Staff
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Date Manufactured
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Status
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Actions
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {productChecks.map((order) => (
                      <tr key={order.id}>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                          {order.id}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {order.customer}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {order.product}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {order.staff}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                          {order.dateManufactured}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          <Badge variant="secondary">Pending Check</Badge>
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                          <Button variant="outline" size="sm">
                            <Eye className="h-4 w-4 mr-1" />
                            Inspect
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}
