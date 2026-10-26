import GetResult from "@/shared/api/get-result";
import { DepartmentTreeNodeDto } from "../../model/types";

export const fetchDepartmentsTree = (signal: AbortSignal) => {
  return GetResult<DepartmentTreeNodeDto[]>({
    url: "departments/tree",
    signal: signal,
  });
};
