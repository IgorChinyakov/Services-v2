import { queryOptions } from "@tanstack/react-query";
import { fetchDepartments } from "./fetch-departments";

export const departmentListQueryOptions = (
  page: number,
  pageSize: number,
  search: string | null = null,
  locationIds: string[] | null = null,
) =>
  queryOptions({
    queryKey: ["departments", pageSize, page, search, locationIds] as const,
    queryFn: ({ signal }) =>
      fetchDepartments({ signal, page, pageSize, search, locationIds }),
  });
