import { useEffect, useState } from "react";
import { philippinesFulfillmentClock } from "@/features/customer-ordering/preferred-fulfillment-display";

/** Philippines (Asia/Manila) date + time for the checkout fulfillment title row. */
export function CheckoutFulfillmentPhClock({ testId = "checkout-fulfillment-ph-clock" }: { testId?: string }) {
  const [clock, setClock] = useState(() => philippinesFulfillmentClock());

  useEffect(() => {
    const tick = () => setClock(philippinesFulfillmentClock());
    tick();
    const id = window.setInterval(tick, 15_000);
    return () => window.clearInterval(id);
  }, []);

  return (
    <div className="pc-checkout-fulfillment-clock" data-testid={testId} aria-live="polite">
      <span className="pc-checkout-fulfillment-clock__date" data-testid={`${testId}-date`}>
        {clock.dateLong}
      </span>
      <span className="pc-checkout-fulfillment-clock__time" data-testid={`${testId}-time`}>
        {clock.timeDisplay}
      </span>
    </div>
  );
}
