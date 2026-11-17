import { toast } from "@/shared/ui/toast";
import { useQueryClient, useMutation } from "@tanstack/react-query";
import { restoreLocation } from "./api/restore-location";

export const useRestoreLocation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, signal }: { id: string; signal: AbortSignal }) =>
      restoreLocation(id, signal),
    onError: (error, { signal }) => {
      if (signal.aborted) {
        return; // пользователь закрыл форму — это не ошибка для него
      }

      toast.add({ type: "error", description: error.message });
    },
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["locations"],
      }),
  });
};
