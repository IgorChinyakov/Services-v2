import { LocationFormValues } from "@/entities/location";
import PatchResult from "@/shared/api/results/patch-result";

export const patchLocation = async (
  id: string,
  request: LocationFormValues,
  signal: AbortSignal,
): Promise<void> => {
  return await PatchResult({
    url: `locations/${id}/`,
    data: request,
    signal: signal,
  });
};
