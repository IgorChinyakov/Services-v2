import { LocationsList } from "@/widgets/locations-list";
import type { Metadata } from "next";
import { Suspense } from "react";

export const metadata: Metadata = {
  title: "Locations | Directory Service",
};

export default function Locations() {
  return (
    <Suspense
      fallback={
        <div className="mx-auto w-full max-w-7xl rounded-2xl border bg-muted/30 p-6 text-sm text-muted-foreground">
          Загрузка локаций...
        </div>
      }
    >
      <LocationsList />
    </Suspense>
  );
}
