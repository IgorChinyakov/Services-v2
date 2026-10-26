import { UpdateDepartmentParentRequest } from "@/entities/department/model/types";
import PutResult from "@/shared/api/results/put-result";

export const editParent = async (
  departmentId: string,
  request: UpdateDepartmentParentRequest,
  signal: AbortSignal,
): Promise<void> => {
  return await PutResult({
    url: `departments/${departmentId}/parent`,
    data: request,
    signal: signal,
  });
};
