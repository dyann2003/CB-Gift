"use client";

import { useState, useEffect } from "react";
import ManagerSidebar from "@/components/layout/manager/sidebar";
import ManagerHeader from "@/components/layout/manager/header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Trash2, Plus, ChevronLeft, ChevronRight, Edit2 } from "lucide-react";
import { useToast } from "@/hooks/use-toast";

export default function ManageCategory() {
  const { toast } = useToast();
  const [currentPage, setCurrentPage] = useState("manage-category");
  const [categories, setCategories] = useState([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [page, setPage] = useState(1);
  const [itemsPerPage, setItemsPerPage] = useState(10);
  const [newCategoryName, setNewCategoryName] = useState("");
  const [showAddModal, setShowAddModal] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [editingName, setEditingName] = useState("");
  const [showEditModal, setShowEditModal] = useState(false);

  // Mock categories data
  const mockCategories = [
    {
      id: "CAT-001",
      name: "Keychains",
      productsCount: 12,
      createdDate: "2024-01-01",
    },
    {
      id: "CAT-002",
      name: "Phone Accessories",
      productsCount: 8,
      createdDate: "2024-01-02",
    },
    {
      id: "CAT-003",
      name: "Desk Accessories",
      productsCount: 15,
      createdDate: "2024-01-03",
    },
    {
      id: "CAT-004",
      name: "Home Decor",
      productsCount: 6,
      createdDate: "2024-01-04",
    },
    {
      id: "CAT-005",
      name: "Gifts",
      productsCount: 20,
      createdDate: "2024-01-05",
    },
    {
      id: "CAT-006",
      name: "Mugs",
      productsCount: 10,
      createdDate: "2024-01-06",
    },
    {
      id: "CAT-007",
      name: "T-Shirts",
      productsCount: 14,
      createdDate: "2024-01-07",
    },
    {
      id: "CAT-008",
      name: "Caps",
      productsCount: 9,
      createdDate: "2024-01-08",
    },
    {
      id: "CAT-009",
      name: "Stickers",
      productsCount: 25,
      createdDate: "2024-01-09",
    },
    {
      id: "CAT-010",
      name: "Magnets",
      productsCount: 7,
      createdDate: "2024-01-10",
    },
  ];

  useEffect(() => {
    // Simulate API call
    const filteredCategories = mockCategories.filter((cat) =>
      cat.name.toLowerCase().includes(searchTerm.toLowerCase())
    );

    const startIndex = (page - 1) * itemsPerPage;
    const paginatedCategories = filteredCategories.slice(
      startIndex,
      startIndex + itemsPerPage
    );
    setCategories(paginatedCategories);
  }, [page, itemsPerPage, searchTerm]);

  const totalCategories = mockCategories.filter((cat) =>
    cat.name.toLowerCase().includes(searchTerm.toLowerCase())
  ).length;
  const totalPages = Math.ceil(totalCategories / itemsPerPage);

  const handleAddCategory = () => {
    if (!newCategoryName.trim()) {
      toast({
        title: "Error",
        description: "Category name cannot be empty",
        variant: "destructive",
      });
      return;
    }

    const newCategory = {
      id: `CAT-${Math.random().toString(36).substr(2, 9).toUpperCase()}`,
      name: newCategoryName,
      productsCount: 0,
      createdDate: new Date().toISOString().split("T")[0],
    };

    setCategories((prev) => [newCategory, ...prev]);
    setNewCategoryName("");
    setShowAddModal(false);

    toast({
      title: "Success",
      description: "Category added successfully",
      variant: "default",
    });
  };

  const handleEditCategory = (category) => {
    setEditingId(category.id);
    setEditingName(category.name);
    setShowEditModal(true);
  };

  const handleSaveEdit = () => {
    if (!editingName.trim()) {
      toast({
        title: "Error",
        description: "Category name cannot be empty",
        variant: "destructive",
      });
      return;
    }

    setCategories((prev) =>
      prev.map((cat) =>
        cat.id === editingId ? { ...cat, name: editingName } : cat
      )
    );

    setEditingId(null);
    setEditingName("");
    setShowEditModal(false);

    toast({
      title: "Success",
      description: "Category updated successfully",
      variant: "default",
    });
  };

  const handleDeleteCategory = (categoryId) => {
    if (window.confirm("Are you sure you want to delete this category?")) {
      setCategories((prev) => prev.filter((cat) => cat.id !== categoryId));

      toast({
        title: "Success",
        description: "Category deleted successfully",
        variant: "default",
      });
    }
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
            Manage Categories
          </h1>
          <p className="text-gray-600 mt-1">
            Organize and manage product categories
          </p>
        </header>

        <main className="flex-1 overflow-y-auto p-6">
          <div className="space-y-6">
            {/* Search and Add Section */}
            <div className="bg-white p-6 rounded-lg shadow">
              <div className="flex flex-col md:flex-row gap-4 items-center justify-between">
                <div className="flex-1 max-w-md">
                  <Input
                    placeholder="Search categories..."
                    value={searchTerm}
                    onChange={(e) => {
                      setSearchTerm(e.target.value);
                      setPage(1);
                    }}
                  />
                </div>
                <Button
                  onClick={() => setShowAddModal(true)}
                  className="bg-blue-600 hover:bg-blue-700"
                >
                  <Plus className="h-4 w-4 mr-2" />
                  Add Category
                </Button>
              </div>
            </div>

            {/* Categories Table */}
            <div className="bg-white rounded-lg shadow">
              <div className="px-6 py-4 border-b border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900">
                  Categories ({totalCategories})
                </h2>
                <p className="text-sm text-gray-600">
                  Manage your product categories
                </p>
              </div>
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow className="bg-blue-100">
                      <TableHead className="font-medium text-slate-700 uppercase text-xs tracking-wide">
                        ID
                      </TableHead>
                      <TableHead className="font-medium text-slate-700 uppercase text-xs tracking-wide">
                        Category Name
                      </TableHead>
                      <TableHead className="font-medium text-slate-700 uppercase text-xs tracking-wide">
                        Products
                      </TableHead>
                      <TableHead className="font-medium text-slate-700 uppercase text-xs tracking-wide">
                        Created Date
                      </TableHead>
                      <TableHead className="font-medium text-slate-700 uppercase text-xs tracking-wide">
                        Actions
                      </TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {categories.length === 0 ? (
                      <TableRow>
                        <TableCell
                          colSpan="5"
                          className="text-center py-8 text-gray-500"
                        >
                          No categories found
                        </TableCell>
                      </TableRow>
                    ) : (
                      categories.map((category) => (
                        <TableRow
                          key={category.id}
                          className="hover:bg-blue-50"
                        >
                          <TableCell className="font-medium text-sm">
                            {category.id}
                          </TableCell>
                          <TableCell>{category.name}</TableCell>
                          <TableCell>
                            <Badge variant="outline">
                              {category.productsCount}
                            </Badge>
                          </TableCell>
                          <TableCell className="text-sm text-gray-600">
                            {category.createdDate}
                          </TableCell>
                          <TableCell>
                            <div className="flex gap-2">
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => handleEditCategory(category)}
                              >
                                <Edit2 className="h-4 w-4" />
                              </Button>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() =>
                                  handleDeleteCategory(category.id)
                                }
                                className="text-red-600 hover:text-red-700 border-red-200"
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            </div>
                          </TableCell>
                        </TableRow>
                      ))
                    )}
                  </TableBody>
                </Table>
              </div>

              {/* Pagination */}
              <div className="bg-blue-100 px-4 py-3 border-t border-blue-200 sm:px-6">
                <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
                  <div className="flex items-center gap-2">
                    <span className="text-sm text-slate-600">
                      Items per page:
                    </span>
                    <select
                      value={itemsPerPage}
                      onChange={(e) => {
                        setItemsPerPage(Number(e.target.value));
                        setPage(1);
                      }}
                      className="border border-gray-300 rounded px-2 py-1 text-sm"
                    >
                      <option value="5">5</option>
                      <option value="10">10</option>
                      <option value="15">15</option>
                      <option value="20">20</option>
                    </select>
                  </div>

                  <div className="text-sm text-slate-600">
                    Showing{" "}
                    {categories.length > 0 ? (page - 1) * itemsPerPage + 1 : 0}{" "}
                    to {Math.min(page * itemsPerPage, totalCategories)} of{" "}
                    {totalCategories}
                  </div>

                  <div className="flex items-center gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={page === 1}
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
                      disabled={page === totalPages}
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

      {/* Add Category Modal */}
      <Dialog open={showAddModal} onOpenChange={setShowAddModal}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Add New Category</DialogTitle>
            <DialogDescription>Create a new product category</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Input
                placeholder="Category name"
                value={newCategoryName}
                onChange={(e) => setNewCategoryName(e.target.value)}
              />
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => setShowAddModal(false)}>
                Cancel
              </Button>
              <Button
                onClick={handleAddCategory}
                className="bg-blue-600 hover:bg-blue-700"
              >
                Add Category
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>

      {/* Edit Category Modal */}
      <Dialog open={showEditModal} onOpenChange={setShowEditModal}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Edit Category</DialogTitle>
            <DialogDescription>Update the category name</DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div>
              <Input
                placeholder="Category name"
                value={editingName}
                onChange={(e) => setEditingName(e.target.value)}
              />
            </div>
            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => setShowEditModal(false)}>
                Cancel
              </Button>
              <Button
                onClick={handleSaveEdit}
                className="bg-blue-600 hover:bg-blue-700"
              >
                Save Changes
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
