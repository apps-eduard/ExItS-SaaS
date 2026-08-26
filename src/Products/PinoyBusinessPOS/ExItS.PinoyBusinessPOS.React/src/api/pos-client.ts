import { posRequest, type ApiRequestOptions } from "@/api/http";

/** Typed POS HTTP foundation. No selling, checkout, or invented endpoints. */
export const posApi = {
  request<T>(options: ApiRequestOptions): Promise<T> {
    return posRequest<T>(options);
  },
};
