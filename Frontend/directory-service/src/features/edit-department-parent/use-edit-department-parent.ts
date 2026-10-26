import { UpdateDepartmentParentRequest } from "@/entities/department/model/types";
import { toast } from "@/shared/ui/toast";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { editParent } from "./api/edit-parent";

export function useEditDepartmentParent() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      departmentId,
      values,
      signal,
    }: {
      departmentId: string;
      values: UpdateDepartmentParentRequest;
      signal: AbortSignal;
    }) => editParent(departmentId, values, signal),
    onSuccess: (_result, { departmentId }) => {
      void queryClient.invalidateQueries({
        queryKey: ["department", departmentId],
      });
      void queryClient.invalidateQueries({
        queryKey: ["departments"],
      });
    },
    onError: (error, { signal }) => {
      if (signal.aborted) {
        return; // пользователь закрыл форму — это не ошибка для него
      }

      toast.add({
        type: "error",
        description: error.message || "Не удалось изменить родителя подразделения.",
      });
    },
  });
}
