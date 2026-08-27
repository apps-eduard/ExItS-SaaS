import { Navigate } from "react-router-dom";

/** @deprecated Add person is inline on PeoplePage */
export function AddLocalPersonPage() {
  return <Navigate to="/personal/people?add=1&kind=walkin" replace />;
}
