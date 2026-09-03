import { ApiError } from "./error";

export type Envelope<T> = {
  result: T | null;
  error: ApiError | null;
  isError: boolean;
  timeGenerated: string;
};
