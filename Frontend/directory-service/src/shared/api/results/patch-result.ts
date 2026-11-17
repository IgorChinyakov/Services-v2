import axios from "axios";
import { httpClient } from "../axios-client";
import { Envelope } from "../envelope";
import { ApiRequestError } from "../error";
import ExportData, { ExportDataWithoutResult } from "../get-repsonse-data-helper";

export type PatchResultProps = {
  data: unknown;
  url: string;
  signal: AbortSignal;
};

async function sendPatch<T>({
  data,
  url,
  signal,
}: PatchResultProps) {
  let response;

  try {
    response = await httpClient.patch<Envelope<T>>(url, data, { signal });
  } catch (error) {
    if (axios.isAxiosError<Envelope<unknown>>(error)) {
      const errors = error.response?.data?.errors;

      if (Array.isArray(errors) && errors.length > 0) {
        throw new ApiRequestError(error.response?.status ?? 500, errors);
      }
    }

    throw error;
  }

  return response;
}

export default async function PatchResult(props: PatchResultProps): Promise<void> {
  const response = await sendPatch<null>(props);
  return ExportDataWithoutResult(response);
}

export async function PatchResultWithData<T>(props: PatchResultProps): Promise<T> {
  const response = await sendPatch<T>(props);
  return ExportData<T>(response);
}
