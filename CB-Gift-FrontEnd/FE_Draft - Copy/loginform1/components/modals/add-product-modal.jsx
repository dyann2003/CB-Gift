"use client";

import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useToast } from "@/hooks/use-toast";

export default function AddProductModal({
  isOpen,
  onClose,
  onAddProduct,
  categories,
}) {
  const { toast } = useToast();
  const [formData, setFormData] = useState({
    productName: "",
    description: "",
    category: "",
    image: "",
    baseCost: "",
    shipCost: "",
    size: "",
    thickness: "",
    weight: "",
    layer: "",
    customShape: "",
    length: "",
    width: "",
    height: "",
    scanTiktok: "",
  });

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSelectChange = (name, value) => {
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = (e) => {
    e.preventDefault();

    // Validate required fields
    if (!formData.productName || !formData.category || !formData.baseCost) {
      toast({
        title: "Validation Error",
        description: "Please fill in all required fields",
        variant: "destructive",
      });
      return;
    }

    // Create new product
    const newProduct = {
      id: `PRD-${Math.random().toString(36).substr(2, 9).toUpperCase()}`,
      productName: formData.productName,
      description: formData.description,
      category: formData.category,
      image: formData.image || "/diverse-products-still-life.png",
      status: "Active",
      basePrice: formData.baseCost,
      baseCost: formData.baseCost,
      shipCost: formData.shipCost,
      createdDate: new Date().toISOString().split("T")[0],
    };

    toast({
      title: "Success",
      description: "Product added successfully",
      variant: "default",
    });

    onAddProduct(newProduct);
    setFormData({
      productName: "",
      description: "",
      category: "",
      image: "",
      baseCost: "",
      shipCost: "",
      size: "",
      thickness: "",
      weight: "",
      layer: "",
      customShape: "",
      length: "",
      width: "",
      height: "",
      scanTiktok: "",
    });
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-3xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Add New Product</DialogTitle>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Product Information Section */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold text-gray-900">
              Product Information
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Product Name *
                </Label>
                <Input
                  name="productName"
                  placeholder="Enter product name"
                  value={formData.productName}
                  onChange={handleInputChange}
                  className="mt-1"
                  required
                />
              </div>
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Category *
                </Label>
                <Select
                  value={formData.category}
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
              </div>
              <div className="md:col-span-2">
                <Label className="text-sm font-medium text-gray-700">
                  Description
                </Label>
                <Input
                  name="description"
                  placeholder="Enter product description"
                  value={formData.description}
                  onChange={handleInputChange}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Image URL
                </Label>
                <Input
                  name="image"
                  placeholder="Enter image URL"
                  value={formData.image}
                  onChange={handleInputChange}
                  className="mt-1"
                />
              </div>
            </div>
          </div>

          {/* Product Variables Section */}
          <div className="space-y-4">
            <h3 className="text-lg font-semibold text-gray-900">
              Product Variables
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Size
                </Label>
                <Input
                  name="size"
                  placeholder="e.g., 5x5cm"
                  value={formData.size}
                  onChange={handleInputChange}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Layer
                </Label>
                <Input
                  name="layer"
                  placeholder="Enter layer"
                  value={formData.layer}
                  onChange={handleInputChange}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Thickness
                </Label>
                <Input
                  name="thickness"
                  placeholder="Enter thickness"
                  value={formData.thickness}
                  onChange={handleInputChange}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Custom Shape
                </Label>
                <Input
                  name="customShape"
                  placeholder="Enter custom shape"
                  value={formData.customShape}
                  onChange={handleInputChange}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Weight (gram)
                </Label>
                <Input
                  name="weight"
                  placeholder="Enter weight"
                  type="number"
                  value={formData.weight}
                  onChange={handleInputChange}
                  className="mt-1"
                />
              </div>
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Dimension(L×W×H) (cm)
                </Label>
                <div className="grid grid-cols-3 gap-2 mt-1">
                  <Input
                    name="length"
                    placeholder="Length"
                    type="number"
                    value={formData.length}
                    onChange={handleInputChange}
                  />
                  <Input
                    name="width"
                    placeholder="Width"
                    type="number"
                    value={formData.width}
                    onChange={handleInputChange}
                  />
                  <Input
                    name="height"
                    placeholder="Height"
                    type="number"
                    value={formData.height}
                    onChange={handleInputChange}
                  />
                </div>
              </div>
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Base Cost *
                </Label>
                <Input
                  name="baseCost"
                  placeholder="Enter base cost"
                  type="number"
                  value={formData.baseCost}
                  onChange={handleInputChange}
                  className="mt-1"
                  required
                />
              </div>
              <div>
                <Label className="text-sm font-medium text-gray-700">
                  Ship Cost
                </Label>
                <Input
                  name="shipCost"
                  placeholder="Enter ship cost"
                  type="number"
                  value={formData.shipCost}
                  onChange={handleInputChange}
                  className="mt-1"
                />
              </div>
              <div className="md:col-span-2">
                <Label className="text-sm font-medium text-gray-700">
                  Scan Tiktok
                </Label>
                <Input
                  name="scanTiktok"
                  placeholder="Enter scan tiktok code"
                  value={formData.scanTiktok}
                  onChange={handleInputChange}
                  className="mt-1"
                />
              </div>
            </div>
          </div>

          {/* Buttons */}
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" className="bg-blue-600 hover:bg-blue-700">
              Save Product
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
