"use client";

import { useState } from "react";
import RoleSidebar from "@/components/layout/shared/role-sidebar";
import StaffSidebar from "@/components/layout/staff/sidebar";

import SellerHeader from "@/components/layout/seller/header";
import StaffHeader from "@/components/layout/staff/header";

import { Badge } from "@/components/ui/badge";

export default function StaffDashboard() {
  const [currentPage, setCurrentPage] = useState("dashboard");

  // Mock data for QC approved orders assigned to staff
  const assignedOrders = [
    {
      id: "ORD-001",
      customer: "John Doe",
      product: "Acrylic Keychain",
      designer: "Alice Smith",
      dateAssigned: "2024-01-15",
      status: "qc_approved",
      priority: "high",
    },
    {
      id: "ORD-002",
      customer: "Jane Smith",
      product: "Acrylic Stand",
      designer: "Bob Johnson",
      dateAssigned: "2024-01-14",
      status: "qc_approved",
      priority: "medium",
    },
    {
      id: "ORD-005",
      customer: "Tom Wilson",
      product: "Acrylic Charm",
      designer: "Alice Smith",
      dateAssigned: "2024-01-13",
      status: "qc_approved",
      priority: "low",
    },
  ];

  const getStatusBadge = (status) => {
    switch (status) {
      case "qc_approved":
        return (
          <Badge className="bg-green-100 text-green-800">QC Approved</Badge>
        );
      case "in_production":
        return (
          <Badge className="bg-blue-100 text-blue-800">In Production</Badge>
        );
      case "production_complete":
        return (
          <Badge className="bg-purple-100 text-purple-800">
            Production Complete
          </Badge>
        );
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  };

  const getPriorityBadge = (priority) => {
    switch (priority) {
      case "high":
        return <Badge variant="destructive">High</Badge>;
      case "medium":
        return <Badge className="bg-yellow-100 text-yellow-800">Medium</Badge>;
      case "low":
        return <Badge className="bg-gray-100 text-gray-800">Low</Badge>;
      default:
        return <Badge variant="secondary">{priority}</Badge>;
    }
  };

  return (
    <div className="flex h-screen bg-gray-50">
      <StaffSidebar currentPage={currentPage} setCurrentPage={setCurrentPage} />
      <div className="flex-1 flex flex-col overflow-hidden">
        <StaffHeader />
        <main className="flex-1 overflow-y-auto p-6">
          <div className="space-y-6">
            {/* Header */}
            <div className="bg-white p-4 rounded-lg border-2 border-blue-200">
              <h1 className="text-xl font-semibold text-gray-900">
                Staff Dashboard
              </h1>
              <p className="text-gray-600 mt-1">
                Welcome to the Staff workspace
              </p>
            </div>

            {/* Stats Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-sm font-medium text-gray-500">
                  Assigned Orders
                </h3>
                <p className="text-2xl font-bold text-gray-900 mt-2">
                  {assignedOrders.length}
                </p>
              </div>
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-sm font-medium text-gray-500">
                  In Production
                </h3>
                <p className="text-2xl font-bold text-gray-900 mt-2">0</p>
              </div>
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-sm font-medium text-gray-500">
                  Completed Today
                </h3>
                <p className="text-2xl font-bold text-gray-900 mt-2">0</p>
              </div>
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-sm font-medium text-gray-500">
                  Efficiency
                </h3>
                <p className="text-2xl font-bold text-gray-900 mt-2">95%</p>
              </div>
            </div>

            {/* Recent Assigned Orders */}
            <div className="bg-white rounded-lg shadow">
              <div className="p-4 border-b border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900">
                  Recent Assigned Orders
                </h2>
                <p className="text-gray-600 text-sm">
                  Orders approved by QC and assigned to production
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
                        Date Assigned
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Priority
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Status
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {assignedOrders.map((order) => (
                      <tr key={order.id} className="hover:bg-gray-50">
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
                          {order.dateAssigned}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          {getPriorityBadge(order.priority)}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap">
                          {getStatusBadge(order.status)}
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
