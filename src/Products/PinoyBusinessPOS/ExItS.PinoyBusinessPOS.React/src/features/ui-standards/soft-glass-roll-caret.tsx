/** Soft Glass roll up/down mark — stroke caret (chevron), not a solid triangle. */
export function SoftGlassRollCaret({ direction }: { direction: "up" | "down" }) {
  return (
    <svg
      className={"ui-standards-qdate__roll-caret ui-standards-qdate__roll-caret--" + direction}
      viewBox="0 0 24 24"
      width="16"
      height="16"
      fill="none"
      aria-hidden
    >
      <path
        d={direction === "up" ? "M6 14.5 12 8.5 18 14.5" : "M6 9.5 12 15.5 18 9.5"}
        stroke="currentColor"
        strokeWidth="2.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}
