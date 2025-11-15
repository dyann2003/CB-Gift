"use client";

import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { ChevronDown } from 'lucide-react';
import { useState } from "react";
import ProductTimeline from "./product-timeline";

export default function ProductItem({
  product,
  showDetails = false,
  onDetailClick,
}) {
  const [expanded, setExpanded] = useState(false);

  return (
    <div className="border border-gray-200 rounded-lg p-6 mb-4">
      <div className="flex items-start gap-4">
        {/* Product Image */}
        <div className="w-20 h-20 flex-shrink-0">
          <img
            src={product.image || "/placeholder.svg"}
            alt={product.name}
            className="w-full h-full object-cover rounded border border-gray-200"
          />
        </div>

        {/* Product Details */}
        <div className="flex-1">
          <h3 className="text-lg font-semibold text-gray-900 mb-2">
            {product.name}
          </h3>
          <p className="text-sm text-gray-600 mb-3">
            <span className="font-medium">SKU:</span> {product.sku} - {product.color} / {product.size}
          </p>
          <p className="text-sm text-gray-600">
            <span className="font-medium">Print in:</span> {product.supplier}
          </p>

          {/* Timeline */}
          <div className="mt-4">
            <ProductTimeline productDetails={product} />
          </div>

          {/* Product Info Grid */}
          <div className="grid grid-cols-3 gap-4 mt-4 pt-4 border-t border-gray-200">
            <div>
              <p className="text-xs text-gray-500 font-medium mb-1">
                Print side
              </p>
              <p className="font-semibold text-gray-900">{product.printSide}</p>
            </div>
            <div>
              <p className="text-xs text-gray-500 font-medium mb-1">
                Production cost
              </p>
              <p className="font-semibold text-gray-900">
                {product.productionCost}
              </p>
            </div>
            <div>
              <p className="text-xs text-gray-500 font-medium mb-1">
                Tracking detail
              </p>
              <p className="font-semibold text-gray-900">
                {product.trackingStatus}
              </p>
            </div>
          </div>
        </div>

        {/* Detail Button */}
        <div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              setExpanded(!expanded);
              onDetailClick && onDetailClick();
            }}
            className="border-blue-200 text-blue-600 hover:bg-blue-50"
          >
            <ChevronDown
              className={`h-4 w-4 transition-transform ${
                expanded ? "rotate-180" : ""
              }`}
            />
            Detail
          </Button>
        </div>
      </div>

      {/* Expanded Details */}
      {expanded && (
        <div className="mt-6 pt-6 border-t border-gray-200">
          <div className="grid grid-cols-2 gap-6">
            <div>
              <h4 className="font-semibold text-gray-900 mb-4">
                Product Specifications
              </h4>
              <div className="space-y-3">
                <div>
                  <p className="text-xs text-gray-500 font-medium">Name</p>
                  <p className="text-sm text-gray-900">{product.name}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500 font-medium">SKU</p>
                  <p className="text-sm text-gray-900">{product.sku}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500 font-medium">Supplier</p>
                  <p className="text-sm text-gray-900">{product.supplier}</p>
                </div>
              </div>
            </div>
            <div>
              <h4 className="font-semibold text-gray-900 mb-4">
                Production Details
              </h4>
              <div className="space-y-3">
                <div>
                  <p className="text-xs text-gray-500 font-medium">
                    Production Cost
                  </p>
                  <p className="text-sm font-semibold text-gray-900">
                    {product.productionCost}
                  </p>
                </div>
                <div>
                  <p className="text-xs text-gray-500 font-medium">
                    Print Side
                  </p>
                  <p className="text-sm text-gray-900">{product.printSide}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
