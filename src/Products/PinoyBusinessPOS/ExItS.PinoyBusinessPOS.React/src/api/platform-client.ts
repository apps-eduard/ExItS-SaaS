import { platformRequest, type ApiRequestOptions } from "@/api/http";

/** Typed Platform HTTP foundation. No auth or invented product endpoints. */
export const platformApi = {
  request<T>(options: ApiRequestOptions): Promise<T> {
    return platformRequest<T>(options);
  },
};
