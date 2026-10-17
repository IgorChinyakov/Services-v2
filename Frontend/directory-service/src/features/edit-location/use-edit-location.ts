import { LocationFormValues } from "@/entities/location";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { UseFormSetError } from "react-hook-form";
import { patchLocation } from "./api/patch-location";
import { toast } from "@/shared/ui/toast";
import { applyLocationFormErrors } from "../create-location/model/apply-location-form-errors";

export default function UseEditLocation(
  setError: UseFormSetError<LocationFormValues>,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      id,
      values,
      signal,
    }: {
      id: string;
      values: LocationFormValues;
      signal: AbortSignal;
    }) => patchLocation(id, values, signal),
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
