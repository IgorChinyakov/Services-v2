import GetResult from "@/shared/api/get-result";
import { DepartmentTreeNodeDto } from "../../model/types";

export const fetchDepartmentsChildren = (
  departmentId: string,
  signal: AbortSignal,
) => {
  return GetResult<DepartmentTreeNodeDto[]>({
    url: `departments/${departmentId}/children`,
    signal: signal,
  });
};
