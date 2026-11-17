"use client";

import { fetchLocationById, LocationCard } from "@/entities/location";
import EditLocationDialog from "@/features/edit-location/edit-location-dialog";
import { FocusedResultPanel } from "@/shared/ui/focused-result-panel";
import { useQuery } from "@tanstack/react-query";

export default function FocusedLocation({ locationId }: { locationId: string }) {
  const locationQuery = useQuery({
    queryKey: ["locations", "details", locationId],
    queryFn: ({ signal }) => fetchLocationById(locationId, signal),
  });

  return (
    <FocusedResultPanel
      tone="emerald"
      isPending={locationQuery.isPending}
      isFetching={locationQuery.isFetching}
      error={locationQuery.error}
      hasData={locationQuery.data !== undefined}
      onRetry={() => void locationQuery.refetch()}
    >
      {locationQuery.data && (
        <LocationCard
          dto={locationQuery.data}
          actions={
            locationQuery.data.deletedAt === null
              ? <EditLocationDialog location={locationQuery.data} />
              : undefined
          }
        />
      )}
    </FocusedResultPanel>
  );
}
