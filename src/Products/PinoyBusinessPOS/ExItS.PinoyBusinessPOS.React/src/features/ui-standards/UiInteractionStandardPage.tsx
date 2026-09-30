import { Navigate } from "react-router-dom";

/**
 * Alias — TASK-66A route consolidates into canonical /ui-standards.
 */
export function UiInteractionStandardPage() {
  return <Navigate to="/ui-standards?category=buttons" replace />;
}
