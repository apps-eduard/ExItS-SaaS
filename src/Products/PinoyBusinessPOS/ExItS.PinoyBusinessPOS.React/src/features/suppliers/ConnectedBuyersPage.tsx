import { Navigate, useParams } from "react-router-dom";

/**
 * Legacy supplier-shell routes for connected buyers.
 * Canonical UX is Customers → Businesses (/customers?kind=businesses).
 */
export function ConnectedBuyersPage() {
  const { relationshipId } = useParams<{ relationshipId?: string }>();
  if (relationshipId) {
    return <Navigate to={`/customers/business/${relationshipId}`} replace />;
  }
  return <Navigate to="/customers?kind=businesses" replace />;
}
