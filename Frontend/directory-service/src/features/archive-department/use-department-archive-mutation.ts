"use client";

import { ApiRequestError } from "@/shared/api/error";
import { toast } from "@/shared/ui/toast";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { archiveDepartment } from "./api/archive-department";
import { restoreDepartment } from "./api/restore-department";

export type DepartmentArchiveAction = "archive" | "restore";

export function useDepartmentArchiveMutation(action: DepartmentArchiveAction) {
  const queryClient = useQueryClient();
  const isRestoring = action === "restore";

  return useMutation({
    mutationFn: ({ id, signal }: { id: string; signal: AbortSignal }) =>
      isRestoring
        ? restoreDepartment(id, signal)
        : archiveDepartment(id, signal),
    onSuccess: () => {
      for (const queryKey of [
        ["departments"],
        ["department"],
        ["locations"],
        ["positions"],
      ]) {
        void queryClient.invalidateQueries({ queryKey });
      }
      toast.add({
        type: "success",
        description: isRestoring
          ? "Подразделение восстановлено"
          : "Подразделение перемещено в архив",
      });
    },
    onError: (error, { signal }) => {
      if (signal.aborted) return;

      let description = isRestoring
        ? "Не удалось восстановить подразделение. Попробуй ещё раз."
        : "Не удалось архивировать подразделение. Попробуй ещё раз.";
      if (error instanceof ApiRequestError) {
        if (
          error.errors.some(
            (item) => item.code === "department.restore.parent_deleted",
          )
        ) {
          description = "Сначала восстанови родительское подразделение.";
        } else if (
          error.errors.some((item) => item.code === "department.path.duplicate")
        ) {
          description =
            "Путь подразделения уже занят. Проверь структуру перед восстановлением.";
        } else if (error.status === 404) {
          description =
            "Подразделение не найдено или его состояние уже изменилось. Обнови список.";
        } else if (error.status === 409) {
          description =
            "Структура изменилась. Обнови страницу и повтори действие.";
        }
      }
      toast.add({ type: "error", description });
    },
  });
}
