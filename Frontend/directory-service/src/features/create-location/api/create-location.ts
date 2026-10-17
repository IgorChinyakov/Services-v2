import type { LocationFormValues } from "@/entities/location";
import PostResult from "@/shared/api/results/post-result";

export const createLocation = async (
  request: LocationFormValues,
  signal: AbortSignal,
): Promise<string> => {
  return await PostResult<string>({
    url: "locations/",
    data: request,
    signal: signal,
  });
};
