import {
  Children,
  Fragment,
  cloneElement,
  isValidElement,
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  useState,
  type AnimationEvent as ReactAnimationEvent,
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
import appearanceStyles from "../input/appearance.module.scss";
import { hideOverlay, showOverlay } from "../overlay/overlay";
import styles from "./Select.module.scss";
import { applySelectValues, SELECT_MENU_MOTION_DEFAULT, type SelectMenuMotion } from "./select-menu-motion";

type SelectAppearance = "outlined" | "soft" | "standard" | "stacked";

type SelectProps = SelectHTMLAttributes<HTMLSelectElement> & {
  label: string;
  hint?: string;
  error?: string;
  success?: string;
  readOnly?: boolean;
  searchable?: boolean;
  appearance?: SelectAppearance;
  menuMotion?: SelectMenuMotion;
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

function markOptions(children: ReactNode, values: readonly string[]): ReactNode {
  return Children.map(children, (child) => {
    if (!isValidElement<{ value?: string | number; children?: ReactNode; selected?: boolean }>(child)) return child;
    if (child.type === Fragment || child.type === "optgroup") {
      return cloneElement(child, undefined, markOptions(child.props.children, values));
    }
    if (child.type === "option") {
      const optionValue = child.props.value == null ? optionText(child.props.children, "") : String(child.props.value);
      return cloneElement(child, { selected: values.includes(optionValue) });
    }
    return child;
  });
}

function listed(value: unknown) {
  return Array.isArray(value) ? value.map(String) : null;
}

function resolveAppearance(value: SelectAppearance | undefined) {
  if (value === "outlined" || value === "soft") return value;
  return "standard" as const;
}

function prefersReducedMotion() {
  return typeof window.matchMedia === "function" && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
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
  appearance: appearanceProp,
  menuMotion = SELECT_MENU_MOTION_DEFAULT,
  multiple = false,
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
  const closeTimer = useRef(0);
  const appearance = resolveAppearance(appearanceProp);
  const onBorder = appearance === "outlined" || appearance === "soft";
  const [uncontrolled, setUncontrolled] = useState(defaultValue == null || Array.isArray(defaultValue) ? "" : String(defaultValue));
  const [multi, setMulti] = useState<string[]>(() => listed(defaultValue) ?? listed(value) ?? []);
  const [phase, setPhase] = useState<"closed" | "open" | "closing">("closed");
  const [motionEpoch, setMotionEpoch] = useState(0);
  const [active, setActive] = useState(0);
  const [query, setQuery] = useState("");
  const [chosen, setChosen] = useState(false);
  const menuOpen = phase === "open";
  const selected = controlled && !Array.isArray(value) ? String(value ?? "") : uncontrolled;
  const selectedMany = listed(value) ?? multi;
  const menuOptions = options.filter((option) => option.value !== "");
  const shown = searchable && query.trim()
    ? menuOptions.filter((option) => option.label.toLowerCase().includes(query.trim().toLowerCase()))
    : menuOptions;
  const selectedOption = options.find((option) => option.value === selected);
  const selectedLabels = options.filter((option) => option.value !== "" && selectedMany.includes(option.value)).map((option) => option.label);
  const display = multiple
    ? selectedLabels.length === 0
      ? "Choose"
      : selectedLabels.length > 2
        ? `${selectedLabels.length} selected`
        : selectedLabels.join(", ")
    : selectedOption?.label || "Choose";
  const placeholder = multiple ? selectedLabels.length === 0 : !selectedOption || selectedOption.value === "";

  useEffect(() => {
    const select = nativeRef.current;
    const form = select?.form;
    if (!select || !form) return;
    const onReset = () => {
      window.setTimeout(() => {
        if (multiple) setMulti([...select.selectedOptions].map((option) => option.value));
        else setUncontrolled(select.value);
        window.clearTimeout(closeTimer.current);
        setPhase("closed");
        setQuery("");
      }, 0);
    };
    form.addEventListener("reset", onReset);
    return () => form.removeEventListener("reset", onReset);
  }, [multiple]);

  const enabled = shown.flatMap((option, index) => (option.disabled ? [] : [index]));
  const safeActive = enabled.includes(active) ? active : (enabled[0] ?? 0);

  useEffect(() => {
    if (!chosen) return;
    const timer = window.setTimeout(() => setChosen(false), 180);
    return () => window.clearTimeout(timer);
  }, [chosen]);

  useEffect(() => () => window.clearTimeout(closeTimer.current), []);

  function enabledIndexes() {
    return enabled;
  }

  function optionId(index: number) {
    return `${listId}-option-${index}`;
  }

  function openPanel() {
    if (disabled || readOnly) return;
    window.clearTimeout(closeTimer.current);
    const current = shown.findIndex((option) => !option.disabled && (multiple ? option.value !== "" && selectedMany.includes(option.value) : option.value === selected));
    const fallback = enabledIndexes()[0] ?? 0;
    setActive(current >= 0 ? current : fallback);
    setQuery("");
    setMotionEpoch((currentEpoch) => currentEpoch + 1);
    setPhase("open");
  }

  function finishClose() {
    window.clearTimeout(closeTimer.current);
    setPhase("closed");
  }

  function closePanel(restore = true) {
    setQuery("");
    if (restore) triggerRef.current?.focus();
    if (phase !== "open") return;
    if (narrow || prefersReducedMotion()) {
      finishClose();
      return;
    }
    setPhase("closing");
    window.clearTimeout(closeTimer.current);
    closeTimer.current = window.setTimeout(finishClose, 160);
  }

  function commit(next: string) {
    const option = options.find((item) => item.value === next);
    if (!option || option.disabled) return;
    const select = nativeRef.current;
    if (!select) return;
    if (multiple) {
      if (next === "") return;
      const current = listed(value) ?? multi;
      const updated = current.includes(next) ? current.filter((item) => item !== next) : [...current, next];
      applySelectValues(select, updated);
      setMulti(updated);
      setChosen(true);
      select.dispatchEvent(new Event("change", { bubbles: true }));
      return;
    }
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
    if (!menuOpen && ["ArrowDown", "ArrowUp", "Enter", " "].includes(event.key)) {
      event.preventDefault();
      openPanel();
      return;
    }
    if (!menuOpen && event.key.length === 1 && /\S/.test(event.key)) {
      event.preventDefault();
      openPanel();
      typeAhead(event.key, event.timeStamp);
      return;
    }
    if (!menuOpen || searchable) return;
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

  function alignMenu() {
    const panel = panelRef.current;
    const trigger = triggerRef.current;
    if (!panel || !trigger) return;
    const anchor = trigger.getBoundingClientRect();
    const gap = 6;
    const margin = 8;
    const width = Math.round(anchor.width);
    panel.style.width = `${width}px`;
    const menuHeight = panel.offsetHeight || panel.getBoundingClientRect().height;
    const spaceBelow = window.innerHeight - anchor.bottom - gap - margin;
    const spaceAbove = anchor.top - gap - margin;
    const placeAbove = spaceBelow < Math.min(menuHeight, 160) && spaceAbove > spaceBelow;
    const available = Math.max(96, Math.floor(placeAbove ? spaceAbove : spaceBelow));
    panel.style.setProperty("--select-max", `${available}px`);
    const used = Math.min(menuHeight, available);
    let top = placeAbove ? anchor.top - gap - used : anchor.bottom + gap;
    let left = anchor.left;
    if (top < margin) top = margin;
    if (top + used > window.innerHeight - margin) top = Math.max(margin, window.innerHeight - margin - used);
    if (left + width > window.innerWidth - margin) left = window.innerWidth - width - margin;
    if (left < margin) left = margin;
    panel.style.top = `${Math.round(top)}px`;
    panel.style.left = `${Math.round(left)}px`;
    panel.dataset.placement = placeAbove ? "above" : "below";
    panel.style.setProperty("--select-shift", placeAbove ? "6px" : "-6px");
  }

  function onMenuAnimationEnd(event: ReactAnimationEvent<HTMLDivElement>) {
    if (event.target !== event.currentTarget) return;
    if (event.currentTarget.dataset.motion !== "exit") return;
    finishClose();
  }

  useLayoutEffect(() => {
    const panel = panelRef.current;
    const trigger = triggerRef.current;
    if (phase === "closed" || narrow || !panel || !trigger) return;
    showOverlay(panel);
    alignMenu();
    if (menuOpen && searchable) searchRef.current?.focus();
  }, [phase, menuOpen, narrow, searchable, shown.length, motionEpoch]);

  useEffect(() => {
    if (!menuOpen || narrow) return;
    function onPointerDown(event: PointerEvent) {
      const target = event.target;
      if (!(target instanceof Node)) return;
      if (panelRef.current?.contains(target) || triggerRef.current?.contains(target)) return;
      setQuery("");
      if (narrow || prefersReducedMotion()) {
        window.clearTimeout(closeTimer.current);
        setPhase("closed");
        return;
      }
      setPhase("closing");
      window.clearTimeout(closeTimer.current);
      closeTimer.current = window.setTimeout(finishClose, 160);
    }
    document.addEventListener("pointerdown", onPointerDown);
    window.addEventListener("resize", alignMenu);
    window.addEventListener("scroll", alignMenu, true);
    window.visualViewport?.addEventListener("resize", alignMenu);
    window.visualViewport?.addEventListener("scroll", alignMenu);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      window.removeEventListener("resize", alignMenu);
      window.removeEventListener("scroll", alignMenu, true);
      window.visualViewport?.removeEventListener("resize", alignMenu);
      window.visualViewport?.removeEventListener("scroll", alignMenu);
    };
  }, [menuOpen, narrow]);

  useEffect(() => {
    if (phase !== "closed" && !narrow) return;
    hideOverlay(panelRef.current);
  }, [phase, narrow]);

  useEffect(() => {
    if (!menuOpen) return;
    const node = document.getElementById(`${listId}-option-${safeActive}`);
    if (typeof node?.scrollIntoView === "function") node.scrollIntoView({ block: "nearest" });
  }, [menuOpen, safeActive, listId]);

  useEffect(() => {
    if (!menuOpen || !narrow) return;
    const timer = window.setTimeout(() => {
      if (searchable) searchRef.current?.focus();
      else document.getElementById(`${listId}-option-${safeActive}`)?.focus();
    }, 0);
    return () => window.clearTimeout(timer);
  }, [menuOpen, narrow, searchable, safeActive, listId]);

  function onNativeChange(event: ChangeEvent<HTMLSelectElement>) {
    if (multiple) setMulti([...event.target.selectedOptions].map((option) => option.value));
    else setUncontrolled(event.target.value);
    onChange?.(event);
  }

  let previousGroup: string | undefined;
  const optionNodes = shown.length === 0 ? (
    <p className={styles.empty}>No matches</p>
  ) : (
    shown.map((option, index) => {
      const group = option.group && option.group !== previousGroup ? option.group : "";
      previousGroup = option.group;
      const selectedOption = multiple ? option.value !== "" && selectedMany.includes(option.value) : option.value === selected;
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

  function clearSearch() {
    setQuery("");
    setActive(0);
    window.requestAnimationFrame(() => searchRef.current?.focus());
  }

  const search = searchable ? (
    <div className={styles.search}>
      <ExIcon name="search" className={styles.searchIcon} size={18} />
      <input
        ref={searchRef}
        id={searchId}
        className={styles.searchInput}
        role="combobox"
        aria-autocomplete="list"
        aria-controls={listId}
        aria-expanded={menuOpen}
        aria-activedescendant={menuOpen && shown[safeActive] ? optionId(safeActive) : undefined}
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
        <button type="button" className={styles.clear} aria-label="Clear search" onClick={clearSearch}>
          <ExIcon name="close" size={16} />
        </button>
      ) : null}
    </div>
  ) : null;

  const list = (
    <div id={listId} className={styles.list} role="listbox" aria-label={label} aria-multiselectable={multiple || undefined}>
      {optionNodes}
    </div>
  );

  return (
    <Field
      id={fieldId}
      label={label}
      hint={hint}
      error={error}
      success={success}
      required={props.required}
      readOnly={readOnly}
      placement={onBorder ? "float" : "stacked"}
    >
      <div className={styles.wrap} data-state={state}>
        <select
          {...props}
          ref={nativeRef}
          id={`${fieldId}-value`}
          className={styles.native}
          tabIndex={-1}
          aria-hidden="true"
          disabled={disabled}
          multiple={multiple || undefined}
          value={multiple ? undefined : controlled ? selected : undefined}
          defaultValue={multiple || controlled ? undefined : defaultValue}
          onChange={onNativeChange}
        >
          {multiple ? markOptions(children, selectedMany) : children}
        </select>
        {/*
          Focus Draw integration point. This trigger is the focus host and keeps the
          existing :focus-visible border and ring in Select.module.scss.
          When Agent 4's reusable Focus Draw is shared, attach it here
          (data-focus-draw / focusMotion="focus-draw"). Do not add a second border trace.
        */}
        <button
          ref={triggerRef}
          type="button"
          id={fieldId}
          className={[styles.trigger, onBorder ? appearanceStyles.control : ""].filter(Boolean).join(" ")}
          data-focus-host="select-trigger"
          role={searchable ? undefined : "combobox"}
          aria-haspopup="listbox"
          aria-expanded={menuOpen}
          aria-controls={menuOpen ? listId : undefined}
          aria-activedescendant={!searchable && menuOpen && shown[safeActive] ? optionId(safeActive) : undefined}
          aria-label={`${label}, ${display}`}
          aria-invalid={error ? true : undefined}
          aria-describedby={message ? `${fieldId}-message` : undefined}
          aria-readonly={readOnly || undefined}
          disabled={disabled}
          autoFocus={autoFocus}
          onFocus={onFocus as FocusEventHandler<HTMLButtonElement> | undefined}
          onBlur={onBlur as FocusEventHandler<HTMLButtonElement> | undefined}
          data-appearance={onBorder ? appearance : undefined}
          data-filled={placeholder ? undefined : "true"}
          data-state={state}
          data-placeholder={placeholder ? "true" : undefined}
          data-chosen={chosen ? "true" : undefined}
          data-readonly={readOnly ? "true" : undefined}
          onClick={() => (menuOpen ? closePanel(false) : openPanel())}
          onKeyDown={onTriggerKeyDown}
        >
          <span className={styles.value}>{display}</span>
          <ExIcon name="expand_more" className={styles.chevron} />
        </button>
      </div>
      {phase !== "closed" && !narrow
        ? createPortal(
            <div
              ref={panelRef}
              key={motionEpoch}
              className={styles.panel}
              popover="manual"
              data-component="select-menu"
              data-motion={phase === "closing" ? "exit" : "enter"}
              data-transition={menuMotion}
              data-placement="below"
              aria-hidden={phase === "closing" || undefined}
              inert={phase === "closing" || undefined}
              onAnimationEnd={onMenuAnimationEnd}
            >
              <div className={styles.scroller}>
                {search}
                {list}
              </div>
            </div>,
            document.body,
          )
        : null}
      {narrow ? (
        <BottomSheet open={menuOpen} title={label} onClose={() => closePanel()}>
          {search}
          {list}
        </BottomSheet>
      ) : null}
    </Field>
  );
}
