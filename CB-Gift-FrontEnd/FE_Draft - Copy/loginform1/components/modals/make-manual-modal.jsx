"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
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
import { ChevronDown, ChevronUp } from "lucide-react";

// Mock product data
const availableProducts = [
  {
    id: 1,
    name: "Custom Acrylic Keychain",
    description: "High-quality acrylic keychain with custom design",
    image: "/acrylic-keychain.jpg",
    basePrice: 5.99,
    sizes: ["Small (2 inch)", "Medium (3 inch)", "Large (4 inch)"],
    accessories: ["None", "Keyring", "Phone Strap", "Carabiner"],
  },
  {
    id: 2,
    name: "Custom Acrylic Stand",
    description: "Durable acrylic display stand for photos or artwork",
    image: "/acrylic-stand.jpg",
    basePrice: 12.99,
    sizes: ["Small (4x6 inch)", "Medium (5x7 inch)", "Large (8x10 inch)"],
    accessories: ["None", "LED Base", "Rotating Base", "Weighted Base"],
  },
  {
    id: 3,
    name: "Custom Acrylic Charm",
    description: "Cute acrylic charm perfect for bags and accessories",
    image: "/acrylic-charm.jpg",
    basePrice: 3.99,
    sizes: ["Mini (1 inch)", "Small (1.5 inch)", "Medium (2 inch)"],
    accessories: ["None", "Bell", "Tassel", "Chain"],
  },
];

