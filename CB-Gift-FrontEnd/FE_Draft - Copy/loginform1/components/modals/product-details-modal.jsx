"use client";

import { useState, useEffect } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useToast } from "@/hooks/use-toast";

export default function ProductDetailsModal({
  product,
  isOpen,
  onClose,
  categories,
}) {
  const { toast } = useToast();
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState({});

  useEffect(() => {
    if (product) {
      setFormData(product);
    }
  }, [product]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSelectChange = (name, value) => {
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSave = () => {
    toast({
      title: "Success",
      description: "Product updated successfully",
      variant: "default",
    });
    setIsEditing(false);
  };

  const handleStatusToggle = () => {
    const newStatus = formData.status === "Active" ? "Inactive" : "Active";
    setFormData((prev) => ({ ...prev, status: newStatus }));
    toast({
      title: "Success",
      description: `Product status changed to ${newStatus}`,
      variant: "default",
    });
  };

  if (!product) return null;

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <div className="flex items-center justify-between w-full">
            <DialogTitle>Product Details - {product?.id}</DialogTitle>
            <Badge
              className={
                product?.status === "Active" ? "bg-green-500" : "bg-gray-500"
              }
            >
              {product?.status}
            </Badge>
          </div>
        </DialogHeader>

        <div className="space-y-6">
          {/* Product Image */}
          <div className="flex justify-center">
            <img
              src={formData.image || "/placeholder.svg"}
              alt={formData.productName}
              className="h-40 w-40 rounded-lg object-cover border border-gray-200"
            />
          </div>

          {/* Product Information Section */}
          <div className="space-y-4">
            <h4 className="font-semibold text-gray-900">Product Information</h4>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Product Name
                </Label>
                <Input
                  name="productName"
                  value={formData.productName || ""}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Category
                </Label>
                {isEditing ? (
                  <Select
                    value={formData.category || ""}
                    onValueChange={(value) =>
                      handleSelectChange("category", value)
                    }
                  >
                    <SelectTrigger className="mt-1">
                      <SelectValue placeholder="Select category" />
                    </SelectTrigger>
                    <SelectContent>
                      {categories.map((cat) => (
                        <SelectItem key={cat} value={cat}>
                          {cat}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                ) : (
                  <Input
                    name="category"
                    value={formData.category || ""}
                    disabled
                    className="mt-1"
                  />
                )}
              </div>
              <div className="md:col-span-2">
                <Label className="text-sm font-medium text-gray-700">
                  Description
                </Label>
                <Input
                  name="description"
                  value={formData.description || ""}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Image URL
                </Label>
                <Input
                  name="image"
                  value={formData.image || ""}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                  className="mt-1"
                />
              </div>
            </div>
          </div>

          {/* Product Variables Section */}
          <div className="space-y-4 bg-gray-50 p-4 rounded-lg">
            <h4 className="font-semibold text-gray-900">Product Variables</h4>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <Label className="text-xs font-medium text-gray-600">
                  Size
                </Label>
                <Input
                  name="size"
                  value={formData.size || ""}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-xs font-medium text-gray-600">
                  Layer
                </Label>
                <Input
                  name="layer"
                  value={formData.layer || ""}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-xs font-medium text-gray-600">
                  Thickness
                </Label>
                <Input
                  name="thickness"
                  value={formData.thickness || ""}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-xs font-medium text-gray-600">
                  Custom Shape
                </Label>
                <Input
                  name="customShape"
                  value={formData.customShape || ""}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-xs font-medium text-gray-600">
                  Weight (gram)
                </Label>
                <Input
                  name="weight"
                  value={formData.weight || ""}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                  type="number"
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-xs font-medium text-gray-600">
                  Dimension (L×W×H) cm
                </Label>
                <div className="grid grid-cols-3 gap-2 mt-1">
                  <Input
                    placeholder="Length"
                    value={formData.length || ""}
                    onChange={(e) =>
                      handleInputChange({
                        ...e,
                        target: { ...e.target, name: "length" },
                      })
                    }
                    disabled={!isEditing}
                  />
                  <Input
                    placeholder="Width"
                    value={formData.width || ""}
                    onChange={(e) =>
                      handleInputChange({
                        ...e,
                        target: { ...e.target, name: "width" },
                      })
                    }
                    disabled={!isEditing}
                  />
                  <Input
                    placeholder="Height"
                    value={formData.height || ""}
                    onChange={(e) =>
                      handleInputChange({
                        ...e,
                        target: { ...e.target, name: "height" },
                      })
                    }
                    disabled={!isEditing}
                  />
                </div>
              </div>
              <div>
                <Label className="text-xs font-medium text-gray-600">
                  Scan TikTok
                </Label>
                <Input
                  name="scanTiktok"
                  value={formData.scanTiktok || ""}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                  className="mt-1"
                />
              </div>
            </div>
          </div>

          {/* Pricing Section */}
          <div className="bg-gray-50 p-4 rounded-lg">
            <h4 className="font-medium mb-3 text-gray-900">Pricing</h4>
            <div className="grid grid-cols-3 gap-3">
              <div>
                <Label className="text-xs font-medium text-gray-600">
                  Base Cost
                </Label>
                <Input
                  name="baseCost"
                  value={formData.baseCost || ""}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-xs font-medium text-gray-600">
                  Base Price
                </Label>
                <Input
                  name="basePrice"
                  value={formData.basePrice || ""}
                  disabled
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-xs font-medium text-gray-600">
                  Ship Cost
                </Label>
                <Input
                  name="shipCost"
                  value={formData.shipCost || ""}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                  className="mt-1"
                />
              </div>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button variant="outline" onClick={onClose}>
              Close
            </Button>
            <Button
              variant={formData.status === "Active" ? "destructive" : "default"}
              onClick={handleStatusToggle}
            >
              {formData.status === "Active" ? "Deactivate" : "Activate"}
            </Button>
            {isEditing ? (
              <>
                <Button variant="outline" onClick={() => setIsEditing(false)}>
                  Cancel
                </Button>
                <Button
                  onClick={handleSave}
                  className="bg-blue-600 hover:bg-blue-700"
                >
                  Save Changes
                </Button>
              </>
            ) : (
              <Button
                onClick={() => setIsEditing(true)}
                className="bg-blue-600 hover:bg-blue-700"
              >
                Edit Product
              </Button>
            )}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
