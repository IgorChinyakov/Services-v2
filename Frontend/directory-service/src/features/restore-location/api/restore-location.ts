import PatchResult from "@/shared/api/results/patch-result";

export const restoreLocation = async (
  id: string,
  signal: AbortSignal,
): Promise<void> => {
  return await PatchResult({
    url: `locations/${id}/restore`,
    data: null,
    signal,
  });
};
