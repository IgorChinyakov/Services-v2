export type ApiError = {
  messages: ApiErrorMessage[];
  type: ApiErrorType;
};

export type ApiErrorMessage = {
  code: string;
  message: string;
  invalidField: string | null;
};

export type ApiErrorType =
  | "Validation"
  | "NotFound"
  | "Failure"
  | "Conflict"
  | "Authentication"
  | "Authorization";

export class ApiRequestError extends Error {
  constructor(
    readonly status: number,
    readonly errors: ApiErrorMessage[],
  ) {
    super(errors.map((item) => item.message).join("; ") || `HTTP ${status}`);
  }
}
