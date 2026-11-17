import { ApiRequestError } from "@/shared/api/error";

export function getDepartmentActivityErrorMessage(error: unknown): string {
  if (error instanceof ApiRequestError) {
    for (const item of error.errors) {
      switch (item.code) {
        case "department.activity.active_descendants":
          return "Сначала выключи активные дочерние подразделения.";
        case "department.activity.parent_inactive":
          return "Сначала активируй родительские подразделения. Если они в архиве, восстанови их.";
        case "department.activity.archived":
          return "Сначала восстанови подразделение из архива.";
        case "department.not_found":
          return "Подразделение не найдено. Обнови список и попробуй ещё раз.";
      }
    }
  }

  return "Не удалось изменить активность. Попробуй ещё раз.";
}
