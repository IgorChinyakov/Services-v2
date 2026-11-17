"use client";

import { Button } from "@/shared/ui/button";
import {
  Archive,
  CheckCheck,
  Power,
  PowerOff,
  RotateCcw,
  X,
} from "lucide-react";
import DepartmentBulkConfirmDialog from "./department-bulk-confirm-dialog";
import { useState } from "react";
import { BulkDepartmentAction } from "./model/types";
import { useIsMutating } from "@tanstack/react-query";

type DepartmentBulkActionBarProps = {
  isArchived: boolean;
  selectedIds: string[];
  onClearSelection: () => void;
  onSucceeded: (ids: string[]) => void;
};

export default function DepartmentBulkActionBar({
  selectedIds,
  isArchived,
  onClearSelection,
  onSucceeded,
}: DepartmentBulkActionBarProps) {
  const [open, setOpen] = useState(false);
  const [action, setAction] = useState<BulkDepartmentAction>("archive");

  const isPending = useIsMutating({ mutationKey: ["departments", "bulk"] }) > 0;

  if (selectedIds.length < 1) return null;

  function handleClick(action: BulkDepartmentAction) {
    setAction(action);
    setOpen(true);
  }

  return (
    <section
      aria-label="Массовые действия с подразделениями"
      className="sticky top-20 z-20 rounded-2xl border border-sky-200 bg-background/95 p-4 shadow-md shadow-sky-950/5 backdrop-blur-sm sm:p-5 dark:border-sky-900"
    >
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-start gap-3">
          <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-sky-100 text-sky-700 dark:bg-sky-950 dark:text-sky-300">
            <CheckCheck aria-hidden="true" className="size-5" />
          </span>
          <div>
            <p role="status" className="text-sm font-semibold">
              Выбрано подразделений:{" "}
              <span className="tabular-nums">{selectedIds.length}</span>
            </p>
            <p className="mt-1 text-xs leading-relaxed text-muted-foreground">
              Действие применяется к выбранным карточкам текущей страницы.
            </p>
          </div>
        </div>

        <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center">
          <div
            className="grid gap-2 sm:flex sm:flex-wrap"
            role="group"
            aria-label="Действия над выбранными подразделениями"
          >
            {isArchived ? (
              <Button
                type="button"
                variant="outline"
                className="gap-2 border-emerald-200 text-emerald-700 hover:bg-emerald-50 hover:text-emerald-800 dark:border-emerald-900 dark:text-emerald-400 dark:hover:bg-emerald-950 dark:hover:text-emerald-300"
                onClick={() => handleClick("restore")}
                disabled={isPending}
              >
                <RotateCcw aria-hidden="true" className="size-4" />
                Восстановить
              </Button>
            ) : (
              <>
                <Button
                  type="button"
                  variant="outline"
                  className="gap-2 border-destructive/20 text-destructive hover:bg-destructive/5 hover:text-destructive"
                  onClick={() => handleClick("archive")}
                  disabled={isPending}
                >
                  <Archive aria-hidden="true" className="size-4" />
                  Архивировать
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="gap-2"
                  onClick={() => handleClick("activate")}
                  disabled={isPending}
                >
                  <Power
                    aria-hidden="true"
                    className="size-4 text-sky-700 dark:text-sky-400"
                  />
                  Включить
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="gap-2"
                  onClick={() => handleClick("deactivate")}
                  disabled={isPending}
                >
                  <PowerOff
                    aria-hidden="true"
                    className="size-4 text-amber-700 dark:text-amber-400"
                  />
                  Выключить
                </Button>
              </>
            )}
          </div>
          <DepartmentBulkConfirmDialog
            selectedIds={selectedIds}
            action={action}
            open={open}
            onOpenChange={setOpen}
            onSucceeded={onSucceeded}
          ></DepartmentBulkConfirmDialog>
          <Button
            type="button"
            variant="ghost"
            className="gap-2 text-muted-foreground"
            onClick={onClearSelection}
            disabled={isPending}
          >
            <X aria-hidden="true" className="size-4" />
            Снять выбор
          </Button>
        </div>
      </div>
    </section>
  );
}
