"use client";

const transactionHistory = [
  {
    id: "TH-001",
    month: "January 2025",
    date: "15/01/2025",
    type: "Payment",
    paymentMethod: "Full Payment",
    amount: 8_500_000,
    remaining: 0,
    status: "paid",
  },
  {
    id: "TH-002",
    month: "December 2024",
    date: "20/12/2024",
    type: "Partial Payment",
    paymentMethod: "50%",
    amount: 3_600_000,
    remaining: 3_600_000,
    status: "partial",
  },
  {
    id: "TH-003",
    month: "November 2024",
    date: "15/11/2024",
    type: "Partial Payment",
    paymentMethod: "30%",
    amount: 2_040_000,
    remaining: 4_760_000,
    status: "partial",
  },
  {
    id: "TH-004",
    month: "October 2024",
    date: "05/10/2024",
    type: "Payment",
    paymentMethod: "Full Payment",
    amount: 5_200_000,
    remaining: 0,
    status: "paid",
  },
  {
    id: "TH-005",
    month: "September 2024",
    date: "28/09/2024",
    type: "Partial Payment",
    paymentMethod: "20%",
    amount: 920_000,
    remaining: 3_680_000,
    status: "partial",
  },
];

// [XÓA] mock 'transactionHistory'

const SellerTransactionTab = ({ seller, onActionDone }) => {
  // [THÊM] State cho API
  const [transactions, setTransactions] = useState([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage] = useState(10); // Có thể thêm Select để đổi

  const totalPages = Math.ceil(total / itemsPerPage);

  // [THÊM] useEffect để tải lịch sử thanh toán hhhhhhhhhh
  useEffect(() => {
    if (!seller?.id) return;

    const fetchTransactions = async () => {
      setLoading(true);
      setError(null);
      const params = new URLSearchParams({
        page: currentPage,
        pageSize: itemsPerPage,
      });

      try {
        const response = await fetch(
          `https://localhost:7015/api/invoices/seller-payments/${seller.id}?${params.toString()}`,
          { credentials: "include" }
        );
        if (!response.ok) throw new Error("Failed to fetch payment history.");
        const data = await response.json();
        setTransactions(data.items || []);
        setTotal(data.total || 0);
      } catch (e) {
        setError(e.message);
      } finally {
        setLoading(false);
      }
    };

    fetchTransactions();
  }, [seller, currentPage, itemsPerPage, onActionDone]); // [SỬA] Tải lại khi onActionDone thay đổi

  // Hàm format tiền tệ
  const formatCurrency = (value) => {
    if (value === 0) return "0";
    if (!value) return "-";
    if (value < 1_000_000) {
      return new Intl.NumberFormat("vi-VN").format(value);
    }
    return (value / 1_000_000).toFixed(1) + "M";
  };
  
  // [SỬA] Cập nhật các thẻ tóm tắt (Summary Cards)
  // GHI CHÚ: Các thẻ này nên được tính toán từ API riêng,
  // vì dữ liệu phân trang không đại diện cho toàn bộ lịch sử.
  // Tạm thời ẩn đi để tránh hiển thị sai.
  
  return (
    <div className="space-y-4">
      <div className="mb-4">
        <h3 className="text-lg font-semibold text-gray-900 mb-2">
          Payment History
        </h3>
        <p className="text-sm text-gray-600">
          View payment transaction history by month
        </p>
      </div>

      <div className="overflow-x-auto rounded-lg border border-gray-200">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-blue-100">
              <th className="px-4 py-3 text-left font-semibold text-gray-700">
                Transaction ID
              </th>
              <th className="px-4 py-3 text-left font-semibold text-gray-700">
                Month
              </th>
              <th className="px-4 py-3 text-left font-semibold text-gray-700">
                Date
              </th>
              <th className="px-4 py-3 text-left font-semibold text-gray-700">
                Type
              </th>
              <th className="px-4 py-3 text-left font-semibold text-gray-700">
                Payment Method
              </th>
              <th className="px-4 py-3 text-right font-semibold text-gray-700">
                Amount Paid
              </th>
              <th className="px-4 py-3 text-right font-semibold text-gray-700">
                Remaining
              </th>
            </tr>
          </thead>
          <tbody>
            {transactionHistory.map((transaction) => (
              <tr
                key={transaction.id}
                className={`border-t border-gray-200 ${
                  transaction.status === "paid"
                    ? "bg-green-50 hover:bg-green-100"
                    : "bg-yellow-50 hover:bg-yellow-100"
                }`}
              >
                <td className="px-4 py-3 font-semibold text-blue-600">
                  {transaction.id}
                </td>
                <td className="px-4 py-3 text-gray-700">{transaction.month}</td>
                <td className="px-4 py-3 text-gray-600">{transaction.date}</td>
                <td className="px-4 py-3 text-gray-700">{transaction.type}</td>
                <td className="px-4 py-3">
                  <span
                    className={`px-2 py-1 rounded text-xs font-semibold ${
                      transaction.status === "paid"
                        ? "bg-green-200 text-green-800"
                        : "bg-yellow-200 text-yellow-800"
                    }`}
                  >
                    {transaction.paymentMethod}
                  </span>
                </td>
                <td className="px-4 py-3 text-right font-semibold text-green-600">
                  {(transaction.amount / 1_000_000).toFixed(1)}M
                </td>
                <td className="px-4 py-3 text-right font-semibold text-red-600">
                  {(transaction.remaining / 1_000_000).toFixed(1)}M
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-3 gap-4 mt-6">
        <div className="bg-green-50 p-4 rounded-lg border border-green-200">
          <p className="text-xs text-gray-600 mb-1 font-semibold">Fully Paid</p>
          <p className="text-lg font-bold text-green-600">2 months</p>
        </div>
        <div className="bg-yellow-50 p-4 rounded-lg border border-yellow-200">
          <p className="text-xs text-gray-600 mb-1 font-semibold">
            Partial Payment
          </p>
          <p className="text-lg font-bold text-yellow-600">3 months</p>
        </div>
        <div className="bg-blue-50 p-4 rounded-lg border border-blue-200">
          <p className="text-xs text-gray-600 mb-1 font-semibold">
            Total Current Debt
          </p>
          <p className="text-lg font-bold text-blue-600">12.04M</p>
        </div>
      </div>
    </div>
  );
};

export default SellerTransactionTab;
