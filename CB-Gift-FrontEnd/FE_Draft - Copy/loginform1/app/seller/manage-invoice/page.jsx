"use client";

import { useState, useEffect } from "react";
import { CheckCircle, Clock, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import SellerHeader from "@/components/layout/seller/header";
import SellerSidebar from "@/components/layout/seller/sidebar";
import SellerInvoiceList from "@/components/invoices/seller-invoice-list";
import SellerInvoiceDetailsModal from "@/components/invoices/seller-invoice-details-modal";

const mockInvoices = [
  {
    id: "REC-2025-01-001",
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
    id: "REC-2025-02-001",
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
    id: "REC-2025-03-001",
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
    id: "REC-2025-04-001",
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
    id: "REC-2025-05-001",
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

const PaymentMethodSelector = ({ invoice, onConfirm, onClose }) => {
  const [selectedMethod, setSelectedMethod] = useState(null);
  const [showQR, setShowQR] = useState(false);

  const isPartial = invoice.status === "partial";

  useEffect(() => {
    if (isPartial) {
      setSelectedMethod("full");
    }
  }, [isPartial]);

  const paymentMethods = [
    { key: "full", label: "Full Payment", percentage: 100 },
    { key: "20", label: "20%", percentage: 20 },
    { key: "30", label: "30%", percentage: 30 },
    { key: "50", label: "50%", percentage: 50 },
  ];

  const calculateAmount = () => {
    if (!selectedMethod) return 0;
    const method = paymentMethods.find((m) => m.key === selectedMethod);
    if (isPartial) {
      return invoice.totalAmount - invoice.paidAmount;
    }
    return (invoice.totalAmount * method.percentage) / 100;
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4 p-6">
        {!showQR ? (
          <>
            <h3 className="text-xl font-bold text-gray-900 mb-4">
              {isPartial ? "Pay Remaining Amount" : "Select Payment Method"}
            </h3>
            <p className="text-sm text-gray-600 mb-4">Receipt: {invoice.id}</p>

            {!isPartial && (
              <div className="space-y-2 mb-6">
                {paymentMethods.map((method) => (
                  <button
                    key={method.key}
                    onClick={() => setSelectedMethod(method.key)}
                    className={`w-full p-3 border-2 rounded-lg text-left transition-all ${
                      selectedMethod === method.key
                        ? "border-green-500 bg-green-50"
                        : "border-gray-200 bg-white hover:border-green-300"
                    }`}
                  >
                    <div className="font-semibold text-gray-900">
                      {method.label}
                    </div>
                    <div className="text-sm text-gray-600">
                      {(
                        (invoice.totalAmount * method.percentage) /
                        100 /
                        1_000_000
                      ).toFixed(1)}
                      M VND
                    </div>
                  </button>
                ))}
              </div>
            )}

            <div className="border-t pt-4 mb-4">
              <div className="flex justify-between mb-2">
                <span className="text-gray-600">Total Amount:</span>
                <span className="font-semibold">
                  {(invoice.totalAmount / 1_000_000).toFixed(1)}M VND
                </span>
              </div>
              {!isPartial && (
                <div className="flex justify-between mb-2">
                  <span className="text-gray-600">Paid Amount:</span>
                  <span className="font-semibold">
                    {(invoice.paidAmount / 1_000_000).toFixed(1)}M VND
                  </span>
                </div>
              )}
              <div className="flex justify-between mb-2">
                <span className="text-gray-600">
                  {isPartial ? "Remaining to Pay:" : "Payment Amount:"}
                </span>
                <span className="font-bold text-green-600 text-lg">
                  {(calculateAmount() / 1_000_000).toFixed(1)}M VND
                </span>
              </div>
            </div>

            <div className="flex gap-2">
              <Button
                variant="outline"
                onClick={onClose}
                className="flex-1 bg-transparent"
              >
                Cancel
              </Button>
              <Button
                onClick={() => setShowQR(true)}
                disabled={!selectedMethod}
                className="flex-1 bg-green-600 hover:bg-green-700"
              >
                Continue
              </Button>
            </div>
          </>
        ) : (
          <>
            <h3 className="text-xl font-bold text-gray-900 mb-4">
              Scan QR Code to Pay
            </h3>

            <div className="bg-gray-100 rounded-lg p-6 mb-4 flex items-center justify-center h-64">
              <div className="w-48 h-48 bg-white border-4 border-gray-300 rounded-lg flex items-center justify-center">
                <svg viewBox="0 0 200 200" className="w-full h-full">
                  <rect width="200" height="200" fill="white" />
                  <g fill="black">
                    <rect x="10" y="10" width="40" height="40" />
                    <rect x="15" y="15" width="30" height="30" fill="white" />
                    <rect x="20" y="20" width="20" height="20" />

                    <rect x="150" y="10" width="40" height="40" />
                    <rect x="155" y="15" width="30" height="30" fill="white" />
                    <rect x="160" y="20" width="20" height="20" />

                    <rect x="10" y="150" width="40" height="40" />
                    <rect x="15" y="155" width="30" height="30" fill="white" />
                    <rect x="20" y="160" width="20" height="20" />
                  </g>
                  {Array.from({ length: 12 }).map((_, i) =>
                    Array.from({ length: 12 }).map((_, j) => (
                      <rect
                        key={`${i}-${j}`}
                        x={60 + i * 10}
                        y={60 + j * 10}
                        width="8"
                        height="8"
                        fill={Math.random() > 0.5 ? "black" : "white"}
                      />
                    ))
                  )}
                </svg>
              </div>
            </div>

            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-4">
              <p className="text-sm text-blue-800">
                <span className="font-semibold">Amount:</span>{" "}
                {(calculateAmount() / 1_000_000).toFixed(1)}M VND
              </p>
              <p className="text-sm text-blue-800">
                <span className="font-semibold">Method:</span>{" "}
                {isPartial
                  ? "Full Payment"
                  : paymentMethods.find((m) => m.key === selectedMethod)?.label}
              </p>
            </div>

            <div className="flex gap-2">
              <Button
                variant="outline"
                onClick={() => setShowQR(false)}
                className="flex-1"
              >
                Back
              </Button>
              <Button
                onClick={() => {
                  alert(
                    `Payment of ${(calculateAmount() / 1_000_000).toFixed(
                      1
                    )}M VND completed successfully!`
                  );
                  onClose();
                }}
                className="flex-1 bg-green-600 hover:bg-green-700"
              >
                Confirm Payment
              </Button>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default function SellerManageInvoice() {
  const [selectedInvoice, setSelectedInvoice] = useState(null);
  const [showDetailsModal, setShowDetailsModal] = useState(false);
  const [showPaymentModal, setShowPaymentModal] = useState(false);

  const paidInvoices = mockInvoices.filter(
    (inv) => inv.status === "paid"
  ).length;
  const partialInvoices = mockInvoices.filter(
    (inv) => inv.status === "partial"
  ).length;
  const pendingInvoices = mockInvoices.filter(
    (inv) => inv.status === "pending"
  ).length;
  const totalAmount = mockInvoices.reduce(
    (sum, inv) => sum + inv.totalAmount,
    0
  );
  const totalPaid = mockInvoices.reduce((sum, inv) => sum + inv.paidAmount, 0);
  const totalRemaining = totalAmount - totalPaid;

  const handleViewDetails = (invoice) => {
    setSelectedInvoice(invoice);
    setShowDetailsModal(true);
  };

  const handlePaymentFromDetails = (invoice) => {
    setShowDetailsModal(false);
    setSelectedInvoice(invoice);
    if (invoice.status === "pending") {
      setShowPaymentModal(true);
    } else if (invoice.status === "partial") {
      setShowPaymentModal(true);
    }
  };

  return (
    <div className="flex h-screen bg-gray-50">
      <SellerSidebar />
      <div className="flex-1 flex flex-col overflow-hidden">
        <SellerHeader />
        <main className="flex-1 overflow-y-auto p-6">
          <div className="max-w-6xl mx-auto space-y-6">
            <div className="bg-white p-6 rounded-lg shadow-sm border-b-4 border-b-blue-500">
              <h1 className="text-3xl font-bold text-gray-900 mb-2">
                Manage Payment Receipts
              </h1>
              <p className="text-gray-600">
                View and pay payment receipts from staff
              </p>
            </div>

            <div className="grid grid-cols-4 gap-4">
              <div className="bg-white rounded-lg p-4 border border-gray-200 shadow-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-600 mb-1">Total Receipts</p>
                    <p className="text-2xl font-bold text-gray-900">
                      {mockInvoices.length}
                    </p>
                  </div>
                  <AlertCircle className="h-8 w-8 text-gray-400" />
                </div>
              </div>

              <div className="bg-white rounded-lg p-4 border border-gray-200 shadow-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-600 mb-1">Paid</p>
                    <p className="text-2xl font-bold text-green-600">
                      {paidInvoices}
                    </p>
                  </div>
                  <CheckCircle className="h-8 w-8 text-green-500" />
                </div>
              </div>

              <div className="bg-white rounded-lg p-4 border border-gray-200 shadow-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-600 mb-1">Partial</p>
                    <p className="text-2xl font-bold text-yellow-600">
                      {partialInvoices}
                    </p>
                  </div>
                  <Clock className="h-8 w-8 text-yellow-500" />
                </div>
              </div>

              <div className="bg-white rounded-lg p-4 border border-gray-200 shadow-sm">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-gray-600 mb-1">Pending</p>
                    <p className="text-2xl font-bold text-red-600">
                      {pendingInvoices}
                    </p>
                  </div>
                  <AlertCircle className="h-8 w-8 text-red-500" />
                </div>
              </div>
            </div>

            <div className="grid grid-cols-3 gap-4">
              <div className="bg-blue-50 rounded-lg p-4 border border-blue-200">
                <p className="text-sm text-blue-600 mb-1">Total Debt</p>
                <p className="text-2xl font-bold text-blue-900">
                  {(totalAmount / 1_000_000).toFixed(1)}M VND
                </p>
              </div>
              <div className="bg-green-50 rounded-lg p-4 border border-green-200">
                <p className="text-sm text-green-600 mb-1">Amount Paid</p>
                <p className="text-2xl font-bold text-green-900">
                  {(totalPaid / 1_000_000).toFixed(1)}M VND
                </p>
              </div>
              <div className="bg-orange-50 rounded-lg p-4 border border-orange-200">
                <p className="text-sm text-orange-600 mb-1">Remaining Due</p>
                <p className="text-2xl font-bold text-orange-900">
                  {(totalRemaining / 1_000_000).toFixed(1)}M VND
                </p>
              </div>
            </div>

            <SellerInvoiceList onPayment={handleViewDetails} />
          </div>
        </main>
      </div>

      {showDetailsModal && selectedInvoice && (
        <SellerInvoiceDetailsModal
          invoice={selectedInvoice}
          isOpen={showDetailsModal}
          onClose={() => {
            setShowDetailsModal(false);
            setSelectedInvoice(null);
          }}
          onPayment={handlePaymentFromDetails}
        />
      )}

      {showPaymentModal && selectedInvoice && (
        <PaymentMethodSelector
          invoice={selectedInvoice}
          onConfirm={() => {
            setSelectedInvoice(null);
            setShowPaymentModal(false);
          }}
          onClose={() => {
            setSelectedInvoice(null);
            setShowPaymentModal(false);
            setShowDetailsModal(true);
          }}
        />
      )}
    </div>
  );
}
