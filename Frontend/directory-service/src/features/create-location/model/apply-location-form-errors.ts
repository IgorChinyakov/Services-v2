import { ApiRequestError } from "@/shared/api/error";
import axios from "axios";
import type { FieldPath, UseFormSetError } from "react-hook-form";
import type { LocationFormValues } from "@/entities/location";

const fieldByErrorCode: Record<string, FieldPath<LocationFormValues>> = {
  "location.name.duplicate": "name",
  "location.address.duplicate": "address",
};

const fieldByInvalidField: Record<string, FieldPath<LocationFormValues>> = {
  Name: "name",
  "Address.City": "address.city",
  "Address.Street": "address.street",
  "Address.House": "address.house",
  "Address.Apartment": "address.apartment",
};

export function applyLocationFormErrors(
  error: unknown,
  setError: UseFormSetError<LocationFormValues>,
): string | null {
  if (!(error instanceof ApiRequestError)) {
    return axios.isAxiosError(error) && !error.response
      ? "Не удалось связаться с сервером. Проверь подключение и попробуй ещё раз."
      : "Не удалось выполнить операцию. Попробуй ещё раз.";
  }

  const unhandledMessages: string[] = [];

  for (const item of error.errors) {
    const field =
      fieldByErrorCode[item.code] ??
      (item.code === "field.is.invalid" && item.invalidField
        ? fieldByInvalidField[item.invalidField]
        : undefined);

    if (field) {
      setError(field, { type: "server", message: item.message });
    } else {
      unhandledMessages.push(item.message);
    }
  }

  return unhandledMessages.length > 0
    ? unhandledMessages.join("; ")
    : error.errors.length === 0
      ? error.message
      : null;
}
