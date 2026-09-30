import { locationListQueryOptions } from "@/entities/location";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createLocation } from "./api/create-location";
import { CreateLocationRequest } from "./model/create-location-schema";

export function useCreateLocation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      values,
      signal,
    }: {
      values: CreateLocationRequest;
      signal: AbortSignal;
    }) => createLocation(values, signal),
    onSuccess: () =>
      void queryClient.invalidateQueries({
        queryKey: locationListQueryOptions().queryKey,
      }),
  });
}
