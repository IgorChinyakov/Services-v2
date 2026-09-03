"use client";

import { navigationItems } from "@/shared/config/navigation-items";
import { cn } from "@/shared/lib/utils";
import Link from "next/link";
import { usePathname } from "next/navigation";

type SidebarProps = {
  isOpen: boolean;
  onClose: () => void;
};

export default function Sidebar({ isOpen, onClose }: SidebarProps) {
  const pathname = usePathname();

  return (
    <>
      <button
        type="button"
        aria-label="Закрыть боковое меню"
        onClick={onClose}
        className={cn(
          "fixed inset-x-0 bottom-0 top-16 z-30 bg-black/40 transition-opacity md:hidden",
          isOpen ? "opacity-100" : "pointer-events-none opacity-0",
        )}
      />
      <aside
        className={cn(
          "fixed bottom-0 left-0 top-16 z-40 w-64 shrink-0 border-r bg-sidebar transition-transform duration-200 md:sticky md:bottom-auto md:top-0 md:h-screen md:translate-x-0",
          isOpen ? "translate-x-0" : "-translate-x-full",
        )}
      >
        <nav className="flex flex-col gap-1 p-4">
          {navigationItems.map((item) => {
            const isActive = pathname === item.href;

            return (
              <Link
                key={item.href}
                href={item.href}
                onClick={onClose}
                className={cn(
                  "rounded-md px-3 py-2 text-sm transition-colors hover:bg-sidebar-accent",
                  isActive &&
                    "bg-sidebar-accent font-medium text-sidebar-accent-foreground",
                )}
              >
                {item.label}
              </Link>
            );
          })}
        </nav>
      </aside>
    </>
  );
}
