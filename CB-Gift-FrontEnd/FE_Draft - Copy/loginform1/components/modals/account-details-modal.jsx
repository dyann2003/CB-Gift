"use client"

import { useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Label } from "@/components/ui/label"
import { Separator } from "@/components/ui/separator"
import { User, Mail, Calendar, Clock, Shield, Save, X } from "lucide-react"

export default function AccountDetailsModal({ account, isOpen, onClose, onUpdateRole, onToggleBan }) {
  const [selectedRole, setSelectedRole] = useState(account?.role || "")
  const [isEditing, setIsEditing] = useState(false)

  if (!account) return null

  const getRoleBadge = (role) => {
    const roleColors = {
      manager: "bg-purple-500",
      designer: "bg-blue-500",
      qc: "bg-green-500",
      seller: "bg-orange-500",
      staff: "bg-gray-500",
    }
    return <Badge className={roleColors[role] || "bg-gray-500"}>{role.toUpperCase()}</Badge>
  }

  const getStatusBadge = (status) => {
    return status === "active" ? (
      <Badge className="bg-green-500">Active</Badge>
    ) : (
      <Badge className="bg-red-500">Banned</Badge>
    )
  }

  const handleSaveRole = () => {
    if (selectedRole !== account.role) {
      onUpdateRole(account.id, selectedRole)
    }
    setIsEditing(false)
  }

  const handleCancel = () => {
    setSelectedRole(account.role)
    setIsEditing(false)
  }

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <User className="h-5 w-5" />
            Account Details
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-6">
          {/* Basic Information */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-4">
              <div>
                <Label className="text-sm font-medium text-gray-500">Account ID</Label>
                <p className="text-lg font-semibold">{account.id}</p>
              </div>

              <div>
                <Label className="text-sm font-medium text-gray-500">Full Name</Label>
                <p className="text-lg">{account.fullName}</p>
              </div>

              <div>
                <Label className="text-sm font-medium text-gray-500">Username</Label>
                <p className="text-lg font-mono">{account.username}</p>
              </div>

              <div className="flex items-center gap-2">
                <Mail className="h-4 w-4 text-gray-500" />
                <div>
                  <Label className="text-sm font-medium text-gray-500">Email</Label>
                  <p className="text-lg">{account.email}</p>
                </div>
              </div>
            </div>

            <div className="space-y-4">
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4 text-gray-500" />
                <div>
                  <Label className="text-sm font-medium text-gray-500">Created Date</Label>
                  <p className="text-lg">{account.createdDate}</p>
                </div>
              </div>

              <div className="flex items-center gap-2">
                <Clock className="h-4 w-4 text-gray-500" />
                <div>
                  <Label className="text-sm font-medium text-gray-500">Last Login</Label>
                  <p className="text-lg">{account.lastLogin}</p>
                </div>
              </div>

              <div>
                <Label className="text-sm font-medium text-gray-500">Account Status</Label>
                <div className="mt-1">{getStatusBadge(account.status)}</div>
              </div>
            </div>
          </div>

          <Separator />

          {/* Role Management */}
          <div className="space-y-4">
            <div className="flex items-center gap-2">
              <Shield className="h-5 w-5 text-gray-700" />
              <h3 className="text-lg font-semibold">Role Management</h3>
            </div>

            <div className="bg-gray-50 p-4 rounded-lg">
              <div className="flex items-center justify-between">
                <div>
                  <Label className="text-sm font-medium text-gray-500">Current Role</Label>
                  {!isEditing ? (
                    <div className="mt-1">{getRoleBadge(account.role)}</div>
                  ) : (
                    <div className="mt-2">
                      <Select value={selectedRole} onValueChange={setSelectedRole}>
                        <SelectTrigger className="w-48">
                          <SelectValue placeholder="Select role" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="manager">Manager</SelectItem>
                          <SelectItem value="designer">Designer</SelectItem>
                          <SelectItem value="qc">QC</SelectItem>
                          <SelectItem value="seller">Seller</SelectItem>
                          <SelectItem value="staff">Staff</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                  )}
                </div>

                <div className="flex gap-2">
                  {!isEditing ? (
                    <Button variant="outline" onClick={() => setIsEditing(true)}>
                      Edit Role
                    </Button>
                  ) : (
                    <>
                      <Button variant="outline" size="sm" onClick={handleCancel}>
                        <X className="h-4 w-4 mr-1" />
                        Cancel
                      </Button>
                      <Button size="sm" onClick={handleSaveRole}>
                        <Save className="h-4 w-4 mr-1" />
                        Save
                      </Button>
                    </>
                  )}
                </div>
              </div>
            </div>
          </div>

          <Separator />

          {/* Account Actions */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold">Account Actions</h3>
            <div className="flex gap-3">
              <Button
                variant={account.status === "active" ? "destructive" : "default"}
                onClick={() => onToggleBan(account.id)}
              >
                {account.status === "active" ? "Ban Account" : "Unban Account"}
              </Button>
            </div>
          </div>
        </div>

        <div className="flex justify-end pt-4">
          <Button variant="outline" onClick={onClose}>
            Close
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
