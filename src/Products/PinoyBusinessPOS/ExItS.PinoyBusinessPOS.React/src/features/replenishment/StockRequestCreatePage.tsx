import { Navigate } from "react-router-dom";

/** Legacy create route — retail warehouse request-stock is the canonical UX. */
export function StockRequestCreatePage() {
  return <Navigate to="/warehouse/request-stock" replace />;
}
