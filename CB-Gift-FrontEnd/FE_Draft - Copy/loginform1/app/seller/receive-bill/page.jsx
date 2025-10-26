"use client";

import { useState, useEffect } from "react";
import SellerSidebar from "@/components/layout/seller/sidebar";
import SellerHeader from "@/components/layout/seller/header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  Search,
  Eye,
  AlertTriangle,
  Package,
  CheckCircle,
  Download,
  QrCode,
} from "lucide-react";
import { format } from "date-fns";

export default function ReceiveBillPage() {
  const [currentPage, setCurrentPage] = useState("receive-bill");
  const [invoices, setInvoices] = useState([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [isDetailsOpen, setIsDetailsOpen] = useState(false);
  const [selectedInvoice, setSelectedInvoice] = useState(null);

  // Mock data for invoices
  useEffect(() => {
    const mockInvoices = [
      {
        id: 1,
        invoiceNumber: "INV-001",
        orderCode: "ORD-001",
        customerName: "John Doe",
        amount: 1500,
        date: new Date("2024-01-15"),
        status: "Pending",
        image: "/business-invoice.png",
        qrCode: "/qr-code-payment.jpg",
        bankInfo: "Bank: ABC Bank | Account: 123456789 | Name: Your Company",
      },
      {
        id: 2,
        invoiceNumber: "INV-002",
        orderCode: "ORD-002",
        customerName: "Jane Smith",
        amount: 2500,
        date: new Date("2024-01-16"),
        status: "Pending",
        image: "/business-invoice.png",
        qrCode: "/qr-code-payment.jpg",
        bankInfo: "Bank: ABC Bank | Account: 123456789 | Name: Your Company",
      },
      {
        id: 3,
        invoiceNumber: "INV-003",
        orderCode: "ORD-003",
        customerName: "Bob Johnson",
        amount: 3200,
        date: new Date("2024-01-17"),
        status: "Paid",
        image: "/business-invoice.png",
        qrCode: "/qr-code-payment.jpg",
        bankInfo: "Bank: ABC Bank | Account: 123456789 | Name: Your Company",
      },
    ];

    setInvoices(mockInvoices);
    setLoading(false);
  }, []);

  const filteredInvoices = invoices.filter(
    (invoice) =>
      invoice.invoiceNumber.toLowerCase().includes(searchTerm.toLowerCase()) ||
      invoice.orderCode.toLowerCase().includes(searchTerm.toLowerCase()) ||
      invoice.customerName.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const handleViewDetails = (invoice) => {
    setSelectedInvoice(invoice);
    setIsDetailsOpen(true);
  };

  const handleDownload = (invoice) => {
    alert(`Downloading invoice: ${invoice.invoiceNumber}`);
  };

  const stats = [
    {
      title: "Total Invoices",
      value: invoices.length,
      color: "bg-blue-50 border-blue-200",
      icon: Package,
      iconColor: "text-blue-500",
    },
    {
      title: "Pending Payment",
      value: invoices.filter((inv) => inv.status === "Pending").length,
      color: "bg-yellow-50 border-yellow-200",
      icon: AlertTriangle,
      iconColor: "text-yellow-500",
    },
    {
      title: "Paid",
      value: invoices.filter((inv) => inv.status === "Paid").length,
      color: "bg-emerald-50 border-emerald-200",
      icon: CheckCircle,
      iconColor: "text-emerald-500",
    },
  ];

  return (
    <div className="flex h-screen overflow-hidden">
      <SellerSidebar
        currentPage={currentPage}
        setCurrentPage={setCurrentPage}
      />
      <div className="flex-1 flex flex-col overflow-hidden">
        <SellerHeader />
        <main className="flex-1 overflow-y-auto bg-gray-50 p-4 sm:p-6">
          <div className="space-y-6">
            {/* Welcome Header */}
            <div className="bg-white p-4 sm:p-6 rounded-lg border-2 border-blue-200 shadow-sm">
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <h1 className="text-lg sm:text-xl font-semibold text-blue-800">
                    Receive Bill
                  </h1>
                  <p className="text-sm sm:text-base text-blue-600 mt-1">
                    View and manage your invoices
                  </p>
                </div>
                <div className="mt-3 sm:mt-0">
                  <div className="flex items-center gap-2 text-sm text-blue-700">
                    <Package className="h-4 w-4" />
                    <span>{filteredInvoices.length} invoices</span>
                  </div>
                </div>
              </div>
            </div>

            {/* Stats Cards */}
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              {stats.map((stat, index) => {
                const IconComponent = stat.icon;
                return (
                  <div
                    key={index}
                    className={`p-4 rounded-lg border-2 ${stat.color} hover:shadow-lg transition-all`}
                  >
                    <div className="flex flex-col items-center text-center gap-2">
                      <IconComponent className={`h-5 w-5 ${stat.iconColor}`} />
                      <div>
                        <p className="text-lg font-bold text-gray-900">
                          {stat.value}
                        </p>
                        <h3 className="text-xs font-medium text-gray-600 uppercase tracking-wide">
                          {stat.title}
                        </h3>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Search */}
            <div className="bg-white p-4 sm:p-6 rounded-lg shadow-sm border border-gray-200">
              <div className="flex flex-col sm:flex-row gap-4">
                <div className="flex-1 max-w-xs">
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Search Invoices
                  </label>
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                    <Input
                      placeholder="Invoice #, Order #, Customer..."
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      className="pl-10 bg-white"
                    />
                  </div>
                </div>
              </div>
            </div>

            {/* Invoices Table */}
            <div className="bg-white rounded-lg shadow-sm border border-gray-200">
              {loading ? (
                <div className="flex items-center justify-center p-12">
                  <div className="text-center">
                    <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
                    <p className="mt-4 text-gray-600">Loading invoices...</p>
                  </div>
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <Table>
                    <TableHeader>
                      <TableRow className="bg-gray-50">
                        <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                          Invoice #
                        </TableHead>
                        <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                          Order #
                        </TableHead>
                        <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                          Customer
                        </TableHead>
                        <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                          Date
                        </TableHead>
                        <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                          Amount
                        </TableHead>
                        <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                          Status
                        </TableHead>
                        <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide whitespace-nowrap">
                          Actions
                        </TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {filteredInvoices.length === 0 ? (
                        <TableRow>
                          <TableCell
                            colSpan={7}
                            className="text-center py-8 text-gray-500"
                          >
                            No invoices found
                          </TableCell>
                        </TableRow>
                      ) : (
                        filteredInvoices.map((invoice) => (
                          <TableRow
                            key={invoice.id}
                            className="hover:bg-gray-50 transition-colors"
                          >
                            <TableCell className="font-medium text-gray-900">
                              {invoice.invoiceNumber}
                            </TableCell>
                            <TableCell className="text-gray-600">
                              {invoice.orderCode}
                            </TableCell>
                            <TableCell className="text-gray-600">
                              {invoice.customerName}
                            </TableCell>
                            <TableCell className="text-gray-600">
                              {format(invoice.date, "MMM dd, yyyy")}
                            </TableCell>
                            <TableCell className="text-gray-900 font-medium">
                              ${invoice.amount}
                            </TableCell>
                            <TableCell>
                              <span
                                className={`inline-flex items-center text-xs font-medium px-2.5 py-0.5 rounded-full ${
                                  invoice.status === "Paid"
                                    ? "bg-green-100 text-green-800"
                                    : "bg-yellow-100 text-yellow-800"
                                }`}
                              >
                                {invoice.status}
                              </span>
                            </TableCell>
                            <TableCell>
                              <div className="flex items-center gap-2">
                                <Button
                                  variant="outline"
                                  size="sm"
                                  onClick={() => handleViewDetails(invoice)}
                                  className="bg-transparent hover:bg-gray-50"
                                  title="View Details"
                                >
                                  <Eye className="h-4 w-4" />
                                </Button>
                                <Button
                                  variant="outline"
                                  size="sm"
                                  onClick={() => handleDownload(invoice)}
                                  className="bg-transparent hover:bg-blue-50 text-blue-600"
                                  title="Download"
                                >
                                  <Download className="h-4 w-4" />
                                </Button>
                              </div>
                            </TableCell>
                          </TableRow>
                        ))
                      )}
                    </TableBody>
                  </Table>
                </div>
              )}
            </div>
          </div>
        </main>
      </div>

      {/* View Details Modal */}
      <Dialog open={isDetailsOpen} onOpenChange={setIsDetailsOpen}>
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Package className="h-5 w-5" />
              Invoice Details - {selectedInvoice?.invoiceNumber}
            </DialogTitle>
          </DialogHeader>
          {selectedInvoice && (
            <div className="space-y-6">
              {/* Invoice Header */}
              <div className="bg-gray-50 p-4 rounded-lg">
                <h3 className="font-semibold text-lg mb-3 text-gray-900">
                  Invoice Information
                </h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                  <div>
                    <label className="text-sm text-gray-500 font-medium">
                      Invoice Number
                    </label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedInvoice.invoiceNumber}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm text-gray-500 font-medium">
                      Order Code
                    </label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedInvoice.orderCode}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm text-gray-500 font-medium">
                      Date
                    </label>
                    <p className="font-medium text-gray-900 mt-1">
                      {format(selectedInvoice.date, "MMM dd, yyyy")}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm text-gray-500 font-medium">
                      Status
                    </label>
                    <p
                      className={`font-medium mt-1 ${
                        selectedInvoice.status === "Paid"
                          ? "text-green-600"
                          : "text-yellow-600"
                      }`}
                    >
                      {selectedInvoice.status}
                    </p>
                  </div>
                </div>
              </div>

              {/* Customer Information */}
              <div className="bg-gray-50 p-4 rounded-lg">
                <h3 className="font-semibold text-lg mb-3 text-gray-900">
                  Customer Information
                </h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className="text-sm text-gray-500 font-medium">
                      Customer Name
                    </label>
                    <p className="font-medium text-gray-900 mt-1">
                      {selectedInvoice.customerName}
                    </p>
                  </div>
                  <div>
                    <label className="text-sm text-gray-500 font-medium">
                      Amount
                    </label>
                    <p className="font-bold text-blue-600 text-lg mt-1">
                      ${selectedInvoice.amount}
                    </p>
                  </div>
                </div>
              </div>

              {/* Invoice Image */}
              <div className="bg-gray-50 p-4 rounded-lg">
                <h3 className="font-semibold text-lg mb-3 text-gray-900">
                  Invoice Document
                </h3>
                <div className="border border-gray-200 rounded-lg p-4 bg-white">
                  <img
                    src={selectedInvoice.image || "/placeholder.svg"}
                    alt="Invoice"
                    className="w-full h-auto rounded border"
                    onError={(e) => {
                      e.target.style.display = "none";
                      e.target.nextElementSibling.style.display = "flex";
                    }}
                  />
                  <div
                    className="w-full h-64 bg-gray-100 rounded border flex items-center justify-center"
                    style={{ display: "none" }}
                  >
                    <AlertTriangle className="h-12 w-12 text-gray-400" />
                  </div>
                </div>
              </div>

              {/* Payment QR Code */}
              <div className="bg-gray-50 p-4 rounded-lg">
                <h3 className="font-semibold text-lg mb-3 text-gray-900">
                  Payment Information
                </h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {/* QR Code */}
                  <div className="border border-gray-200 rounded-lg p-4 bg-white flex flex-col items-center">
                    <label className="text-sm text-gray-500 font-medium mb-3">
                      Payment QR Code
                    </label>
                    <img
                      src={selectedInvoice.qrCode || "/placeholder.svg"}
                      alt="Payment QR Code"
                      className="w-40 h-40 rounded border"
                      onError={(e) => {
                        e.target.style.display = "none";
                        e.target.nextElementSibling.style.display = "flex";
                      }}
                    />
                    <div
                      className="w-40 h-40 bg-gray-100 rounded border flex items-center justify-center"
                      style={{ display: "none" }}
                    >
                      <QrCode className="h-12 w-12 text-gray-400" />
                    </div>
                    <p className="text-xs text-gray-600 mt-3 text-center">
                      Scan to pay
                    </p>
                  </div>

                  {/* Bank Information */}
                  <div className="border border-gray-200 rounded-lg p-4 bg-white">
                    <label className="text-sm text-gray-500 font-medium">
                      Bank Details
                    </label>
                    <div className="mt-3 space-y-2">
                      {selectedInvoice.bankInfo
                        .split("|")
                        .map((info, index) => (
                          <p key={index} className="text-sm text-gray-700">
                            {info.trim()}
                          </p>
                        ))}
                    </div>
                    <div className="mt-4 p-3 bg-blue-50 border border-blue-200 rounded">
                      <p className="text-xs text-blue-700">
                        <strong>Note:</strong> Please include the invoice number
                        in the payment reference.
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}
          <DialogFooter className="flex gap-2">
            <Button variant="outline" onClick={() => setIsDetailsOpen(false)}>
              Close
            </Button>
            <Button
              onClick={() => handleDownload(selectedInvoice)}
              className="bg-blue-600 hover:bg-blue-700 text-white"
            >
              <Download className="h-4 w-4 mr-2" />
              Download Invoice
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
