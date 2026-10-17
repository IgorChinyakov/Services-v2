import { useMutation, useQueryClient } from "@tanstack/react-query";
import { deleteLocation } from "./api/delete-location";
import { toast } from "@/shared/ui/toast";

export const useDeleteLocation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, signal }: { id: string; signal: AbortSignal }) =>
      deleteLocation(id, signal),
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
