import GetResult from "@/shared/api/get-result";
import type { GlobalSearchResultDto } from "../model/types";

export const fetchGlobalSearch = (
  q: string,
  signal: AbortSignal,
): Promise<GlobalSearchResultDto[]> =>
  GetResult<GlobalSearchResultDto[]>({
    url: "search",
    signal,
    params: { q: q.trim() },
  });
