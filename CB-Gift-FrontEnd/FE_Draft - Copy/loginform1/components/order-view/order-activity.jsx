"use client";

import { Badge } from "@/components/ui/badge";

export default function OrderActivity({ activities }) {
  return (
    <div className="bg-white border border-gray-200 rounded-lg p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-6">
        Order Activity Timeline
      </h3>
      <div className="space-y-4">
        {activities && activities.length > 0 ? (
          activities.map((activity, idx) => (
            <div key={idx} className="flex gap-4">
              <div className="flex flex-col items-center">
                <div className="w-3 h-3 rounded-full bg-blue-600 mt-1.5"></div>
                {idx < activities.length - 1 && (
                  <div className="w-0.5 h-12 bg-gray-300 mt-2"></div>
                )}
              </div>
              <div className="pb-4">
                <p className="text-xs text-gray-500 mb-1">{activity.date}</p>
                <p className="text-sm font-medium text-gray-900">
                  {activity.title}
                </p>
                {activity.description && (
                  <p className="text-sm text-gray-600 mt-1">
                    {activity.description}
                  </p>
                )}
              </div>
            </div>
          ))
        ) : (
          <p className="text-sm text-gray-500">No activity yet</p>
        )}
      </div>
    </div>
  );
}
