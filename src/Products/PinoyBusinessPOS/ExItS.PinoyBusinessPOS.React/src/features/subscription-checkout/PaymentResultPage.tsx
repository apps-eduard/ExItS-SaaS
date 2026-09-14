import { Navigate, useParams } from "react-router-dom";

/** Legacy /result route — checkout hub owns all payment states. */
export function PaymentResultPage() {
  const { paymentId = "" } = useParams();
  return <Navigate to={`/subscription-checkout/${paymentId}`} replace />;
}
