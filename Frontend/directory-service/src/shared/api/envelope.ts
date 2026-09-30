import type { ApiErrorMessage } from "./error";

export type Envelope<T> = {
  result: T | null;
  errors: ApiErrorMessage[];
  isError: boolean;
  timeGenerated: string;
};
