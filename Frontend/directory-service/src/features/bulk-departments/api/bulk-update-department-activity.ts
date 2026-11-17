import { PatchResultWithData } from "@/shared/api/results/patch-result";
import type {
  BulkDepartmentActivityRequest,
  BulkDepartmentResultDto,
} from "../model/types";

export const bulkUpdateDepartmentActivity = (
  request: BulkDepartmentActivityRequest,
  signal: AbortSignal,
): Promise<BulkDepartmentResultDto> =>
  PatchResultWithData<BulkDepartmentResultDto>({
    url: "departments/bulk/activity",
    data: request,
    signal,
  });
