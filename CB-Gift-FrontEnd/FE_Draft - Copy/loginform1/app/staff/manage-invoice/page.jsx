"use client";

import { useState } from "react";
import { Eye, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import StaffSidebar from "@/components/layout/staff/sidebar";
import StaffHeader from "@/components/layout/staff/header";
import SellerDetailModal from "@/components/invoices/seller-detail-modal";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

const mockSellers = [
  {
    id: "S001",
    name: "Kim Ma Trading Company",
    email: "sales@kimma.com",
    phone: "0978458754",
    address: "123 Pho Hue, Hanoi",
    totalDebt: 7_399_000,
    totalSales: 45_000_000,
  },
  {
    id: "S002",
    name: "Saigon Business Enterprise",
    email: "contact@saigon.vn",
    phone: "0965687458",
    address: "456 Nguyen Hue Street, HCMC",
    totalDebt: 4_955_000,
    totalSales: 38_500_000,
  },
  {
    id: "S003",
    name: "Hanoi Trading Ltd",
    email: "info@hanoi-trading.com",
    phone: "0945784578",
    address: "789 Ba Trieu Street, Hanoi",
    totalDebt: 5_316_000,
    totalSales: 52_000_000,
  },
  {
    id: "S004",
    name: "Pham Thu Huong Ltd",
    email: "pth@phamthihuong.com",
    phone: "0932546875",
    address: "321 Hung Vuong Street, Da Nang",
    totalDebt: 3_322_000,
    totalSales: 28_900_000,
  },
  {
    id: "S005",
    name: "Nguyen Van Hai Trading",
    email: "nvhai@trading.vn",
    phone: "0987854587",
    address: "654 Le Loi Street, Hai Phong",
    totalDebt: 2_262_000,
    totalSales: 35_200_000,
  },
];

export default function ManageInvoicePage() {
  const [expandedSeller, setExpandedSeller] = useState(null);
  const [selectedSeller, setSelectedSeller] = useState(null);
  const [showModal, setShowModal] = useState(false);
  const [itemsPerPageInModal, setItemsPerPageInModal] = useState(10);
  const [currentPageInModal, setCurrentPageInModal] = useState(1);
  const [sellersItemsPerPage, setSellersItemsPerPage] = useState(10);
  const [sellersCurrentPage, setSellersCurrentPage] = useState(1);

  const handleExpandSeller = (sellerId) => {
    if (expandedSeller === sellerId) {
      setExpandedSeller(null);
    } else {
      setExpandedSeller(sellerId);
    }
  };

  const handleViewDetail = (seller) => {
    setSelectedSeller(seller);
    setShowModal(true);
  };

  const handleSellersPageChange = (page) => {
    setSellersCurrentPage(Math.max(1, Math.min(page, totalSellersPages)));
  };

  const handleSellersItemsPerPageChange = (value) => {
    setSellersItemsPerPage(Number(value));
    setSellersCurrentPage(1);
  };

  const totalDebt = mockSellers.reduce(
    (sum, seller) => sum + seller.totalDebt,
    0
  );
  const totalSales = mockSellers.reduce(
    (sum, seller) => sum + seller.totalSales,
    0
  );

  const totalSellersPages = Math.ceil(mockSellers.length / sellersItemsPerPage);
  const startIdx = (sellersCurrentPage - 1) * sellersItemsPerPage;
  const paginatedSellers = mockSellers.slice(
    startIdx,
    startIdx + sellersItemsPerPage
  );

  return (
    <div className="flex h-screen bg-gray-50">
      <StaffSidebar />
      <div className="flex-1 flex flex-col overflow-hidden">
        <StaffHeader />
        <main className="flex-1 overflow-y-auto">
          <div className="p-6 space-y-6">
            {/* Header */}
            <div className="bg-white p-6 rounded-lg shadow-sm border-b-4 border-b-blue-500">
              <h1 className="text-3xl font-bold text-gray-900">
                Manage Seller Receivables
              </h1>
              <p className="mt-2 text-gray-600">
                Track and manage payment receivables and payment history from
                sellers
              </p>
            </div>

            {/* Summary Cards */}
            <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
              <div className="bg-white p-6 rounded-lg shadow-sm border-l-4 border-l-blue-500">
                <p className="text-sm text-gray-600">Total Sellers</p>
                <p className="mt-2 text-3xl font-bold text-blue-600">
                  {mockSellers.length}
                </p>
              </div>
              <div className="bg-white p-6 rounded-lg shadow-sm border-l-4 border-l-green-500">
                <p className="text-sm text-gray-600">Total Sales</p>
                <p className="mt-2 text-3xl font-bold text-green-600">
                  {(totalSales / 1_000_000).toFixed(1)}M
                </p>
              </div>
              <div className="bg-white p-6 rounded-lg shadow-sm border-l-4 border-l-red-500">
                <p className="text-sm text-gray-600">Total Receivables</p>
                <p className="mt-2 text-3xl font-bold text-red-600">
                  {(totalDebt / 1_000_000).toFixed(1)}M
                </p>
              </div>
            </div>

            {/* Sellers List */}
            <div className="bg-white rounded-lg shadow-sm overflow-hidden">
              <div className="bg-blue-50 px-6 py-4 border-b border-gray-200 flex items-center justify-between">
                <h2 className="text-lg font-semibold text-gray-900">
                  Seller List
                </h2>
                <Button className="bg-green-600 hover:bg-green-700">
                  <Plus className="mr-2 h-4 w-4" />
                  Add Seller
                </Button>
              </div>

              {/* Table Header */}
              <div className="grid grid-cols-12 gap-4 bg-blue-100 px-6 py-3 text-sm font-semibold text-gray-700">
                <div className="col-span-2">Seller ID</div>
                <div className="col-span-3">Seller Name</div>
                <div className="col-span-2">Phone</div>
                <div className="col-span-2">Total Sales</div>
                <div className="col-span-2">Receivables</div>
                <div className="col-span-1 text-center">Action</div>
              </div>

              {/* Sellers Rows */}
              <div className="divide-y">
                {paginatedSellers.map((seller) => (
                  <div
                    key={seller.id}
                    className="grid grid-cols-12 gap-4 px-6 py-4 hover:bg-gray-50 items-center"
                  >
                    <div className="col-span-2 font-semibold text-blue-600">
                      {seller.id}
                    </div>
                    <div className="col-span-3 text-gray-900 font-medium">
                      {seller.name}
                    </div>
                    <div className="col-span-2 text-gray-600">
                      {seller.phone}
                    </div>
                    <div className="col-span-2 text-right font-semibold text-green-600">
                      {(seller.totalSales / 1_000_000).toFixed(1)}M
                    </div>
                    <div className="col-span-2 text-right font-semibold text-red-600">
                      {(seller.totalDebt / 1_000_000).toFixed(1)}M
                    </div>
                    <div className="col-span-1 text-center">
                      <button
                        onClick={() => handleViewDetail(seller)}
                        className="p-2 hover:bg-blue-100 rounded-lg transition-colors text-blue-600 hover:text-blue-700 inline-flex"
                        title="View Details"
                      >
                        <Eye className="h-5 w-5" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>

              {/* Pagination Footer */}
              <div className="bg-blue-100 px-4 py-3 border-t border-blue-200">
                <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
                  <div className="text-sm text-gray-700 font-medium">
                    Showing {paginatedSellers.length > 0 ? startIdx + 1 : 0} to{" "}
                    {Math.min(
                      startIdx + sellersItemsPerPage,
                      mockSellers.length
                    )}{" "}
                    of {mockSellers.length} sellers
                  </div>

                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() =>
                        handleSellersPageChange(sellersCurrentPage - 1)
                      }
                      disabled={sellersCurrentPage === 1}
                      className="border-blue-200 hover:bg-blue-50 text-sm"
                    >
                      Previous
                    </Button>

                    <div className="flex items-center gap-1">
                      {Array.from(
                        { length: totalSellersPages },
                        (_, i) => i + 1
                      )
                        .filter(
                          (page) =>
                            page === 1 ||
                            page === totalSellersPages ||
                            (page >= sellersCurrentPage - 1 &&
                              page <= sellersCurrentPage + 1)
                        )
                        .map((page, idx, arr) => (
                          <div key={page}>
                            {idx > 0 && page > arr[idx - 1] + 1 && (
                              <span className="mx-1 text-gray-600 text-xs">
                                ...
                              </span>
                            )}
                            <Button
                              variant={
                                sellersCurrentPage === page
                                  ? "default"
                                  : "outline"
                              }
                              size="sm"
                              onClick={() => handleSellersPageChange(page)}
                              className={`min-w-8 h-8 p-0 text-sm ${
                                sellersCurrentPage === page
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
                      onClick={() =>
                        handleSellersPageChange(sellersCurrentPage + 1)
                      }
                      disabled={sellersCurrentPage === totalSellersPages}
                      className="border-blue-200 hover:bg-blue-50 text-sm"
                    >
                      Next
                    </Button>
                  </div>

                  <div className="flex items-center gap-2">
                    <span className="text-sm text-gray-700 font-medium">
                      Items Per Page
                    </span>
                    <Select
                      value={sellersItemsPerPage.toString()}
                      onValueChange={handleSellersItemsPerPageChange}
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
          </div>
        </main>
      </div>

      {/* Detail Modal */}
      {showModal && selectedSeller && (
        <SellerDetailModal
          seller={selectedSeller}
          isOpen={showModal}
          onClose={() => {
            setShowModal(false);
            setSelectedSeller(null);
          }}
          itemsPerPage={itemsPerPageInModal}
          currentPage={currentPageInModal}
          setItemsPerPage={setItemsPerPageInModal}
          setCurrentPage={setCurrentPageInModal}
        />
      )}
    </div>
  );
}
