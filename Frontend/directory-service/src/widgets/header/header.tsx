import { Menu, X } from "lucide-react";

type HeaderProps = {
  title: string;
  isSidebarOpen: boolean;
  onSidebarToggle: () => void;
};

export function Header({
  title,
  isSidebarOpen,
  onSidebarToggle,
}: HeaderProps) {
  return (
    <header className="sticky top-0 z-50 flex h-16 items-center gap-3 border-b bg-background/95 px-4 backdrop-blur md:z-30 md:px-6">
      <button
        type="button"
        aria-label={isSidebarOpen ? "Закрыть боковое меню" : "Открыть боковое меню"}
        onClick={onSidebarToggle}
        className="inline-flex size-10 items-center justify-center rounded-md border hover:bg-accent md:hidden"
      >
        {isSidebarOpen ? (
          <X className="size-5" />
        ) : (
          <Menu className="size-5" />
        )}
      </button>
      <h1 className="text-lg font-semibold">{title}</h1>
    </header>
  );
}
