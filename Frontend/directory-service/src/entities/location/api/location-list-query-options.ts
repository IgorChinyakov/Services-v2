import { queryOptions } from "@tanstack/react-query";
import { fetchLocations } from "./fetch-locations";

export const locationListQueryOptions = (
  page: number,
  pageSize: number,
  search: string | null = null,
  departmentIds: string[] | null = null,
) =>
  queryOptions({
    queryKey: ["locations", pageSize, page, search, departmentIds] as const,
    queryFn: ({ signal }) =>
      fetchLocations(signal, pageSize, page, search, departmentIds),
  });
