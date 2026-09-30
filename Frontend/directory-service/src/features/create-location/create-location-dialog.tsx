"use client";

import { buttonVariants } from "@/shared/ui/button";
import { Dialog } from "@base-ui/react/dialog";
import { Plus, X } from "lucide-react";
import { useState } from "react";
import CreateLocationForm from "./create-location-form";

export default function CreateLocationDialog() {
  const [open, setOpen] = useState(false);

  return (
    <Dialog.Root open={open} onOpenChange={setOpen}>
      <Dialog.Trigger
        className={buttonVariants({ size: "lg", className: "gap-2 px-4" })}
      >
        <Plus className="size-4" aria-hidden="true" />
        Создать локацию
      </Dialog.Trigger>
      <Dialog.Portal>
        <Dialog.Backdrop className="fixed inset-0 z-50 bg-foreground/50 backdrop-blur-[2px]" />
        <Dialog.Viewport className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto p-4 sm:p-6">
          <Dialog.Popup className="relative max-h-[calc(100dvh-2rem)] w-full max-w-2xl overflow-y-auto rounded-2xl bg-background shadow-2xl ring-1 ring-foreground/10 outline-none">
            <div className="border-b px-6 py-5 pr-16 sm:px-7 sm:pr-16">
              <Dialog.Title className="text-xl font-semibold tracking-tight">
                Новая локация
              </Dialog.Title>
              <Dialog.Description className="mt-1 text-sm text-muted-foreground">
                Укажи название и адрес места, где работают подразделения
                компании.
              </Dialog.Description>
            </div>
            <Dialog.Close
              aria-label="Закрыть форму"
              className={buttonVariants({
                variant: "ghost",
                size: "icon",
                className: "absolute right-5 top-5",
              })}
            >
              <X className="size-4" aria-hidden="true" />
            </Dialog.Close>
            <div className="px-6 py-6 sm:px-7">
              {open && (
                <CreateLocationForm
                  onSuccess={() => {
                    setOpen(false);
                  }}
                />
              )}
            </div>
          </Dialog.Popup>
        </Dialog.Viewport>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
