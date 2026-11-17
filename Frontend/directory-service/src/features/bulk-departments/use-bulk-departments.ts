import { useMutation, useQueryClient } from "@tanstack/react-query";
import { bulkArchiveDepartments } from "./api/bulk-archive-departments";
import { bulkRestoreDepartments } from "./api/bulk-restore-departments";
import { bulkUpdateDepartmentActivity } from "./api/bulk-update-department-activity";
import { toast } from "@/shared/ui/toast";
import { getBulkDepartmentRequestErrorMessage } from "./model/get-request-error-message";
import { getBulkDepartmentResultMessage } from "./model/get-result-message";

type BulkVariables = {
  action: "archive" | "restore" | "activate" | "deactivate";
  ids: string[];
  signal: AbortSignal;
};

export default function useBulkDepartments() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationKey: ["departments", "bulk"],
    mutationFn: ({ action, ids, signal }: BulkVariables) => {
      switch (action) {
        case "archive":
          return bulkArchiveDepartments({ ids }, signal);
        case "restore":
          return bulkRestoreDepartments({ ids }, signal);

        case "activate":
        case "deactivate":
          return bulkUpdateDepartmentActivity(
            { ids, isActive: action === "activate" },
            signal,
          );
      }
    },
    onSuccess: async (data, { action }) => {
      if (data.failedCount === 0 && data.succeededCount > 0) {
        toast.add({
          type: "success",
          description: getBulkDepartmentResultMessage(action, data),
        });
      }

      if (data.succeededCount === 0) return;

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["departments"] }),
        queryClient.invalidateQueries({ queryKey: ["department"] }),
        queryClient.invalidateQueries({ queryKey: ["locations"] }),
        queryClient.invalidateQueries({ queryKey: ["positions"] }),
      ]);
    },
    onError: (error, { signal }) => {
      if (signal.aborted) {
        return; // пользователь закрыл форму — это не ошибка для него
      }

      const message = getBulkDepartmentRequestErrorMessage(error);
      if (message) {
        toast.add({ type: "error", description: message });
      }
    },
  });
}
