import type { LucideIcon } from "lucide-react";
import {
  ArrowLeftRight,
  BarChart3,
  Boxes,
  ClipboardList,
  Clock3,
  LayoutDashboard,
  Package,
  PackagePlus,
  Receipt,
  RotateCcw,
  TrendingUp,
  Truck,
  Wallet,
} from "lucide-react";
import type { ClassicReportKind, OperationalReportKind } from "@/features/reports/report-access";

const operationalReportIcons: Record<OperationalReportKind, LucideIcon> = {
  overview: LayoutDashboard,
  "sales-summary": BarChart3,
  "sales-by-payment": Wallet,
  "sales-by-product": Package,
  returns: RotateCcw,
  profitability: TrendingUp,
  "product-profitability": BarChart3,
  shifts: Clock3,
  "cash-variance": Wallet,
  "inventory-status": Boxes,
  "inventory-movements": ArrowLeftRight,
  "stock-count-variance": ClipboardList,
  "purchasing-summary": PackagePlus,
  "purchase-outstanding": Truck,
  "supplier-purchasing": Truck,
  "expenses-summary": Receipt,
  "utang-by-product": Wallet,
};

const classicReportIcons: Record<ClassicReportKind, LucideIcon> = {
  sales: BarChart3,
  utang: Wallet,
  inventory: Boxes,
  expenses: Receipt,
};

export function iconForOperationalReport(kind: OperationalReportKind): LucideIcon {
  return operationalReportIcons[kind];
}

export function iconForClassicReport(kind: ClassicReportKind): LucideIcon {
  return classicReportIcons[kind];
}
