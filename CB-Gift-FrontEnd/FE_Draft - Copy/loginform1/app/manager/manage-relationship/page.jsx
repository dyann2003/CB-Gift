"use client";

import { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Search, Plus, Eye } from "lucide-react";
import RoleSidebar from "@/components/layout/shared/role-sidebar";
import RelationshipDetailsModal from "@/components/modals/relationship-details-modal";
import AddRelationshipModal from "@/components/modals/add-relationship-modal";
import ManagerSidebar from "@/components/layout/manager/sidebar";
import ManagerHeader from "@/components/layout/manager/header";

export default function ManageRelationshipPage() {
  const [currentPage, setCurrentPage] = useState("manage-relationship");
  const [relationships, setRelationships] = useState([]);
  const [filteredRelationships, setFilteredRelationships] = useState([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");
  const [loading, setLoading] = useState(true);
  const [showAddModal, setShowAddModal] = useState(false);
  const [showDetailsModal, setShowDetailsModal] = useState(false);
  const [selectedRelationship, setSelectedRelationship] = useState(null);

  useEffect(() => {
    const mockRelationships = [
      {
        id: 1,
        seller: { id: 1, name: "John Seller", email: "john@seller.com" },
        designer: {
          id: 1,
          name: "Alice Designer",
          email: "alice@designer.com",
        },
        status: "active",
        createdAt: "2024-01-15",
        projectsCount: 12,
      },
      {
        id: 2,
        seller: { id: 2, name: "Mike Seller", email: "mike@seller.com" },
        designer: { id: 2, name: "Bob Designer", email: "bob@designer.com" },
        status: "inactive",
        createdAt: "2024-01-10",
        projectsCount: 8,
      },
      {
        id: 3,
        seller: { id: 3, name: "Sarah Seller", email: "sarah@seller.com" },
        designer: {
          id: 1,
          name: "Alice Designer",
          email: "alice@designer.com",
        },
        status: "active",
        createdAt: "2024-01-20",
        projectsCount: 5,
      },
    ];

    setTimeout(() => {
      setRelationships(mockRelationships);
      setFilteredRelationships(mockRelationships);
      setLoading(false);
    }, 1000);
  }, []);

  // Filter relationships based on search and status
  useEffect(() => {
    let filtered = relationships;

    if (searchTerm) {
      filtered = filtered.filter(
        (rel) =>
          rel.seller.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
          rel.designer.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
          rel.seller.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
          rel.designer.email.toLowerCase().includes(searchTerm.toLowerCase())
      );
    }

    if (statusFilter !== "all") {
      filtered = filtered.filter((rel) => rel.status === statusFilter);
    }

    setFilteredRelationships(filtered);
  }, [searchTerm, statusFilter, relationships]);

  const handleViewDetails = (relationship) => {
    setSelectedRelationship(relationship);
    setShowDetailsModal(true);
  };

  const handleUpdateRelationship = (updatedRelationship) => {
    const updatedRelationships = relationships.map((rel) =>
      rel.id === updatedRelationship.id ? updatedRelationship : rel
    );
    setRelationships(updatedRelationships);
    setFilteredRelationships(updatedRelationships);
    setShowDetailsModal(false);
  };

  const handleAddRelationship = (newRelationship) => {
    const updatedRelationships = [...relationships, newRelationship];
    setRelationships(updatedRelationships);
    setFilteredRelationships(updatedRelationships);
  };

  const getStatusBadge = (status) => {
    return status === "active" ? (
      <Badge className="bg-green-100 text-green-800">Active</Badge>
    ) : (
      <Badge className="bg-red-100 text-red-800">Inactive</Badge>
    );
  };

  if (loading) {
    return (
      <div className="flex h-screen bg-gray-50">
        <ManagerSidebar
          currentPage={currentPage}
          setCurrentPage={setCurrentPage}
        />
        <div className="flex-1 flex items-center justify-center">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
            <p className="mt-4 text-gray-600">Loading relationships...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-screen bg-gray-50">
      <ManagerSidebar
        currentRole="manager"
        currentPage={currentPage}
        setCurrentPage={setCurrentPage}
      />

      <div className="flex-1 flex flex-col overflow-hidden">
        <ManagerHeader />
        <div className="bg-white shadow-sm border-b border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-gray-900">
                Manage Relationships
              </h1>
              <p className="text-gray-600 mt-1">
                Manage seller-designer relationships
              </p>
            </div>
          </div>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-auto p-6">
          {/* Search and Filters */}
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6 mb-6">
            <div className="flex flex-col sm:flex-row gap-4">
              <div className="flex-1 relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                <Input
                  placeholder="Search by seller or designer name, email..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10"
                />
              </div>
              <Select value={statusFilter} onValueChange={setStatusFilter}>
                <SelectTrigger className="w-full sm:w-48">
                  <SelectValue placeholder="Filter by status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Status</SelectItem>
                  <SelectItem value="active">Active</SelectItem>
                  <SelectItem value="inactive">Inactive</SelectItem>
                </SelectContent>
              </Select>
              <Button
                onClick={() => setShowAddModal(true)}
                className="flex items-center gap-2"
              >
                <Plus className="h-4 w-4" />
                Add Relationship
              </Button>
            </div>
          </div>

          {/* Relationships List */}
          <div className="grid gap-4">
            {filteredRelationships.length === 0 ? (
              <Card>
                <CardContent className="p-8 text-center">
                  <p className="text-gray-500">
                    No relationships found matching your criteria.
                  </p>
                </CardContent>
              </Card>
            ) : (
              filteredRelationships.map((relationship) => (
                <Card
                  key={relationship.id}
                  className="hover:shadow-md transition-shadow"
                >
                  <CardContent className="p-6">
                    <div className="flex items-center justify-between">
                      <div className="flex-1 grid grid-cols-1 md:grid-cols-4 gap-4">
                        <div>
                          <p className="text-sm font-medium text-gray-900">
                            Seller
                          </p>
                          <p className="text-sm text-gray-600">
                            {relationship.seller.name}
                          </p>
                          <p className="text-xs text-gray-500">
                            {relationship.seller.email}
                          </p>
                        </div>
                        <div>
                          <p className="text-sm font-medium text-gray-900">
                            Designer
                          </p>
                          <p className="text-sm text-gray-600">
                            {relationship.designer.name}
                          </p>
                          <p className="text-xs text-gray-500">
                            {relationship.designer.email}
                          </p>
                        </div>
                        <div>
                          <p className="text-sm font-medium text-gray-900">
                            Status
                          </p>
                          {getStatusBadge(relationship.status)}
                          <p className="text-xs text-gray-500 mt-1">
                            Projects: {relationship.projectsCount}
                          </p>
                        </div>
                        <div>
                          <p className="text-sm font-medium text-gray-900">
                            Created
                          </p>
                          <p className="text-sm text-gray-600">
                            {relationship.createdAt}
                          </p>
                        </div>
                      </div>
                      <div className="flex items-center gap-2 ml-4">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleViewDetails(relationship)}
                          className="flex items-center gap-1"
                        >
                          <Eye className="h-4 w-4" />
                          Details
                        </Button>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))
            )}
          </div>
        </div>
      </div>

      {/* Modals */}
      <RelationshipDetailsModal
        isOpen={showDetailsModal}
        onClose={() => setShowDetailsModal(false)}
        relationship={selectedRelationship}
        onUpdate={handleUpdateRelationship}
      />

      <AddRelationshipModal
        isOpen={showAddModal}
        onClose={() => setShowAddModal(false)}
        onAdd={handleAddRelationship}
      />
    </div>
  );
}
