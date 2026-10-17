import GetResult from "@/shared/api/get-result";
import { PositionsCursorPageDto } from "../model/types";

export const fetchPositions = async ({
  signal,
  pageParam,
}: {
  signal: AbortSignal;
  pageParam: string | null;
}): Promise<PositionsCursorPageDto> => {
  return await GetResult<PositionsCursorPageDto>({
    url: "positions",
    signal,
    params: {
      size: 20,
      cursor: pageParam,
    },
  });
};
