import "./globals.css";
import { Inter } from "next/font/google";

const inter = Inter({ subsets: ["latin"] });

export const metadata = {
  title: "CNC Seller Dashboard",
  description: "Dashboard for CNC sellers",
};

export default function RootLayout({ children }) {
  return (
    <html lang="en">
      <body style={inter.style}>{children}</body>
    </html>
  );
}
