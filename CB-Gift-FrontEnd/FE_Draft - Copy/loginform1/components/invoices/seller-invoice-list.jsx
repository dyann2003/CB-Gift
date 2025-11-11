"use client";

import { useState, useMemo } from "react";
import { Filter, Eye } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Input } from "@/components/ui/input";

const mockInvoices = [
  {
    id: "INV-2025-01-001",
    month: "January 2025",
    totalAmount: 8_500_000,
    status: "paid",
    createdDate: "15/01/2025",
    dueDate: "25/01/2025",
    paidDate: "20/01/2025",
    paidAmount: 8_500_000,
    paymentType: "Full Payment",
    transactionId: "TXN-2025-001",
  },
  {
    id: "INV-2025-02-001",
    month: "December 2024",
    totalAmount: 7_200_000,
    status: "pending",
    createdDate: "10/01/2025",
    dueDate: "20/01/2025",
    paidDate: null,
    paidAmount: 0,
    paymentType: null,
    transactionId: null,
  },
  {
    id: "INV-2025-03-001",
    month: "November 2024",
    totalAmount: 6_800_000,
    status: "partial",
    createdDate: "05/01/2025",
    dueDate: "15/01/2025",
    paidDate: "12/01/2025",
    paidAmount: 3_400_000,
    paymentType: "50%",
    transactionId: "TXN-2025-002",
  },
  {
    id: "INV-2025-04-001",
    month: "October 2024",
    totalAmount: 5_500_000,
    status: "partial",
    createdDate: "01/01/2025",
    dueDate: "10/01/2025",
    paidDate: "08/01/2025",
    paidAmount: 1_650_000,
    paymentType: "30%",
    transactionId: "TXN-2025-003",
  },
  {
    id: "INV-2025-05-001",
    month: "September 2024",
    totalAmount: 4_200_000,
    status: "partial",
    createdDate: "20/12/2024",
    dueDate: "30/12/2024",
    paidDate: "28/12/2024",
    paidAmount: 840_000,
    paymentType: "20%",
    transactionId: "TXN-2024-012",
  },
];

