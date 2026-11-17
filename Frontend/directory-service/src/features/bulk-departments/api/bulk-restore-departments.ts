import PostResult from "@/shared/api/results/post-result";
import type { BulkDepartmentRequest, BulkDepartmentResultDto } from "../model/types";

export const bulkRestoreDepartments = (
  request: BulkDepartmentRequest,
  signal: AbortSignal,
): Promise<BulkDepartmentResultDto> =>
  PostResult<BulkDepartmentResultDto>({
    url: "departments/bulk/restore",
    data: request,
    signal,
  });
