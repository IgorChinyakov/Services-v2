import { queryOptions } from "@tanstack/react-query";
import { fetchLocations } from "./fetch-locations";

export const locationListQueryOptions = () =>
  queryOptions({
    queryKey: ["locations"] as const,
    queryFn: ({ signal }) => fetchLocations(signal),
  });