export default function SellerInvoiceList({ onPayment }) {
  const [searchTerm, setSearchTerm] = useState("");
  const [paymentTypeFilter, setPaymentTypeFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");
  const [itemsPerPage, setItemsPerPage] = useState(10);
  const [currentPage, setCurrentPage] = useState(1);

  const filteredInvoices = useMemo(() => {
    return mockInvoices.filter((invoice) => {
      const matchesSearch =
        invoice.id.toLowerCase().includes(searchTerm.toLowerCase()) ||
        invoice.month.toLowerCase().includes(searchTerm.toLowerCase());

      const matchesPaymentType =
        paymentTypeFilter === "all" ||
        (paymentTypeFilter === "full" &&
          invoice.paymentType === "Full Payment") ||
        (paymentTypeFilter === "20" && invoice.paymentType === "20%") ||
        (paymentTypeFilter === "30" && invoice.paymentType === "30%") ||
        (paymentTypeFilter === "50" && invoice.paymentType === "50%") ||
        (paymentTypeFilter === "none" && invoice.paymentType === null);

      const matchesStatus =
        statusFilter === "all" || invoice.status === statusFilter;

      return matchesSearch && matchesPaymentType && matchesStatus;
    });
  }, [searchTerm, paymentTypeFilter, statusFilter]);

  const totalPages = Math.ceil(filteredInvoices.length / itemsPerPage);
  const startIdx = (currentPage - 1) * itemsPerPage;
  const paginatedInvoices = filteredInvoices.slice(
    startIdx,
    startIdx + itemsPerPage
  );

  const getStatusBadge = (status) => {
    switch (status) {
      case "paid":
        return { bg: "bg-green-100", text: "text-green-800", label: "Paid" };
      case "partial":
        return {
          bg: "bg-yellow-100",
          text: "text-yellow-800",
          label: "Partial",
        };
      default:
        return { bg: "bg-red-100", text: "text-red-800", label: "Pending" };
    }
  };

  const handlePageChange = (page) => {
    setCurrentPage(Math.max(1, Math.min(page, totalPages)));
  };

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="bg-blue-50 rounded-lg p-4 border border-blue-200 space-y-4">
        <div className="flex items-center gap-2 mb-4">
          <Filter className="h-5 w-5 text-blue-600" />
          <h3 className="font-semibold text-gray-900">Filters</h3>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="text-sm font-medium text-gray-700 mb-2 block">
              Search
            </label>
            <Input
              placeholder="Search invoice ID or month..."
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
                setCurrentPage(1);
              }}
              className="border-blue-200 focus:border-blue-500"
            />
          </div>

          <div>
            <label className="text-sm font-medium text-gray-700 mb-2 block">
              Status
            </label>
            <Select
              value={statusFilter}
              onValueChange={(value) => {
                setStatusFilter(value);
                setCurrentPage(1);
              }}
            >
              <SelectTrigger className="border-blue-200">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="paid">Paid</SelectItem>
                <SelectItem value="partial">Partial</SelectItem>
                <SelectItem value="pending">Pending</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div>
            <label className="text-sm font-medium text-gray-700 mb-2 block">
              Payment Type
            </label>
            <Select
              value={paymentTypeFilter}
              onValueChange={(value) => {
                setPaymentTypeFilter(value);
                setCurrentPage(1);
              }}
            >
              <SelectTrigger className="border-blue-200">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Types</SelectItem>
                <SelectItem value="full">Full Payment</SelectItem>
                <SelectItem value="20">20%</SelectItem>
                <SelectItem value="30">30%</SelectItem>
                <SelectItem value="50">50%</SelectItem>
                <SelectItem value="none">Not Paid</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="bg-blue-100 border-b border-gray-200">
                <th className="px-4 py-3 text-left font-semibold text-gray-700 text-sm uppercase tracking-wide">
                  Invoice ID
                </th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700 text-sm uppercase tracking-wide">
                  Month
                </th>
                <th className="px-4 py-3 text-right font-semibold text-gray-700 text-sm uppercase tracking-wide">
                  Amount
                </th>
                <th className="px-4 py-3 text-right font-semibold text-gray-700 text-sm uppercase tracking-wide">
                  Paid
                </th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700 text-sm uppercase tracking-wide">
                  Payment Type
                </th>
                <th className="px-4 py-3 text-left font-semibold text-gray-700 text-sm uppercase tracking-wide">
                  Status
                </th>
                <th className="px-4 py-3 text-center font-semibold text-gray-700 text-sm uppercase tracking-wide">
                  Action
                </th>
              </tr>
            </thead>
            <tbody>
              {paginatedInvoices.map((invoice) => {
                const badge = getStatusBadge(invoice.status);
                return (
                  <tr
                    key={invoice.id}
                    className="border-b border-gray-100 hover:bg-gray-50 transition-colors"
                  >
                    <td className="px-4 py-3 font-semibold text-blue-600">
                      {invoice.id}
                    </td>
                    <td className="px-4 py-3 text-gray-700">{invoice.month}</td>
                    <td className="px-4 py-3 text-right font-semibold text-gray-900">
                      {(invoice.totalAmount / 1_000_000).toFixed(1)}M
                    </td>
                    <td className="px-4 py-3 text-right font-semibold text-green-600">
                      {(invoice.paidAmount / 1_000_000).toFixed(1)}M
                    </td>
                    <td className="px-4 py-3 text-gray-700">
                      {invoice.paymentType || "-"}
                    </td>
                    <td className="px-4 py-3">
                      <span
                        className={`px-3 py-1 rounded-full text-xs font-semibold ${badge.bg} ${badge.text}`}
                      >
                        {badge.label}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-center">
                      <button
                        onClick={() => onPayment(invoice)}
                        className="p-2 hover:bg-blue-100 rounded-lg transition-colors text-blue-600 hover:text-blue-700"
                        title="View Details"
                      >
                        <Eye className="h-5 w-5" />
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* Pagination Footer */}
      <div className="bg-blue-100 px-4 py-3 border-t border-blue-200">
        <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
          {/* Left: Showing X to Y of Z */}
          <div className="text-sm text-gray-700 font-medium">
            Showing {paginatedInvoices.length > 0 ? startIdx + 1 : 0} to{" "}
            {Math.min(startIdx + itemsPerPage, filteredInvoices.length)} of{" "}
            {filteredInvoices.length} invoices
          </div>

          {/* Center: Pagination Controls */}
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => handlePageChange(currentPage - 1)}
              disabled={currentPage === 1}
              className="border-blue-200 hover:bg-blue-50 text-sm"
            >
              Previous
            </Button>

            {/* Page Numbers */}
            <div className="flex items-center gap-1">
              {Array.from({ length: totalPages }, (_, i) => i + 1)
                .filter(
                  (page) =>
                    page === 1 ||
                    page === totalPages ||
                    (page >= currentPage - 1 && page <= currentPage + 1)
                )
                .map((page, idx, arr) => (
                  <div key={page}>
                    {idx > 0 && page > arr[idx - 1] + 1 && (
                      <span className="mx-1 text-gray-600 text-xs">...</span>
                    )}
                    <Button
                      variant={currentPage === page ? "default" : "outline"}
                      size="sm"
                      onClick={() => handlePageChange(page)}
                      className={`min-w-8 h-8 p-0 text-sm ${
                        currentPage === page
                          ? "bg-indigo-600 hover:bg-indigo-700 text-white"
                          : "border-blue-200 hover:bg-blue-50"
                      }`}
                    >
                      {page}
                    </Button>
                  </div>
                ))}
            </div>

            <Button
              variant="outline"
              size="sm"
              onClick={() => handlePageChange(currentPage + 1)}
              disabled={currentPage === totalPages}
              className="border-blue-200 hover:bg-blue-50 text-sm"
            >
              Next
            </Button>
          </div>

          {/* Right: Items Per Page */}
          <div className="flex items-center gap-2">
            <span className="text-sm text-gray-700 font-medium">
              Items Per Page
            </span>
            <Select
              value={itemsPerPage.toString()}
              onValueChange={(value) => {
                setItemsPerPage(Number(value));
                setCurrentPage(1);
              }}
            >
              <SelectTrigger className="w-20 border-blue-200 hover:bg-blue-50 text-sm h-8">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="5">5</SelectItem>
                <SelectItem value="10">10</SelectItem>
                <SelectItem value="15">15</SelectItem>
                <SelectItem value="20">20</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
      </div>
    </div>
  );
}
