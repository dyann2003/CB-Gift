"use client";

import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";

export default function ManagerSidebar({ currentPage, setCurrentPage }) {
  const router = useRouter();

  const menuItems = [
    { id: "dashboard", label: "Dashboard", path: "/manager/dashboard" },
    {
      id: "manage-order",
      label: "Manage Order",
      path: "/manager/manage-order",
    },
    {
      id: "manage-account",
      label: "Manage Account",
      path: "/manager/manage-account",
    },
    {
      id: "manage-relationship",
      label: "Manage Relationship",
      path: "/manager/manage-relationship",
    },
    {
      id: "manage-topup",
      label: "Manage Top Up Money",
      path: "/manager/manage-topup",
    },
    {
      id: "manage-catalog",
      label: "Manage Catalog",
      path: "/manager/manage-catalog",
    },
    {
      id: "manage-category",
      label: "Manage Category",
      path: "/manager/manage-category",
    },
    {
      id: "report-analyst",
      label: "Report and Analyst",
      path: "/manager/report-analyst",
    },
  ];

  const handleNavigation = (item) => {
    setCurrentPage(item.id);
    router.push(item.path);
  };

  return (
    <div className="w-64 bg-white shadow-lg flex flex-col">
      <div className="p-6 border-b border-gray-200">
        <h1 className="text-xl font-bold text-gray-900">CNC - Manager</h1>
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
