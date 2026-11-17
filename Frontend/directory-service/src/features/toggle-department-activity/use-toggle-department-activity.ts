import { useMutation, useQueryClient } from "@tanstack/react-query";
import { patchDepartmentActivity } from "./api/patch-department-activity";
import { toast } from "@/shared/ui/toast";
import { DepartmentsPagedResultDto } from "@/entities/department";
import { getDepartmentActivityErrorMessage } from "./model/get-error-message";

export function useToggleDepartmentActivity() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ["departments", "activity"],
    onMutate: async ({ departmentId, isActive }) => {
      const filter = { queryKey: ["departments", "list"] };

      await queryClient.cancelQueries(filter);

      const previousData =
        queryClient.getQueriesData<DepartmentsPagedResultDto>(filter);

      queryClient.setQueriesData<DepartmentsPagedResultDto>(filter, (old) => {
        if (!old) return old;

        return {
          ...old,
          items: old.items.map((value) => {
            if (value.id === departmentId)
              return {
                ...value,
                isActive: isActive,
              };

            return value;
          }),
        };
      });

      return { previousData };
    },
    mutationFn: ({
      departmentId,
      isActive,
      signal,
    }: {
      departmentId: string;
      isActive: boolean;
      signal: AbortSignal;
    }) => patchDepartmentActivity(departmentId, isActive, signal),
    onError: (error, { departmentId, signal }, onMutateData) => {
      for (const [queryKey, snapshot] of onMutateData?.previousData ?? []) {
        const previousDepartment = snapshot?.items.find(
          (department) => department.id === departmentId,
        );

        if (!previousDepartment) continue;

        queryClient.setQueryData<DepartmentsPagedResultDto>(
          queryKey,
          (current) => {
            if (!current) return current;

            return {
              ...current,
              items: current.items.map((department) =>
                department.id === departmentId
                  ? {
                      ...department,
                      isActive: previousDepartment.isActive,
                    }
                  : department,
              ),
            };
          },
        );
      }

      if (signal.aborted) return;

      toast.add({
        type: "error",
        description: getDepartmentActivityErrorMessage(error),
      });
    },
    onSettled: async (data, error, { departmentId }) => {
      await queryClient.invalidateQueries({
        queryKey: ["department", departmentId],
      });

      if (
        queryClient.isMutating({
          mutationKey: ["departments", "activity"],
        }) === 1
      ) {
        await queryClient.invalidateQueries({
          queryKey: ["departments"],
        });
      }
    },
  });
}
