import GetResult from "@/shared/api/get-result";
import type { DepartmentsPagedResultDto } from "../../model/types";

export const fetchDepartments = ({
  signal,
  page,
  pageSize,
  search = null,
  locationIds = null,
  excludeIds = null,
  excludeSubtreeOf = null,
  isActive = null,
}: {
  signal: AbortSignal;
  page: number;
  pageSize: number;
  search?: string | null;
  locationIds?: string[] | null;
  excludeIds?: string[] | null;
  excludeSubtreeOf?: string | null;
  isActive?: boolean | null;
}): Promise<DepartmentsPagedResultDto> =>
  GetResult<DepartmentsPagedResultDto>({
    url: "departments/",
    signal,
    params: {
      page,
      pageSize,
      search,
      locationIds,
      excludeIds,
      excludeSubtreeOf,
      isActive: isActive === null ? null : String(isActive),
    },
  });
