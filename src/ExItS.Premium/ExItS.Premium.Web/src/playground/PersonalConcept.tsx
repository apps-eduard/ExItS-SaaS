import { useState } from "react";
import { motion } from "motion/react";
import { Badge } from "../components/badge/Badge";
import { Button } from "../components/button/Button";
import { Card } from "../components/card/Card";
import { ExIcon } from "../icons/ExIcon";
import { MOTION_SPRING } from "../motion/motion";
import { SearchField } from "../components/search/SearchField";
import styles from "./Playground.module.scss";

export function PersonalConcept() {
  const [query, setQuery] = useState("");

  return (
    <motion.div
      className={styles.concept}
      initial={{ y: 8 }}
      animate={{ y: 0 }}
      transition={MOTION_SPRING}
    >
      <p className="type-caption">Sample data. Visual evaluation only. Not connected to ExItS.</p>
      <Card eyebrow="Available balance">
        <p className={styles.amount}>₱24,580.00</p>
        <p className="type-secondary">Personal · sample figure</p>
      </Card>
      <div className={styles.actions}>
        <Button variant="secondary" fullWidth icon={<ExIcon name="add" />}>
          Add
        </Button>
        <Button variant="secondary" fullWidth icon={<ExIcon name="arrow_outward" />}>
          Send
        </Button>
        <Button variant="secondary" fullWidth icon={<ExIcon name="south_west" />}>
          Request
        </Button>
      </div>
      <SearchField label="Search" value={query} onChange={setQuery} placeholder="Name or note" />
      <Card>
        <div className={styles.tx}>
          <span className={styles.txMark} aria-hidden="true">
            M
          </span>
          <span className={styles.txBody}>
            <span className={`${styles.txTitle} type-card`}>Market</span>
            <span className={`${styles.txMeta} type-caption`}>Today · Groceries</span>
          </span>
          <span className={styles.txEnd}>
            <span className={styles.txAmt}>−₱860.00</span>
            <Badge tone="success">Paid</Badge>
          </span>
        </div>
      </Card>
      <Button fullWidth>Record payment</Button>
    </motion.div>
  );
}
