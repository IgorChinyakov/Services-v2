import GetResult from "@/shared/api/get-result";
import type { PositionDto } from "../model/types";

export const fetchPositionById = (id: string, signal: AbortSignal) =>
  GetResult<PositionDto>({ url: `positions/${id}`, signal });
