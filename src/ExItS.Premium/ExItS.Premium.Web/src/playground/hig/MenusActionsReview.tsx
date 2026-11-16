import { useState } from "react";
import { Badge } from "../../components/badge/Badge";
import { BUTTON_APPEARANCES, Button, type ButtonAppearance } from "../../components/button/Button";
import { Card } from "../../components/card/Card";
import { ActionMenu, DropdownMenu } from "../../components/menu/DropdownMenu";
import { ExIcon } from "../../icons/ExIcon";
import styles from "../Playground.module.scss";
import review from "./review.module.scss";

const APPEARANCE_LABELS: Record<ButtonAppearance, string> = {
  solid: "Solid",
  gradient: "Gradient",
  soft: "Soft",
  outline: "Outline",
  ghost: "Ghost",
  raise: "Raise",
  "soft-raise": "Soft raise",
  "solid-raise": "Solid raise",
  "convex-solid": "Convex Solid",
};

const MENU_ITEMS = [
  { id: "edit", label: "Edit", icon: "edit_note" as const },
  { id: "copy", label: "Copy", icon: "add" as const },
  { id: "archive", label: "Archive", icon: "inbox" as const, disabled: true },
  { id: "delete", label: "Delete", icon: "close" as const, tone: "danger" as const },
];

const LONG_ITEMS = [
  { id: "receipt", label: "Open the neighborhood market receipt from April", icon: "edit_note" as const },
  { id: "share", label: "Share this record with the household", icon: "add" as const },
  { id: "hold", label: "Hold is unavailable for this record", icon: "inbox" as const, disabled: true },
  { id: "remove", label: "Remove this record permanently", icon: "close" as const, tone: "danger" as const },
];

export function MenusActionsReview() {
  const [notice, setNotice] = useState("Choose an action to see it here.");
  const [saving, setSaving] = useState(false);

  function save() {
    setSaving(true);
    setNotice("Saving this preview.");
    window.setTimeout(() => {
      setSaving(false);
      setNotice("Saved in this preview.");
    }, 700);
  }

  return (
    <>
      <header className={styles.intro}>
        <h1 className="type-page">Menus & Actions</h1>
        <p className="type-secondary">
          Primary, secondary, and destructive actions use the existing Button. Menus use Dropdown Menu and Action Menu. Sizes follow the live Compact, Standard, and Large density.
        </p>
      </header>

      <section className={styles.stageBlock}>
        <h2 className="type-section">Actions</h2>
        <p className="type-secondary">Each button records a result. Nothing is sent to a server.</p>
        <div className={styles.stage}>
          <Button onClick={() => setNotice("Primary action recorded.")}>Save</Button>
          <Button variant="secondary" onClick={() => setNotice("Secondary action recorded.")}>
            Cancel
          </Button>
          <Button variant="danger" icon={<ExIcon name="close" />} onClick={() => setNotice("Destructive action recorded.")}>
            Delete
          </Button>
        </div>
        <p className="type-secondary" role="status">
          {notice}
        </p>
      </section>

      <section className={styles.stageBlock}>
        <h2 className="type-section">Appearances</h2>
        <p className="type-secondary">The same primary action in each shipped appearance.</p>
        <div className={styles.stage}>
          {BUTTON_APPEARANCES.map((appearance) => (
            <Button key={appearance} appearance={appearance} onClick={() => setNotice(`${APPEARANCE_LABELS[appearance]} appearance.`)}>
              {APPEARANCE_LABELS[appearance]}
            </Button>
          ))}
        </div>
      </section>

      <section className={styles.stageBlock}>
        <h2 className="type-section">Sizes</h2>
        <p className="type-secondary">Button has no separate size prop. These columns set the density token locally so Compact, Standard, and Large can be compared.</p>
        <div className={styles.densityGrid}>
          {(["compact", "standard", "large"] as const).map((density) => (
            <section key={density} className={styles.densityColumn} data-density={density} aria-label={density}>
              <h3 className="type-section">{density[0]!.toUpperCase() + density.slice(1)}</h3>
              <Button onClick={() => setNotice(`${density} primary.`)}>Save</Button>
              <Button variant="secondary" onClick={() => setNotice(`${density} secondary.`)}>
                Cancel
              </Button>
            </section>
          ))}
        </div>
      </section>

      <section className={styles.stageBlock}>
        <h2 className="type-section">Disabled and loading</h2>
        <div className={styles.stage}>
          <Button disabled>Disabled</Button>
          <Button loading={saving} onClick={save}>
            {saving ? "Saving" : "Save record"}
          </Button>
        </div>
      </section>

      <section className={styles.stageBlock}>
        <h2 className="type-section">Menus</h2>
        <p className="type-secondary">
          Both start closed. Open Record for the standard menu, or the three-dot button for the same items. Each row has an icon. Edit and Copy are normal. Archive is disabled. Delete is destructive. Arrow keys, Home, and End move. Disabled items are skipped. Escape closes and returns focus. A click outside closes. Tab leaves the menu.
        </p>
        <div className={styles.stage}>
          <DropdownMenu label="Record" items={MENU_ITEMS} onSelect={(id) => setNotice(`Menu: ${id}.`)} />
          <ActionMenu label="More record actions" items={MENU_ITEMS} onSelect={(id) => setNotice(`Action menu: ${id}.`)} />
        </div>
      </section>

      <section className={styles.stageBlock}>
        <h2 className="type-section">Long labels at the edge</h2>
        <p className="type-secondary">
          The menu stays a popover. Long labels wrap inside the panel. Placed at the end of the row, the panel shifts inward so it stays on screen.
        </p>
        <div className={styles.stage}>
          <div className={review.edgeRow}>
            <DropdownMenu label="Edge record" items={LONG_ITEMS} onSelect={(id) => setNotice(`Edge menu: ${id}.`)} />
          </div>
        </div>
      </section>

      <section className={styles.stageBlock}>
        <h2 className="type-section">Desktop and mobile</h2>
        <p className="type-secondary">
          These menus use one popover on every width. They do not become a bottom sheet. Searchable Select, on Navigation & Search, does open in a sheet under 767px.
        </p>
      </section>

      <section className={styles.stageBlock}>
        <h2 className="type-section">Planned</h2>
        <div className={styles.pair}>
          <Card title="Button dropdown">
            <Badge tone="warning" appearance="soft">Planned</Badge>
            <p className="type-secondary">A split button is not built. The reserved page is Button dropdown.</p>
            <a href="#button-dropdown">Open Button dropdown</a>
          </Card>
          <Card title="Button group">
            <Badge tone="info" appearance="soft">Coming next</Badge>
            <p className="type-secondary">Attached actions are not built. The reserved page is Button group.</p>
            <a href="#button-group">Open Button group</a>
          </Card>
        </div>
      </section>
    </>
  );
}
