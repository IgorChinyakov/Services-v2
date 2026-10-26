import { queryOptions } from "@tanstack/react-query";
import { fetchDepartmentsTree } from "./fetch-departments-tree";

export const departmentsTreeQueryOptions = queryOptions({
  queryKey: ["departments", "tree"],
  queryFn: ({ signal }) => fetchDepartmentsTree(signal),
});
