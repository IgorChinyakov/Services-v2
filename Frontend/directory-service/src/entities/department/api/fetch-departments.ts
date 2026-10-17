import GetResult from "@/shared/api/get-result";
import type { DepartmentsPagedResultDto } from "../model/types";

export const fetchDepartments = ({
  signal,
  page,
  pageSize,
  search = null,
  locationIds = null,
}: {
  signal: AbortSignal;
  page: number;
  pageSize: number;
  search: string | null;
  locationIds: string[] | null;
}): Promise<DepartmentsPagedResultDto> =>
  GetResult<DepartmentsPagedResultDto>({
    url: "departments/",
    signal,
    params: { page, pageSize, search, locationIds },
  });
