"use client";

import { useState, useEffect } from "react";
import ManagerSidebar from "@/components/layout/manager/sidebar";
import ManagerHeader from "@/components/layout/manager/header";
import AddProductModal from "@/components/modals/add-product-modal";
import ProductDetailsModal from "@/components/modals/product-details-modal";
import ManageCategoryModal from "@/components/modals/manage-category-modal";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Eye,
  Plus,
  Settings,
  Search,
  Filter,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";

export default function ManageCatalog() {
  const [currentPage, setCurrentPage] = useState("manage-catalog");
  const [searchTerm, setSearchTerm] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");
  const [page, setPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(10);
  const [selectedProducts, setSelectedProducts] = useState([]);
  const [selectAll, setSelectAll] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState(null);
  const [showAddModal, setShowAddModal] = useState(false);
  const [showDetailsModal, setShowDetailsModal] = useState(false);
  const [showCategoryModal, setShowCategoryModal] = useState(false);
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [totalProductsCount, setTotalProductsCount] = useState(0);
  const [categories, setCategories] = useState([]);

  // Mock data - will be replaced with API calls
  const mockProducts = [
    {
      id: "PRD-0001",
      productName: "Acrylic Keychain",
      description: "Custom acrylic keychain with engravable surface",
      category: "Keychains",
      image: "/acrylic-keychain.jpg",
      status: "Active",
      basePrice: "$2.50",
      baseCost: "$1.20",
      shipCost: "$0.80",
      createdDate: "2024-01-10",
    },
    {
      id: "PRD-0002",
      productName: "Custom Phone Stand",
      description: "Personalized acrylic phone stand",
      category: "Phone Accessories",
      image: "/phone-stand.jpg",
      status: "Active",
      basePrice: "$5.00",
      baseCost: "$2.50",
      shipCost: "$1.50",
      createdDate: "2024-01-12",
    },
    {
      id: "PRD-0003",
      productName: "Acrylic Name Plate",
      description: "Customizable acrylic desk nameplate",
      category: "Desk Accessories",
      image: "/nameplate.jpg",
      status: "Inactive",
      basePrice: "$8.00",
      baseCost: "$4.00",
      shipCost: "$2.00",
      createdDate: "2024-01-15",
    },
  ];

  const mockCategories = [
    "Keychains",
    "Phone Accessories",
    "Desk Accessories",
    "Home Decor",
    "Gifts",
  ];

  useEffect(() => {
    setCategories(mockCategories);
    // Simulate API call
    const filteredProducts = mockProducts.filter((product) => {
      const matchesSearch =
        product.productName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        product.id.toLowerCase().includes(searchTerm.toLowerCase()) ||
        product.description.toLowerCase().includes(searchTerm.toLowerCase());

      const matchesCategory =
        categoryFilter === "all" || product.category === categoryFilter;
      const matchesStatus =
        statusFilter === "all" || product.status === statusFilter;

      return matchesSearch && matchesCategory && matchesStatus;
    });

    setTotalProductsCount(filteredProducts.length);
    const startIndex = (page - 1) * itemsPerPage;
    const paginatedProducts = filteredProducts.slice(
      startIndex,
      startIndex + itemsPerPage
    );
    setProducts(paginatedProducts);
    setSelectedProducts([]);
    setSelectAll(false);
  }, [page, itemsPerPage, searchTerm, categoryFilter, statusFilter]);

  const totalPages = Math.ceil(totalProductsCount / itemsPerPage);

  const getStatusBadge = (status) => {
    return status === "Active" ? (
      <Badge className="bg-green-500">Active</Badge>
    ) : (
      <Badge className="bg-gray-500">Inactive</Badge>
    );
  };

  const handleSelectAll = () => {
    const newSelectAll = !selectAll;
    setSelectAll(newSelectAll);
    if (newSelectAll) {
      setSelectedProducts(products.map((product) => product.id));
    } else {
      setSelectedProducts([]);
    }
  };

  const handleProductSelect = (productId) => {
    if (selectedProducts.includes(productId)) {
      setSelectedProducts(selectedProducts.filter((id) => id !== productId));
      setSelectAll(false);
    } else {
      setSelectedProducts([...selectedProducts, productId]);
    }
  };

  const handleSearchChange = (value) => {
    setSearchTerm(value);
    setPage(1);
  };

  const handleCategoryFilterChange = (value) => {
    setCategoryFilter(value);
    setPage(1);
  };

  const handleStatusFilterChange = (value) => {
    setStatusFilter(value);
    setPage(1);
  };

  const handleItemsPerPageChange = (value) => {
    setItemsPerPage(Number(value));
    setPage(1);
  };

  const handleViewDetails = (product) => {
    setSelectedProduct(product);
    setShowDetailsModal(true);
  };

  const handleAddProduct = (newProduct) => {
    setProducts((prev) => [newProduct, ...prev]);
    setShowAddModal(false);
  };

  return (
    <div className="flex h-screen bg-gray-50">
      <ManagerSidebar
        currentPage={currentPage}
        setCurrentPage={setCurrentPage}
      />
      <div className="flex-1 flex flex-col overflow-hidden">
        <ManagerHeader />
        <header className="bg-white shadow-sm border-b border-gray-200 px-6 py-4">
          <h1 className="text-2xl font-semibold text-gray-900">
            Manage Catalog
          </h1>
          <p className="text-gray-600 mt-1">Manage products and categories</p>
        </header>

        <main className="flex-1 overflow-y-auto p-6">
          <div className="space-y-6">
            {/* Search and Filter Section */}
            <div className="bg-white p-6 rounded-lg shadow">
              <div className="flex flex-col md:flex-row gap-4 items-center justify-between">
                <div className="flex flex-col md:flex-row gap-4 flex-1">
                  <div className="relative flex-1 max-w-md">
                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                    <Input
                      placeholder="Search products..."
                      value={searchTerm}
                      onChange={(e) => handleSearchChange(e.target.value)}
                      className="pl-10"
                    />
                  </div>

                  <Select
                    value={categoryFilter}
                    onValueChange={handleCategoryFilterChange}
                  >
                    <SelectTrigger className="w-40">
                      <Filter className="h-4 w-4 mr-2" />
                      <SelectValue placeholder="Filter by category" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">All Categories</SelectItem>
                      {categories.map((cat) => (
                        <SelectItem key={cat} value={cat}>
                          {cat}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>

                  <Select
                    value={statusFilter}
                    onValueChange={handleStatusFilterChange}
                  >
                    <SelectTrigger className="w-40">
                      <SelectValue placeholder="Filter by status" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">All Status</SelectItem>
                      <SelectItem value="Active">Active</SelectItem>
                      <SelectItem value="Inactive">Inactive</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="flex gap-2">
                  <a href="/manager/manage-category">
                    <Button variant="outline">
                      <Settings className="h-4 w-4 mr-2" />
                      Manage Category
                    </Button>
                  </a>
                  <Button
                    onClick={() => setShowAddModal(true)}
                    className="bg-blue-600 hover:bg-blue-700"
                  >
                    <Plus className="h-4 w-4 mr-2" />
                    Add Product
                  </Button>
                </div>
              </div>
            </div>

            {/* Products Table */}
            <div className="bg-white rounded-lg shadow">
              <div className="px-6 py-4 border-b border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900">
                  Products ({totalProductsCount})
                </h2>
                <p className="text-sm text-gray-600">
                  {selectedProducts.length > 0 && (
                    <>Selected: {selectedProducts.length} | </>
                  )}
                  Manage and organize your product catalog
                </p>
              </div>
              <div className="overflow-x-auto">
                {error && (
                  <div className="p-4 bg-red-50 text-red-700 border-b border-red-200">
                    Error loading products: {error}
                  </div>
                )}
                <Table>
                  <TableHeader>
                    <TableRow className="bg-blue-100">
                      <TableHead className="font-medium text-slate-700 uppercase text-xs tracking-wide whitespace-nowrap w-12">
                        <Checkbox
                          checked={selectAll}
                          onCheckedChange={handleSelectAll}
                          disabled={loading}
                        />
                      </TableHead>
                      <TableHead>ID</TableHead>
                      <TableHead>Product Name</TableHead>
                      <TableHead>Description</TableHead>
                      <TableHead>Image</TableHead>
                      <TableHead>Category</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {loading ? (
                      <TableRow>
                        <TableCell
                          colSpan="8"
                          className="text-center py-8 text-gray-500"
                        >
                          Loading products...
                        </TableCell>
                      </TableRow>
                    ) : products.length === 0 ? (
                      <TableRow>
                        <TableCell
                          colSpan="8"
                          className="text-center py-8 text-gray-500"
                        >
                          No products found
                        </TableCell>
                      </TableRow>
                    ) : (
                      products.map((product) => (
                        <TableRow key={product.id} className="hover:bg-blue-50">
                          <TableCell>
                            <Checkbox
                              checked={selectedProducts.includes(product.id)}
                              onCheckedChange={() =>
                                handleProductSelect(product.id)
                              }
                            />
                          </TableCell>
                          <TableCell className="font-medium text-sm">
                            {product.id}
                          </TableCell>
                          <TableCell className="font-medium">
                            {product.productName}
                          </TableCell>
                          <TableCell className="text-sm text-gray-600">
                            {product.description}
                          </TableCell>
                          <TableCell>
                            <img
                              src={product.image || "/placeholder.svg"}
                              alt={product.productName}
                              className="h-10 w-10 rounded object-cover"
                            />
                          </TableCell>
                          <TableCell>{product.category}</TableCell>
                          <TableCell>
                            {getStatusBadge(product.status)}
                          </TableCell>
                          <TableCell>
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleViewDetails(product)}
                            >
                              <Eye className="h-4 w-4 mr-1" />
                              View Details
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </div>

              <div className="bg-blue-100 px-4 py-3 border-t border-blue-200 sm:px-6">
                <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
                  {/* Items Per Page Selector */}
                  <div className="flex items-center gap-2">
                    <span className="text-sm text-slate-600">
                      Items per page:
                    </span>
                    <Select
                      value={itemsPerPage.toString()}
                      onValueChange={handleItemsPerPageChange}
                      disabled={loading}
                    >
                      <SelectTrigger className="w-[70px] bg-white border-blue-200 hover:bg-blue-50">
                        <SelectValue placeholder={itemsPerPage} />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="5">5</SelectItem>
                        <SelectItem value="10">10</SelectItem>
                        <SelectItem value="15">15</SelectItem>
                        <SelectItem value="20">20</SelectItem>
                        <SelectItem value="50">50</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>

                  {/* Page Info */}
                  <div className="text-sm text-slate-600">
                    Showing{" "}
                    {products.length > 0 ? (page - 1) * itemsPerPage + 1 : 0} to{" "}
                    {Math.min(page * itemsPerPage, totalProductsCount)} of{" "}
                    {totalProductsCount}
                  </div>

                  {/* Pagination Controls */}
                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={page === 1 || loading}
                      onClick={() => setPage(page - 1)}
                    >
                      <ChevronLeft className="h-4 w-4" />
                    </Button>
                    <span className="text-sm text-slate-600 min-w-[60px] text-center">
                      Page {page} of {totalPages || 1}
                    </span>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={page === totalPages || loading}
                      onClick={() => setPage(page + 1)}
                    >
                      <ChevronRight className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </main>
      </div>

      {/* Add Product Modal */}
      <AddProductModal
        isOpen={showAddModal}
        onClose={() => setShowAddModal(false)}
        onAddProduct={handleAddProduct}
        categories={categories}
      />

      {/* Product Details Modal */}
      <ProductDetailsModal
        product={selectedProduct}
        isOpen={showDetailsModal}
        onClose={() => {
          setShowDetailsModal(false);
          setSelectedProduct(null);
        }}
        categories={categories}
      />

      {/* Manage Category Modal */}
      <ManageCategoryModal
        isOpen={showCategoryModal}
        onClose={() => setShowCategoryModal(false)}
        categories={categories}
        onUpdateCategories={setCategories}
      />
    </div>
  );
}
