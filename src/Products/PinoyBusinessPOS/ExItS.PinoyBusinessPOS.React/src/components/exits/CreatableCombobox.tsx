import {
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent,
} from "react";
import { ChevronDown, Search } from "lucide-react";
import { cn } from "@/lib/cn";

/**
 * Searchable creatable combobox for free-text catalogs (department, job title).
 * Presentation only — callers own persistence / org scoping.
 */
export function CreatableCombobox({
  value,
  onChange,
  options,
  placeholder = "Select or type…",
  searchPlaceholder = "Search…",
  createLabel = (query) => `Add “${query}”`,
  emptyLabel = "No matches",
  disabled = false,
  testId = "creatable-combobox",
}: {
  value: string;
  onChange: (next: string) => void;
  options: readonly string[];
  placeholder?: string;
  searchPlaceholder?: string;
  createLabel?: (query: string) => string;
  emptyLabel?: string;
  disabled?: boolean;
  testId?: string;
}) {
  const listId = useId();
  const rootRef = useRef<HTMLDivElement | null>(null);
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);

  useEffect(() => {
    if (!open) {
      return;
    }
    setQuery("");
    setActiveIndex(0);
  }, [open]);

  useEffect(() => {
    if (!open) {
      return;
    }
    function onPointerDown(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", onPointerDown);
    return () => document.removeEventListener("mousedown", onPointerDown);
  }, [open]);

  const filtered = useMemo(() => {
    const q = query.trim().toLocaleLowerCase();
    if (!q) {
      return [...options];
    }
    return options.filter((option) => option.toLocaleLowerCase().includes(q));
  }, [options, query]);

  const trimmedQuery = query.trim();
  const exactMatch = options.some(
    (option) => option.localeCompare(trimmedQuery, undefined, { sensitivity: "accent" }) === 0,
  );
  const canCreate = trimmedQuery.length > 0 && !exactMatch;
  const items = canCreate ? [...filtered, `__create__:${trimmedQuery}`] : filtered;

  function select(next: string) {
    onChange(next);
    setOpen(false);
  }

  function onTriggerKeyDown(event: KeyboardEvent) {
    if (disabled) {
      return;
    }
    if (event.key === "ArrowDown" || event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      setOpen(true);
    }
  }

  function onSearchKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "Escape") {
      event.preventDefault();
      setOpen(false);
      return;
    }
    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActiveIndex((i) => Math.min(i + 1, Math.max(items.length - 1, 0)));
      return;
    }
    if (event.key === "ArrowUp") {
      event.preventDefault();
      setActiveIndex((i) => Math.max(i - 1, 0));
      return;
    }
    if (event.key === "Enter") {
      event.preventDefault();
      const item = items[activeIndex];
      if (!item) {
        return;
      }
      if (item.startsWith("__create__:")) {
        select(trimmedQuery);
      } else {
        select(item);
      }
    }
  }

  return (
    <div className="exits-creatable-combobox" ref={rootRef} data-testid={testId}>
      {!open ? (
        <button
          type="button"
          className="exits-creatable-combobox__trigger"
          data-testid={`${testId}-trigger`}
          disabled={disabled}
          aria-haspopup="listbox"
          aria-expanded={false}
          onClick={() => setOpen(true)}
          onKeyDown={onTriggerKeyDown}
        >
          <span
            className={cn(
              "exits-creatable-combobox__trigger-label",
              !value.trim() && "text-muted",
            )}
          >
            {value.trim() || placeholder}
          </span>
          <ChevronDown className="size-4 shrink-0 opacity-70" aria-hidden />
        </button>
      ) : (
        <>
          <div className="relative block min-w-0">
            <Search
              className="pointer-events-none absolute top-1/2 left-3 z-[1] size-4 -translate-y-1/2 text-muted"
              aria-hidden
            />
            <input
              id={`${testId}-search`}
              type="search"
              data-testid={`${testId}-search`}
              className="exits-input w-full"
              style={{ paddingInlineStart: "2.5rem" }}
              value={query}
              onChange={(event) => {
                setQuery(event.target.value);
                setActiveIndex(0);
              }}
              onKeyDown={onSearchKeyDown}
              placeholder={searchPlaceholder}
              autoComplete="off"
              autoFocus
              aria-controls={listId}
              aria-autocomplete="list"
              aria-label={searchPlaceholder}
            />
          </div>
          <div className="exits-creatable-combobox__panel" data-testid={`${testId}-panel`}>
            {items.length === 0 ? (
              <p className="exits-creatable-combobox__hint m-0">{emptyLabel}</p>
            ) : (
              <ul id={listId} role="listbox" className="exits-creatable-combobox__list m-0 list-none p-0">
                {items.map((item, index) => {
                  const isCreate = item.startsWith("__create__:");
                  const label = isCreate ? createLabel(trimmedQuery) : item;
                  const selected =
                    !isCreate &&
                    value.localeCompare(item, undefined, { sensitivity: "accent" }) === 0;
                  return (
                    <li key={item} role="none">
                      <button
                        type="button"
                        role="option"
                        aria-selected={selected}
                        className={cn(
                          "exits-creatable-combobox__option",
                          index === activeIndex && "is-active",
                        )}
                        data-testid={
                          isCreate ? `${testId}-create` : `${testId}-option`
                        }
                        onMouseEnter={() => setActiveIndex(index)}
                        onClick={() => select(isCreate ? trimmedQuery : item)}
                      >
                        {label}
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>
        </>
      )}
    </div>
  );
}
