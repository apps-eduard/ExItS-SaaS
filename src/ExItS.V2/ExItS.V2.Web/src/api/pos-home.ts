import { posRequest, type PosWorkspaceScope } from "./pos-http";
import { asRecord, num, str } from "./widget";

export type ManagementOverviewDto = {
  businessDate: string;
  todaySalesTotal: number;
  todaySaleCount: number;
  openUtangOutstanding: number;
  lowStockProductCount: number;
  nearExpiryLotCount: number;
  expiredLotCount: number;
  openShiftCount: number;
};

export async function getManagementOverview(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<ManagementOverviewDto> {
  const raw = asRecord(
    await posRequest<unknown>({
      path: "/api/v1/pos/management/overview",
      workspace,
      signal,
    }),
  );
  return {
    businessDate: str(raw, "businessDate", "BusinessDate"),
    todaySalesTotal: num(raw, "todaySalesTotal", "TodaySalesTotal"),
    todaySaleCount: num(raw, "todaySaleCount", "TodaySaleCount"),
    openUtangOutstanding: num(raw, "openUtangOutstanding", "OpenUtangOutstanding"),
    lowStockProductCount: num(raw, "lowStockProductCount", "LowStockProductCount"),
    nearExpiryLotCount: num(raw, "nearExpiryLotCount", "NearExpiryLotCount"),
    expiredLotCount: num(raw, "expiredLotCount", "ExpiredLotCount"),
    openShiftCount: num(raw, "openShiftCount", "OpenShiftCount"),
  };
}
