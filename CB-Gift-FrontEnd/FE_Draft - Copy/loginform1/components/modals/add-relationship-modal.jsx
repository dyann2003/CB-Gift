"use client"

import { useState, useEffect } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Card, CardContent } from "@/components/ui/card"
import { Search, Plus } from "lucide-react"

export default function AddRelationshipModal({ isOpen, onClose, onAdd }) {
  const [sellers, setSellers] = useState([])
  const [designers, setDesigners] = useState([])
  const [selectedSeller, setSelectedSeller] = useState("")
  const [selectedDesigner, setSelectedDesigner] = useState("")
  const [sellerSearch, setSellerSearch] = useState("")
  const [designerSearch, setDesignerSearch] = useState("")
  const [notes, setNotes] = useState("")
  const [loading, setLoading] = useState(false)

  // Mock data - replace with actual API calls
  useEffect(() => {
    if (isOpen) {
      const mockSellers = [
        { id: 1, name: "John Seller", email: "john@seller.com" },
        { id: 2, name: "Mike Seller", email: "mike@seller.com" },
        { id: 3, name: "Sarah Seller", email: "sarah@seller.com" },
        { id: 4, name: "Tom Seller", email: "tom@seller.com" },
      ]

      const mockDesigners = [
        { id: 1, name: "Alice Designer", email: "alice@designer.com" },
        { id: 2, name: "Bob Designer", email: "bob@designer.com" },
        { id: 3, name: "Carol Designer", email: "carol@designer.com" },
        { id: 4, name: "David Designer", email: "david@designer.com" },
      ]

      setSellers(mockSellers)
      setDesigners(mockDesigners)
    }
  }, [isOpen])

  const filteredSellers = sellers.filter(
    (seller) =>
      seller.name.toLowerCase().includes(sellerSearch.toLowerCase()) ||
      seller.email.toLowerCase().includes(sellerSearch.toLowerCase()),
  )

  const filteredDesigners = designers.filter(
    (designer) =>
      designer.name.toLowerCase().includes(designerSearch.toLowerCase()) ||
      designer.email.toLowerCase().includes(designerSearch.toLowerCase()),
  )

  const handleSubmit = async (e) => {
    e.preventDefault()

    if (!selectedSeller || !selectedDesigner) {
      alert("Please select both seller and designer")
      return
    }

    if (selectedSeller === selectedDesigner) {
      alert("Seller and designer cannot be the same person")
      return
    }

    setLoading(true)
    try {
      // Simulate API call
      await new Promise((resolve) => setTimeout(resolve, 1000))

      const seller = sellers.find((s) => s.id.toString() === selectedSeller)
      const designer = designers.find((d) => d.id.toString() === selectedDesigner)

      const newRelationship = {
        id: Date.now(),
        seller,
        designer,
        status: "active",
        createdAt: new Date().toISOString().split("T")[0],
        projectsCount: 0,
        notes,
      }

      onAdd(newRelationship)
      handleClose()

      alert("Relationship created successfully!")
    } catch (error) {
      alert("Failed to create relationship. Please try again.")
    } finally {
      setLoading(false)
    }
  }

  const handleClose = () => {
    setSelectedSeller("")
    setSelectedDesigner("")
    setSellerSearch("")
    setDesignerSearch("")
    setNotes("")
    onClose()
  }

  return (
    <Dialog open={isOpen} onOpenChange={handleClose}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Add New Relationship</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Seller Selection */}
            <Card>
              <CardContent className="p-4">
                <Label className="text-sm font-medium text-gray-700 mb-2 block">Select Seller</Label>
                <div className="space-y-3">
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                    <Input
                      placeholder="Search sellers..."
                      value={sellerSearch}
                      onChange={(e) => setSellerSearch(e.target.value)}
                      className="pl-10"
                    />
                  </div>
                  <Select value={selectedSeller} onValueChange={setSelectedSeller}>
                    <SelectTrigger>
                      <SelectValue placeholder="Choose a seller" />
                    </SelectTrigger>
                    <SelectContent>
                      {filteredSellers.map((seller) => (
                        <SelectItem key={seller.id} value={seller.id.toString()}>
                          <div>
                            <p className="font-medium">{seller.name}</p>
                            <p className="text-xs text-gray-500">{seller.email}</p>
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </CardContent>
            </Card>

            {/* Designer Selection */}
            <Card>
              <CardContent className="p-4">
                <Label className="text-sm font-medium text-gray-700 mb-2 block">Select Designer</Label>
                <div className="space-y-3">
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                    <Input
                      placeholder="Search designers..."
                      value={designerSearch}
                      onChange={(e) => setDesignerSearch(e.target.value)}
                      className="pl-10"
                    />
                  </div>
                  <Select value={selectedDesigner} onValueChange={setSelectedDesigner}>
                    <SelectTrigger>
                      <SelectValue placeholder="Choose a designer" />
                    </SelectTrigger>
                    <SelectContent>
                      {filteredDesigners.map((designer) => (
                        <SelectItem key={designer.id} value={designer.id.toString()}>
                          <div>
                            <p className="font-medium">{designer.name}</p>
                            <p className="text-xs text-gray-500">{designer.email}</p>
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Notes */}
          <div>
            <Label htmlFor="notes" className="text-sm font-medium text-gray-700">
              Notes (Optional)
            </Label>
            <textarea
              id="notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              className="w-full mt-1 p-2 border border-gray-300 rounded-md resize-none"
              rows={3}
              placeholder="Add any notes about this relationship..."
            />
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-3">
            <Button type="button" variant="outline" onClick={handleClose} disabled={loading}>
              Cancel
            </Button>
            <Button type="submit" disabled={loading}>
              <Plus className="h-4 w-4 mr-1" />
              {loading ? "Creating..." : "Create Relationship"}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
