import axios from "axios";
import { httpClient } from "./axios-client";
import { Envelope } from "./envelope";
import { ApiRequestError } from "./error";
import ExportData from "./get-repsonse-data-helper";

type QueryParams = Record<string, string | string[] | number | null>;

export type GetResultProps = {
  url: string;
  signal: AbortSignal;
  params?: QueryParams;
};

export default async function GetResult<T>({
  url,
  signal,
  params,
}: GetResultProps): Promise<T> {
  let response;

  try {
    response = await httpClient.get<Envelope<T>>(url, { signal, params });
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
