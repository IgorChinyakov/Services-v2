import { queryOptions } from "@tanstack/react-query";
import { fetchDepartmentsChildren } from "./fetch-departments-children";

export const departmentsChildrenQueryOptions = (departmentId: string) =>
  queryOptions({
    queryKey: ["departments", "tree", departmentId],
    queryFn: ({ signal }) => fetchDepartmentsChildren(departmentId, signal),
  });
