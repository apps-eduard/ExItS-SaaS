import {
  Children,
  Fragment,
  isValidElement,
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  useState,
  type ChangeEvent,
  type FocusEventHandler,
  type KeyboardEvent as ReactKeyboardEvent,
  type ReactElement,
  type ReactNode,
  type SelectHTMLAttributes,
} from "react";
import { createPortal } from "react-dom";
import { ExIcon } from "../../icons/ExIcon";
import { BottomSheet } from "../sheet/BottomSheet";
import { Field } from "../field/Field";
import { hideOverlay, placeOverlay, showOverlay } from "../overlay/overlay";
import styles from "./Select.module.scss";

type SelectProps = SelectHTMLAttributes<HTMLSelectElement> & {
  label: string;
  hint?: string;
  error?: string;
  success?: string;
  readOnly?: boolean;
  searchable?: boolean;
  children: ReactNode;
};

type ParsedOption = {
  value: string;
  label: string;
  disabled: boolean;
  group?: string;
};

function optionText(children: ReactNode, fallback: string) {
  if (typeof children === "string" || typeof children === "number") return String(children);
  if (Array.isArray(children)) return children.map((item) => (typeof item === "string" || typeof item === "number" ? String(item) : "")).join("");
  return fallback;
}

type OptionProps = { value?: string | number; disabled?: boolean; children?: ReactNode; label?: string };

function collectOptions(children: ReactNode, group?: string, into: ParsedOption[] = []) {
  Children.forEach(children, (child) => {
    if (!isValidElement<OptionProps>(child)) return;
    const element = child as ReactElement<OptionProps>;
    if (element.type === Fragment) {
      collectOptions(element.props.children, group, into);
      return;
    }
    if (element.type === "optgroup") {
      collectOptions(element.props.children, String(element.props.label ?? ""), into);
      return;
    }
    if (element.type === "option") {
      const value = element.props.value == null ? optionText(element.props.children, "") : String(element.props.value);
      into.push({
        value,
        label: optionText(element.props.children, value),
        disabled: Boolean(element.props.disabled),
        group,
      });
    }
  });
  return into;
}

function useNarrowViewport() {
  const [narrow, setNarrow] = useState(false);
  useEffect(() => {
    if (typeof window.matchMedia !== "function") return;
    const query = window.matchMedia("(max-width: 767px)");
    const update = () => setNarrow(query.matches);
    update();
    query.addEventListener("change", update);
    return () => query.removeEventListener("change", update);
  }, []);
  return narrow;
}

