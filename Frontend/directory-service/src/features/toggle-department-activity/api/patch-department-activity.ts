import PatchResult from "@/shared/api/results/patch-result";

export const patchDepartmentActivity = (
  departmentId: string,
  isActive: boolean,
  signal: AbortSignal,
): Promise<void> =>
  PatchResult({
    url: `departments/${departmentId}/activity`,
    data: { isActive },
    signal,
  });
