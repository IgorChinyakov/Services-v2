"use client";

import { Button } from "@/shared/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/shared/ui/dialog";
import {
  Archive,
  CircleAlert,
  Info,
  Power,
  PowerOff,
  RotateCcw,
  X,
} from "lucide-react";
import { useEffect, useRef, useState } from "react";
import type {
  BulkDepartmentAction,
  BulkDepartmentErrorDto,
} from "./model/types";
import {
  getBulkDepartmentItemErrorMessage,
  getBulkDepartmentResultMessage,
} from "./model/get-result-message";
import useBulkDepartments from "./use-bulk-departments";

const actionContent = {
  archive: {
    title: "Архивировать выбранные подразделения?",
    description:
      "Выбранные подразделения перейдут в архив. Позже их можно будет восстановить.",
    warning:
      "Локации и позиции, которые больше не используются другими активными подразделениями, также могут попасть в архив. Невыбранные дочерние подразделения сохранятся.",
    confirmLabel: "Архивировать",
    icon: Archive,
    iconClassName: "bg-destructive/10 text-destructive",
    destructive: true,
  },
  restore: {
    title: "Восстановить выбранные подразделения?",
    description:
      "Выбранные подразделения станут активными и вернутся в список «Не в архиве».",
    warning:
      "Для восстановления нужен активный родитель. Связанные архивные локации и позиции автоматически не восстанавливаются.",
    confirmLabel: "Восстановить",
    icon: RotateCcw,
    iconClassName:
      "bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-400",
    destructive: false,
  },
  activate: {
    title: "Включить выбранные подразделения?",
    description:
      "Подразделения станут активными. Их связи и положение в дереве не изменятся.",
    warning:
      "Архивные подразделения сначала нужно восстановить. Для включения подразделения его родители должны быть активны либо включаться в этой же операции.",
    confirmLabel: "Включить",
    icon: Power,
    iconClassName: "bg-sky-50 text-sky-700 dark:bg-sky-950 dark:text-sky-400",
    destructive: false,
  },
  deactivate: {
    title: "Выключить выбранные подразделения?",
    description:
      "Подразделения станут неактивными, но останутся вне архива. Связанные локации и позиции не изменятся.",
    warning:
      "Активные дочерние подразделения и другие потомки тоже должны быть выбраны для выключения. Иначе выключить их родителя не получится.",
    confirmLabel: "Выключить",
    icon: PowerOff,
    iconClassName:
      "bg-amber-50 text-amber-800 dark:bg-amber-950 dark:text-amber-300",
    destructive: false,
  },
};

type DepartmentBulkConfirmDialogProps = {
  selectedIds: string[];
  action: BulkDepartmentAction;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSucceeded: (ids: string[]) => void;
};

