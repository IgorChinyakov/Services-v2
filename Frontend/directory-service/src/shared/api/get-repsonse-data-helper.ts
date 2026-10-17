import { AxiosResponse } from "axios";
import { Envelope } from "./envelope";
import { ApiRequestError } from "./error";

export default function ExportData<T>(response: AxiosResponse<Envelope<T>>) {
  ExtractError(response);

  const result = response.data.result;

  if (result === null) {
    throw new Error("Backend returned an empty result");
  }

  return result;
}

export function ExportDataWithoutResult(
  response: AxiosResponse<Envelope<null>>,
) {
  ExtractError(response);
}

function ExtractError(response: AxiosResponse<Envelope<unknown>>) {
  const envelope = response.data;

  if (!Array.isArray(envelope.errors)) {
    throw new Error("Backend returned an invalid error response");
  }

  if (envelope.isError) {
    if (envelope.errors.length === 0) {
      throw new Error("Backend returned an inconsistent response");
    }

    throw new ApiRequestError(response.status, envelope.errors);
  }

  if (envelope.errors.length > 0) {
    throw new Error("Backend returned an inconsistent response");
  }
}
