"use client";

import type { DepartmentListItemDto } from "@/entities/department";
import { Button, buttonVariants } from "@/shared/ui/button";
import { Dialog } from "@base-ui/react/dialog";
import { Archive, Loader2, RotateCcw, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import {
  useDepartmentArchiveMutation,
  type DepartmentArchiveAction,
} from "./use-department-archive-mutation";

export default function DepartmentArchiveDialog({
  department,
  action,
}: {
  department: DepartmentListItemDto;
  action: DepartmentArchiveAction;
}) {
  const [open, setOpen] = useState(false);
  const mutation = useDepartmentArchiveMutation(action);
  const controllerRef = useRef<AbortController | null>(null);
  const isRestoring = action === "restore";
  const ActionIcon = isRestoring ? RotateCcw : Archive;

  useEffect(() => {
    if (!open) return;
    const controller = new AbortController();
    controllerRef.current = controller;
    return () => {
      controller.abort();
      controllerRef.current = null;
    };
  }, [open]);

  async function handleConfirm() {
    const controller = controllerRef.current;
    if (!controller || mutation.isPending) return;
    try {
      await mutation.mutateAsync({
        id: department.id,
        signal: controller.signal,
      });
      setOpen(false);
    } catch {
      // The mutation shows the error and leaves the dialog open for another attempt.
    }
  }

  return (
    <Dialog.Root
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen && mutation.isPending) return;
        setOpen(nextOpen);
      }}
    >
      <Dialog.Trigger
        className={buttonVariants({
          variant: isRestoring ? "outline" : "ghost",
          size: "sm",
          className: isRestoring
            ? "gap-2 text-emerald-700 dark:text-emerald-400"
            : "gap-2 text-destructive hover:text-destructive",
        })}
      >
        <ActionIcon className="size-4" aria-hidden="true" />
        {isRestoring ? "Восстановить" : "В архив"}
      </Dialog.Trigger>
      <Dialog.Portal>
        <Dialog.Backdrop className="fixed inset-0 z-50 bg-foreground/50 backdrop-blur-[2px]" />
        <Dialog.Viewport className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto p-4 sm:p-6">
          <Dialog.Popup className="relative max-h-[calc(100dvh-2rem)] w-full max-w-md overflow-y-auto rounded-2xl bg-background shadow-2xl ring-1 ring-foreground/10 outline-none">
            <div className="px-6 pt-7 pb-6 sm:px-7">
              <div
                className={`mb-5 flex size-12 items-center justify-center rounded-xl ${isRestoring ? "bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-400" : "bg-destructive/10 text-destructive"}`}
              >
                <ActionIcon className="size-6" aria-hidden="true" />
              </div>
              <Dialog.Title className="pr-8 text-xl font-semibold tracking-tight">
                {isRestoring
                  ? "Восстановить подразделение?"
                  : "Архивировать подразделение?"}
              </Dialog.Title>
              <Dialog.Description className="mt-3 text-sm leading-relaxed text-muted-foreground">
                Подразделение «
                <span className="font-medium text-foreground">
                  {department.name}
                </span>
                »
                {isRestoring
                  ? " вернётся в активный список. Связанные архивные локации и позиции останутся в архиве."
                  : " исчезнет из активного списка. Локации и позиции, используемые только этим подразделением, также попадут в архив. Дочерние подразделения сохранятся."}
              </Dialog.Description>
              <Dialog.Close
                aria-label="Закрыть диалог"
                disabled={mutation.isPending}
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
                disabled={mutation.isPending}
              >
                Отмена
              </Dialog.Close>
              <Button
                type="button"
                variant={isRestoring ? "default" : "destructive"}
                disabled={mutation.isPending}
                onClick={() => void handleConfirm()}
              >
                {mutation.isPending ? (
                  <Loader2 className="size-4 animate-spin" aria-hidden="true" />
                ) : (
                  <ActionIcon className="size-4" aria-hidden="true" />
                )}
                {mutation.isPending
                  ? "Сохраняем..."
                  : isRestoring
                    ? "Восстановить"
                    : "В архив"}
              </Button>
            </div>
          </Dialog.Popup>
        </Dialog.Viewport>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