export default function MakeManualModal({ isOpen, onClose }) {
  const [currentStep, setCurrentStep] = useState(1);
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [expandedProducts, setExpandedProducts] = useState({});

  const [customerInfo, setCustomerInfo] = useState({
    name: "",
    phone: "",
    email: "",
    address: "",
    address1: "",
    zipcode: "",
    shipState: "",
    shipCity: "",
    shipCountry: "",
  });

  const [cartProducts, setCartProducts] = useState([]);
  const [currentProduct, setCurrentProduct] = useState(null);
  const [currentProductConfig, setCurrentProductConfig] = useState({
    size: "",
    accessory: "",
    linkThanksCard: null,
    linkFileDesign: null,
    quantity: 1,
    activeTTS: false,
    note: "",
    productPrice: 0,
  });

  const calculateProductPrice = () => {
    if (!currentProduct) return 0;
    const basePrice = currentProduct.basePrice;
    const quantity = Number.parseInt(currentProductConfig.quantity) || 1;
    const accessoryPrice =
      currentProductConfig.accessory !== "None" &&
      currentProductConfig.accessory
        ? 2.0
        : 0;
    return (basePrice + accessoryPrice) * quantity;
  };

  const calculateTotalMoney = () => {
    return cartProducts.reduce(
      (total, product) => total + product.totalPrice,
      0
    );
  };

  const handleNext = () => {
    if (currentStep < 3) {
      setCurrentStep(currentStep + 1);
    }
  };

  const handleBack = () => {
    if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
    }
  };

  const handleFileUpload = (field, event) => {
    const file = event.target.files[0];
    if (file) {
      setCurrentProductConfig((prev) => ({ ...prev, [field]: file }));
    }
  };

  const handleProductSelect = (product) => {
    setCurrentProduct(product);
    setCurrentProductConfig({
      size: "",
      accessory: "",
      linkThanksCard: null,
      linkFileDesign: null,
      quantity: 1,
      activeTTS: false,
      note: "",
      productPrice: 0,
    });
    setCurrentStep(3);
  };

  const handleAddToCart = () => {
    if (!currentProduct) return;

    const productPrice = calculateProductPrice();
    const productToAdd = {
      id: Date.now(), // Simple ID for cart item
      product: currentProduct,
      config: { ...currentProductConfig },
      totalPrice: productPrice,
    };

    setCartProducts((prev) => [...prev, productToAdd]);
    setCurrentProduct(null);
    setCurrentProductConfig({
      size: "",
      accessory: "",
      linkThanksCard: null,
      linkFileDesign: null,
      quantity: 1,
      activeTTS: false,
      note: "",
      productPrice: 0,
    });
  };

  const handleRemoveFromCart = (cartItemId) => {
    setCartProducts((prev) => prev.filter((item) => item.id !== cartItemId));
  };

  const handleMakeOrder = () => {
    setShowConfirmDialog(true);
  };

  const confirmMakeOrder = () => {
    console.log("Order created:", {
      customerInfo,
      products: cartProducts,
      totalAmount: calculateTotalMoney(),
    });
    setShowConfirmDialog(false);
    onClose();
    // Reset form
    setCurrentStep(1);
    setCustomerInfo({
      name: "",
      phone: "",
      email: "",
      address: "",
      address1: "",
      zipcode: "",
      shipState: "",
      shipCity: "",
      shipCountry: "",
    });
    setCartProducts([]);
    setCurrentProduct(null);
    setCurrentProductConfig({
      size: "",
      accessory: "",
      linkThanksCard: null,
      linkFileDesign: null,
      quantity: 1,
      activeTTS: false,
      note: "",
      productPrice: 0,
    });
  };

  const toggleProductDetails = (cartItemId) => {
    setExpandedProducts((prev) => ({
      ...prev,
      [cartItemId]: !prev[cartItemId],
    }));
  };

  const updateCartProduct = (cartItemId, field, value) => {
    setCartProducts((prev) =>
      prev.map((item) => {
        if (item.id === cartItemId) {
          const updatedConfig = { ...item.config, [field]: value };
          const basePrice = item.product.basePrice;
          const quantity = Number.parseInt(updatedConfig.quantity) || 1;
          const accessoryPrice =
            updatedConfig.accessory !== "None" && updatedConfig.accessory
              ? 2.0
              : 0;
          const totalPrice = (basePrice + accessoryPrice) * quantity;

          return {
            ...item,
            config: updatedConfig,
            totalPrice: totalPrice,
          };
        }
        return item;
      })
    );
  };

  const handleCartFileUpload = (cartItemId, field, event) => {
    const file = event.target.files[0];
    if (file) {
      updateCartProduct(cartItemId, field, file);
    }
  };

  const renderStep1 = () => (
    <div className="space-y-4">
      <h3 className="text-lg font-semibold">
        Step 1: End Customer Information
      </h3>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <Label htmlFor="name">Name *</Label>
          <Input
            id="name"
            value={customerInfo.name}
            onChange={(e) =>
              setCustomerInfo((prev) => ({ ...prev, name: e.target.value }))
            }
            placeholder="Enter customer name"
          />
        </div>
        <div>
          <Label htmlFor="phone">Phone *</Label>
          <Input
            id="phone"
            value={customerInfo.phone}
            onChange={(e) =>
              setCustomerInfo((prev) => ({ ...prev, phone: e.target.value }))
            }
            placeholder="Enter phone number"
          />
        </div>
        <div>
          <Label htmlFor="email">Email *</Label>
          <Input
            id="email"
            type="email"
            value={customerInfo.email}
            onChange={(e) =>
              setCustomerInfo((prev) => ({ ...prev, email: e.target.value }))
            }
            placeholder="Enter email address"
          />
        </div>
        <div>
          <Label htmlFor="address">Address *</Label>
          <Input
            id="address"
            value={customerInfo.address}
            onChange={(e) =>
              setCustomerInfo((prev) => ({ ...prev, address: e.target.value }))
            }
            placeholder="Enter address"
          />
        </div>
        <div>
          <Label htmlFor="address1">Address 1</Label>
          <Input
            id="address1"
            value={customerInfo.address1}
            onChange={(e) =>
              setCustomerInfo((prev) => ({ ...prev, address1: e.target.value }))
            }
            placeholder="Enter address line 2"
          />
        </div>
        <div>
          <Label htmlFor="zipcode">Zipcode *</Label>
          <Input
            id="zipcode"
            value={customerInfo.zipcode}
            onChange={(e) =>
              setCustomerInfo((prev) => ({ ...prev, zipcode: e.target.value }))
            }
            placeholder="Enter zipcode"
          />
        </div>
        <div>
          <Label htmlFor="shipState">Ship State *</Label>
          <Input
            id="shipState"
            value={customerInfo.shipState}
            onChange={(e) =>
              setCustomerInfo((prev) => ({
                ...prev,
                shipState: e.target.value,
              }))
            }
            placeholder="Enter state"
          />
        </div>
        <div>
          <Label htmlFor="shipCity">Ship City *</Label>
          <Input
            id="shipCity"
            value={customerInfo.shipCity}
            onChange={(e) =>
              setCustomerInfo((prev) => ({ ...prev, shipCity: e.target.value }))
            }
            placeholder="Enter city"
          />
        </div>
        <div>
          <Label htmlFor="shipCountry">Ship Country *</Label>
          <Input
            id="shipCountry"
            value={customerInfo.shipCountry}
            onChange={(e) =>
              setCustomerInfo((prev) => ({
                ...prev,
                shipCountry: e.target.value,
              }))
            }
            placeholder="Enter country"
          />
        </div>
      </div>
    </div>
  );

  const renderStep2 = () => (
    <div className="space-y-4">
      <h3 className="text-lg font-semibold">Step 2: Select Product</h3>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {availableProducts.map((product) => (
          <div
            key={product.id}
            className="border rounded-lg p-4 cursor-pointer hover:border-blue-500 hover:shadow-md transition-all"
            onClick={() => handleProductSelect(product)}
          >
            <img
              src={product.image || "/placeholder.svg"}
              alt={product.name}
              className="w-full h-32 object-cover rounded-md mb-3"
            />
            <h4 className="font-semibold text-sm mb-2">{product.name}</h4>
            <p className="text-xs text-gray-600 mb-2">{product.description}</p>
            <p className="text-sm font-bold text-blue-600">
              ${product.basePrice}
            </p>
          </div>
        ))}
      </div>
    </div>
  );

  const renderStep3 = () => (
    <div className="space-y-4">
      <h3 className="text-lg font-semibold">Step 3: Configure Product</h3>

      {currentProduct && (
        <div className="bg-gray-50 p-4 rounded-lg mb-4">
          <div className="flex items-center gap-4">
            <img
              src={currentProduct.image || "/placeholder.svg"}
              alt={currentProduct.name}
              className="w-16 h-16 object-cover rounded"
            />
            <div>
              <h4 className="font-semibold">{currentProduct.name}</h4>
              <p className="text-sm text-gray-600">
                {currentProduct.description}
              </p>
            </div>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <Label htmlFor="size">Size *</Label>
          <Select
            value={currentProductConfig.size}
            onValueChange={(value) =>
              setCurrentProductConfig((prev) => ({ ...prev, size: value }))
            }
          >
            <SelectTrigger>
              <SelectValue placeholder="Select size" />
            </SelectTrigger>
            <SelectContent>
              {currentProduct?.sizes.map((size) => (
                <SelectItem key={size} value={size}>
                  {size}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div>
          <Label htmlFor="accessory">Accessory</Label>
          <Select
            value={currentProductConfig.accessory}
            onValueChange={(value) =>
              setCurrentProductConfig((prev) => ({ ...prev, accessory: value }))
            }
          >
            <SelectTrigger>
              <SelectValue placeholder="Select accessory" />
            </SelectTrigger>
            <SelectContent>
              {currentProduct?.accessories.map((accessory) => (
                <SelectItem key={accessory} value={accessory}>
                  {accessory}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div>
          <Label htmlFor="linkThanksCard">Link Thanks Card</Label>
          <Input
            id="linkThanksCard"
            type="file"
            accept="image/*"
            onChange={(e) => handleFileUpload("linkThanksCard", e)}
          />
          {currentProductConfig.linkThanksCard && (
            <p className="text-sm text-gray-600 mt-1">
              Selected: {currentProductConfig.linkThanksCard.name}
            </p>
          )}
        </div>

        <div>
          <Label htmlFor="linkFileDesign">Link File Design</Label>
          <Input
            id="linkFileDesign"
            type="file"
            onChange={(e) => handleFileUpload("linkFileDesign", e)}
          />
          {currentProductConfig.linkFileDesign && (
            <p className="text-sm text-gray-600 mt-1">
              Selected: {currentProductConfig.linkFileDesign.name}
            </p>
          )}
        </div>

        <div>
          <Label htmlFor="quantity">Quantity</Label>
          <Input
            id="quantity"
            type="number"
            min="1"
            value={currentProductConfig.quantity}
            onChange={(e) =>
              setCurrentProductConfig((prev) => ({
                ...prev,
                quantity: e.target.value,
              }))
            }
            placeholder="1"
          />
        </div>

        <div className="flex items-center space-x-2">
          <Checkbox
            id="activeTTS"
            checked={currentProductConfig.activeTTS}
            onCheckedChange={(checked) =>
              setCurrentProductConfig((prev) => ({
                ...prev,
                activeTTS: checked,
              }))
            }
          />
          <Label htmlFor="activeTTS">Active TTS</Label>
        </div>

        <div className="md:col-span-2">
          <Label htmlFor="note">Note</Label>
          <Textarea
            id="note"
            value={currentProductConfig.note}
            onChange={(e) =>
              setCurrentProductConfig((prev) => ({
                ...prev,
                note: e.target.value,
              }))
            }
            placeholder="Enter any additional notes"
            rows={3}
          />
        </div>

        <div className="md:col-span-2">
          <div className="bg-blue-50 p-4 rounded-lg">
            <Label className="text-lg font-semibold">
              Product Price: ${calculateProductPrice().toFixed(2)}
            </Label>
          </div>
        </div>
      </div>

      <div className="flex gap-2 pt-4">
        <Button onClick={handleAddToCart} disabled={!currentProductConfig.size}>
          Add to Order
        </Button>
        <Button variant="outline" onClick={() => setCurrentStep(2)}>
          Add More Products
        </Button>
      </div>

      {cartProducts.length > 0 && (
        <div className="mt-6 border-t pt-4">
          <h4 className="font-semibold mb-3">
            Products in Order ({cartProducts.length})
          </h4>
          <div className="space-y-3">
            {cartProducts.map((item) => (
              <div key={item.id} className="border rounded-lg p-4 bg-gray-50">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <img
                      src={item.product.image || "/placeholder.svg"}
                      alt={item.product.name}
                      className="w-12 h-12 object-cover rounded"
                    />
                    <div>
                      <p className="font-medium">{item.product.name}</p>
                      <p className="text-sm text-gray-600">
                        {item.config.size} • Qty: {item.config.quantity}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="font-semibold">
                      ${item.totalPrice.toFixed(2)}
                    </span>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => toggleProductDetails(item.id)}
                    >
                      {expandedProducts[item.id] ? (
                        <>
                          <ChevronUp className="w-4 h-4 mr-1" />
                          Hide Details
                        </>
                      ) : (
                        <>
                          <ChevronDown className="w-4 h-4 mr-1" />
                          View Details
                        </>
                      )}
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleRemoveFromCart(item.id)}
                    >
                      Remove
                    </Button>
                  </div>
                </div>

                {expandedProducts[item.id] && (
                  <div className="mt-4 pt-4 border-t border-gray-200">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <div>
                        <Label>Size</Label>
                        <Select
                          value={item.config.size}
                          onValueChange={(value) =>
                            updateCartProduct(item.id, "size", value)
                          }
                        >
                          <SelectTrigger>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            {item.product.sizes.map((size) => (
                              <SelectItem key={size} value={size}>
                                {size}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>

                      <div>
                        <Label>Accessory</Label>
                        <Select
                          value={item.config.accessory}
                          onValueChange={(value) =>
                            updateCartProduct(item.id, "accessory", value)
                          }
                        >
                          <SelectTrigger>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            {item.product.accessories.map((accessory) => (
                              <SelectItem key={accessory} value={accessory}>
                                {accessory}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>

                      <div>
                        <Label>Link Thanks Card</Label>
                        <Input
                          type="file"
                          accept="image/*"
                          onChange={(e) =>
                            handleCartFileUpload(item.id, "linkThanksCard", e)
                          }
                        />
                        {item.config.linkThanksCard && (
                          <p className="text-sm text-gray-600 mt-1">
                            Selected: {item.config.linkThanksCard.name}
                          </p>
                        )}
                      </div>

                      <div>
                        <Label>Link File Design</Label>
                        <Input
                          type="file"
                          onChange={(e) =>
                            handleCartFileUpload(item.id, "linkFileDesign", e)
                          }
                        />
                        {item.config.linkFileDesign && (
                          <p className="text-sm text-gray-600 mt-1">
                            Selected: {item.config.linkFileDesign.name}
                          </p>
                        )}
                      </div>

                      <div>
                        <Label>Quantity</Label>
                        <Input
                          type="number"
                          min="1"
                          value={item.config.quantity}
                          onChange={(e) =>
                            updateCartProduct(
                              item.id,
                              "quantity",
                              e.target.value
                            )
                          }
                        />
                      </div>

                      <div className="flex items-center space-x-2">
                        <Checkbox
                          checked={item.config.activeTTS}
                          onCheckedChange={(checked) =>
                            updateCartProduct(item.id, "activeTTS", checked)
                          }
                        />
                        <Label>Active TTS</Label>
                      </div>

                      <div className="md:col-span-2">
                        <Label>Note</Label>
                        <Textarea
                          value={item.config.note}
                          onChange={(e) =>
                            updateCartProduct(item.id, "note", e.target.value)
                          }
                          placeholder="Enter any additional notes"
                          rows={2}
                        />
                      </div>
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>

          <div className="mt-4 bg-green-50 p-4 rounded-lg">
            <div className="flex justify-between items-center">
              <Label className="text-lg font-semibold">
                Total Money: ${calculateTotalMoney().toFixed(2)}
              </Label>
              <Button
                onClick={handleMakeOrder}
                disabled={cartProducts.length === 0}
              >
                Make Order
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );

  return (
    <>
      <Dialog open={isOpen} onOpenChange={onClose}>
        <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>
              Make Manual Order - Step {currentStep} of 3
            </DialogTitle>
          </DialogHeader>

          <div className="py-4">
            {/* Progress indicator */}
            <div className="flex items-center justify-between mb-6">
              {[1, 2, 3].map((step) => (
                <div key={step} className="flex items-center">
                  <div
                    className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium ${
                      step <= currentStep
                        ? "bg-blue-600 text-white"
                        : "bg-gray-200 text-gray-600"
                    }`}
                  >
                    {step}
                  </div>
                  {step < 3 && (
                    <div
                      className={`w-16 h-1 mx-2 ${
                        step < currentStep ? "bg-blue-600" : "bg-gray-200"
                      }`}
                    />
                  )}
                </div>
              ))}
            </div>

            {/* Step content */}
            {currentStep === 1 && renderStep1()}
            {currentStep === 2 && renderStep2()}
            {currentStep === 3 && renderStep3()}
          </div>

          {/* Navigation buttons */}
          <div className="flex justify-between pt-4 border-t">
            <Button
              variant="outline"
              onClick={handleBack}
              disabled={currentStep === 1}
            >
              Back
            </Button>

            {currentStep < 3 && (
              <Button onClick={handleNext} disabled={currentStep === 2}>
                Next
              </Button>
            )}
          </div>
        </DialogContent>
      </Dialog>

      {/* Confirmation Dialog */}
      <AlertDialog open={showConfirmDialog} onOpenChange={setShowConfirmDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Confirm Order Creation</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to create this order with{" "}
              {cartProducts.length} product(s) for $
              {calculateTotalMoney().toFixed(2)}?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>No</AlertDialogCancel>
            <AlertDialogAction onClick={confirmMakeOrder}>
              Yes
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
