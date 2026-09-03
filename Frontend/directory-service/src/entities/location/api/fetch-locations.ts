import { type Envelope, httpClient } from "@/shared/api";
import type { LocationsPagedResultDto } from "../model/types";

export const fetchLocations = async (
  signal: AbortSignal,
): Promise<LocationsPagedResultDto> => {
  const response = await httpClient.get<Envelope<LocationsPagedResultDto>>(
    "locations/",
    { signal },
  );

  const envelope = response.data;

  if (envelope.isError) {
    if (envelope.error === null) {
      throw new Error("Backend returned an invalid error response");
    }

    const message = envelope.error.messages
      .map((errorMessage) => errorMessage.message)
      .join("; ");

    throw new Error(message || "Failed to fetch locations");
  }

  if (envelope.error !== null) {
    throw new Error("Backend returned an inconsistent response");
  }

  if (envelope.result === null) {
    throw new Error("Backend returned an empty result");
  }

  return envelope.result;
};
