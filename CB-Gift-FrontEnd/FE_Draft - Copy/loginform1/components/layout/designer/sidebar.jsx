"use client";

import { useRouter } from "next/navigation";
import Link from "next/link";
import {
  Home,
  Palette, // Icon cho Design Assign
  History, // Icon cho Design History
  Settings,
  User,
  LogOut,
  Package, // Thêm icon
  ClipboardList, // Thêm icon
} from "lucide-react";
import { Button } from "@/components/ui/button";

export default function DesignerSidebar({ currentPage, setCurrentPage }) {
  const router = useRouter();

  const menuItems = [
    { id: "dashboard", label: "Dashboard",icon: <Home className="h-4 w-4" />, path: "/designer/dashboard" },
    {
      id: "design-assign",
      label: "Design Assign by Seller",
      icon: <Palette className="h-4 w-4" />, 
      path: "/designer/design-assign",
    },
    {
      label: "Design History",
      path: "/designer/design-history",
      icon: <History className="h-4 w-4" />, // Icon mới
      id: "design-history",
    },
    // {
    //   id: "manage-fail-design",
    //   label: "Manage Fail Design",
    //   path: "/designer/manage-fail-design",
    // },
  ];

  const handleNavigation = (item) => {
    setCurrentPage(item.id);
    router.push(item.path);
  };

  return (
    <div className="w-64 bg-white shadow-lg flex flex-col">
      <div className="p-6 border-b border-gray-200">
        <h1 className="text-xl font-bold text-gray-900">CNC - Designer</h1>
      </div>

      <nav className="flex-1 p-4 space-y-2">
        { menuItems.map((item) => (
          <Button
            key={item.id}
            variant={currentPage === item.id ? "default" : "ghost"}
            className="w-full justify-start gap-3"  // thêm gap-3 để icon và text không dính nhau
            onClick={() => handleNavigation(item)}
          >
            {item.icon}          {/* ← Đây là chỗ bạn quên!!! */}
            <span>{item.label}</span>
          </Button>
        ))}
      </nav>
    </div>
  );
}
