import "./globals.css";
import { Inter } from "next/font/google";
import { SignalRProvider } from '../contexts/SignalRContext'; // 👈 1. Import Provider

const inter = Inter({ subsets: ["latin"] });

export const metadata = {
  title: "CNC Seller Dashboard",
  description: "Dashboard for CNC sellers",
};

export default function RootLayout({ children }) {
  return (
    <html lang="en">
      <body style={inter.style}>
        {/* 👇 2. Bọc {children} bằng Provider */}
        <SignalRProvider>
          {children}
        </SignalRProvider>
      </body>
    </html>
  );
}