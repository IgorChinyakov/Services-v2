import { queryOptions } from "@tanstack/react-query";
import { fetchDepartments } from "./fetch-departments";

export const departmentListQueryOptions = ({
  page,
  pageSize,
  search = null,
  locationIds = null,
  excludeIds = null,
  excludeSubtreeOf = null,
  isActive = null,
  isArchived = null,
}: {
  page: number;
  pageSize: number;
  search?: string | null;
  locationIds?: string[] | null;
  excludeIds?: string[] | null;
  excludeSubtreeOf?: string | null;
  isActive?: boolean | null;
  isArchived?: boolean | null;
}) =>
  queryOptions({
    queryKey: [
      "departments",
      "list",
      {
        page,
        pageSize,
        search,
        locationIds,
        excludeIds,
        excludeSubtreeOf,
        isActive,
        isArchived,
      },
    ] as const,
    queryFn: ({ signal }) =>
      fetchDepartments({
        signal,
        page,
        pageSize,
        search,
        locationIds,
        excludeIds,
        excludeSubtreeOf,
        isActive,
        isArchived,
      }),
  });
