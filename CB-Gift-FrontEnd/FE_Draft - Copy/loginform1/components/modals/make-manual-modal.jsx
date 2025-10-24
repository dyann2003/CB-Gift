"use client";

import { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { useRef } from "react";
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
  const [showErrorDialog, setShowErrorDialog] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [showSuccessDialog, setShowSuccessDialog] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const [orderId, setOrderId] = useState("");
  const [activeTTS, setActiveTTS] = useState(false);
  const linkThanksCardRef = useRef(null);
  const linkFileDesignRef = useRef(null);
  const linkImgRef = useRef(null);

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
    linkImg: null,
    linkThanksCard: null,
    linkFileDesign: null,
    quantity: 1,
    activeTTS: false,
    note: "",
    productPrice: 0,
  });

  const resetForm = () => {
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

  // ─── Step 2: load products từ BE ───
  const [products, setProducts] = useState([]);
  const [loadingProducts, setLoadingProducts] = useState(false);
  const [productsError, setProductsError] = useState(null);

  useEffect(() => {
    if (!isOpen) return;
    fetchProducts();
  }, [isOpen]);

  const fetchProducts = async () => {
    setLoadingProducts(true);
    setProductsError(null);
    try {
      const res = await fetch("https://localhost:7015/api/Product", {
        credentials: "include",
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      setProducts(data || []);
    } catch (err) {
      console.error("fetchProducts error:", err);
      setProductsError(err.message || "Failed to load products");
    } finally {
      setLoadingProducts(false);
    }
  };

  // Upload ảnh thật lên server
  const uploadImage = async (file) => {
    const formData = new FormData();
    formData.append("File", file);

    try {
      const res = await fetch("https://localhost:7015/api/images/upload", {
        method: "POST",
        credentials: "include",
        body: formData,
      });

      if (!res.ok) {
        const errText = await res.text();
        throw new Error(`Upload failed: ${res.status} - ${errText}`);
      }

      const data = await res.json();
      console.log(" Upload success:", data);
      return data.url || data.secureUrl || data.path || null; // backend trả url nào thì lấy
    } catch (err) {
      console.error("Upload error:", err);
      setErrorMessage("Upload failed: " + err.message);
      setShowErrorDialog(true);
      return null;
    }
  };

  const calculateProductPrice = () => {
    if (!currentProduct) return 0;

    const variantId = currentProductConfig.variantId;
    const variant = currentProduct?.variants?.find(
      (v) => v.productVariantId === variantId
    );

    // Giá cơ bản theo size
    const basePrice = variant?.totalCost ?? currentProduct.basePrice ?? 0;

    // Số lượng
    const qty = Number.parseInt(currentProductConfig.quantity) || 1;

    // Active TTS cộng thêm $1
    const ttsExtra = currentProductConfig.activeTTS ? 1.0 : 0.0;

    const total = (basePrice + ttsExtra) * qty;
    return total;
  };

  useEffect(() => {
    const total = calculateProductPrice();
    setCurrentProductConfig((prev) => ({
      ...prev,
      productPrice: total,
    }));
  }, [
    currentProductConfig.variantId,
    currentProductConfig.quantity,
    currentProductConfig.activeTTS,
  ]);

  const calculateTotalMoney = () => {
    let total = cartProducts.reduce(
      (sum, product) => sum + product.totalPrice,
      0
    );
    if (activeTTS) total += 1; // ✅ cộng thêm 1$ khi bật Active TTS tổng
    return total;
  };

  const handleNext = () => {
    if (currentStep === 1) {
      console.log("Step 1 - Customer Info:", customerInfo);

      // kiểm tra các trường bắt buộc
      const requiredFields = [
        "name",
        "phone",
        "email",
        "address",
        "zipcode",
        "shipState",
        "shipCity",
        "shipCountry",
      ];

      const missing = requiredFields.filter((f) => !customerInfo[f]?.trim());
      if (missing.length > 0) {
        setErrorMessage(
          `Please fill all required fields: ${missing.join(", ")}`
        );
        setShowErrorDialog(true);
        return;
      }

      // Nếu đủ -> sang Step 2
      setCurrentStep(2);
      return;
    }

    if (currentStep === 2) {
      console.log(" Step 2 - Selected Product:", currentProduct);
      setCurrentStep(3);
      return;
    }

    // Không cần bước này nữa vì chỉ có 3 step
    // Nhưng nếu sau này có thêm step thì vẫn an toàn
    if (currentStep < 3) {
      setCurrentStep(currentStep + 1);
    }
  };

  const handleBack = () => {
    if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
    }
  };

  // Hàm upload file + hiển thị preview
  const handleFileUpload = async (field, event) => {
    const file = event.target.files[0];
    if (!file) return;

    console.log("Uploading file:", file.name);

    try {
      // Gọi API upload đúng endpoint
      const uploadedUrl = await uploadImage(file);

      if (uploadedUrl) {
        // Lưu link thật
        setCurrentProductConfig((prev) => ({
          ...prev,
          [field]: uploadedUrl,
        }));

        // Hiển thị preview ảnh (nếu là ảnh)
        if (file.type.startsWith("image/")) {
          const previewUrl = URL.createObjectURL(file);
          setCurrentProductConfig((prev) => ({
            ...prev,
            [`${field}Preview`]: previewUrl,
          }));
        }
      }
    } catch (error) {
      console.error("Upload error:", error);
      alert("Upload failed. Please try again.");
    }
  };

  const handleProductSelect = async (product) => {
    // product có thể là object từ products list (có id)
    const id = product?.id ?? product?.productId;
    if (!id) {
      // fallback nếu dùng mock: set trực tiếp
      setCurrentProduct(product);
      setCurrentStep(3);
      return;
    }

    try {
      // fetch detail để lấy variants/sizes
      const res = await fetch(`https://localhost:7015/api/Product/${id}`, {
        credentials: "include",
      });
      if (!res.ok) {
        console.warn("Product detail fetch failed, use base product");
        setCurrentProduct(product);
        setCurrentStep(3);
        return;
      }
      const detail = await res.json();
      setCurrentProduct(detail);
      // reset config
      setCurrentProductConfig({
        size: "",
        accessory: "",
        linkThanksCard: null,
        linkFileDesign: null,
        quantity: 1,
        activeTTS: false,
        note: "",
        productPrice: 0,
        variantId: null, // IMPORTANT: store chosen variant id later
      });
      setCurrentStep(3);
    } catch (err) {
      console.error("handleProductSelect error:", err);
      setCurrentProduct(product);
      setCurrentStep(3);
    }
  };

  const handleAddToCart = () => {
    if (!currentProduct) return;

    const productPrice = calculateProductPrice();
    const productToAdd = {
      id: Date.now(),
      product: currentProduct,
      config: { ...currentProductConfig },
      unitPrice: (() => {
        const vid = currentProductConfig.variantId;
        if (vid && currentProduct.productVariants) {
          const vv = currentProduct.productVariants.find(
            (x) => (x.id ?? x.variantId) === vid
          );
          return vv?.price ?? vv?.unitPrice ?? currentProduct.basePrice ?? 0;
        }
        return currentProduct.basePrice ?? currentProduct.price ?? 0;
      })(),
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

  const confirmMakeOrder = async () => {
    const orderDetails = cartProducts.map((p) => ({
      orderId: 0,
      productVariantID: p.config.variantId,
      quantity: Number(p.config.quantity),
      price: p.config.productPrice || 0,
      linkImg: p.config.linkImg || p.product.image || null,
      needDesign: true,
      linkThanksCard: p.config.linkThanksCard || null,
      linkDesign: p.config.linkFileDesign || null,
      accessory: p.config.accessory || "",
      note: p.config.note || "",
    }));

    const orderPayload = {
      customerInfo: {
        name: customerInfo.name,
        phone: customerInfo.phone,
        email: customerInfo.email,
        address: customerInfo.address,
        address1: customerInfo.address1,
        zipCode: customerInfo.zipcode,
        shipState: customerInfo.shipState,
        shipCity: customerInfo.shipCity,
        shipCountry: customerInfo.shipCountry,
      },
      orderCreate: {
        orderCode: orderId?.trim() || `ORD-${Date.now()}`,
        endCustomerID: 0,
        totalCost: calculateTotalMoney(),
        sellerUserId: null,
        orderDate: new Date().toISOString(),
        costScan: 1,
        activeTTS: activeTTS,
        tracking: "",
        productionStatus: "Pending",
        paymentStatus: "Unpaid",
        orderDetails: orderDetails,
      },
      orderDetails: orderDetails,
    };

    console.log("Sending order payload:", orderPayload);

    try {
      const res = await fetch("https://localhost:7015/api/Order/make-order", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(orderPayload),
      });

      const text = await res.text();
      if (!res.ok) throw new Error(`HTTP ${res.status}: ${text}`);

      console.log("Order created successfully:", text);
      setSuccessMessage("🎉 Order created successfully!");
      setShowSuccessDialog(true);

      setShowConfirmDialog(false);
      onClose();
      resetForm();
      window.location.reload();
    } catch (err) {
      console.error("❌ Create order failed:", err);
      setErrorMessage("❌ Failed to create order: " + err.message);
      setShowErrorDialog(true);
    }
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

      {loadingProducts ? (
        <div className="flex items-center justify-center py-8">
          <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600" />
        </div>
      ) : productsError ? (
        <div className="p-4 bg-red-50 rounded border border-red-200">
          <p className="text-red-700">
            Error loading products: {productsError}
          </p>
          <Button variant="outline" onClick={fetchProducts}>
            Retry
          </Button>
        </div>
      ) : products.length === 0 ? (
        <div className="text-gray-600">No products found</div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {products.map((p) => {
            // lấy giá: variant đầu tiên hoặc min trong variants
            const price = p.variants?.length
              ? Math.min(...p.variants.map((v) => v.totalCost))
              : 0;

            return (
              <div
                key={p.productId}
                className={`border rounded-lg p-4 cursor-pointer hover:border-blue-500 hover:shadow-md transition-all ${
                  currentProduct?.productId === p.productId
                    ? "ring-2 ring-blue-500"
                    : ""
                }`}
                onClick={() => {
                  setCurrentProduct(p);
                  setCurrentStep(3);
                }}
              >
                {/* Ảnh sản phẩm */}
                <img
                  src={p.itemLink || "/placeholder.svg"}
                  alt={p.productName}
                  className="w-full h-32 object-cover rounded-md mb-3"
                />

                {/* Tên sản phẩm */}
                <h4 className="font-semibold text-sm mb-2">{p.productName}</h4>

                {/* Mô tả ngắn */}
                <p className="text-xs text-gray-600 mb-2 line-clamp-2">
                  {p.describe}
                </p>

                {/* Giá */}
                <p className="text-sm font-bold text-blue-600">
                  ${price.toFixed(2)}
                </p>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );

  const renderStep3 = () => (
    <div className="space-y-4">
      <h3 className="text-lg font-semibold">Step 3: Configure Product</h3>

      {currentProduct && (
        <div className="bg-gray-50 p-4 rounded-lg mb-4">
          <div className="flex items-center gap-4">
            <img
              src={currentProduct.itemLink || "/placeholder.svg"}
              alt={currentProduct.productName}
              className="w-16 h-16 object-cover rounded"
            />
            <div>
              <h4 className="font-semibold">{currentProduct.productName}</h4>
              <p className="text-sm text-gray-600">{currentProduct.describe}</p>
            </div>
          </div>
        </div>
      )}

      {/* Nhập Order ID */}
      <div className="mt-2">
        <Label htmlFor="orderId" className="text-sm font-medium text-gray-700">
          Order ID *
        </Label>
        <input
          id="orderId"
          type="text"
          value={orderId}
          onChange={(e) => setOrderId(e.target.value)}
          placeholder="Nhập Order ID..."
          className="border border-gray-300 rounded-md px-3 py-2 w-full sm:w-1/2 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Select Size (map từ variants) */}
        <div>
          <Label htmlFor="size">Size *</Label>
          <Select
            value={currentProductConfig.variantId?.toString() || ""}
            onValueChange={(value) => {
              const selected = currentProduct?.variants?.find(
                (v) => v.productVariantId.toString() === value
              );
              setCurrentProductConfig((prev) => ({
                ...prev,
                size: selected?.sizeInch || "",
                variantId: selected?.productVariantId,
                price: selected?.totalCost || 0,
              }));
            }}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select size" />
            </SelectTrigger>
            <SelectContent>
              {currentProduct?.variants?.map((v) => (
                <SelectItem
                  key={v.productVariantId}
                  value={v.productVariantId.toString()}
                >
                  {v.sizeInch} — ${v.totalCost.toFixed(2)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Link Image */}
        <div>
          <Label htmlFor="linkImg">Link Image</Label>
          <Input
            id="linkImg"
            type="file"
            accept="image/*"
            ref={linkImgRef}
            onChange={(e) => handleFileUpload("linkImg", e)}
          />

          {currentProductConfig.linkImg && (
            <div className="mt-2 space-y-2">
              <p className="text-sm text-green-600 flex items-center gap-1">
                Uploaded:{" "}
                <a
                  href={currentProductConfig.linkImg}
                  target="_blank"
                  className="underline text-blue-600 truncate max-w-[200px]"
                >
                  {currentProductConfig.linkImg.split("/").pop()}
                </a>
              </p>

              {(currentProductConfig.linkImg.endsWith(".png") ||
                currentProductConfig.linkImg.endsWith(".jpg") ||
                currentProductConfig.linkImg.endsWith(".jpeg")) && (
                <div className="relative inline-block">
                  <img
                    src={currentProductConfig.linkImg}
                    alt="Uploaded Image Preview"
                    className="w-20 h-20 object-cover rounded border border-gray-300 shadow-sm"
                  />
                  <button
                    type="button"
                    onClick={() => {
                      setCurrentProductConfig((prev) => ({
                        ...prev,
                        linkImg: null,
                      }));
                      if (linkImgRef.current) {
                        linkImgRef.current.value = "";
                      }
                    }}
                    className="absolute top-0 right-0 bg-red-500 text-white rounded-full w-5 h-5 flex items-center justify-center text-xs hover:bg-red-600 translate-x-1 -translate-y-1"
                  >
                    ×
                  </button>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Link Thanks Card */}
        <div>
          <Label htmlFor="linkThanksCard">Link Thanks Card</Label>
          <Input
            id="linkThanksCard"
            type="file"
            accept="image/*"
            ref={linkThanksCardRef}
            onChange={(e) => handleFileUpload("linkThanksCard", e)}
          />

          {currentProductConfig.linkThanksCard && (
            <div className="mt-2 space-y-2">
              <p className="text-sm text-green-600 flex items-center gap-1">
                Uploaded:{" "}
                <a
                  href={currentProductConfig.linkThanksCard}
                  target="_blank"
                  className="underline text-blue-600 truncate max-w-[200px]"
                >
                  {currentProductConfig.linkThanksCard.split("/").pop()}
                </a>
              </p>

              {/* Ảnh + nút X nằm trong cùng khung */}
              {(currentProductConfig.linkThanksCard.endsWith(".png") ||
                currentProductConfig.linkThanksCard.endsWith(".jpg") ||
                currentProductConfig.linkThanksCard.endsWith(".jpeg")) && (
                <div className="relative inline-block">
                  <img
                    src={currentProductConfig.linkThanksCard}
                    alt="Thanks Card Preview"
                    className="w-20 h-20 object-cover rounded border border-gray-300 shadow-sm"
                  />
                  <button
                    type="button"
                    onClick={() => {
                      setCurrentProductConfig((prev) => ({
                        ...prev,
                        linkThanksCard: null,
                      }));
                      // ✅ Reset input file để hiện “No file chosen”
                      if (linkThanksCardRef.current) {
                        linkThanksCardRef.current.value = "";
                      }
                    }}
                    className="absolute top-0 right-0 bg-red-500 text-white rounded-full w-5 h-5 flex items-center justify-center text-xs hover:bg-red-600 translate-x-1 -translate-y-1"
                  >
                    ×
                  </button>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Link File Design */}
        <div>
          <Label htmlFor="linkFileDesign">Link File Design</Label>
          <Input
            id="linkFileDesign"
            type="file"
            accept="image/*,.pdf,.zip,.ai,.psd"
            ref={linkFileDesignRef}
            onChange={(e) => handleFileUpload("linkFileDesign", e)}
          />

          {currentProductConfig.linkFileDesign && (
            <div className="mt-2 space-y-2">
              <p className="text-sm text-green-600 flex items-center gap-1">
                Uploaded:{" "}
                <a
                  href={currentProductConfig.linkFileDesign}
                  target="_blank"
                  className="underline text-blue-600 truncate max-w-[200px]"
                >
                  {currentProductConfig.linkFileDesign.split("/").pop()}
                </a>
              </p>

              {(currentProductConfig.linkFileDesign.endsWith(".png") ||
                currentProductConfig.linkFileDesign.endsWith(".jpg") ||
                currentProductConfig.linkFileDesign.endsWith(".jpeg")) && (
                <div className="relative inline-block">
                  <img
                    src={currentProductConfig.linkFileDesign}
                    alt="Design File Preview"
                    className="w-20 h-20 object-cover rounded border border-gray-300 shadow-sm"
                  />
                  <button
                    type="button"
                    onClick={() => {
                      setCurrentProductConfig((prev) => ({
                        ...prev,
                        linkFileDesign: null,
                      }));
                      // ✅ Reset input file để hiện “No file chosen”
                      if (linkFileDesignRef.current) {
                        linkFileDesignRef.current.value = "";
                      }
                    }}
                    className="absolute top-0 right-0 bg-red-500 text-white rounded-full w-5 h-5 flex items-center justify-center text-xs hover:bg-red-600 translate-x-1 -translate-y-1"
                  >
                    ×
                  </button>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Quantity */}
        <div>
          <Label htmlFor="quantity">Quantity</Label>
          <Input
            id="quantity"
            type="number"
            min="1"
            value={currentProductConfig.quantity}
            onChange={(e) => {
              const val = e.target.value;
              // Chỉ chấp nhận ký tự số & >= 1
              if (/^\d*$/.test(val)) {
                const num = parseInt(val, 10);
                if (isNaN(num) || num < 1) return;
                setCurrentProductConfig((prev) => ({
                  ...prev,
                  quantity: num,
                }));
              }
            }}
            onKeyDown={(e) => {
              // Ngăn nhập ký tự không hợp lệ như -, e, +, ., v.v.
              if (["e", "E", "+", "-", "."].includes(e.key)) e.preventDefault();
            }}
            placeholder="1"
          />
        </div>

        {/* Note */}
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

        {/* Giá */}
        <div className="md:col-span-2">
          <div className="bg-blue-50 p-4 rounded-lg">
            <Label className="text-lg font-semibold">
              Product Price: ${calculateProductPrice().toFixed(2)}
            </Label>
          </div>
        </div>
      </div>

      {/* Buttons */}
      <div className="flex gap-2 pt-4">
        <Button
          onClick={handleAddToCart}
          disabled={!currentProductConfig.variantId}
        >
          Add to Order
        </Button>
        <Button variant="outline" onClick={() => setCurrentStep(2)}>
          Add More Products
        </Button>
      </div>

      {/* Cart hiển thị */}
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
                      src={item.product.itemLink || "/placeholder.svg"}
                      alt={item.product.productName}
                      className="w-12 h-12 object-cover rounded"
                    />
                    <div>
                      <p className="font-medium">{item.product.productName}</p>
                      <p className="text-sm text-gray-600">
                        {item.config.size} • Qty: {item.config.quantity}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <span className="font-semibold text-blue-600">
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
                        <Input
                          value={item.config.size || ""}
                          readOnly
                          className="bg-gray-100 cursor-not-allowed"
                        />
                      </div>

                      <div>
                        <Label>Quantity</Label>
                        <Input
                          type="number"
                          value={item.config.quantity}
                          readOnly
                          className="bg-gray-100 cursor-not-allowed"
                        />
                      </div>

                      <div className="md:col-span-2">
                        <Label>Note</Label>
                        <Textarea
                          value={item.config.note || ""}
                          readOnly
                          className="bg-gray-100 cursor-not-allowed"
                          rows={2}
                        />
                      </div>
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>

          {/* === Phần tổng cuối === */}
          <div className="mt-4 bg-green-50 p-4 rounded-lg flex flex-col md:flex-row justify-between items-center gap-3">
            <div className="flex flex-col sm:flex-row items-center gap-4 w-full sm:w-auto">
              {/* Checkbox Active TTS */}
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="activeTTS"
                  checked={activeTTS}
                  onCheckedChange={(checked) => setActiveTTS(checked)}
                />
                <Label htmlFor="activeTTS">
                  Active TTS (Cộng 1 Total Order)
                </Label>
              </div>

              {/* Tổng tiền */}
              <Label className="text-lg font-semibold text-green-700">
                Total Money: ${calculateTotalMoney().toFixed(2)}
              </Label>
            </div>

            <Button
              onClick={handleMakeOrder}
              disabled={cartProducts.length === 0}
            >
              Make Order
            </Button>
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
      {/* Error Dialog */}
      <AlertDialog open={showErrorDialog} onOpenChange={setShowErrorDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle> Missing Information</AlertDialogTitle>
            <AlertDialogDescription>{errorMessage}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogAction onClick={() => setShowErrorDialog(false)}>
              OK
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
      {/* ✅ Success Dialog */}
      <AlertDialog open={showSuccessDialog} onOpenChange={setShowSuccessDialog}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Success</AlertDialogTitle>
            <AlertDialogDescription>{successMessage}</AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogAction onClick={() => setShowSuccessDialog(false)}>
              OK
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
