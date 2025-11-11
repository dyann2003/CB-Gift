"use client";

import { useState } from "react";
import { ChevronDown, ChevronUp, Send } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

const months = [
  {
    id: 1,
    name: "January 2025",
    status: "paid",
    paid: true,
    totalAmount: 8_500_000,
  },
  {
    id: 2,
    name: "December 2024",
    status: "partial",
    paid: false,
    totalAmount: 7_200_000,
  },
  {
    id: 3,
    name: "November 2024",
    status: "unpaid",
    paid: false,
    totalAmount: 6_800_000,
  },
];

const generateOrders = (monthId, sellerId) => {
  const orders = [];
  for (let i = 1; i <= 10; i++) {
    const date = new Date(2025, 0, 15 + i);
    orders.push({
      id: `ORD-${sellerId}-${monthId}-${String(i).padStart(3, "0")}`,
      date: date.toLocaleDateString("en-US"),
      customerName: `Customer ${i}`,
      amount: 500_000 + Math.random() * 1_000_000,
      status: Math.random() > 0.5 ? "delivered" : "processing",
    });
  }
  return orders;
};

const SellerOrdersTab = ({ seller }) => {
  const [expandedMonth, setExpandedMonth] = useState(null);
  const [currentPage, setCurrentPage] = useState({});
  const [itemsPerPage, setItemsPerPage] = useState({});

  const handleExpandMonth = (monthId) => {
    setExpandedMonth(expandedMonth === monthId ? null : monthId);
    setCurrentPage((prev) => ({ ...prev, [monthId]: 1 }));
    setItemsPerPage((prev) => ({ ...prev, [monthId]: 5 }));
  };

  const handleCreateReceipt = (month) => {
    alert(
      `Create payment receipt for ${month.name} - Total: ${(
        month.totalAmount / 1_000_000
      ).toFixed(1)}M`
    );
  };

  return (
    <div className="space-y-4">
      <div className="mb-4">
        <h3 className="text-lg font-semibold text-gray-900 mb-2">
          Sales History by Month
        </h3>
        <p className="text-sm text-gray-600">
          View order details and create payment receipts
        </p>
      </div>

      {months.map((month) => {
        const allOrders = generateOrders(month.id, seller.id);
        const perPage = itemsPerPage[month.id] || 5;
        const pageNum = currentPage[month.id] || 1;
        const totalPages = Math.ceil(allOrders.length / perPage);
        const startIdx = (pageNum - 1) * perPage;
        const endIdx = startIdx + perPage;
        const paginatedOrders = allOrders.slice(startIdx, endIdx);

        return (
          <div
            key={month.id}
            className="border border-gray-200 rounded-lg overflow-hidden"
          >
            {/* Month Header */}
            <div
              className={`p-4 flex items-center justify-between cursor-pointer transition-colors ${
                month.status === "paid"
                  ? "bg-green-50 hover:bg-green-100"
                  : month.status === "partial"
                  ? "bg-yellow-50 hover:bg-yellow-100"
                  : "bg-red-50 hover:bg-red-100"
              }`}
              onClick={() => handleExpandMonth(month.id)}
            >
              <div className="flex items-center gap-4 flex-1">
                {expandedMonth === month.id ? (
                  <ChevronUp className="h-5 w-5" />
                ) : (
                  <ChevronDown className="h-5 w-5" />
                )}
                <div>
                  <h4 className="font-semibold text-gray-900">{month.name}</h4>
                  <p className="text-sm text-gray-600">
                    Total: {(month.totalAmount / 1_000_000).toFixed(1)}M
                  </p>
                </div>
              </div>
              <span
                className={`px-3 py-1 rounded-full text-sm font-semibold ${
                  month.status === "paid"
                    ? "bg-green-200 text-green-800"
                    : month.status === "partial"
                    ? "bg-yellow-200 text-yellow-800"
                    : "bg-red-200 text-red-800"
                }`}
              >
                {month.status === "paid"
                  ? "Paid"
                  : month.status === "partial"
                  ? "Partial"
                  : "Unpaid"}
              </span>
            </div>

            {/* Month Details */}
            {expandedMonth === month.id && (
              <div className="bg-gray-50 p-4 border-t border-gray-200">
                {/* Items Per Page Selector */}
                <div className="mb-4 flex items-center gap-2">
                  <span className="text-sm text-gray-600">Items per page:</span>
                  <Select
                    value={(itemsPerPage[month.id] || 5).toString()}
                    onValueChange={(value) =>
                      setItemsPerPage((prev) => ({
                        ...prev,
                        [month.id]: Number(value),
                      }))
                    }
                  >
                    <SelectTrigger className="w-[70px] bg-white border-gray-300">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="5">5</SelectItem>
                      <SelectItem value="10">10</SelectItem>
                      <SelectItem value="15">15</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                {/* Orders Table */}
                <div className="mb-4 overflow-x-auto rounded-lg border border-gray-200">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="bg-blue-100">
                        <th className="px-4 py-2 text-left font-semibold text-gray-700">
                          Order ID
                        </th>
                        <th className="px-4 py-2 text-left font-semibold text-gray-700">
                          Date
                        </th>
                        <th className="px-4 py-2 text-left font-semibold text-gray-700">
                          Customer
                        </th>
                        <th className="px-4 py-2 text-right font-semibold text-gray-700">
                          Amount
                        </th>
                        <th className="px-4 py-2 text-left font-semibold text-gray-700">
                          Status
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {paginatedOrders.map((order) => (
                        <tr
                          key={order.id}
                          className="border-t border-gray-200 hover:bg-white"
                        >
                          <td className="px-4 py-3 font-semibold text-blue-600">
                            {order.id}
                          </td>
                          <td className="px-4 py-3 text-gray-600">
                            {order.date}
                          </td>
                          <td className="px-4 py-3 text-gray-900">
                            {order.customerName}
                          </td>
                          <td className="px-4 py-3 text-right font-semibold text-gray-900">
                            {(order.amount / 1000).toFixed(0)}k
                          </td>
                          <td className="px-4 py-3">
                            <span
                              className={`px-2 py-1 rounded text-xs font-semibold ${
                                order.status === "delivered"
                                  ? "bg-green-100 text-green-800"
                                  : "bg-orange-100 text-orange-800"
                              }`}
                            >
                              {order.status === "delivered"
                                ? "Delivered"
                                : "Processing"}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Pagination Controls */}
                <div className="flex items-center justify-between mb-4">
                  <div className="text-sm text-gray-600">
                    Showing {startIdx + 1} -{" "}
                    {Math.min(endIdx, allOrders.length)} of {allOrders.length}{" "}
                    orders
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() =>
                        setCurrentPage((prev) => ({
                          ...prev,
                          [month.id]: Math.max(1, (prev[month.id] || 1) - 1),
                        }))
                      }
                      disabled={currentPage[month.id] === 1}
                    >
                      Previous
                    </Button>
                    {Array.from({ length: totalPages }, (_, i) => i + 1).map(
                      (page) => (
                        <Button
                          key={page}
                          variant={
                            currentPage[month.id] === page
                              ? "default"
                              : "outline"
                          }
                          size="sm"
                          onClick={() =>
                            setCurrentPage((prev) => ({
                              ...prev,
                              [month.id]: page,
                            }))
                          }
                          className={
                            currentPage[month.id] === page
                              ? "bg-green-600 hover:bg-green-700"
                              : ""
                          }
                        >
                          {page}
                        </Button>
                      )
                    )}
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() =>
                        setCurrentPage((prev) => ({
                          ...prev,
                          [month.id]: Math.min(
                            totalPages,
                            (prev[month.id] || 1) + 1
                          ),
                        }))
                      }
                      disabled={currentPage[month.id] === totalPages}
                    >
                      Next
                    </Button>
                  </div>
                </div>

                {/* Action Button */}
                <div className="flex justify-end">
                  <Button
                    onClick={() => handleCreateReceipt(month)}
                    className="bg-green-600 hover:bg-green-700"
                  >
                    <Send className="h-4 w-4 mr-2" />
                    Create Monthly Receipt
                  </Button>
                </div>
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
};

export default SellerOrdersTab;
