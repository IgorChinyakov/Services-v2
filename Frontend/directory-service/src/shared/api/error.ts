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
