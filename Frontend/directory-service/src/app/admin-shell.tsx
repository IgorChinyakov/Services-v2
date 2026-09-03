"use client";

import { Header } from "@/widgets/header/header";
import Sidebar from "@/widgets/sidebar/sidebar";
import { ReactNode, useState } from "react";

type AdminShellProps = {
  children: ReactNode;
};

export default function AdminShell({ children }: AdminShellProps) {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  return (
    <div className="flex min-h-screen">
      <Sidebar isOpen={isSidebarOpen} onClose={() => setIsSidebarOpen(false)} />
      <div className="flex min-w-0 flex-1 flex-col">
        <Header
          title="Directory service"
          isSidebarOpen={isSidebarOpen}
          onSidebarToggle={() => setIsSidebarOpen((isOpen) => !isOpen)}
        />
        <main className="flex-1 p-4 md:p-6">{children}</main>
      </div>
    </div>
  );
}
