import DeleteResult from "@/shared/api/results/delete-result";

export const archiveDepartment = (
  id: string,
  signal: AbortSignal,
): Promise<void> => DeleteResult({ url: `departments/${id}`, signal });
