import { infiniteQueryOptions } from "@tanstack/react-query";
import { fetchPositions } from "./fetch-positions";

export const positionsInfiniteQueryOptions = (
  departmentId: string | null = null,
) =>
  infiniteQueryOptions({
    queryKey: ["positions", "infinite", departmentId] as const,
    initialPageParam: null as string | null,
    queryFn: ({ signal, pageParam }) =>
      fetchPositions({ signal, pageParam, departmentId }),
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  });
