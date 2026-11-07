import { Search, X } from "lucide-react";
import * as React from "react";
import { cn } from "@/lib/utils";
import { Input } from "./input";

type SearchFieldProps = Omit<React.ComponentProps<"input">, "type"> & {
  onClear?: () => void;
};

function SearchField({
  className,
  value,
  onClear,
  ...props
}: SearchFieldProps) {
  const showClear = typeof value === "string" && value.length > 0;

  return (
    <div className={cn("relative", className)}>
      <Search
        className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
        aria-hidden
      />
      <Input
        type="search"
        value={value}
        className="pl-9 pr-9"
        {...props}
      />
      {showClear ? (
        <button
          type="button"
          className="absolute right-2 top-1/2 inline-flex size-6 -translate-y-1/2 items-center justify-center rounded-md text-primary hover:bg-muted"
          onClick={onClear}
          aria-label="Clear search"
        >
          <X className="size-4" strokeWidth={3} />
        </button>
      ) : null}
    </div>
  );
}

export { SearchField };
