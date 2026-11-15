type ChevronProps = {
  direction: "previous" | "next";
};

export function CalendarGlyph() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
      <path
        d="M7 4.5v2M17 4.5v2M5.75 9.25h12.5M6.75 6.25h10.5A1.5 1.5 0 0 1 18.75 7.75v9.5a1.5 1.5 0 0 1-1.5 1.5H6.75a1.5 1.5 0 0 1-1.5-1.5v-9.5a1.5 1.5 0 0 1 1.5-1.5Z"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

export function ClockGlyph() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
      <circle cx="12" cy="12" r="7.25" fill="none" stroke="currentColor" strokeWidth="1.75" />
      <path d="M12 8.25V12l2.75 1.75" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

export function DateTimeGlyph() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
      <path
        d="M7 4.25v1.5M15 4.25v1.5M5.75 8.5h10.5M6.5 5.75h9A1.25 1.25 0 0 1 16.75 7v4.1"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.75"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <circle cx="16.25" cy="15.75" r="4.25" fill="none" stroke="currentColor" strokeWidth="1.75" />
      <path d="M16.25 13.75v2.15l1.4.9" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

export function ChevronGlyph({ direction }: ChevronProps) {
  const path = direction === "previous" ? "M14.5 6.5 9 12l5.5 5.5" : "M9.5 6.5 15 12l-5.5 5.5";
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
      <path d={path} fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
