"use client";

import { useState, useEffect } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

export default function ProfileModal({ open, onOpenChange }) {
  const [profile, setProfile] = useState({
    id: "",
    userName: "",
    email: "",
    phoneNumber: "",
    fullName: "",
  });

  // Gọi API khi modal được mở
  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const res = await fetch("https://localhost:7015/api/auth/profile", {
          method: "GET",
          credentials: "include", // 👈 quan trọng để gửi cookie lên BE
        });

        if (res.ok) {
          const data = await res.json();
          setProfile(data);
        } else if (res.status === 401) {
          console.warn("Unauthorized - token hết hạn hoặc không hợp lệ");
        } else {
          console.error("Lỗi khi lấy profile:", await res.text());
        }
      } catch (err) {
        console.error("Fetch profile error:", err);
      }
    };

    if (open) {
      fetchProfile();
    }
  }, [open]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>View Profile</DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="fullName">Full Name</Label>
            <Input id="fullName" value={profile.fullName || ""} readOnly />
          </div>
          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              value={profile.email || ""}
              readOnly
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="phone">Phone</Label>
            <Input id="phone" value={profile.phoneNumber || ""} readOnly />
          </div>
          <div className="space-y-2">
            <Label htmlFor="userName">User Name</Label>
            <Input id="userName" value={profile.userName || ""} readOnly />
          </div>
          <div className="flex justify-end">
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              Close
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
