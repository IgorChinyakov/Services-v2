export type ApiErrorMessage = {
  code: string;
  message: string;
  invalidField: string | null;
};

export class ApiRequestError extends Error {
  constructor(
    readonly status: number,
    readonly errors: ApiErrorMessage[],
  ) {
    super(errors.map((item) => item.message).join("; ") || `HTTP ${status}`);
  }
}
