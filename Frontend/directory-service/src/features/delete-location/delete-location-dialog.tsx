"use client";

import type { LocationDto } from "@/entities/location";
import { Button, buttonVariants } from "@/shared/ui/button";
import { Dialog } from "@base-ui/react/dialog";
import { TriangleAlert, Trash2, X } from "lucide-react";
import { useDeleteLocation } from "./use-delete-location";
import { useRef, useEffect, useState } from "react";

export default function DeleteLocationDialog({
  location,
}: {
  location: LocationDto;
}) {
  const [open, setOpen] = useState(false);

  const deleteMutation = useDeleteLocation();

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

  const handleDelete = async (id: string) => {
    const controller = controllerRef.current;
    if (!controller) return;

    try {
      await deleteMutation.mutateAsync({ id, signal: controller.signal });
      setOpen(false); // только после успешного DELETE
    } catch {
      // Toast уже показан в onError; повторять его здесь не нужно.
      return;
    }
  };

  return (
    <Dialog.Root
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen && deleteMutation.isPending) return;
        setOpen(nextOpen);
      }}
    >
      <Dialog.Trigger
        className={buttonVariants({
          variant: "ghost",
          size: "sm",
          className: "gap-2 text-destructive hover:text-destructive",
        })}
      >
        <Trash2 className="size-4" aria-hidden="true" />
        Удалить
      </Dialog.Trigger>

      <Dialog.Portal>
        <Dialog.Backdrop className="fixed inset-0 z-50 bg-foreground/50 backdrop-blur-[2px]" />
        <Dialog.Viewport className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto p-4 sm:p-6">
          <Dialog.Popup className="relative w-full max-w-md overflow-hidden rounded-2xl bg-background shadow-2xl ring-1 ring-foreground/10 outline-none">
            <div className="px-6 pt-7 pb-6 sm:px-7">
              <div className="mb-5 flex size-12 items-center justify-center rounded-xl bg-destructive/10 text-destructive ring-1 ring-destructive/20">
                <TriangleAlert className="size-6" aria-hidden="true" />
              </div>

              <Dialog.Title className="pr-8 text-xl font-semibold tracking-tight">
                Удалить локацию?
              </Dialog.Title>
              <Dialog.Description className="mt-3 text-sm leading-relaxed text-muted-foreground">
                Локация «
                <span className="font-medium text-foreground">
                  {location.name}
                </span>
                » исчезнет из справочника после подтверждения.
              </Dialog.Description>

              <Dialog.Close
                aria-label="Закрыть диалог"
                className={buttonVariants({
                  variant: "ghost",
                  size: "icon",
                  className: "absolute top-5 right-5",
                })}
                disabled={deleteMutation.isPending}
              >
                <X className="size-4" aria-hidden="true" />
              </Dialog.Close>
            </div>

            <div className="flex flex-col-reverse gap-2 border-t bg-muted/30 px-6 py-4 sm:flex-row sm:justify-end sm:px-7">
              <Dialog.Close className={buttonVariants({ variant: "outline" })}>
                Отмена
              </Dialog.Close>
              <Button
                type="button"
                variant="destructive"
                disabled={deleteMutation.isPending}
                onClick={async () => await handleDelete(location.id)}
              >
                Удалить локацию
              </Button>
            </div>
          </Dialog.Popup>
        </Dialog.Viewport>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
