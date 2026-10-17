import axios from "axios";
import { httpClient } from "../axios-client";
import { Envelope } from "../envelope";
import { ApiRequestError } from "../error";
import { ExportDataWithoutResult } from "../get-repsonse-data-helper";

export type PatchResultProps = {
  url: string;
  signal: AbortSignal;
};

export default async function DeleteResult({ url, signal }: PatchResultProps) {
  let response;

  try {
    response = await httpClient.delete<Envelope<null>>(url, { signal });
  } catch (error) {
    if (axios.isAxiosError<Envelope<unknown>>(error)) {
      const errors = error.response?.data?.errors;

      if (Array.isArray(errors) && errors.length > 0) {
        throw new ApiRequestError(error.response?.status ?? 500, errors);
      }
    }

    throw error;
  }

  return ExportDataWithoutResult(response);
}
