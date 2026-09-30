import { AxiosResponse } from "axios";
import { Envelope } from "./envelope";
import { ApiRequestError } from "./error";

export default function ExportData<T>(response: AxiosResponse<Envelope<T>>) {
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

  if (envelope.result === null) {
    throw new Error("Backend returned an empty result");
  }

  return envelope.result;
}
