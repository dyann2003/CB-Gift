"use client";

import { useState } from "react";
import RoleSidebar from "@/components/layout/shared/role-sidebar";
import ManagerSidebar from "@/components/layout/manager/sidebar";
import ManagerHeader from "@/components/layout/manager/header";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
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
  DialogTrigger,
} from "@/components/ui/dialog";
import { Calendar } from "@/components/ui/calendar";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import {
  Eye,
  Search,
  CalendarIcon,
  Download,
  QrCode,
  Package,
} from "lucide-react";
import { format } from "date-fns";

export default function ManagerOrderPage() {
  const [currentPage, setCurrentPage] = useState("manage-order");
  const [searchTerm, setSearchTerm] = useState("");
  const [sellerFilter, setSellerFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");
  const [dateFilter, setDateFilter] = useState(null);
  const [selectedOrder, setSelectedOrder] = useState(null);

  const orders = [
    {
      id: "ORD-001",
      customerName: "John Doe",
      sellerName: "Alice Johnson",
      products: [
        {
          name: "Acrylic Keychain",
          quantity: 50,
          price: 125.0,
          size: "5x5cm",
          accessory: "Key Ring",
        },
      ],
      status: "In Production",
      orderDate: "2024-01-15",
      totalAmount: "$125.00",
      customerInfo: {
        name: "John Doe",
        phone: "+1234567890",
        email: "john@example.com",
        address: "123 Main St",
        city: "New York",
        state: "NY",
        zipcode: "10001",
        country: "USA",
      },
      orderNotes: "Please make it colorful and vibrant",
      uploadedFiles: {
        linkImg: { name: "design-reference.jpg", url: "/acrylic-keychain.jpg" },
        linkThanksCard: { name: "thanks-card.pdf", url: "#" },
        linkFileDesign: { name: "original-design.ai", url: "#" },
      },
    },
    {
      id: "ORD-002",
      customerName: "Jane Smith",
      sellerName: "Bob Wilson",
      products: [
        {
          name: "Acrylic Stand",
          quantity: 25,
          price: 89.5,
          size: "10x15cm",
          accessory: "None",
        },
      ],
      status: "QA Completed",
      orderDate: "2024-01-16",
      totalAmount: "$89.50",
      customerInfo: {
        name: "Jane Smith",
        phone: "+1234567891",
        email: "jane@example.com",
        address: "456 Oak Ave",
        city: "Los Angeles",
        state: "CA",
        zipcode: "90001",
        country: "USA",
      },
      orderNotes: "Minimalist design preferred",
      uploadedFiles: {
        linkImg: { name: "stand-reference.png", url: "/acrylic-stand.jpg" },
        linkThanksCard: { name: "thank-you-card.jpg", url: "#" },
        linkFileDesign: { name: "base-design.psd", url: "#" },
      },
    },
    {
      id: "ORD-003",
      customerName: "Mike Johnson",
      sellerName: "Carol Davis",
      products: [
        {
          name: "Acrylic Charm",
          quantity: 50,
          price: 100.0,
          size: "3x3cm",
          accessory: "Phone Strap",
        },
        {
          name: "Custom Keychain",
          quantity: 30,
          price: 75.0,
          size: "4x4cm",
          accessory: "Key Ring",
        },
        {
          name: "Phone Stand",
          quantity: 20,
          price: 25.0,
          size: "8x10cm",
          accessory: "None",
        },
      ],
      status: "Design Phase",
      orderDate: "2024-01-17",
      totalAmount: "$200.00",
      customerInfo: {
        name: "Mike Johnson",
        phone: "+1234567892",
        email: "mike@example.com",
        address: "789 Pine St",
        city: "Chicago",
        state: "IL",
        zipcode: "60601",
        country: "USA",
      },
      orderNotes: "Cute anime style design for all items",
      uploadedFiles: {
        linkImg: { name: "charm-reference.jpg", url: "/acrylic-charm.jpg" },
        linkThanksCard: { name: "thanks-note.pdf", url: "#" },
        linkFileDesign: { name: "charm-design.ai", url: "#" },
      },
    },
    {
      id: "ORD-004",
      customerName: "Sarah Wilson",
      sellerName: "Alice Johnson",
      products: [
        {
          name: "Custom Keychain Set",
          quantity: 100,
          price: 250.0,
          size: "5x5cm",
          accessory: "Key Ring",
        },
        {
          name: "Acrylic Display",
          quantity: 25,
          price: 100.0,
          size: "15x20cm",
          accessory: "Stand Base",
        },
      ],
      status: "In Production",
      orderDate: "2024-01-18",
      totalAmount: "$350.00",
      customerInfo: {
        name: "Sarah Wilson",
        phone: "+1234567893",
        email: "sarah@example.com",
        address: "321 Elm St",
        city: "Houston",
        state: "TX",
        zipcode: "77001",
        country: "USA",
      },
      orderNotes: "Corporate branding theme, professional look",
      uploadedFiles: {
        linkImg: {
          name: "corporate-design.jpg",
          url: "/corporate-keychain.jpg",
        },
        linkThanksCard: { name: "corporate-thanks.pdf", url: "#" },
        linkFileDesign: { name: "corporate-design.ai", url: "#" },
      },
    },
    {
      id: "ORD-005",
      customerName: "David Brown",
      sellerName: "Bob Wilson",
      products: [
        {
          name: "Acrylic Display",
          quantity: 35,
          price: 175.25,
          size: "12x18cm",
          accessory: "LED Base",
        },
      ],
      status: "QA Completed",
      orderDate: "2024-01-19",
      totalAmount: "$175.25",
      customerInfo: {
        name: "David Brown",
        phone: "+1234567894",
        email: "david@example.com",
        address: "654 Maple Ave",
        city: "Phoenix",
        state: "AZ",
        zipcode: "85001",
        country: "USA",
      },
      orderNotes: "High-quality finish required",
      uploadedFiles: {
        linkImg: { name: "display-reference.png", url: "/acrylic-display.jpg" },
        linkThanksCard: { name: "premium-thanks.jpg", url: "#" },
        linkFileDesign: { name: "display-design.psd", url: "#" },
      },
    },
  ];

  const sellers = ["Alice Johnson", "Bob Wilson", "Carol Davis"];

  const filteredOrders = orders.filter((order) => {
    const matchesSearch =
      order.id.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.customerName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      order.products.some((product) =>
        product.name.toLowerCase().includes(searchTerm.toLowerCase())
      );

    const matchesSeller =
      sellerFilter === "all" || order.sellerName === sellerFilter;
    const matchesStatus =
      statusFilter === "all" || order.status === statusFilter;

    const matchesDate =
      !dateFilter || order.orderDate === format(dateFilter, "yyyy-MM-dd");

    return matchesSearch && matchesSeller && matchesStatus && matchesDate;
  });

  const getStatusBadge = (status) => {
    switch (status) {
      case "In Production":
        return (
          <Badge className="bg-blue-500 hover:bg-blue-600 text-white">
            In Production
          </Badge>
        );
      case "QA Completed":
        return (
          <Badge className="bg-green-500 hover:bg-green-600 text-white">
            QA Completed
          </Badge>
        );
      case "Design Phase":
        return (
          <Badge className="bg-yellow-500 hover:bg-yellow-600 text-white">
            Design Phase
          </Badge>
        );
      default:
        return <Badge variant="secondary">{status}</Badge>;
    }
  };

  const handleDownload = (file) => {
    console.log(`Downloading file: ${file.name}`);
    // Simulate file download
    const link = document.createElement("a");
    link.href = file.url;
    link.download = file.name;
    link.click();
  };

  return (
    <div className="flex h-screen bg-gray-50">
      <ManagerSidebar
        currentPage={currentPage}
        setCurrentPage={setCurrentPage}
      />

      <div className="flex-1 flex flex-col overflow-hidden">
        <ManagerHeader />
        <header className="bg-white shadow-sm border-b border-gray-200 px-4 sm:px-6 py-4">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h1 className="text-xl sm:text-2xl font-semibold text-gray-900">
                Manager Order
              </h1>
              <p className="text-sm sm:text-base text-gray-600 mt-1">
                Manage and monitor all orders across sellers
              </p>
            </div>
            <div className="mt-3 sm:mt-0">
              <div className="bg-blue-50 border-2 border-blue-200 rounded-lg px-4 py-2">
                <p className="text-sm font-medium text-blue-800">
                  Order Management
                </p>
              </div>
            </div>
          </div>
        </header>

        {/* Main Content */}
        <main className="flex-1 overflow-y-auto p-4 sm:p-6">
          <div className="bg-white p-4 sm:p-6 rounded-lg shadow-sm border border-gray-200 mb-6">
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <div>
                <Label
                  htmlFor="search"
                  className="text-sm font-medium text-gray-700"
                >
                  Search Orders
                </Label>
                <div className="relative mt-1">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                  <Input
                    id="search"
                    placeholder="Order ID, Customer, Product..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-10 bg-white"
                  />
                </div>
              </div>

              <div>
                <Label
                  htmlFor="seller-filter"
                  className="text-sm font-medium text-gray-700"
                >
                  Filter by Seller
                </Label>
                <Select value={sellerFilter} onValueChange={setSellerFilter}>
                  <SelectTrigger className="mt-1 bg-white">
                    <SelectValue placeholder="All Sellers" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Sellers</SelectItem>
                    {sellers.map((seller) => (
                      <SelectItem key={seller} value={seller}>
                        {seller}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div>
                <Label
                  htmlFor="status-filter"
                  className="text-sm font-medium text-gray-700"
                >
                  Filter by Status
                </Label>
                <Select value={statusFilter} onValueChange={setStatusFilter}>
                  <SelectTrigger className="mt-1 bg-white">
                    <SelectValue placeholder="All Status" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Status</SelectItem>
                    <SelectItem value="Design Phase">Design Phase</SelectItem>
                    <SelectItem value="In Production">In Production</SelectItem>
                    <SelectItem value="QA Completed">QA Completed</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div>
                <Label
                  htmlFor="date-filter"
                  className="text-sm font-medium text-gray-700"
                >
                  Filter by Date
                </Label>
                <Popover>
                  <PopoverTrigger asChild>
                    <Button
                      variant="outline"
                      className="w-full justify-start text-left font-normal bg-white mt-1"
                    >
                      <CalendarIcon className="mr-2 h-4 w-4" />
                      {dateFilter ? format(dateFilter, "PPP") : "Pick a date"}
                    </Button>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0">
                    <Calendar
                      mode="single"
                      selected={dateFilter}
                      onSelect={setDateFilter}
                      initialFocus
                    />
                  </PopoverContent>
                </Popover>
              </div>
            </div>

            {dateFilter && (
              <div className="mt-4">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setDateFilter(null)}
                  className="bg-transparent"
                >
                  Clear Date Filter
                </Button>
              </div>
            )}
          </div>

          <div className="bg-white rounded-lg shadow-sm border border-gray-200">
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow className="bg-gray-50">
                    <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide">
                      Order ID
                    </TableHead>
                    <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide">
                      Customer
                    </TableHead>
                    <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide">
                      Seller
                    </TableHead>
                    <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide">
                      Products
                    </TableHead>
                    <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide">
                      Status
                    </TableHead>
                    <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide">
                      Order Date
                    </TableHead>
                    <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide">
                      Amount
                    </TableHead>
                    <TableHead className="font-medium text-gray-600 uppercase text-xs tracking-wide">
                      Actions
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredOrders.map((order) => (
                    <TableRow
                      key={order.id}
                      className="hover:bg-gray-50 transition-colors"
                    >
                      <TableCell className="font-medium text-gray-900">
                        {order.id}
                      </TableCell>
                      <TableCell className="text-gray-600">
                        {order.customerName}
                      </TableCell>
                      <TableCell className="text-gray-600">
                        {order.sellerName}
                      </TableCell>
                      <TableCell className="text-gray-600">
                        <div className="space-y-1">
                          {order.products.slice(0, 2).map((product, index) => (
                            <div key={index} className="text-sm">
                              {product.name} (x{product.quantity})
                            </div>
                          ))}
                          {order.products.length > 2 && (
                            <div className="text-xs text-gray-500">
                              +{order.products.length - 2} more items
                            </div>
                          )}
                        </div>
                      </TableCell>
                      <TableCell>{getStatusBadge(order.status)}</TableCell>
                      <TableCell className="text-gray-600">
                        {order.orderDate}
                      </TableCell>
                      <TableCell className="font-medium text-gray-900">
                        {order.totalAmount}
                      </TableCell>
                      <TableCell>
                        <Dialog>
                          <DialogTrigger asChild>
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => setSelectedOrder(order)}
                              className="bg-transparent hover:bg-gray-50"
                            >
                              <Eye className="h-4 w-4 mr-1" />
                              <span className="hidden sm:inline">
                                View Details
                              </span>
                            </Button>
                          </DialogTrigger>
                          <DialogContent className="max-w-4xl max-h-[80vh] overflow-y-auto">
                            <DialogHeader>
                              <DialogTitle className="flex items-center gap-2">
                                <Package className="h-5 w-5" />
                                Order Details - {order.id}
                              </DialogTitle>
                            </DialogHeader>
                            {selectedOrder && (
                              <div className="space-y-6">
                                {/* Customer Information */}
                                <div className="bg-gray-50 p-4 rounded-lg">
                                  <h3 className="font-semibold text-lg mb-3 text-gray-900">
                                    Customer Information
                                  </h3>
                                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                                    <div>
                                      <Label className="text-sm text-gray-500 font-medium">
                                        Name
                                      </Label>
                                      <p className="font-medium text-gray-900">
                                        {selectedOrder.customerInfo.name}
                                      </p>
                                    </div>
                                    <div>
                                      <Label className="text-sm text-gray-500 font-medium">
                                        Phone
                                      </Label>
                                      <p className="font-medium text-gray-900">
                                        {selectedOrder.customerInfo.phone}
                                      </p>
                                    </div>
                                    <div>
                                      <Label className="text-sm text-gray-500 font-medium">
                                        Email
                                      </Label>
                                      <p className="font-medium text-gray-900">
                                        {selectedOrder.customerInfo.email}
                                      </p>
                                    </div>
                                    <div>
                                      <Label className="text-sm text-gray-500 font-medium">
                                        Address
                                      </Label>
                                      <p className="font-medium text-gray-900">
                                        {selectedOrder.customerInfo.address},{" "}
                                        {selectedOrder.customerInfo.city},{" "}
                                        {selectedOrder.customerInfo.state}{" "}
                                        {selectedOrder.customerInfo.zipcode}
                                      </p>
                                    </div>
                                  </div>
                                </div>

                                {/* Product Details */}
                                <div>
                                  <h3 className="font-semibold text-lg mb-3 text-gray-900">
                                    Product Details
                                  </h3>
                                  <div className="space-y-4">
                                    {selectedOrder.products.map(
                                      (product, index) => (
                                        <div
                                          key={index}
                                          className="border border-gray-200 rounded-lg p-4"
                                        >
                                          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                                            <div>
                                              <Label className="text-sm text-gray-500 font-medium">
                                                Product
                                              </Label>
                                              <p className="font-medium text-gray-900">
                                                {product.name}
                                              </p>
                                            </div>
                                            <div>
                                              <Label className="text-sm text-gray-500 font-medium">
                                                Size
                                              </Label>
                                              <p className="font-medium text-gray-900">
                                                {product.size}
                                              </p>
                                            </div>
                                            <div>
                                              <Label className="text-sm text-gray-500 font-medium">
                                                Quantity
                                              </Label>
                                              <p className="font-medium text-gray-900">
                                                {product.quantity}
                                              </p>
                                            </div>
                                            <div>
                                              <Label className="text-sm text-gray-500 font-medium">
                                                Accessory
                                              </Label>
                                              <p className="font-medium text-gray-900">
                                                {product.accessory}
                                              </p>
                                            </div>
                                          </div>
                                        </div>
                                      )
                                    )}
                                  </div>
                                  {selectedOrder.orderNotes && (
                                    <div className="mt-4 p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                                      <Label className="text-sm text-yellow-700 font-medium">
                                        Order Notes
                                      </Label>
                                      <p className="text-yellow-800 mt-1">
                                        {selectedOrder.orderNotes}
                                      </p>
                                    </div>
                                  )}
                                </div>

                                {/* Order Files */}
                                <div>
                                  <h3 className="font-semibold text-lg mb-3 text-gray-900">
                                    Order Files
                                  </h3>
                                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                    <div className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow">
                                      <Label className="text-sm text-gray-500 font-medium">
                                        Reference Image
                                      </Label>
                                      <div className="mt-2">
                                        <img
                                          src={
                                            selectedOrder.uploadedFiles.linkImg
                                              .url || "/placeholder.svg"
                                          }
                                          alt="Reference"
                                          className="w-full h-32 object-cover rounded border"
                                        />
                                        <p className="text-sm mt-2 text-gray-600">
                                          {
                                            selectedOrder.uploadedFiles.linkImg
                                              .name
                                          }
                                        </p>
                                        <Button
                                          variant="outline"
                                          size="sm"
                                          className="mt-2 w-full bg-transparent hover:bg-gray-50"
                                          onClick={() =>
                                            handleDownload(
                                              selectedOrder.uploadedFiles
                                                .linkImg
                                            )
                                          }
                                        >
                                          <Download className="h-4 w-4 mr-2" />
                                          Download
                                        </Button>
                                      </div>
                                    </div>

                                    <div className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow">
                                      <Label className="text-sm text-gray-500 font-medium">
                                        Thanks Card
                                      </Label>
                                      <div className="mt-2">
                                        <div className="w-full h-32 bg-gray-100 rounded border flex items-center justify-center">
                                          <QrCode className="h-8 w-8 text-gray-400" />
                                        </div>
                                        <p className="text-sm mt-2 text-gray-600">
                                          {
                                            selectedOrder.uploadedFiles
                                              .linkThanksCard.name
                                          }
                                        </p>
                                        <Button
                                          variant="outline"
                                          size="sm"
                                          className="mt-2 w-full bg-transparent hover:bg-gray-50"
                                          onClick={() =>
                                            handleDownload(
                                              selectedOrder.uploadedFiles
                                                .linkThanksCard
                                            )
                                          }
                                        >
                                          <Download className="h-4 w-4 mr-2" />
                                          Download
                                        </Button>
                                      </div>
                                    </div>

                                    <div className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow">
                                      <Label className="text-sm text-gray-500 font-medium">
                                        Design File
                                      </Label>
                                      <div className="mt-2">
                                        <div className="w-full h-32 bg-gray-100 rounded border flex items-center justify-center">
                                          <QrCode className="h-8 w-8 text-gray-400" />
                                        </div>
                                        <p className="text-sm mt-2 text-gray-600">
                                          {
                                            selectedOrder.uploadedFiles
                                              .linkFileDesign.name
                                          }
                                        </p>
                                        <Button
                                          variant="outline"
                                          size="sm"
                                          className="mt-2 w-full bg-transparent hover:bg-gray-50"
                                          onClick={() =>
                                            handleDownload(
                                              selectedOrder.uploadedFiles
                                                .linkFileDesign
                                            )
                                          }
                                        >
                                          <Download className="h-4 w-4 mr-2" />
                                          Download
                                        </Button>
                                      </div>
                                    </div>
                                  </div>
                                </div>

                                {/* QR Code Section */}
                                <div className="bg-gray-50 p-4 rounded-lg">
                                  <h4 className="font-medium mb-2 text-gray-900">
                                    QR Code for Order {selectedOrder.id}
                                  </h4>
                                  <div className="w-32 h-32 bg-white border-2 border-gray-300 rounded flex items-center justify-center">
                                    <QrCode className="h-16 w-16 text-gray-400" />
                                  </div>
                                </div>
                              </div>
                            )}
                          </DialogContent>
                        </Dialog>
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
