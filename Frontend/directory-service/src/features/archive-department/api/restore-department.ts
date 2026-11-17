import PatchResult from "@/shared/api/results/patch-result";

export const restoreDepartment = (
  id: string,
  signal: AbortSignal,
): Promise<void> =>
  PatchResult({ url: `departments/${id}/restore`, data: null, signal });
