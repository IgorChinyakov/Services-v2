import { ApiRequestError } from "@/shared/api/error";
import axios from "axios";

const unknownResultMessage =
  "Не удалось получить результат операции. Обнови список перед повтором: часть изменений могла сохраниться.";

function getHttpErrorMessage(status: number): string {
  if (status >= 500) {
    return `На сервере произошла ошибка. ${unknownResultMessage}`;
  }

  switch (status) {
    case 400:
      return "Параметры массовой операции некорректны. Обнови список и повтори выбор.";
    case 401:
      return "Сессия истекла. Войди в систему и повтори операцию.";
    case 403:
      return "У тебя нет прав для выполнения этой операции.";
    case 404:
      return "Сервис массовых операций недоступен. Обнови страницу и попробуй позже.";
    case 409:
      return "Данные изменились. Обнови список и повтори операцию.";
    case 413:
      return "Выбрано слишком много подразделений. Отправь меньшую группу записей.";
    case 429:
      return "Слишком много запросов. Подожди немного и попробуй ещё раз.";
    default:
      return unknownResultMessage;
  }
}

export function getBulkDepartmentRequestErrorMessage(error: unknown): string | null {
  if (axios.isCancel(error)) return null;

  if (error instanceof ApiRequestError) {
    if (error.status === 400) {
      if (error.errors.some(item => item.invalidField?.startsWith("Ids"))) {
        return "Выбери от 1 до 100 подразделений без повторений и повтори операцию.";
      }
      if (error.errors.some(item => item.invalidField === "IsActive")) {
        return "Не удалось определить новое состояние активности. Повтори выбор действия.";
      }
    }

    return getHttpErrorMessage(error.status);
  }

  if (axios.isAxiosError(error)) {
    if (error.code === "ERR_CANCELED") return null;
    if (error.code === "ECONNABORTED" || error.code === "ETIMEDOUT") {
      return "Сервер не ответил вовремя. Обнови список перед повтором: часть изменений могла сохраниться.";
    }
    if (error.response) return getHttpErrorMessage(error.response.status);

    return "Не удалось получить ответ от сервера. Проверь подключение и обнови список перед повтором: часть изменений могла сохраниться.";
  }

  return unknownResultMessage;
}
