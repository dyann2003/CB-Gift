"use client";

import { useState } from "react";
import OrderView from "@/components/order-view/order-view";

// Mock order data matching your screenshot
const mockOrder = {
  id: "CYP20254349YPaza",
  status: "CANCELLED",
  pendingStatus: "PENDING",
  createdAt: "2025-09-23 03:55:29",
  products: [
    {
      id: 1,
      name: "Unisex Jersey Short Sleeve Tee 3001",
      sku: "P1-dark-grey-heather-m-BjYAS10c",
      color: "Dark Grey Heather",
      size: "M",
      supplier: "Print in SignaturePrint Co. (United States)",
      image: "https://via.placeholder.com/100",
      timeline: [
        {
          status: "Created",
          date: "Sep 23 03:55 AM",
          completed: true,
        },
        {
          status: "On hold",
          date: "Sep 23 03:55 AM",
          completed: true,
        },
        {
          status: "Shipping",
          date: null,
          completed: false,
        },
        {
          status: "Delivered",
          date: null,
          completed: false,
        },
      ],
      printSide: "BOTHSIDES",
      productionCost: "USD 12.72",
      trackingDetail: "Pending",
    },
    {
      id: 2,
      name: "Men's Sports Shorts (AOP)",
      sku: "P24-xl-XHDrt50B",
      size: "XL",
      image: "https://via.placeholder.com/100",
      timeline: [
        {
          status: "Created",
          date: "Sep 23 03:55 AM",
          completed: true,
        },
      ],
    },
  ],
  customer: {
    firstName: "aaaaaaaaaaaaa",
    lastName: "aaaaaaaaaaaaa",
    email: "aaaaaaaaaa@gmail.com",
    mobile: "",
  },
  shippingAddress: {
    fullAddress: "aaaaaaaaaaaaaaaa aaaaaaaaaaaaa",
    state: "HI",
    country: "United States",
    zipCode: "aaaaaaaaaaaaa",
  },
  billing: {
    productionCosts: "USD 33.15",
    shippingStandard: "USD 11.28",
    shippingCost: "USD 11.28",
    surchargeFee: "USD 0.00",
    taxCost: "USD 0",
    totalCosts: "USD 44.43",
  },
  activities: [
    {
      date: "Sep 23 03:55 AM",
      title: "Created order",
      description: "",
    },
  ],
};

export default function OrderViewDemo() {
  const handleCancel = () => {
    console.log("Cancel order");
  };

  const handleBack = () => {
    console.log("Go back");
  };

  return (
    <OrderView
      order={mockOrder}
      onCancel={handleCancel}
      onBack={handleBack}
    />
  );
}
