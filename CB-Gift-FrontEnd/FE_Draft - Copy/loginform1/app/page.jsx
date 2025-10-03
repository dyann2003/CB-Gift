"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { jwtDecode } from "jwt-decode";

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [rememberMe, setRememberMe] = useState(false);
  const router = useRouter();

  const handleLogin = async (e) => {
    e.preventDefault();

    if (!email || !password) {
      alert("Please enter both email and password");
      return;
    }

    try {
      const res = await fetch("https://localhost:7015/api/auth/login", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          userNameOrEmail: email,
          password: password,
        }),
      });

      if (!res.ok) {
        const err = await res.json();
        alert(err.message || "Login failed");
        return;
      }

      const data = await res.json();
      const token = data.accessToken; // 👈 fix ở đây

      if (!token) {
        alert("No token received from server");
        return;
      }

      // decode JWT để lấy role
      const decoded = jwtDecode(token);
      const roles =
        decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];

      console.log("Decoded token:", decoded);
      console.log("Roles:", roles);

      // Lưu token (có thể đổi sang cookie nếu muốn an toàn hơn)
      if (rememberMe) {
        localStorage.setItem("token", token);
      } else {
        sessionStorage.setItem("token", token);
      }

      // Điều hướng theo role
      if (roles?.includes("Seller")) {
        router.push("/seller/dashboard");
      } else if (roles?.includes("Manager")) {
        router.push("/manager/dashboard");
      } else if (roles?.includes("QC")) {
        router.push("/qc/dashboard");
      } else if (roles?.includes("Staff")) {
        router.push("/staff/dashboard");
      } else {
        router.push("/"); // fallback
      }
    } catch (error) {
      console.error("Login error:", error);
      alert("Something went wrong!");
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 flex">
      {/* Left side - Login Form */}
      <div className="w-full lg:w-1/2 flex items-center justify-center p-8">
        <div className="w-full max-w-md space-y-6">
          {/* Logo */}
          <div className="flex items-center space-x-2 mb-8">
            <div className="w-8 h-8 bg-cyan-400 rounded"></div>
          </div>

          {/* Login Form */}
          <div className="space-y-6">
            <div>
              <h1 className="text-2xl font-semibold text-gray-900">Login</h1>
              <p className="text-gray-600 mt-1">
                See your growth and get support!
              </p>
            </div>

            <form onSubmit={handleLogin} className="space-y-4">
              {/* Email Input */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-gray-700">
                  Email*
                </label>
                <Input
                  type="email"
                  placeholder="Enter your email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="w-full"
                />
              </div>

              {/* Password Input */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-gray-700">
                  Password*
                </label>
                <Input
                  type="password"
                  placeholder="minimum 8 characters"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full"
                />
              </div>

              {/* Remember Me */}
              <div className="flex items-center space-x-2">
                <Checkbox
                  id="remember"
                  checked={rememberMe}
                  onCheckedChange={setRememberMe}
                />
                <label htmlFor="remember" className="text-sm text-gray-600">
                  Remember me
                </label>
              </div>

              {/* Login Button */}
              <Button
                type="submit"
                className="w-full bg-gray-900 hover:bg-gray-800 text-white py-3"
              >
                Login
              </Button>
            </form>
          </div>
        </div>
      </div>

      {/* Right side - Illustration */}
      <div className="hidden lg:flex lg:w-1/2 bg-gradient-to-br from-blue-50 to-purple-50 items-center justify-center p-8">
        <div className="w-full max-w-lg">
          <img
            src="/isometric-3d-illustration-of-people-working-with-d.jpg"
            alt="People working with data visualization"
            className="w-full h-auto"
          />
        </div>
      </div>
    </div>
  );
}
