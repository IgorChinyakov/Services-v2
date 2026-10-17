import { infiniteQueryOptions } from "@tanstack/react-query";
import { fetchPositions } from "./fetch-positions";

export const positionsInfiniteQueryOptions = () =>
  infiniteQueryOptions({
    queryKey: ["positions", "infinite"] as const,
    initialPageParam: null as string | null,
    queryFn: ({ signal, pageParam }) => fetchPositions({ signal, pageParam }),
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  });
