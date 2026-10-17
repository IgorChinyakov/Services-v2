import DeleteResult from "@/shared/api/results/delete-result";

export const deleteLocation = async (
  id: string,
  signal: AbortSignal,
): Promise<void> => {
  return await DeleteResult({
    url: `locations/${id}/`,
    signal,
  });
};
