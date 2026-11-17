import GetResult from "@/shared/api/get-result";
import type { LocationDetailsDto } from "../model/types";

export const fetchLocationById = (id: string, signal: AbortSignal) =>
  GetResult<LocationDetailsDto>({ url: `locations/${id}`, signal });
