"use client";

import { useState } from "react";
import RoleSidebar from "@/components/layout/shared/role-sidebar";
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
import { Eye } from "lucide-react";

export default function DesignerDashboard() {
  const [currentPage, setCurrentPage] = useState("dashboard");

  const recentAssignments = [
    {
      id: "ORD-001",
      customerName: "John Doe",
      productName: "Acrylic Keychain",
      quantity: 50,
      assignedDate: "2024-01-15",
      status: "pending",
    },
    {
      id: "ORD-002",
      customerName: "Jane Smith",
      productName: "Acrylic Stand",
      quantity: 25,
      assignedDate: "2024-01-16",
      status: "accepted",
    },
    {
      id: "ORD-003",
      customerName: "Mike Johnson",
      productName: "Acrylic Charm",
      quantity: 100,
      assignedDate: "2024-01-17",
      status: "uploaded",
    },
  ];

  const getStatusBadge = (status) => {
    switch (status) {
      case "pending":
        return <Badge variant="secondary">Pending</Badge>;
      case "accepted":
        return (
          <Badge variant="default" className="bg-green-500">
            Accepted
          </Badge>
        );
      case "uploaded":
        return (
          <Badge variant="default" className="bg-blue-500">
            Design Uploaded
          </Badge>
        );
      default:
        return <Badge variant="secondary">Unknown</Badge>;
    }
  };

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
                Welcome to the Designer workspace
              </p>
            </div>

            {/* Stats Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-sm font-medium text-gray-500">
                  Pending Designs
                </h3>
                <p className="text-2xl font-bold text-gray-900 mt-2">1</p>
              </div>
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-sm font-medium text-gray-500">
                  In Progress
                </h3>
                <p className="text-2xl font-bold text-gray-900 mt-2">1</p>
              </div>
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-sm font-medium text-gray-500">
                  Completed Today
                </h3>
                <p className="text-2xl font-bold text-gray-900 mt-2">1</p>
              </div>
              <div className="bg-white p-6 rounded-lg shadow">
                <h3 className="text-sm font-medium text-gray-500">
                  Total Designs
                </h3>
                <p className="text-2xl font-bold text-gray-900 mt-2">3</p>
              </div>
            </div>

            <div className="bg-white rounded-lg shadow">
              <div className="p-6 border-b border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900">
                  Recent Assignments
                </h2>
                <p className="text-gray-600 mt-1">
                  Latest design assignments from sellers
                </p>
              </div>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Order ID</TableHead>
                    <TableHead>Customer</TableHead>
                    <TableHead>Product</TableHead>
                    <TableHead>Quantity</TableHead>
                    <TableHead>Assigned Date</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {recentAssignments.map((assignment) => (
                    <TableRow key={assignment.id}>
                      <TableCell className="font-medium">
                        {assignment.id}
                      </TableCell>
                      <TableCell>{assignment.customerName}</TableCell>
                      <TableCell>{assignment.productName}</TableCell>
                      <TableCell>{assignment.quantity}</TableCell>
                      <TableCell>{assignment.assignedDate}</TableCell>
                      <TableCell>{getStatusBadge(assignment.status)}</TableCell>
                      <TableCell>
                        <Button variant="outline" size="sm">
                          <Eye className="h-4 w-4 mr-1" />
                          View
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}
