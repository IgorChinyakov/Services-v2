import { queryOptions } from "@tanstack/react-query";
import { fetchGlobalSearch } from "./fetch-global-search";

export const globalSearchQueryOptions = (q: string) => {
  const search = q.trim();

  return queryOptions({
    queryKey: ["search", search] as const,
    queryFn: ({ signal }) => fetchGlobalSearch(search, signal),
    enabled: search.length >= 2 && search.length <= 200,
  });
};
