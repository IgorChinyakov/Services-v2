import { PositionsList } from "@/widgets/positions-list";
import { FocusedPosition } from "@/widgets/focused-position";
import type { Metadata } from "next";
import { Suspense } from "react";

export const metadata: Metadata = {
  title: "Positions | Directory Service",
};

export default async function Positions({
  searchParams,
}: {
  searchParams: Promise<{ focusId?: string | string[] }>;
}) {
  const { focusId } = await searchParams;
  const positionId = Array.isArray(focusId) ? focusId[0] : focusId;

  return (
    <Suspense
      fallback={
        <p className="p-6 text-sm text-muted-foreground">Загрузка позиций…</p>
      }
    >
      <PositionsList
        focusedPosition={
          positionId ? <FocusedPosition positionId={positionId} /> : null
        }
      />
    </Suspense>
  );
}
