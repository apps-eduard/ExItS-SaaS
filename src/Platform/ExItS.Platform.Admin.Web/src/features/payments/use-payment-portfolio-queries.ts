import { useQuery } from "@tanstack/react-query";
import {
  getPayment,
  listPaymentPortfolio,
  type PaymentPortfolioUrlState,
} from "@/api/payments/payment-client";
import { env } from "@/lib/env";

export const paymentPortfolioQueryKey = (state: PaymentPortfolioUrlState) =>
  [
    "payments",
    "portfolio",
    state.page,
    state.pageSize,
    state.status,
    state.productCode,
    state.method,
  ] as const;

export const paymentDetailQueryKey = (paymentId: string) =>
  ["payments", "detail", paymentId] as const;

export function usePaymentPortfolioQuery(state: PaymentPortfolioUrlState, enabled: boolean) {
  return useQuery({
    queryKey: paymentPortfolioQueryKey(state),
    enabled,
    queryFn: ({ signal }) => listPaymentPortfolio(env.platformApiBaseUrl, state, signal),
  });
}

export function usePaymentDetailQuery(paymentId: string | null, enabled: boolean) {
  return useQuery({
    queryKey: paymentId ? paymentDetailQueryKey(paymentId) : ["payments", "detail"],
    enabled: enabled && paymentId != null,
    queryFn: ({ signal }) => getPayment(env.platformApiBaseUrl, paymentId!, signal),
  });
}
