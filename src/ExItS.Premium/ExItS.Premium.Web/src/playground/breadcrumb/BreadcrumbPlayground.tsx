import { useState } from "react";
import { Breadcrumb, type BreadcrumbItem } from "../../components/breadcrumb";
import { Button } from "../../components/button/Button";
import { Select } from "../../components/select/Select";
import { Switch } from "../../components/switch/Switch";
import { CursorCopyCommand } from "../CursorCopyCommand";
import { PlaygroundShell } from "../shell/PlaygroundShell";
import styles from "./BreadcrumbPlayground.module.scss";

type SampleDepth = "root" | "section" | "page";

const DEPTH_SAMPLES: Record<SampleDepth, BreadcrumbItem[]> = {
  root: [{ id: "design-system", label: "Design System", icon: "dashboard" }],
  section: [
    { id: "design-system", label: "Design System", href: "#overview", icon: "dashboard" },
    { id: "components", label: "Components" },
  ],
  page: [
    { id: "design-system", label: "Design System", href: "#overview", icon: "dashboard" },
    { id: "branch-Buttons", label: "Buttons", href: "#button" },
    { id: "button", label: "Button" },
  ],
};

export function BreadcrumbPlayground() {
  const [depth, setDepth] = useState<SampleDepth>("page");
  const [showHomeIcon, setShowHomeIcon] = useState(true);

  const items = DEPTH_SAMPLES[depth];
  const cursorCommand = [
    "ext breadcrumb premium",
    showHomeIcon ? "home-icon" : "no-home-icon",
    depth,
  ].join(" ");

  function reset() {
    setDepth("page");
    setShowHomeIcon(true);
  }

  return (
    <PlaygroundShell
      title="Breadcrumbs"
      description="Premium path trail for Design System and future Personal pages. Prior levels are links; the current page uses aria-current. Mobile collapses intermediate crumbs. Home icon, labels, and truncation stay on the API."
      panes="equal"
      propertiesColumns={2}
      preview={
        <div className={styles.previewStack}>
          <div className={styles.previewField}>
            <Breadcrumb items={items} showHomeIcon={showHomeIcon} />
          </div>
          <CursorCopyCommand standard="Breadcrumb" command={cursorCommand} />
        </div>
      }
      properties={
        <>
          <Select
            label="Depth"
            value={depth}
            onChange={(event) => setDepth(event.target.value as SampleDepth)}
          >
            <option value="root">Root only</option>
            <option value="section">Home › Section</option>
            <option value="page">Home › Section › Page</option>
          </Select>
          <Switch
            label="Home icon"
            checked={showHomeIcon}
            onChange={(event) => setShowHomeIcon(event.target.checked)}
          />
          <div className={styles.resetRow}>
            <Button type="button" variant="ghost" onClick={reset}>
              Default global
            </Button>
          </div>
        </>
      }
      examples={
        <div className={styles.examples}>
          <div className={styles.exampleCard}>
            <h3 className={styles.exampleTitle}>Root</h3>
            <Breadcrumb items={DEPTH_SAMPLES.root} />
          </div>
          <div className={styles.exampleCard}>
            <h3 className={styles.exampleTitle}>Section</h3>
            <Breadcrumb items={DEPTH_SAMPLES.section} />
          </div>
          <div className={styles.exampleCard}>
            <h3 className={styles.exampleTitle}>Page</h3>
            <Breadcrumb items={DEPTH_SAMPLES.page} />
          </div>
          <div className={styles.exampleCard}>
            <h3 className={styles.exampleTitle}>Long labels</h3>
            <Breadcrumb
              maxLabelLength={18}
              items={[
                { id: "home", label: "Design System", href: "#overview", icon: "dashboard" },
                { id: "long-section", label: "Advanced Components Gallery", href: "#advanced" },
                { id: "long-page", label: "Very Long Current Page Title" },
              ]}
            />
          </div>
        </div>
      }
    />
  );
}
