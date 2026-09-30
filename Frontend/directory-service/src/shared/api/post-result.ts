import axios from "axios";
import { httpClient } from "./axios-client";
import { Envelope } from "./envelope";
import { ApiRequestError } from "./error";
import ExportData from "./get-repsonse-data-helper";

export type PostResultProps = {
  data: unknown;
  url: string;
  signal: AbortSignal;
};

export default async function PostResult<T>({
  data,
  url,
  signal,
}: PostResultProps): Promise<T> {
  let response;

  try {
    response = await httpClient.post<Envelope<T>>(url, data, { signal });
  } catch (error) {
    if (axios.isAxiosError<Envelope<unknown>>(error)) {
      const errors = error.response?.data?.errors;

      if (Array.isArray(errors) && errors.length > 0) {
        throw new ApiRequestError(error.response?.status ?? 500, errors);
      }
    }

    throw error;
  }

  return ExportData<T>(response);
}
