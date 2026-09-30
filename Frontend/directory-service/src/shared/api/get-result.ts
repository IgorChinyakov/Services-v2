import axios from "axios";
import { httpClient } from "./axios-client";
import { Envelope } from "./envelope";
import { ApiRequestError } from "./error";
import ExportData from "./get-repsonse-data-helper";

export type GetResultProps = {
  url: string;
  signal: AbortSignal;
};

export default async function GetResult<T>({
  url,
  signal,
}: GetResultProps): Promise<T> {
  let response;

  try {
    response = await httpClient.get<Envelope<T>>(url, { signal });
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