export default function DepartmentBulkConfirmDialog({
  selectedIds,
  action,
  open,
  onOpenChange,
  onSucceeded,
}: DepartmentBulkConfirmDialogProps) {
  const cancelRef = useRef<HTMLButtonElement | null>(null);
  const content = actionContent[action];
  const ActionIcon = content.icon;

  const controllerRef = useRef<AbortController | null>(null);

  const [feedback, setFeedback] = useState<{
    message: string;
    errors: BulkDepartmentErrorDto[];
    isPartial: boolean;
  } | null>(null);
  const submittingRef = useRef(false);

  useEffect(() => {
    if (!open) return;
    const controller = new AbortController();
    controllerRef.current = controller;

    return () => {
      controller.abort();
      controllerRef.current = null;
    };
  }, [open]);

  const bulkMutation = useBulkDepartments();

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) setFeedback(null);
    onOpenChange(nextOpen);
  }

  async function handleMutation() {
    if (submittingRef.current || bulkMutation.isPending) return;
    const abortSignal = controllerRef.current?.signal;
    if (!abortSignal || abortSignal.aborted) return;

    const ids = [...selectedIds];
    if (
      ids.length < 1 ||
      ids.length > 100 ||
      new Set(ids).size !== ids.length
    ) {
      setFeedback({
        message: "Выбери от 1 до 100 подразделений без повторений.",
        errors: [],
        isPartial: false,
      });
      return;
    }

    submittingRef.current = true;
    setFeedback(null);

    try {
      const mutationResult = await bulkMutation.mutateAsync({
        action,
        ids,
        signal: abortSignal,
      });

      if (abortSignal.aborted) return;
      const message = getBulkDepartmentResultMessage(action, mutationResult);

      if (
        mutationResult.failedCount === 0 &&
        mutationResult.succeededCount > 0
      ) {
        handleOpenChange(false);
        onSucceeded(mutationResult.succeededIds);
        return;
      }

      setFeedback({
        message,
        errors: mutationResult.errors,
        isPartial: mutationResult.succeededCount > 0,
      });
      if (mutationResult.succeededCount > 0) {
        onSucceeded(mutationResult.succeededIds);
      }
    } catch {
      // Network/HTTP errors are shown by the mutation hook; keep the selection.
    } finally {
      submittingRef.current = false;
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen, eventDetails) => {
        if (!nextOpen && bulkMutation.isPending) {
          eventDetails.cancel();
          return;
        }

        handleOpenChange(nextOpen);
      }}
    >
      <DialogContent
        initialFocus={cancelRef}
        showCloseButton={false}
        className="max-h-[calc(100dvh-2rem)] gap-0 overflow-y-auto rounded-2xl p-0 sm:max-w-lg"
      >
        <div className="border-b bg-linear-to-br from-muted/40 via-background to-background px-5 pt-6 pb-5 sm:px-7 sm:pt-7">
          <div className="mb-5 flex items-center gap-3 pr-8">
            <span
              className={`flex size-12 shrink-0 items-center justify-center rounded-xl ${content.iconClassName}`}
            >
              <ActionIcon aria-hidden="true" className="size-6" />
            </span>
            <span className="rounded-full border bg-background px-3 py-1 text-xs font-medium text-muted-foreground">
              Массовая операция
            </span>
          </div>
          <DialogTitle className="pr-5 text-xl leading-snug font-semibold tracking-tight">
            {content.title}
          </DialogTitle>
          <DialogDescription className="mt-3 text-sm leading-relaxed text-muted-foreground">
            {content.description}
          </DialogDescription>
          <DialogClose
            disabled={bulkMutation.isPending}
            aria-label="Закрыть подтверждение"
            render={
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="absolute top-4 right-4"
              />
            }
          >
            <X aria-hidden="true" className="size-4" />
          </DialogClose>
        </div>

        <div className="space-y-4 px-5 py-5 sm:px-7">
          <div className="flex items-center justify-between gap-4 rounded-xl border bg-muted/30 px-4 py-3">
            <span className="text-sm text-muted-foreground">
              Выбрано подразделений
            </span>
            <span className="text-2xl font-semibold tabular-nums">
              {selectedIds.length}
            </span>
          </div>
          <div className="flex items-start gap-3 rounded-xl border border-amber-200 bg-amber-50/60 p-4 dark:border-amber-900 dark:bg-amber-950/30">
            <CircleAlert
              aria-hidden="true"
              className="mt-0.5 size-4 shrink-0 text-amber-700 dark:text-amber-400"
            />
            <p className="text-xs leading-relaxed text-amber-950 dark:text-amber-200">
              {content.warning}
            </p>
          </div>
          <div className="flex items-start gap-3 px-1 text-muted-foreground">
            <Info aria-hidden="true" className="mt-0.5 size-4 shrink-0" />
            <p className="text-xs leading-relaxed">
              Если часть подразделений не удастся обработать, остальные
              изменения сохранятся. После выполнения покажем результат и причины
              ошибок.
            </p>
          </div>
          {feedback && (
            <div
              role="alert"
              className={`space-y-3 rounded-xl border p-4 text-sm ${
                feedback.isPartial
                  ? "border-amber-200 bg-amber-50/60 text-amber-950 dark:border-amber-900 dark:bg-amber-950/30 dark:text-amber-200"
                  : "border-destructive/20 bg-destructive/5 text-destructive"
              }`}
            >
              <p className="leading-relaxed">{feedback.message}</p>
              {feedback.errors.length > 0 && (
                <ul className="max-h-52 space-y-3 overflow-y-auto">
                  {feedback.errors.map((error) => (
                    <li
                      key={error.id}
                      className="space-y-1 border-t border-current/10 pt-3"
                    >
                      <p className="font-mono text-xs break-all">
                        ID: {error.id}
                      </p>
                      <p className="text-xs leading-relaxed">
                        {getBulkDepartmentItemErrorMessage(error.code)}
                      </p>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </div>

        <div className="flex flex-col-reverse gap-2 border-t bg-muted/30 px-5 py-4 sm:flex-row sm:justify-end sm:px-7">
          <DialogClose
            disabled={bulkMutation.isPending}
            render={
              <Button
                ref={cancelRef}
                type="button"
                variant="outline"
                disabled={bulkMutation.isPending}
              />
            }
          >
            {feedback ? "Закрыть" : "Отмена"}
          </DialogClose>
          <Button
            type="button"
            variant={content.destructive ? "destructive" : "default"}
            disabled={selectedIds.length < 1 || bulkMutation.isPending}
            className="gap-2"
            onClick={() => void handleMutation()}
          >
            <ActionIcon aria-hidden="true" className="size-4" />
            {bulkMutation.isPending
              ? "Выполняем…"
              : feedback?.errors.length
                ? "Повторить для оставшихся"
                : content.confirmLabel}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
