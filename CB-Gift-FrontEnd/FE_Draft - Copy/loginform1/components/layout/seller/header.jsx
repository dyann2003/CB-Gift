"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import TopUpModal from "@/components/modals/top-up-modal"
import ProfileModal from "@/components/modals/profile-modal"
import PasswordModal from "@/components/modals/password-modal"

export default function SellerHeader() {
  const router = useRouter()
  const [showTopUpModal, setShowTopUpModal] = useState(false)
  const [showProfileModal, setShowProfileModal] = useState(false)
  const [showPasswordModal, setShowPasswordModal] = useState(false)

  const handleLogout = () => {
    router.push("/")
  }

  return (
    <>
      <header className="bg-white shadow-sm border-b border-gray-200 p-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold text-gray-900">Welcome , bach</h2>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="w-10 h-10 rounded-full bg-gray-200">
                <div className="w-8 h-8 rounded-full bg-gray-400"></div>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56">
              <div className="px-2 py-1.5 text-sm font-medium">Balance: $100</div>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => setShowProfileModal(true)}>View Profile</DropdownMenuItem>
              <DropdownMenuItem onClick={() => setShowPasswordModal(true)}>Change Password</DropdownMenuItem>
              <DropdownMenuItem onClick={() => setShowTopUpModal(true)}>Request Top-Up Money</DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={handleLogout}>Log out</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </header>

      <TopUpModal open={showTopUpModal} onOpenChange={setShowTopUpModal} />
      <ProfileModal open={showProfileModal} onOpenChange={setShowProfileModal} />
      <PasswordModal open={showPasswordModal} onOpenChange={setShowPasswordModal} />
    </>
  )
}
