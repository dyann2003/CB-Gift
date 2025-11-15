"use client";

import { Badge } from "@/components/ui/badge";
import { CheckCircle2, Clock, AlertCircle } from 'lucide-react';

export default function ProductTimeline({ productDetails }) {
  const getStatusIcon = (status) => {
    switch (status) {
      case "Created":
        return <CheckCircle2 className="h-5 w-5 text-green-600" />;
      case "On hold":
        return <Clock className="h-5 w-5 text-yellow-600" />;
      case "Shipping":
        return <Clock className="h-5 w-5 text-blue-600" />;
      case "Delivered":
        return <CheckCircle2 className="h-5 w-5 text-green-600" />;
      default:
        return <AlertCircle className="h-5 w-5 text-gray-600" />;
    }
  };

  const getStatusColor = (status) => {
    switch (status) {
      case "Created":
        return "bg-green-100 text-green-800";
      case "On hold":
        return "bg-yellow-100 text-yellow-800";
      case "Shipping":
        return "bg-blue-100 text-blue-800";
      case "Delivered":
        return "bg-green-100 text-green-800";
      default:
        return "bg-gray-100 text-gray-800";
    }
  };

  return (
    <div className="flex gap-8 items-start mb-6">
      <div className="flex-1">
        <div className="flex items-center gap-2 mb-4">
          {/* SỬA 1: Đổi 'statuses' thành 'timeline' */}
          {productDetails.timeline && productDetails.timeline.map((status, idx) => (
            <div key={idx} className="flex items-center gap-1">
              <div
                className={`px-3 py-1 rounded text-xs font-medium ${getStatusColor(
                  status.status // SỬA 2: Đổi 'status.name' thành 'status.status'
                )}`}
              >
                {status.status} {/* SỬA 3: Đổi 'status.name' thành 'status.status' */}
              </div>
              {/* SỬA 4: Đổi 'statuses' thành 'timeline' */}
              {idx < productDetails.timeline.length - 1 && (
                <div className="text-gray-300 mx-1">→</div>
              )}
            </div>
          ))}
        </div>
        <div className="flex gap-8">
          {/* SỬA 5: Đổi 'statuses' thành 'timeline' */}
          {productDetails.timeline && productDetails.timeline.map((status, idx) => (
            <div key={idx} className="text-xs text-gray-500">
              {status.date}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
