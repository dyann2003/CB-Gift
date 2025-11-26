"use client";
import { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  ChevronLeft,
  ChevronRight,
  Package,
  MapPin,
  ShoppingCart,
  DollarSign,
} from "lucide-react";
import apiClient from "@/lib/apiClient";

export default function OrderEditModal({ isOpen, onClose, order, onSave }) {
  const [currentStep, setCurrentStep] = useState(1);
  const [editData, setEditData] = useState(null);
  const [showConfirmSave, setShowConfirmSave] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [showErrorDialog, setShowErrorDialog] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const [showSuccessDialog, setShowSuccessDialog] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  // Location data
  const [provinces, setProvinces] = useState([]);
  const [districts, setDistricts] = useState([]);
  const [wards, setWards] = useState([]);
  const [loadingLocations, setLoadingLocations] = useState(false);

  // Products
  const [availableProducts, setAvailableProducts] = useState([]);
  const [loadingProducts, setLoadingProducts] = useState(false);
  const [selectedNewProduct, setSelectedNewProduct] = useState(null);

  useEffect(() => {
    if (!isOpen || !order) return;

    // Initialize edit data from order
    setEditData({
      id: order.id,
      orderId: order.orderId,
      customerInfo: { ...order.customerInfo },
      products: [...order.products],
      orderNotes: order.orderNotes || "",
    });

    setCurrentStep(1);
    fetchProvinces();
    fetchProducts();
  }, [isOpen, order]);

  const fetchProvinces = async () => {
    setLoadingLocations(true);
    try {
      const res = await fetch("https://localhost:7015/api/Location/provinces", {
        credentials: "include",
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      const normalized = data.map((p) => ({
        id: p.ProvinceID,
        name: p.ProvinceName,
      }));
      setProvinces(normalized);
    } catch (err) {
      console.error("Failed to fetch provinces:", err);
      setErrorMessage("Failed to load provinces");
      setShowErrorDialog(true);
    } finally {
      setLoadingLocations(false);
    }
  };

  const fetchDistricts = async (provinceId) => {
    if (!provinceId) {
      setDistricts([]);
      return;
    }
    setLoadingLocations(true);
    try {
      const res = await fetch(
        `https://localhost:7015/api/Location/districts/${provinceId}`,
        {
          credentials: "include",
        }
      );
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      const normalized = data.map((d) => ({
        id: d.DistrictID,
        name: d.DistrictName,
      }));
      setDistricts(normalized);
      setWards([]);
    } catch (err) {
      console.error("Failed to fetch districts:", err);
    } finally {
      setLoadingLocations(false);
    }
  };

  const fetchWards = async (districtId) => {
    if (!districtId) {
      setWards([]);
      return;
    }
    setLoadingLocations(true);
    try {
      const res = await fetch(
        `https://localhost:7015/api/Location/wards/${districtId}`,
        {
          credentials: "include",
        }
      );
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      const normalized = data.map((w) => ({
        id: w.WardCode,
        name: w.WardName,
      }));
      setWards(normalized);
    } catch (err) {
      console.error("Failed to fetch wards:", err);
    } finally {
      setLoadingLocations(false);
    }
  };

  const fetchProducts = async () => {
    setLoadingProducts(true);
    try {
      const res = await fetch(`${apiClient.defaults.baseURL}/api/Product`, {
        credentials: "include",
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      setAvailableProducts(data || []);
    } catch (err) {
      console.error("Failed to fetch products:", err);
    } finally {
      setLoadingProducts(false);
    }
  };

  const handleCustomerInfoChange = (field, value) => {
    setEditData((prev) => ({
      ...prev,
      customerInfo: { ...prev.customerInfo, [field]: value },
    }));

    // Load districts/wards when province changes
    if (field === "provinceId") {
      fetchDistricts(value);
    }
    if (field === "districtId") {
      fetchWards(value);
    }
  };

  const handleProductChange = (index, field, value) => {
    setEditData((prev) => ({
      ...prev,
      products: prev.products.map((p, i) =>
        i === index ? { ...p, [field]: value } : p
      ),
    }));
  };

  const handleAddProductToOrder = () => {
    if (!selectedNewProduct) return;

    const newProduct = {
      ...selectedNewProduct,
      quantity: 1,
      size: "",
      accessory: "",
      activeTTS: false,
      note: "",
      linkImg: selectedNewProduct.linkImg || "/placeholder.svg",
      linkThanksCard: "/placeholder.svg",
      linkFileDesign: "/placeholder.svg",
    };

    setEditData((prev) => ({
      ...prev,
      products: [...prev.products, newProduct],
    }));

    setSelectedNewProduct(null);
  };

  const handleRemoveProduct = (index) => {
    setEditData((prev) => ({
      ...prev,
      products: prev.products.filter((_, i) => i !== index),
    }));
  };

  const handleNext = () => {
    // Validation
    if (currentStep === 1) {
      const required = [
        "name",
        "phone",
        "email",
        "address",
        "provinceId",
        "districtId",
        "wardId",
      ];
      const missing = required.filter(
        (f) => !editData.customerInfo[f]?.toString().trim()
      );

      if (missing.length > 0) {
        setErrorMessage(
          `Please fill all required fields: ${missing.join(", ")}`
        );
        setShowErrorDialog(true);
        return;
      }

      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(editData.customerInfo.email)) {
        setErrorMessage("Please enter a valid email address.");
        setShowErrorDialog(true);
        return;
      }
    }

    if (currentStep === 3 && editData.products.length === 0) {
      setErrorMessage("Please add at least one product to the order.");
      setShowErrorDialog(true);
      return;
    }

    if (currentStep < 4) {
      setCurrentStep(currentStep + 1);
    }
  };

  const handleBack = () => {
    if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
    }
  };

  const handleSave = async () => {
    setShowConfirmSave(false);
    setIsSaving(true);

    try {
      // Call onSave callback with updated data
      await onSave(editData);

      setSuccessMessage("Order updated successfully!");
      setShowSuccessDialog(true);

      setTimeout(() => {
        setShowSuccessDialog(false);
        onClose();
      }, 2000);
    } catch (err) {
      setErrorMessage(err.message || "Failed to save order");
      setShowErrorDialog(true);
    } finally {
      setIsSaving(false);
    }
  };

  if (!editData) return null;

  // Calculate total
  const totalAmount = editData.products.reduce(
    (sum, p) => sum + (p.price || 0) * p.quantity,
    0
  );

  return (
    <>
      <Dialog open={isOpen} onOpenChange={onClose}>
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <Package className="h-5 w-5" />
              Edit Order - {editData.orderId}
            </DialogTitle>
          </DialogHeader>

          {/* Step Indicator */}
          <div className="flex justify-between items-center mb-6 px-2">
            {[1, 2, 3, 4].map((step) => (
              <div key={step} className="flex items-center flex-1">
                <div
                  className={`h-10 w-10 rounded-full flex items-center justify-center font-semibold text-sm ${
                    currentStep >= step
                      ? "bg-blue-600 text-white"
                      : "bg-gray-200 text-gray-600"
                  }`}
                >
                  {step}
                </div>
                {step < 4 && (
                  <div
                    className={`h-1 flex-1 mx-2 ${
                      currentStep > step ? "bg-blue-600" : "bg-gray-200"
                    }`}
                  />
                )}
              </div>
            ))}
          </div>

          {/* Step Labels */}
          <div className="flex justify-between text-xs text-gray-600 mb-6">
            <span>Customer Info</span>
            <span>Add Products</span>
            <span>Product Details</span>
            <span>Review & Save</span>
          </div>

          {/* Step 1: Customer Information */}
          {currentStep === 1 && (
            <div className="space-y-4">
              <h3 className="font-semibold text-lg flex items-center gap-2 mb-4">
                <MapPin className="h-5 w-5" />
                Customer Information
              </h3>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {/* Name */}
                <div>
                  <Label>Name *</Label>
                  <Input
                    value={editData.customerInfo.name}
                    onChange={(e) =>
                      handleCustomerInfoChange("name", e.target.value)
                    }
                    placeholder="Full name"
                    className="mt-1"
                  />
                </div>

                {/* Phone */}
                <div>
                  <Label>Phone *</Label>
                  <Input
                    value={editData.customerInfo.phone}
                    onChange={(e) =>
                      handleCustomerInfoChange("phone", e.target.value)
                    }
                    placeholder="Phone number"
                    className="mt-1"
                  />
                </div>

                {/* Email */}
                <div className="sm:col-span-2">
                  <Label>Email *</Label>
                  <Input
                    value={editData.customerInfo.email}
                    onChange={(e) =>
                      handleCustomerInfoChange("email", e.target.value)
                    }
                    placeholder="Email address"
                    className="mt-1"
                  />
                </div>

                {/* Address */}
                <div className="sm:col-span-2">
                  <Label>Address *</Label>
                  <Input
                    value={editData.customerInfo.address}
                    onChange={(e) =>
                      handleCustomerInfoChange("address", e.target.value)
                    }
                    placeholder="Street address"
                    className="mt-1"
                  />
                </div>

                {/* Address Line 2 */}
                <div className="sm:col-span-2">
                  <Label>Address Line 2</Label>
                  <Input
                    value={editData.customerInfo.address1 || ""}
                    onChange={(e) =>
                      handleCustomerInfoChange("address1", e.target.value)
                    }
                    placeholder="Apartment, floor, etc."
                    className="mt-1"
                  />
                </div>

                {/* Zipcode */}
                <div>
                  <Label>Zipcode *</Label>
                  <Input
                    value={editData.customerInfo.zipcode || ""}
                    onChange={(e) =>
                      handleCustomerInfoChange("zipcode", e.target.value)
                    }
                    placeholder="Postal code"
                    className="mt-1"
                  />
                </div>

                {/* Province */}
                <div>
                  <Label>Province *</Label>
                  <Select
                    value={editData.customerInfo.provinceId || ""}
                    onValueChange={(val) =>
                      handleCustomerInfoChange("provinceId", val)
                    }
                  >
                    <SelectTrigger className="mt-1">
                      <SelectValue placeholder="Select Province" />
                    </SelectTrigger>
                    <SelectContent>
                      {provinces.map((p) => (
                        <SelectItem key={p.id} value={p.id}>
                          {p.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                {/* District */}
                <div>
                  <Label>District *</Label>
                  <Select
                    value={editData.customerInfo.districtId || ""}
                    onValueChange={(val) =>
                      handleCustomerInfoChange("districtId", val)
                    }
                  >
                    <SelectTrigger className="mt-1">
                      <SelectValue placeholder="Select District" />
                    </SelectTrigger>
                    <SelectContent>
                      {districts.map((d) => (
                        <SelectItem key={d.id} value={d.id}>
                          {d.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                {/* Ward */}
                <div>
                  <Label>Ward *</Label>
                  <Select
                    value={editData.customerInfo.wardId || ""}
                    onValueChange={(val) =>
                      handleCustomerInfoChange("wardId", val)
                    }
                  >
                    <SelectTrigger className="mt-1">
                      <SelectValue placeholder="Select Ward" />
                    </SelectTrigger>
                    <SelectContent>
                      {wards.map((w) => (
                        <SelectItem key={w.id} value={w.id}>
                          {w.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </div>
          )}

          {/* Step 2: Add Products */}
          {currentStep === 2 && (
            <div className="space-y-4">
              <h3 className="font-semibold text-lg flex items-center gap-2 mb-4">
                <ShoppingCart className="h-5 w-5" />
                Add Products to Order
              </h3>

              <div>
                <Label>Select Product to Add</Label>
                <Select
                  value={selectedNewProduct?.id || ""}
                  onValueChange={(id) => {
                    const product = availableProducts.find(
                      (p) => (p.id || p.productId) === id
                    );
                    setSelectedNewProduct(product);
                  }}
                >
                  <SelectTrigger className="mt-1">
                    <SelectValue placeholder="Choose a product" />
                  </SelectTrigger>
                  <SelectContent className="max-h-60">
                    {availableProducts.map((product) => (
                      <SelectItem
                        key={product.id || product.productId}
                        value={product.id || product.productId}
                      >
                        {product.productName || product.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {selectedNewProduct && (
                <div className="bg-blue-50 p-4 rounded-lg border border-blue-200">
                  <p className="text-sm">
                    <strong>Selected:</strong>{" "}
                    {selectedNewProduct.productName || selectedNewProduct.name}
                  </p>
                  <Button
                    onClick={handleAddProductToOrder}
                    className="mt-3 bg-blue-600 hover:bg-blue-700"
                  >
                    Add to Order
                  </Button>
                </div>
              )}

              {/* Current Products */}
              <div className="mt-6">
                <h4 className="font-semibold text-sm mb-3">
                  Current Products in Order
                </h4>
                <div className="space-y-2 max-h-64 overflow-y-auto">
                  {editData.products.map((product, idx) => (
                    <div
                      key={idx}
                      className="flex items-center justify-between p-3 bg-gray-50 rounded border"
                    >
                      <div className="flex-1">
                        <p className="text-sm font-medium">{product.name}</p>
                        <p className="text-xs text-gray-600">
                          Qty: {product.quantity}
                        </p>
                      </div>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleRemoveProduct(idx)}
                        className="text-red-600 hover:text-red-700"
                      >
                        Remove
                      </Button>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* Step 3: Product Details */}
          {currentStep === 3 && (
            <div className="space-y-4">
              <h3 className="font-semibold text-lg flex items-center gap-2 mb-4">
                <Package className="h-5 w-5" />
                Product Configuration
              </h3>

              <div className="space-y-6 max-h-96 overflow-y-auto">
                {editData.products.map((product, idx) => (
                  <div key={idx} className="border rounded-lg p-4 bg-gray-50">
                    <h4 className="font-semibold text-sm mb-4">
                      {product.name} (Qty: {product.quantity})
                    </h4>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                      {/* Quantity */}
                      <div>
                        <Label>Quantity *</Label>
                        <Input
                          type="number"
                          min="1"
                          value={product.quantity}
                          onChange={(e) =>
                            handleProductChange(
                              idx,
                              "quantity",
                              Number.parseInt(e.target.value) || 1
                            )
                          }
                          className="mt-1"
                        />
                      </div>

                      {/* Size */}
                      <div>
                        <Label>Size</Label>
                        <Input
                          value={product.size || ""}
                          onChange={(e) =>
                            handleProductChange(idx, "size", e.target.value)
                          }
                          placeholder="e.g., Medium"
                          className="mt-1"
                        />
                      </div>

                      {/* Accessory */}
                      <div>
                        <Label>Accessory</Label>
                        <Input
                          value={product.accessory || ""}
                          onChange={(e) =>
                            handleProductChange(
                              idx,
                              "accessory",
                              e.target.value
                            )
                          }
                          placeholder="e.g., Keyring"
                          className="mt-1"
                        />
                      </div>

                      {/* TTS Service */}
                      <div className="flex items-end">
                        <div className="w-full">
                          <Label>TTS Service (+$1)</Label>
                          <div className="flex items-center mt-2">
                            <Checkbox
                              checked={product.activeTTS || false}
                              onCheckedChange={(checked) =>
                                handleProductChange(idx, "activeTTS", checked)
                              }
                            />
                            <span className="ml-2 text-sm">
                              {product.activeTTS ? "Enabled" : "Disabled"}
                            </span>
                          </div>
                        </div>
                      </div>

                      {/* Product Price */}
                      <div>
                        <Label>Unit Price</Label>
                        <p className="text-sm font-semibold text-blue-600 mt-2">
                          ${product.price?.toFixed(2) || "0.00"}
                        </p>
                      </div>

                      {/* Total */}
                      <div>
                        <Label>Total</Label>
                        <p className="text-sm font-semibold text-green-600 mt-2">
                          $
                          {((product.price || 0) * product.quantity).toFixed(2)}
                        </p>
                      </div>
                    </div>

                    {/* Notes */}
                    <div className="mt-4">
                      <Label>Order Notes</Label>
                      <Textarea
                        value={product.note || ""}
                        onChange={(e) =>
                          handleProductChange(idx, "note", e.target.value)
                        }
                        placeholder="Add notes for this product"
                        rows={3}
                        className="mt-1"
                      />
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Step 4: Review & Save */}
          {currentStep === 4 && (
            <div className="space-y-4">
              <h3 className="font-semibold text-lg flex items-center gap-2 mb-4">
                <DollarSign className="h-5 w-5" />
                Review Order Summary
              </h3>

              <div className="bg-blue-50 p-4 rounded-lg">
                <h4 className="font-semibold text-sm mb-2">
                  Customer Information
                </h4>
                <div className="text-sm space-y-1 text-gray-700">
                  <p>
                    <strong>Name:</strong> {editData.customerInfo.name}
                  </p>
                  <p>
                    <strong>Email:</strong> {editData.customerInfo.email}
                  </p>
                  <p>
                    <strong>Phone:</strong> {editData.customerInfo.phone}
                  </p>
                  <p>
                    <strong>Address:</strong> {editData.customerInfo.address}
                  </p>
                  <p>
                    <strong>Location:</strong> {editData.customerInfo.wardName},{" "}
                    {editData.customerInfo.districtName},{" "}
                    {editData.customerInfo.provinceName}
                  </p>
                </div>
              </div>

              <div className="bg-amber-50 p-4 rounded-lg">
                <h4 className="font-semibold text-sm mb-3">Products</h4>
                <div className="space-y-2 text-sm">
                  {editData.products.map((product, idx) => (
                    <div key={idx} className="flex justify-between">
                      <span>
                        {product.name} x{product.quantity}
                      </span>
                      <span className="font-semibold">
                        ${((product.price || 0) * product.quantity).toFixed(2)}
                      </span>
                    </div>
                  ))}
                  <div className="border-t pt-2 mt-2 flex justify-between font-bold text-lg text-blue-600">
                    <span>Total</span>
                    <span>${totalAmount.toFixed(2)}</span>
                  </div>
                </div>
              </div>

              {editData.orderNotes && (
                <div className="bg-green-50 p-4 rounded-lg">
                  <h4 className="font-semibold text-sm mb-2">Order Notes</h4>
                  <p className="text-sm text-gray-700">{editData.orderNotes}</p>
                </div>
              )}
            </div>
          )}

          {/* Footer Navigation */}
          <DialogFooter className="gap-2 mt-6 flex justify-between">
            <Button
              variant="outline"
              onClick={handleBack}
              disabled={currentStep === 1}
            >
              <ChevronLeft className="h-4 w-4 mr-2" />
              Back
            </Button>

            <div className="flex gap-2">
              <Button variant="outline" onClick={onClose}>
                Cancel
              </Button>

              {currentStep < 4 ? (
                <Button
                  onClick={handleNext}
                  className="bg-blue-600 hover:bg-blue-700"
                >
                  Next
                  <ChevronRight className="h-4 w-4 ml-2" />
                </Button>
              ) : (
                <Button
                  onClick={() => setShowConfirmSave(true)}
                  className="bg-green-600 hover:bg-green-700"
                >
                  Save Changes
                </Button>
              )}
            </div>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Confirm Save Dialog */}
      <AlertDialog open={showConfirmSave} onOpenChange={setShowConfirmSave}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Confirm Save</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to save all changes to this order?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleSave}
              disabled={isSaving}
              className="bg-green-600"
            >
              {isSaving ? "Saving..." : "Save"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Error Dialog */}
      <AlertDialog open={showErrorDialog} onOpenChange={setShowErrorDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Error</AlertDialogTitle>
            <AlertDialogDescription>{errorMessage}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogAction>OK</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Success Dialog */}
      <AlertDialog open={showSuccessDialog} onOpenChange={setShowSuccessDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Success</AlertDialogTitle>
            <AlertDialogDescription>{successMessage}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogAction>OK</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
