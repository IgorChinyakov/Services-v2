"use client";

import type { LocationDto } from "@/entities/location";
import { Button, buttonVariants } from "@/shared/ui/button";
import { Dialog } from "@base-ui/react/dialog";
import { RotateCcw, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { useRestoreLocation } from "./useRestoreLocation";
import { toast } from "@/shared/ui/toast";

export default function RestoreLocationDialog({
  location,
}: {
  location: LocationDto;
}) {
  const [open, setOpen] = useState(false);

  const restoreMutation = useRestoreLocation();

  const controllerRef = useRef<AbortController | null>(null);

  useEffect(() => {
    if (!open) return;
    const controller = new AbortController();
    controllerRef.current = controller;

    return () => {
      controller.abort();
      controllerRef.current = null;
    };
  }, [open]);

  const handleRestore = async (id: string) => {
    // TODO: Restore location.id via a mutation and close the dialog on success.
    const controller = controllerRef.current;
    if (!controller) return;

    try {
      await restoreMutation.mutateAsync({ id, signal: controller.signal });
      setOpen(false); // только после успешного DELETE

      toast.add({
        type: "success",
        description: "Location restored successfully",
      });
    } catch {
      // Toast уже показан в onError; повторять его здесь не нужно.
      return;
    }
  };

  return (
    <Dialog.Root
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen && restoreMutation.isPending) return;
        setOpen(nextOpen);
      }}
    >
      <Dialog.Trigger
        className={buttonVariants({
          variant: "outline",
          size: "sm",
          className:
            "gap-2 text-emerald-700 hover:text-emerald-800 dark:text-emerald-400 dark:hover:text-emerald-300",
        })}
      >
        <RotateCcw className="size-4" aria-hidden="true" />
        Восстановить
      </Dialog.Trigger>

      <Dialog.Portal>
        <Dialog.Backdrop className="fixed inset-0 z-50 bg-foreground/50 backdrop-blur-[2px]" />
        <Dialog.Viewport className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto p-4 sm:p-6">
          <Dialog.Popup className="relative max-h-[calc(100dvh-2rem)] w-full max-w-md overflow-y-auto rounded-2xl bg-background shadow-2xl ring-1 ring-foreground/10 outline-none">
            <div className="px-6 pt-7 pb-6 sm:px-7">
              <div className="mb-5 flex size-12 items-center justify-center rounded-xl bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200 dark:bg-emerald-950 dark:text-emerald-400 dark:ring-emerald-800">
                <RotateCcw className="size-6" aria-hidden="true" />
              </div>

              <Dialog.Title className="pr-8 text-xl font-semibold tracking-tight">
                Восстановить локацию?
              </Dialog.Title>
              <Dialog.Description className="mt-3 text-sm leading-relaxed text-muted-foreground">
                Локация «
                <span className="font-medium text-foreground">
                  {location.name}
                </span>
                » вернётся в активный список и снова будет доступна в
                справочнике.
              </Dialog.Description>

              <Dialog.Close
                aria-label="Закрыть диалог"
                disabled={restoreMutation.isPending}
                className={buttonVariants({
                  variant: "ghost",
                  size: "icon",
                  className: "absolute top-5 right-5",
                })}
              >
                <X className="size-4" aria-hidden="true" />
              </Dialog.Close>
            </div>

            <div className="flex flex-col-reverse gap-2 border-t bg-muted/30 px-6 py-4 sm:flex-row sm:justify-end sm:px-7">
              <Dialog.Close
                className={buttonVariants({ variant: "outline" })}
                disabled={restoreMutation.isPending}
              >
                Отмена
              </Dialog.Close>
              <Button
                type="button"
                disabled={restoreMutation.isPending}
                onClick={() => handleRestore(location.id)}
              >
                <RotateCcw className="size-4" aria-hidden="true" />
                Восстановить локацию
              </Button>
            </div>
          </Dialog.Popup>
        </Dialog.Viewport>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
