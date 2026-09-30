import GetResult from "@/shared/api/get-result";
import type { DepartmentsPagedResultDto } from "../model/types";

export const fetchDepartments = ({
  signal,
}: {
  signal: AbortSignal;
}): Promise<DepartmentsPagedResultDto> =>
  GetResult<DepartmentsPagedResultDto>({
    url: "departments/",
    signal,
  });
