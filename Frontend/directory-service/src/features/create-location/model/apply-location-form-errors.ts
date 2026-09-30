import { ApiRequestError } from "@/shared/api/error";
import type { FieldPath, UseFormSetError } from "react-hook-form";
import type { CreateLocationRequest } from "./create-location-schema";

const fieldByErrorCode: Record<string, FieldPath<CreateLocationRequest>> = {
  "location.name.duplicate": "name",
};

const fieldByInvalidField: Record<string, FieldPath<CreateLocationRequest>> = {
  Name: "name",
  "Address.City": "address.city",
  "Address.Street": "address.street",
  "Address.House": "address.house",
  "Address.Apartment": "address.apartment",
};

export function applyLocationFormErrors(
  error: unknown,
  setError: UseFormSetError<CreateLocationRequest>,
): string | null {
  if (!(error instanceof ApiRequestError)) {
    return error instanceof Error ? error.message : "Failed to create location";
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
