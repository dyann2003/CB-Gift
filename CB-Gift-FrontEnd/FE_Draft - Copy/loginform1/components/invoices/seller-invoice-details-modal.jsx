"use client";

import { useState } from "react";
import { ChevronLeft, ChevronRight, DollarSign, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

const mockOrders = {
  "REC-2025-01-001": [
    { id: "ORD001", date: "01/01/2025", amount: 1_200_000, status: "Paid" },
    { id: "ORD002", date: "05/01/2025", amount: 950_000, status: "Paid" },
    { id: "ORD003", date: "08/01/2025", amount: 2_150_000, status: "Paid" },
    { id: "ORD004", date: "12/01/2025", amount: 1_800_000, status: "Paid" },
    { id: "ORD005", date: "15/01/2025", amount: 1_400_000, status: "Paid" },
    { id: "ORD006", date: "18/01/2025", amount: 1_100_000, status: "Paid" },
    { id: "ORD007", date: "20/01/2025", amount: 2_300_000, status: "Paid" },
    { id: "ORD008", date: "22/01/2025", amount: 1_550_000, status: "Paid" },
    { id: "ORD009", date: "25/01/2025", amount: 1_750_000, status: "Paid" },
    { id: "ORD010", date: "28/01/2025", amount: 1_200_000, status: "Paid" },
  ],
  "REC-2025-02-001": [
    { id: "ORD011", date: "01/12/2024", amount: 1_100_000, status: "Pending" },
    { id: "ORD012", date: "03/12/2024", amount: 1_300_000, status: "Pending" },
    { id: "ORD013", date: "05/12/2024", amount: 2_000_000, status: "Pending" },
    { id: "ORD014", date: "08/12/2024", amount: 900_000, status: "Pending" },
    { id: "ORD015", date: "10/12/2024", amount: 1_500_000, status: "Pending" },
    { id: "ORD016", date: "12/12/2024", amount: 1_200_000, status: "Pending" },
    { id: "ORD017", date: "15/12/2024", amount: 1_800_000, status: "Pending" },
    { id: "ORD018", date: "18/12/2024", amount: 1_100_000, status: "Pending" },
    { id: "ORD019", date: "20/12/2024", amount: 1_450_000, status: "Pending" },
    { id: "ORD020", date: "22/12/2024", amount: 950_000, status: "Pending" },
  ],
  "REC-2025-03-001": [
    { id: "ORD021", date: "01/11/2024", amount: 1_100_000, status: "Partial" },
    { id: "ORD022", date: "05/11/2024", amount: 1_300_000, status: "Partial" },
    { id: "ORD023", date: "08/11/2024", amount: 2_000_000, status: "Partial" },
    { id: "ORD024", date: "10/11/2024", amount: 900_000, status: "Partial" },
    { id: "ORD025", date: "12/11/2024", amount: 1_500_000, status: "Partial" },
    { id: "ORD026", date: "15/11/2024", amount: 1_200_000, status: "Partial" },
    { id: "ORD027", date: "18/11/2024", amount: 1_800_000, status: "Partial" },
    { id: "ORD028", date: "20/11/2024", amount: 1_100_000, status: "Partial" },
    { id: "ORD029", date: "22/11/2024", amount: 1_450_000, status: "Partial" },
    { id: "ORD030", date: "25/11/2024", amount: 950_000, status: "Partial" },
  ],
};

export default function SellerInvoiceDetailsModal({
  invoice,
  isOpen,
  onClose,
  onPayment,
}) {
  const [itemsPerPage, setItemsPerPage] = useState(5);
  const [currentPage, setCurrentPage] = useState(1);

  if (!isOpen) return null;

  const orders = mockOrders[invoice.id] || [];
  const totalPages = Math.ceil(orders.length / itemsPerPage);
  const startIdx = (currentPage - 1) * itemsPerPage;
  const paginatedOrders = orders.slice(startIdx, startIdx + itemsPerPage);

  const getStatusBadge = (status) => {
    return status === "Paid"
      ? { bg: "bg-green-100", text: "text-green-800", label: "Paid" }
      : status === "Pending"
      ? { bg: "bg-red-100", text: "text-red-800", label: "Pending" }
      : { bg: "bg-yellow-100", text: "text-yellow-800", label: "Partial" };
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
      <div className="w-full max-w-5xl max-h-[90vh] bg-white rounded-lg shadow-xl overflow-hidden flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-gray-200 p-6 bg-gradient-to-r from-blue-500 to-blue-600">
          <div>
            <h2 className="text-2xl font-bold text-white">
              Invoice {invoice.id}
            </h2>
            <p className="text-blue-100 text-sm mt-1">{invoice.month}</p>
          </div>
          <button
            onClick={onClose}
            className="p-1 hover:bg-blue-400 rounded-full transition-colors"
          >
            <X className="h-6 w-6 text-white" />
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6 space-y-4">
          {/* Summary Cards */}
          <div className="grid grid-cols-3 gap-4">
            <div className="bg-blue-50 p-4 rounded-lg border border-blue-200">
              <p className="text-xs text-blue-600 font-semibold uppercase">
                Total Amount
              </p>
              <p className="text-xl font-bold text-blue-900 mt-1">
                {(invoice.totalAmount / 1_000_000).toFixed(1)}M VND
              </p>
            </div>
            <div className="bg-green-50 p-4 rounded-lg border border-green-200">
              <p className="text-xs text-green-600 font-semibold uppercase">
                Paid Amount
              </p>
              <p className="text-xl font-bold text-green-900 mt-1">
                {(invoice.paidAmount / 1_000_000).toFixed(1)}M VND
              </p>
            </div>
            <div className="bg-orange-50 p-4 rounded-lg border border-orange-200">
              <p className="text-xs text-orange-600 font-semibold uppercase">
                Remaining
              </p>
              <p className="text-xl font-bold text-orange-900 mt-1">
                {(
                  (invoice.totalAmount - invoice.paidAmount) /
                  1_000_000
                ).toFixed(1)}
                M VND
              </p>
            </div>
          </div>

          {/* Orders Table */}
          <div className="border border-gray-200 rounded-lg overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr className="bg-blue-100 border-b border-gray-200">
                    <th className="px-4 py-3 text-left font-semibold text-gray-700 text-sm">
                      Order ID
                    </th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700 text-sm">
                      Date
                    </th>
                    <th className="px-4 py-3 text-right font-semibold text-gray-700 text-sm">
                      Amount
                    </th>
                    <th className="px-4 py-3 text-left font-semibold text-gray-700 text-sm">
                      Status
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {paginatedOrders.map((order) => {
                    const badge = getStatusBadge(order.status);
                    return (
                      <tr
                        key={order.id}
                        className="border-b border-gray-100 hover:bg-gray-50"
                      >
                        <td className="px-4 py-3 font-semibold text-blue-600">
                          {order.id}
                        </td>
                        <td className="px-4 py-3 text-gray-700">
                          {order.date}
                        </td>
                        <td className="px-4 py-3 text-right font-semibold">
                          {(order.amount / 1_000_000).toFixed(1)}M
                        </td>
                        <td className="px-4 py-3">
                          <span
                            className={`px-3 py-1 rounded-full text-xs font-semibold ${badge.bg} ${badge.text}`}
                          >
                            {badge.label}
                          </span>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {/* Pagination Footer */}
            <div className="bg-blue-100 px-4 py-3 border-t border-blue-200 flex items-center justify-between flex-wrap gap-4">
              <div className="text-sm text-gray-700">
                Showing {startIdx + 1} to{" "}
                {Math.min(startIdx + itemsPerPage, orders.length)} of{" "}
                {orders.length} orders
              </div>

              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
                  disabled={currentPage === 1}
                  className="border-blue-200"
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>

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
                        <span className="mx-1 text-gray-600">...</span>
                      )}
                      <Button
                        variant={currentPage === page ? "default" : "outline"}
                        size="sm"
                        onClick={() => setCurrentPage(page)}
                        className={`w-8 h-8 p-0 ${
                          currentPage === page
                            ? "bg-indigo-600 hover:bg-indigo-700"
                            : "border-blue-200"
                        }`}
                      >
                        {page}
                      </Button>
                    </div>
                  ))}

                <Button
                  variant="outline"
                  size="sm"
                  onClick={() =>
                    setCurrentPage(Math.min(totalPages, currentPage + 1))
                  }
                  disabled={currentPage === totalPages}
                  className="border-blue-200"
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>

                {/* Items Per Page Selector */}
                <div className="ml-4 flex items-center gap-2">
                  <label className="text-xs font-medium text-gray-700">
                    Per Page
                  </label>
                  <Select
                    value={itemsPerPage.toString()}
                    onValueChange={(value) => {
                      setItemsPerPage(Number(value));
                      setCurrentPage(1);
                    }}
                  >
                    <SelectTrigger className="border-blue-200 w-16">
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
        </div>

        {/* Footer */}
        <div className="flex justify-end gap-3 border-t border-gray-200 bg-gray-50 p-6">
          <Button variant="outline" onClick={onClose}>
            Close
          </Button>
          {invoice.status !== "paid" && (
            <Button
              onClick={() => onPayment(invoice)}
              className="bg-green-600 hover:bg-green-700 flex items-center gap-2"
            >
              <DollarSign className="h-4 w-4" />
              Pay Remaining{" "}
              {((invoice.totalAmount - invoice.paidAmount) / 1_000_000).toFixed(
                1
              )}
              M VND
            </Button>
          )}
        </div>
      </div>
    </div>
  );
}
