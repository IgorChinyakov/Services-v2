import { LocationsList } from "@/widgets/locations-list";
import { FocusedLocation } from "@/widgets/focused-location";
import type { Metadata } from "next";
import { Suspense } from "react";

export const metadata: Metadata = {
  title: "Locations | Directory Service",
};

export default async function Locations({
  searchParams,
}: {
  searchParams: Promise<{ focusId?: string | string[] }>;
}) {
  const { focusId } = await searchParams;
  const locationId = Array.isArray(focusId) ? focusId[0] : focusId;
  return (
    <Suspense
      fallback={
        <div className="mx-auto w-full max-w-7xl rounded-2xl border bg-muted/30 p-6 text-sm text-muted-foreground">
          Загрузка локаций...
        </div>
      }
    >
      <LocationsList
        focusedLocation={
          locationId ? <FocusedLocation locationId={locationId} /> : null
        }
      />
    </Suspense>
  );
}
