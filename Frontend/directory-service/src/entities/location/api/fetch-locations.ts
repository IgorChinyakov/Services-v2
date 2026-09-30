import type { LocationsPagedResultDto } from "../model/types";
import GetResult from "@/shared/api/get-result";

export const fetchLocations = async (
  signal: AbortSignal,
): Promise<LocationsPagedResultDto> => {
  return await GetResult<LocationsPagedResultDto>({
    url: "locations",
    signal,
  });
};