export function Select({
  label,
  hint,
  error,
  success,
  readOnly = false,
  searchable = false,
  id,
  children,
  disabled,
  value,
  defaultValue,
  onChange,
  autoFocus,
  onFocus,
  onBlur,
  onKeyDown,
  ...props
}: SelectProps) {
  const autoId = useId();
  const fieldId = id ?? autoId;
  const listId = `${fieldId}-list`;
  const searchId = `${fieldId}-search`;
  const message = error ?? success ?? hint;
  const state = error ? "error" : success ? "success" : undefined;
  const options = collectOptions(children);
  const narrow = useNarrowViewport();
  const controlled = value !== undefined;
  const nativeRef = useRef<HTMLSelectElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);
  const typedRef = useRef({ text: "", at: 0 });
  const [uncontrolled, setUncontrolled] = useState(defaultValue == null ? "" : String(defaultValue));
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(0);
  const [query, setQuery] = useState("");
  const [chosen, setChosen] = useState(false);
  const selected = controlled ? String(value ?? "") : uncontrolled;
  const shown = searchable && query.trim()
    ? options.filter((option) => option.label.toLowerCase().includes(query.trim().toLowerCase()))
    : options;
  const selectedOption = options.find((option) => option.value === selected);
  const display = selectedOption?.label || "Choose";
  const placeholder = !selectedOption || selectedOption.value === "";

  useEffect(() => {
    const select = nativeRef.current;
    const form = select?.form;
    if (!select || !form) return;
    const onReset = () => {
      window.setTimeout(() => {
        setUncontrolled(select.value);
        setOpen(false);
        setQuery("");
      }, 0);
    };
    form.addEventListener("reset", onReset);
    return () => form.removeEventListener("reset", onReset);
  }, []);

  const enabled = shown.flatMap((option, index) => (option.disabled ? [] : [index]));
  const safeActive = enabled.includes(active) ? active : (enabled[0] ?? 0);

  useEffect(() => {
    if (!chosen) return;
    const timer = window.setTimeout(() => setChosen(false), 180);
    return () => window.clearTimeout(timer);
  }, [chosen]);

  function enabledIndexes() {
    return enabled;
  }

  function optionId(index: number) {
    return `${listId}-option-${index}`;
  }

  function openPanel() {
    if (disabled || readOnly) return;
    const current = shown.findIndex((option) => option.value === selected && !option.disabled);
    const fallback = enabledIndexes()[0] ?? 0;
    setActive(current >= 0 ? current : fallback);
    setQuery("");
    setOpen(true);
  }

  function closePanel(restore = true) {
    setOpen(false);
    setQuery("");
    if (restore) triggerRef.current?.focus();
  }

  function commit(next: string) {
    const option = options.find((item) => item.value === next);
    if (!option || option.disabled) return;
    const select = nativeRef.current;
    if (!select) return;
    const setter = Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, "value")?.set;
    setter?.call(select, next);
    setUncontrolled(next);
    setChosen(true);
    select.dispatchEvent(new Event("change", { bubbles: true }));
    closePanel();
  }

  function move(direction: 1 | -1 | "first" | "last") {
    const indexes = enabledIndexes();
    if (indexes.length === 0) return;
    if (direction === "first") {
      setActive(indexes[0]);
      return;
    }
    if (direction === "last") {
      setActive(indexes[indexes.length - 1]);
      return;
    }
    const position = Math.max(0, indexes.indexOf(safeActive));
    const next = indexes[(position + direction + indexes.length) % indexes.length];
    setActive(next);
  }

  function typeAhead(key: string, at: number) {
    const text = at - typedRef.current.at < 700 ? `${typedRef.current.text}${key}` : key;
    typedRef.current = { text, at };
    const needle = text.toLowerCase();
    const start = safeActive + 1;
    const order = [...shown.keys()].slice(start).concat([...shown.keys()].slice(0, start));
    const match = order.find((index) => !shown[index].disabled && shown[index].label.toLowerCase().startsWith(needle));
    if (match !== undefined) setActive(match);
  }

  function onTriggerKeyDown(event: ReactKeyboardEvent<HTMLButtonElement>) {
    onKeyDown?.(event as unknown as ReactKeyboardEvent<HTMLSelectElement>);
    if (event.defaultPrevented || disabled || readOnly) return;
    if (!open && ["ArrowDown", "ArrowUp", "Enter", " "].includes(event.key)) {
      event.preventDefault();
      openPanel();
      return;
    }
    if (!open && event.key.length === 1 && /\S/.test(event.key)) {
      event.preventDefault();
      openPanel();
      typeAhead(event.key, event.timeStamp);
      return;
    }
    if (!open || searchable) return;
    if (event.key === "ArrowDown") {
      event.preventDefault();
      move(1);
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      move(-1);
    } else if (event.key === "Home") {
      event.preventDefault();
      move("first");
    } else if (event.key === "End") {
      event.preventDefault();
      move("last");
    } else if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      const option = shown[safeActive];
      if (option) commit(option.value);
    } else if (event.key === "Escape") {
      event.preventDefault();
      closePanel();
    } else if (event.key.length === 1 && /\S/.test(event.key)) {
      event.preventDefault();
      typeAhead(event.key, event.timeStamp);
    }
  }

  function onSearchKeyDown(event: ReactKeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowDown") {
      event.preventDefault();
      move(1);
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      move(-1);
    } else if (event.key === "Home" && event.currentTarget.selectionStart === 0) {
      event.preventDefault();
      move("first");
    } else if (event.key === "End" && event.currentTarget.selectionStart === event.currentTarget.value.length) {
      event.preventDefault();
      move("last");
    } else if (event.key === "Enter") {
      event.preventDefault();
      const option = shown[safeActive];
      if (option) commit(option.value);
    } else if (event.key === "Escape") {
      event.preventDefault();
      closePanel();
    }
  }

  useLayoutEffect(() => {
    const panel = panelRef.current;
    const trigger = triggerRef.current;
    if (!open || narrow || !panel || !trigger) return;
    panel.style.width = `${Math.round(trigger.getBoundingClientRect().width)}px`;
    showOverlay(panel);
    placeOverlay(trigger, panel, "bottom");
    if (searchable) searchRef.current?.focus();
  }, [open, narrow, searchable, shown.length]);

  useEffect(() => {
    if (!open || narrow) return;
    function onPointerDown(event: PointerEvent) {
      const target = event.target;
      if (!(target instanceof Node)) return;
      if (panelRef.current?.contains(target) || triggerRef.current?.contains(target)) return;
      setOpen(false);
      setQuery("");
    }
    function onPlace() {
      const panel = panelRef.current;
      const trigger = triggerRef.current;
      if (!panel || !trigger) return;
      panel.style.width = `${Math.round(trigger.getBoundingClientRect().width)}px`;
      placeOverlay(trigger, panel, "bottom");
    }
    document.addEventListener("pointerdown", onPointerDown);
    window.addEventListener("resize", onPlace);
    window.addEventListener("scroll", onPlace, true);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      window.removeEventListener("resize", onPlace);
      window.removeEventListener("scroll", onPlace, true);
    };
  }, [open, narrow]);

  useEffect(() => {
    if (!open || narrow) hideOverlay(panelRef.current);
  }, [open, narrow]);

  useEffect(() => {
    if (!open) return;
    const node = document.getElementById(`${listId}-option-${safeActive}`);
    if (typeof node?.scrollIntoView === "function") node.scrollIntoView({ block: "nearest" });
  }, [open, safeActive, listId]);

  useEffect(() => {
    if (!open || !narrow) return;
    const timer = window.setTimeout(() => {
      if (searchable) searchRef.current?.focus();
      else document.getElementById(`${listId}-option-${safeActive}`)?.focus();
    }, 0);
    return () => window.clearTimeout(timer);
  }, [open, narrow, searchable, safeActive, listId]);

  function onNativeChange(event: ChangeEvent<HTMLSelectElement>) {
    setUncontrolled(event.target.value);
    onChange?.(event);
  }

  let previousGroup: string | undefined;
  const optionNodes = shown.length === 0 ? (
    <p className={styles.empty}>No matches</p>
  ) : (
    shown.map((option, index) => {
      const group = option.group && option.group !== previousGroup ? option.group : "";
      previousGroup = option.group;
      const selectedOption = option.value === selected;
      return (
        <div key={`${option.group ?? ""}-${option.value}-${index}`} className={group ? styles.group : undefined}>
          {group ? <p className={styles.groupLabel}>{group}</p> : null}
          <button
            type="button"
            id={optionId(index)}
            className={styles.option}
            role="option"
            aria-selected={selectedOption}
            aria-disabled={option.disabled || undefined}
            disabled={option.disabled}
            data-active={index === safeActive ? "true" : undefined}
            tabIndex={-1}
            onMouseEnter={() => {
              if (!option.disabled) setActive(index);
            }}
            onClick={() => commit(option.value)}
          >
            <span>{option.label}</span>
            <ExIcon name="check" className={styles.mark} />
          </button>
        </div>
      );
    })
  );

  const search = searchable ? (
    <div className={styles.search}>
      <ExIcon name="search" className={styles.searchIcon} />
      <input
        ref={searchRef}
        id={searchId}
        className={styles.searchInput}
        role="combobox"
        aria-autocomplete="list"
        aria-controls={listId}
        aria-expanded={open}
            aria-activedescendant={shown[safeActive] ? optionId(safeActive) : undefined}
        aria-label={`Search ${label}`}
        placeholder="Search"
        value={query}
        onChange={(event) => {
          setQuery(event.target.value);
          setActive(0);
        }}
        onKeyDown={onSearchKeyDown}
      />
      {query ? (
        <button type="button" className={styles.clear} aria-label="Clear search" onClick={() => setQuery("")}>
          <ExIcon name="close" />
        </button>
      ) : null}
    </div>
  ) : null;

  const list = (
    <div id={listId} className={styles.list} role="listbox" aria-label={label}>
      {optionNodes}
    </div>
  );

  return (
    <Field id={fieldId} label={label} hint={hint} error={error} success={success} required={props.required} readOnly={readOnly}>
      <div className={styles.wrap} data-state={state}>
        <select
          {...props}
          ref={nativeRef}
          id={`${fieldId}-value`}
          className={styles.native}
          tabIndex={-1}
          aria-hidden="true"
          disabled={disabled}
          value={controlled ? selected : undefined}
          defaultValue={controlled ? undefined : defaultValue}
          onChange={onNativeChange}
        >
          {children}
        </select>
        <button
          ref={triggerRef}
          type="button"
          id={fieldId}
          className={styles.trigger}
          role={searchable ? undefined : "combobox"}
          aria-haspopup="listbox"
          aria-expanded={open}
          aria-controls={open ? listId : undefined}
          aria-activedescendant={!searchable && open && shown[safeActive] ? optionId(safeActive) : undefined}
          aria-label={`${label}, ${display}`}
          aria-invalid={error ? true : undefined}
          aria-describedby={message ? `${fieldId}-message` : undefined}
          aria-readonly={readOnly || undefined}
          disabled={disabled}
          autoFocus={autoFocus}
          onFocus={onFocus as FocusEventHandler<HTMLButtonElement> | undefined}
          onBlur={onBlur as FocusEventHandler<HTMLButtonElement> | undefined}
          data-state={state}
          data-placeholder={placeholder ? "true" : undefined}
          data-chosen={chosen ? "true" : undefined}
          data-readonly={readOnly ? "true" : undefined}
          onClick={() => (open ? closePanel(false) : openPanel())}
          onKeyDown={onTriggerKeyDown}
        >
          <span className={styles.value}>{display}</span>
          <ExIcon name="expand_more" className={styles.chevron} />
        </button>
      </div>
      {open && !narrow
        ? createPortal(
            <div ref={panelRef} className={styles.panel} popover="manual">
              {search}
              {list}
            </div>,
            document.body,
          )
        : null}
      {narrow ? (
        <BottomSheet open={open} title={label} onClose={() => closePanel()}>
          {search}
          {list}
        </BottomSheet>
      ) : null}
    </Field>
  );
}
