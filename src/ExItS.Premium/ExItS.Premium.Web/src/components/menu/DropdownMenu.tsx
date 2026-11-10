import { useEffect, useId, useLayoutEffect, useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { ExIcon } from "../../icons/ExIcon";
import type { IconName } from "../../icons/registry";
import { hideOverlay, placeOverlay, showOverlay } from "../overlay/overlay";
import { Button } from "../button/Button";
import styles from "./DropdownMenu.module.scss";

export type MenuItem = {
  id: string;
  label: string;
  icon?: IconName;
  disabled?: boolean;
  tone?: "default" | "danger";
};

type DropdownMenuProps = {
  label: string;
  items: MenuItem[];
  onSelect: (id: string) => void;
  iconOnly?: boolean;
};

function enabledIndexes(items: MenuItem[]) {
  return items.flatMap((item, index) => (item.disabled ? [] : [index]));
}

export function DropdownMenu({ label, items, onSelect, iconOnly = false }: DropdownMenuProps) {
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(0);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const menuId = useId();

  useLayoutEffect(() => {
    const menu = menuRef.current;
    const trigger = triggerRef.current;
    if (!open || !menu || !trigger) return;
    showOverlay(menu);
    placeOverlay(trigger, menu, "bottom");
    const current = enabledIndexes(items)[0] ?? 0;
    setActive(current);
    menu.querySelectorAll<HTMLButtonElement>("[role='menuitem']")[current]?.focus();
  }, [open, items]);

  useEffect(() => {
    if (!open) return;
    function onPointerDown(event: PointerEvent) {
      const target = event.target;
      if (!(target instanceof Node)) return;
      if (menuRef.current?.contains(target) || triggerRef.current?.contains(target)) return;
      setOpen(false);
    }
    function onPlace() {
      if (triggerRef.current && menuRef.current) placeOverlay(triggerRef.current, menuRef.current, "bottom");
    }
    document.addEventListener("pointerdown", onPointerDown);
    window.addEventListener("resize", onPlace);
    window.addEventListener("scroll", onPlace, true);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      window.removeEventListener("resize", onPlace);
      window.removeEventListener("scroll", onPlace, true);
    };
  }, [open]);

  useEffect(() => {
    if (!open) hideOverlay(menuRef.current);
  }, [open]);

  function close(restore = true) {
    setOpen(false);
    if (restore) triggerRef.current?.focus();
  }

  function move(direction: 1 | -1) {
    const indexes = enabledIndexes(items);
    if (indexes.length === 0) return;
    const position = indexes.indexOf(active);
    const next = indexes[(position + direction + indexes.length) % indexes.length];
    setActive(next);
    menuRef.current?.querySelectorAll<HTMLButtonElement>("[role='menuitem']")[next]?.focus();
  }

  function choose(item: MenuItem) {
    if (item.disabled) return;
    onSelect(item.id);
    close();
  }

  const trigger: ReactNode = (
    <Button
      ref={triggerRef}
      variant={iconOnly ? "ghost" : "secondary"}
      iconOnly={iconOnly}
      icon={iconOnly ? <ExIcon name="more_vert" /> : <ExIcon name="expand_more" />}
      aria-haspopup="menu"
      aria-expanded={open}
      aria-controls={menuId}
      aria-label={iconOnly ? label : undefined}
      onClick={() => setOpen((value) => !value)}
    >
      {iconOnly ? null : label}
    </Button>
  );

  return (
    <>
      {trigger}
      {createPortal(
        <div
          ref={menuRef}
          id={menuId}
          className={styles.menu}
          popover="manual"
          hidden={!open}
          role="menu"
          aria-label={label}
          onKeyDown={(event) => {
            if (event.key === "Escape") {
              event.preventDefault();
              close();
            } else if (event.key === "ArrowDown") {
              event.preventDefault();
              move(1);
            } else if (event.key === "ArrowUp") {
              event.preventDefault();
              move(-1);
            } else if (event.key === "Home") {
              event.preventDefault();
              const first = enabledIndexes(items)[0];
              if (first !== undefined) {
                setActive(first);
                menuRef.current?.querySelectorAll<HTMLButtonElement>("[role='menuitem']")[first]?.focus();
              }
            } else if (event.key === "End") {
              event.preventDefault();
              const indexes = enabledIndexes(items);
              const last = indexes[indexes.length - 1];
              if (last !== undefined) {
                setActive(last);
                menuRef.current?.querySelectorAll<HTMLButtonElement>("[role='menuitem']")[last]?.focus();
              }
            } else if (event.key === "Tab") {
              close(false);
            }
          }}
        >
          {items.map((item, index) => (
            <button
              key={item.id}
              type="button"
              role="menuitem"
              className={item.tone === "danger" ? `${styles.item} ${styles.danger}` : styles.item}
              disabled={item.disabled}
              tabIndex={index === active ? 0 : -1}
              onClick={() => choose(item)}
              onFocus={() => setActive(index)}
            >
              {item.icon ? <ExIcon name={item.icon} /> : null}
              {item.label}
            </button>
          ))}
        </div>,
        document.body,
      )}
    </>
  );
}

export function ActionMenu(props: Omit<DropdownMenuProps, "iconOnly">) {
  return <DropdownMenu {...props} iconOnly />;
}
