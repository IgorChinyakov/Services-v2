import type { BulkDepartmentAction, BulkDepartmentResultDto } from "./types";

const successLabels: Record<BulkDepartmentAction, string> = {
  archive: "Архивировано подразделений",
  restore: "Восстановлено подразделений",
  activate: "Включено подразделений",
  deactivate: "Выключено подразделений",
};

const itemErrorMessages: Record<string, string> = {
  "department.not_found": "Подразделение не найдено в выбранном состоянии. Обнови список.",
  "department.activity.active_descendants": "Сначала выключи активные дочерние подразделения.",
  "department.activity.parent_inactive": "Сначала включи или восстанови родительские подразделения.",
  "department.activity.archived": "Сначала восстанови подразделение из архива.",
  "department.restore.parent_deleted": "Сначала восстанови родительское подразделение.",
  "department.restore.conflict": "Структура изменилась. Обнови список и повтори восстановление.",
  "department.path.duplicate": "Подразделение с таким путём уже существует.",
  "department.slug.duplicate": "Подразделение с таким slug уже существует.",
};

export function getBulkDepartmentItemErrorMessage(code: string): string {
  return itemErrorMessages[code] ?? "Не удалось обработать подразделение. Обнови список и попробуй ещё раз.";
}

export function getBulkDepartmentResultMessage(
  action: BulkDepartmentAction,
  result: BulkDepartmentResultDto,
): string {
  const { succeededCount, failedCount } = result;
  const total = succeededCount + failedCount;

  if (total === 0) {
    return "Сервер не вернул результаты обработки. Обнови список перед повторной попыткой.";
  }

  if (succeededCount === 0) {
    return `Не удалось обработать ни одно подразделение из ${total}. Проверь причины ниже. Выбор сохранён.`;
  }

  if (failedCount > 0) {
    return `Обработано ${succeededCount} из ${total}. Не удалось обработать: ${failedCount}. Успешные изменения сохранены. В выборе остались только подразделения с ошибками.`;
  }

  return `${successLabels[action]}: ${succeededCount}.`;
}
