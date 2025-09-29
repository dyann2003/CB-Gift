"use client";

import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";

export default function QcSidebar({ currentPage, setCurrentPage }) {
  const router = useRouter();

  const menuItems = [
    { id: "dashboard", label: "Dashboard", path: "/qc/dashboard" },
    { id: "check-design", label: "Check Design", path: "/qc/check-design" },
    { id: "check-product", label: "Check Product", path: "/qc/check-product" },
  ];

  const handleNavigation = (item) => {
    setCurrentPage(item.id);
    router.push(item.path);
  };

  return (
    <div className="w-64 bg-white shadow-lg flex flex-col">
      <div className="p-6 border-b border-gray-200">
        <h1 className="text-xl font-bold text-gray-900">CNC - QC</h1>
      </div>

      <nav className="flex-1 p-4 space-y-2">
        {menuItems.map((item) => (
          <Button
            key={item.id}
            variant={currentPage === item.id ? "default" : "ghost"}
            className="w-full justify-start"
            onClick={() => handleNavigation(item)}
          >
            {item.label}
          </Button>
        ))}
      </nav>
    </div>
  );
}
