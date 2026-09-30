import { queryOptions } from "@tanstack/react-query";
import { fetchDepartments } from "./fetch-departments";

export const departmentListQueryOptions = () =>
  queryOptions({
    queryKey: ["departments"] as const,
    queryFn: ({ signal }) => fetchDepartments({ signal }),
  });
