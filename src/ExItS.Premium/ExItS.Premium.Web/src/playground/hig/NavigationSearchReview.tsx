import { useEffect, useRef, useState } from "react";
import { Breadcrumb, type BreadcrumbItem } from "../../components/breadcrumb";
import { Button } from "../../components/button/Button";
import { Card } from "../../components/card/Card";
import { SearchField } from "../../components/search/SearchField";
import { Select } from "../../components/select/Select";
import { Tabs } from "../../components/tabs/Tabs";
import { ExIcon } from "../../icons/ExIcon";
import styles from "../Playground.module.scss";
import review from "./review.module.scss";

const PLACES = ["Home", "Activity", "People"] as const;

const SAMPLE_NAV = [
  { id: "home", label: "Home", icon: "dashboard" as const },
  { id: "activity", label: "Activity", icon: "menu" as const },
] as const;

const SAMPLE_GROUP = [
  { id: "lent", label: "Lent" },
  { id: "owe", label: "Owe" },
] as const;

const RECORDS = [
  { id: "market", name: "Market", note: "Groceries" },
  { id: "rent", name: "Rent", note: "April" },
  { id: "maya", name: "Maya", note: "Transfer" },
];

export function NavigationSearchReview() {
  const [place, setPlace] = useState<(typeof PLACES)[number]>("Activity");
  const [tab, setTab] = useState("open");
  const [query, setQuery] = useState("Market");
  const [empty, setEmpty] = useState("");
  const [filter, setFilter] = useState("");
  const [person, setPerson] = useState("ana");
  const [navItem, setNavItem] = useState("activity");
  const [groupOpen, setGroupOpen] = useState(true);
  const [collapsed, setCollapsed] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const drawerButton = useRef<HTMLButtonElement>(null);
  const drawerRef = useRef<HTMLDialogElement>(null);

  const trail: BreadcrumbItem[] = PLACES.map((label) => ({
    id: label.toLowerCase(),
    label,
    href: label === place ? undefined : "#navigation-search",
    icon: label === "Home" ? "dashboard" : undefined,
  }));

  const longTrail: BreadcrumbItem[] = [
    { id: "home", label: "Home", href: "#navigation-search", icon: "dashboard" },
    { id: "money", label: "Money and payments", href: "#navigation-search" },
    { id: "stores", label: "Stores to pay this month", href: "#navigation-search" },
    { id: "receipt", label: "Neighborhood market receipt" },
  ];

  const shown = RECORDS.filter((record) => `${record.name} ${record.note}`.toLowerCase().includes(filter.trim().toLowerCase()));

  useEffect(() => {
    const dialog = drawerRef.current;
    if (!dialog) return;
    if (drawerOpen && !dialog.open) {
      try {
        dialog.showModal();
      } catch {
        dialog.setAttribute("open", "");
      }
    }
    if (!drawerOpen && dialog.open) {
      if (typeof dialog.close === "function") dialog.close();
      else dialog.removeAttribute("open");
    }
  }, [drawerOpen]);

  function closeDrawer() {
    setDrawerOpen(false);
    drawerButton.current?.focus();
  }

  return (
    <>
      <header className={styles.intro}>
        <h1 className="type-page">Navigation & Search</h1>
        <p className="type-secondary">
          Sidebar, breadcrumbs, tabs, search, and searchable select use the components already in this playground. This page does not add a second navigation system.
        </p>
      </header>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Sidebar</h2>
        <p className="type-secondary">
          The sidebar on this page is the live shell, including the active item and nested groups. Collapse it with Collapse sidebar. On a phone that shell is hidden and Open sections uses the same list in a drawer. This sample uses those same nav styles.
        </p>
        <div className={styles.stage}>
          <Button
            variant="secondary"
            aria-pressed={collapsed}
            onClick={() => setCollapsed((value) => !value)}
          >
            {collapsed ? "Expand sample" : "Collapse sample"}
          </Button>
          <div className={review.navFrame} data-collapsed={collapsed ? "true" : "false"}>
            <div className={styles.sidebarInner} data-collapsed={collapsed ? "true" : "false"}>
              <nav className={styles.nav} aria-label="Sample sections">
                {SAMPLE_NAV.map((item) => (
                  <button
                    key={item.id}
                    type="button"
                    className={styles.navButton}
                    aria-current={navItem === item.id ? "page" : undefined}
                    onClick={() => setNavItem(item.id)}
                  >
                    <ExIcon name={item.icon} />
                    <span className={styles.navLabel}>{item.label}</span>
                  </button>
                ))}
                <div className={styles.navSubgroup} data-open={groupOpen ? "true" : undefined}>
                  <button
                    type="button"
                    className={styles.navBranch}
                    aria-expanded={groupOpen}
                    aria-controls="sample-money"
                    onClick={() => setGroupOpen((value) => !value)}
                  >
                    <ExIcon name="payments" />
                    <span className={styles.navLabel}>Money</span>
                    <ExIcon name="expand_more" className={styles.navChevron} />
                  </button>
                  <div id="sample-money" className={styles.navChildren} hidden={!groupOpen} role="group" aria-label="Money">
                    {SAMPLE_GROUP.map((item) => (
                      <button
                        key={item.id}
                        type="button"
                        className={`${styles.navButton} ${styles.navChild}`}
                        aria-current={navItem === item.id ? "page" : undefined}
                        onClick={() => setNavItem(item.id)}
                      >
                        <span className={styles.navLabel}>{item.label}</span>
                      </button>
                    ))}
                  </div>
                </div>
              </nav>
            </div>
          </div>
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Mobile drawer</h2>
        <p className="type-secondary">Open sections on a phone is the live drawer. This sample uses the same drawer shell and returns focus to the button that opened it.</p>
        <div className={styles.stage}>
          <Button ref={drawerButton} variant="secondary" onClick={() => setDrawerOpen(true)}>
            Open sample sections
          </Button>
        </div>
        <dialog
          ref={drawerRef}
          className={styles.drawer}
          aria-label="Sample sections drawer"
          onClose={closeDrawer}
          onClick={(event) => {
            if (event.target === event.currentTarget) closeDrawer();
          }}
        >
          <div className={styles.drawerBody}>
            <div className={styles.dialogHeader}>
              <h2 className="type-section">Sections</h2>
              <Button variant="ghost" iconOnly icon={<ExIcon name="close" />} aria-label="Close sample sections" onClick={closeDrawer} />
            </div>
            <nav className={styles.nav} aria-label="Drawer sections">
              {SAMPLE_NAV.map((item) => (
                <button
                  key={item.id}
                  type="button"
                  className={styles.navButton}
                  aria-current={navItem === item.id ? "page" : undefined}
                  onClick={() => {
                    setNavItem(item.id);
                    closeDrawer();
                  }}
                >
                  <ExIcon name={item.icon} />
                  <span className={styles.navLabel}>{item.label}</span>
                </button>
              ))}
            </nav>
          </div>
        </dialog>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Breadcrumbs</h2>
        <p className="type-secondary">
          The trail above the title is the live route. The short trail stays on this page. The long trail truncates each label, and on a narrow screen the middle crumbs collapse to an ellipsis.
        </p>
        <div className={styles.stage}>
          <Breadcrumb
            aria-label="Sample trail"
            items={trail}
            onNavigate={(item) => {
              const next = PLACES.find((label) => label.toLowerCase() === item.id);
              if (next) setPlace(next);
            }}
          />
          <Breadcrumb aria-label="Long trail" items={longTrail} />
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Tabs</h2>
        <p className="type-secondary">Arrow keys move between enabled tabs. Home and End jump to the ends. Archive is disabled.</p>
        <Tabs
          label="Records"
          value={tab}
          onChange={setTab}
          tabs={[
            { value: "open", label: "Open", content: <p className="type-secondary">Open records in this preview.</p> },
            { value: "done", label: "Done", content: <p className="type-secondary">Finished records in this preview.</p> },
            { value: "archive", label: "Archive", content: <p className="type-secondary">Unavailable.</p>, disabled: true },
          ]}
        />
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Search</h2>
        <p className="type-secondary">Clear appears when the field has text. Error is a message under the field. Disabled keeps the value and blocks typing.</p>
        <div className={styles.stage}>
          <SearchField label="Search" value={query} onChange={setQuery} placeholder="Name or note" />
          <SearchField label="Empty" value={empty} onChange={setEmpty} placeholder="Search activity" />
          <SearchField label="Disabled" value="Locked" onChange={() => undefined} disabled />
          <SearchField label="Error" value="?" onChange={() => undefined} error="Enter at least two letters." />
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Live filter</h2>
        <p className="type-secondary">This page filters the sample list. The search field itself does not.</p>
        <div className={styles.stage}>
          <SearchField label="Filter records" value={filter} onChange={setFilter} placeholder="Name or note" />
          <ul className={review.results} aria-live="polite">
            {shown.length === 0 ? (
              <li className="type-secondary">No matches</li>
            ) : (
              shown.map((record) => (
                <li key={record.id} className={review.result}>
                  <p className={`${review.resultName} type-card`}>{record.name}</p>
                  <p className={`${review.resultNote} type-secondary`}>{record.note}</p>
                </li>
              ))
            )}
          </ul>
        </div>
      </section>

      <section className={`${styles.stageBlock} ${review.block}`}>
        <h2 className="type-section">Searchable select</h2>
        <p className="type-secondary">
          This is the existing Select, separate from Search. Type to filter. A query with no match shows No matches. On a phone the same list opens in a bottom sheet. Arrow keys move the desktop list.
        </p>
        <div className={styles.stage}>
          <Select label="Person" searchable value={person} onChange={(event) => setPerson(event.target.value)}>
            <option value="ana">Ana Cruz</option>
            <option value="leo">Leo Santos</option>
            <option value="mia" disabled>
              Mia Reyes
            </option>
          </Select>
          <Select label="Required person" searchable value="" error="Choose a person." onChange={() => undefined}>
            <option value="">Choose</option>
            <option value="ana">Ana Cruz</option>
          </Select>
        </div>
      </section>

      <Card title="Live shell">
        <p className="type-secondary">
          Collapse sidebar, the active Navigation & Search item, and the nested Buttons and Inputs groups are the production shell. Open sections is the phone drawer for that same list.
        </p>
      </Card>
    </>
  );
}
