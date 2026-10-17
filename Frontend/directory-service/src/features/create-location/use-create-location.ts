import { type LocationFormValues } from "@/entities/location";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createLocation } from "./api/create-location";
import { UseFormSetError } from "react-hook-form";
import { applyLocationFormErrors } from "./model/apply-location-form-errors";
import { toast } from "@/shared/ui/toast";

export function useCreateLocation(
  setError: UseFormSetError<LocationFormValues>,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      values,
      signal,
    }: {
      values: LocationFormValues;
      signal: AbortSignal;
    }) => createLocation(values, signal),
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: ["locations"],
      }),
    onError: (error, { signal }) => {
      if (signal.aborted) {
        return; // пользователь закрыл форму — это не ошибка для него
      }

      const message = applyLocationFormErrors(error, setError);
      if (message) {
        toast.add({ type: "error", description: message });
      }
    },
  });
}
