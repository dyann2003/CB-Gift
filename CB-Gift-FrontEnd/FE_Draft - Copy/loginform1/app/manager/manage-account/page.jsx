"use client";

import { useState } from "react";
import RoleSidebar from "@/components/layout/shared/role-sidebar";
import ManagerSidebar from "@/components/layout/manager/sidebar";
import ManagerHeader from "@/components/layout/manager/header";
import AccountDetailsModal from "@/components/modals/account-details-modal";
import AssignAccountModal from "@/components/modals/assign-account-modal";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
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
import { Eye, UserPlus, Search, Filter, Ban, CheckCircle } from "lucide-react";

export default function ManageAccount() {
  const [currentPage, setCurrentPage] = useState("manage-account");
  const [searchTerm, setSearchTerm] = useState("");
  const [roleFilter, setRoleFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");
  const [selectedAccount, setSelectedAccount] = useState(null);
  const [showDetailsModal, setShowDetailsModal] = useState(false);
  const [showAssignModal, setShowAssignModal] = useState(false);

  // Mock data for accounts
  const [accounts, setAccounts] = useState([
    {
      id: "ACC-001",
      username: "john.doe",
      email: "john.doe@company.com",
      fullName: "John Doe",
      role: "designer",
      status: "active",
      createdDate: "2024-01-15",
      lastLogin: "2024-01-20",
    },
    {
      id: "ACC-002",
      username: "jane.smith",
      email: "jane.smith@company.com",
      fullName: "Jane Smith",
      role: "qc",
      status: "active",
      createdDate: "2024-01-16",
      lastLogin: "2024-01-19",
    },
    {
      id: "ACC-003",
      username: "mike.johnson",
      email: "mike.johnson@company.com",
      fullName: "Mike Johnson",
      role: "staff",
      status: "banned",
      createdDate: "2024-01-17",
      lastLogin: "2024-01-18",
    },
    {
      id: "ACC-004",
      username: "sarah.wilson",
      email: "sarah.wilson@company.com",
      fullName: "Sarah Wilson",
      role: "seller",
      status: "active",
      createdDate: "2024-01-18",
      lastLogin: "2024-01-20",
    },
  ]);

  const filteredAccounts = accounts.filter((account) => {
    const matchesSearch =
      account.fullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      account.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      account.username.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesRole = roleFilter === "all" || account.role === roleFilter;
    const matchesStatus =
      statusFilter === "all" || account.status === statusFilter;
    return matchesSearch && matchesRole && matchesStatus;
  });

  const getRoleBadge = (role) => {
    const roleColors = {
      manager: "bg-purple-500",
      designer: "bg-blue-500",
      qc: "bg-green-500",
      seller: "bg-orange-500",
      staff: "bg-gray-500",
    };
    return (
      <Badge className={roleColors[role] || "bg-gray-500"}>
        {role.toUpperCase()}
      </Badge>
    );
  };

  const getStatusBadge = (status) => {
    return status === "active" ? (
      <Badge className="bg-green-500">Active</Badge>
    ) : (
      <Badge className="bg-red-500">Banned</Badge>
    );
  };

  const handleViewDetails = (account) => {
    setSelectedAccount(account);
    setShowDetailsModal(true);
  };

  const handleUpdateRole = (accountId, newRole) => {
    setAccounts(
      accounts.map((account) =>
        account.id === accountId ? { ...account, role: newRole } : account
      )
    );
    // Update selected account if it's the one being modified
    if (selectedAccount && selectedAccount.id === accountId) {
      setSelectedAccount({ ...selectedAccount, role: newRole });
    }
  };

  const handleToggleBan = (accountId) => {
    setAccounts(
      accounts.map((account) =>
        account.id === accountId
          ? {
              ...account,
              status: account.status === "active" ? "banned" : "active",
            }
          : account
      )
    );
    // Update selected account if it's the one being modified
    if (selectedAccount && selectedAccount.id === accountId) {
      const newStatus =
        selectedAccount.status === "active" ? "banned" : "active";
      setSelectedAccount({ ...selectedAccount, status: newStatus });
    }
  };

  const handleAssignAccount = (newAccount) => {
    setAccounts((prev) => [...prev, newAccount]);
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
            Manage Accounts
          </h1>
          <p className="text-gray-600 mt-1">
            Manage user accounts, roles, and permissions
          </p>
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
                      placeholder="Search by name, email, or username..."
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      className="pl-10"
                    />
                  </div>

                  <Select value={roleFilter} onValueChange={setRoleFilter}>
                    <SelectTrigger className="w-40">
                      <Filter className="h-4 w-4 mr-2" />
                      <SelectValue placeholder="Filter by role" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">All Roles</SelectItem>
                      <SelectItem value="manager">Manager</SelectItem>
                      <SelectItem value="designer">Designer</SelectItem>
                      <SelectItem value="qc">QC</SelectItem>
                      <SelectItem value="seller">Seller</SelectItem>
                      <SelectItem value="staff">Staff</SelectItem>
                    </SelectContent>
                  </Select>

                  <Select value={statusFilter} onValueChange={setStatusFilter}>
                    <SelectTrigger className="w-40">
                      <SelectValue placeholder="Filter by status" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">All Status</SelectItem>
                      <SelectItem value="active">Active</SelectItem>
                      <SelectItem value="banned">Banned</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <Button
                  onClick={() => setShowAssignModal(true)}
                  className="bg-blue-600 hover:bg-blue-700"
                >
                  <UserPlus className="h-4 w-4 mr-2" />
                  Assign Account
                </Button>
              </div>
            </div>

            {/* Accounts Table */}
            <div className="bg-white rounded-lg shadow">
              <div className="px-6 py-4 border-b border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900">
                  User Accounts ({filteredAccounts.length})
                </h2>
                <p className="text-sm text-gray-600">
                  Manage and monitor all user accounts
                </p>
              </div>
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Account ID</TableHead>
                      <TableHead>Full Name</TableHead>
                      <TableHead>Username</TableHead>
                      <TableHead>Email</TableHead>
                      <TableHead>Role</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Created Date</TableHead>
                      <TableHead>Last Login</TableHead>
                      <TableHead>Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {filteredAccounts.map((account) => (
                      <TableRow key={account.id}>
                        <TableCell className="font-medium">
                          {account.id}
                        </TableCell>
                        <TableCell>{account.fullName}</TableCell>
                        <TableCell>{account.username}</TableCell>
                        <TableCell>{account.email}</TableCell>
                        <TableCell>{getRoleBadge(account.role)}</TableCell>
                        <TableCell>{getStatusBadge(account.status)}</TableCell>
                        <TableCell>{account.createdDate}</TableCell>
                        <TableCell>{account.lastLogin}</TableCell>
                        <TableCell>
                          <div className="flex gap-2">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => handleViewDetails(account)}
                            >
                              <Eye className="h-4 w-4 mr-1" />
                              View
                            </Button>
                            <Button
                              variant={
                                account.status === "active"
                                  ? "destructive"
                                  : "default"
                              }
                              size="sm"
                              onClick={() => handleToggleBan(account.id)}
                            >
                              {account.status === "active" ? (
                                <>
                                  <Ban className="h-4 w-4 mr-1" />
                                  Ban
                                </>
                              ) : (
                                <>
                                  <CheckCircle className="h-4 w-4 mr-1" />
                                  Unban
                                </>
                              )}
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </div>
          </div>
        </main>
      </div>

      {/* Account Details Modal */}
      <AccountDetailsModal
        account={selectedAccount}
        isOpen={showDetailsModal}
        onClose={() => {
          setShowDetailsModal(false);
          setSelectedAccount(null);
        }}
        onUpdateRole={handleUpdateRole}
        onToggleBan={handleToggleBan}
      />

      {/* Assign Account Modal */}
      <AssignAccountModal
        isOpen={showAssignModal}
        onClose={() => setShowAssignModal(false)}
        onAssignAccount={handleAssignAccount}
      />
    </div>
  );
}
