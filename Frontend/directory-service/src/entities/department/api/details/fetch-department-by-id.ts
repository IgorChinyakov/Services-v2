import GetResult from "@/shared/api/get-result";
import type { DepartmentDetailsDto } from "../../model/types";

export const fetchDepartmentById = (
  departmentId: string,
  signal: AbortSignal,
): Promise<DepartmentDetailsDto> =>
  GetResult<DepartmentDetailsDto>({
    url: `departments/${departmentId}`,
    signal,
  });
