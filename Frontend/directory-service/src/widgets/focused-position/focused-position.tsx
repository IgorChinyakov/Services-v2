"use client";

import { fetchPositionById, PositionCard } from "@/entities/position";
import { FocusedResultPanel } from "@/shared/ui/focused-result-panel";
import { useQuery } from "@tanstack/react-query";

export default function FocusedPosition({ positionId }: { positionId: string }) {
  const positionQuery = useQuery({
    queryKey: ["positions", "details", positionId],
    queryFn: ({ signal }) => fetchPositionById(positionId, signal),
  });

  return (
    <FocusedResultPanel
      tone="amber"
      isPending={positionQuery.isPending}
      isFetching={positionQuery.isFetching}
      error={positionQuery.error}
      hasData={positionQuery.data !== undefined}
      onRetry={() => void positionQuery.refetch()}
    >
      {positionQuery.data && <PositionCard dto={positionQuery.data} />}
    </FocusedResultPanel>
  );
}
