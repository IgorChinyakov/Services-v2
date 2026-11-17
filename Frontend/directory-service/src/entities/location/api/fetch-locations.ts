import type { LocationsPagedResultDto } from "../model/types";
import GetResult from "@/shared/api/get-result";

export const fetchLocations = async (
  signal: AbortSignal,
  pageSize: number,
  page: number,
  search: string | null = null,
  departmentIds: string[] | null = null,
  isArchived: boolean = false,
): Promise<LocationsPagedResultDto> => {
  return await GetResult<LocationsPagedResultDto>({
    url: "locations",
    signal,
    params: {
      pageSize,
      page,
      search,
      departmentIds,
      isArchived,
    },
  });
};
