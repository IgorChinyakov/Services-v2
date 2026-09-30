import { CreateLocationRequest } from "../model/create-location-schema";
import PostResult from "@/shared/api/post-result";

export const createLocation = async (
  request: CreateLocationRequest,
  signal: AbortSignal,
): Promise<string> => {
  return await PostResult<string>({
    url: "locations/",
    data: request,
    signal: signal,
  });
};
