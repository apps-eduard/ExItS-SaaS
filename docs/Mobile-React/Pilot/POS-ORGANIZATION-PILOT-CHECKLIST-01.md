# Organization POS — Pilot Checklist

**Pilot target:** Single-branch small store  
**Related:** [Pilot Guide](POS-ORGANIZATION-PILOT-GUIDE-01.md) · [Feedback template](POS-PILOT-FEEDBACK-TEMPLATE-01.md)

---

## PRE-PILOT

- [ ] Platform API + POS API + React client running (local validation or pilot host)
- [ ] Organization created; **one** active branch (Main Store)
- [ ] Owner account works
- [ ] Cashier + Inventory Staff invited (Scenario B)
- [ ] ReportingUser invited (optional)
- [ ] 10–20 products loaded (unit + weighted + barcode + expiry-tracked + low-stock)
- [ ] Opening stock recorded for tracked items
- [ ] 1–2 suppliers created
- [ ] 2–3 customers created (for Utang)
- [ ] Device registration path understood (PWA register)
- [ ] Operators briefed: Manual GCash = typed reference; Utang ≠ Supplier Credit
- [ ] Feedback template printed/shared

### Recommended demo seed (checklist only — not production data)

| Item | Suggestion |
|------|------------|
| Organization | Demo Sari-Sari Store |
| Branch | Main Store |
| Staff | 1 Owner, 1 Cashier, 1 Inventory Staff |
| Products | 10–20 real store items |
| Suppliers | 1–2 |
| Customers | 2–3 |
| Payments to practice | Cash, Manual GCash, Utang, Supplier Credit |

---

## FIRST DAY

- [ ] Owner completes setup / ready next steps
- [ ] Cashier registers device (if required) and opens shift
- [ ] First Cash sale succeeds
- [ ] Manual GCash sale with reference succeeds
- [ ] Utang sale updates customer balance
- [ ] Direct Purchase increases stock
- [ ] Supplier credit receive + later payment works
- [ ] Owner opens sales report and (if entitled) exports CSV
- [ ] Cashier closes shift; variance visible

---

## DAILY

- [ ] Open shift before selling
- [ ] Sell with Cash / Manual GCash / Utang as needed
- [ ] Receive stock / direct buy as needed
- [ ] Record waste/loss for damaged/expired
- [ ] Note any P0/P1 issues in feedback template

---

## END OF DAY

- [ ] Close all open cashier shifts
- [ ] Review sales + payments
- [ ] Review Utang and Supplier Credit
- [ ] Review low/expiring stock
- [ ] File feedback for confusing steps

---

## ISSUE SEVERITY

| Severity | Meaning |
|----------|---------|
| **P0** | Cannot sell; wrong money/Utang/payable; data loss; cross-org/security leak |
| **P1** | Common flow blocked; major UX failure; report wrong enough to hurt operations |
| **P2** | Confusing but usable; polish; minor responsive issue |

---

## ACCEPTANCE SCENARIOS

| ID | Scenario | Expected |
|----|----------|----------|
| P1 | First Cash sale after setup | PASS |
| P2 | Manual GCash with reference | PASS |
| P3 | Utang sale + balance update | PASS |
| P4 | Direct Purchase → stock up | PASS |
| P5 | Supplier credit + later payment | PASS |
| P6 | Waste decreases stock | PASS |
| P7 | Stock count variance | PASS |
| P8 | Daily report + CSV | PASS |
| P9 | Shift close / cash variance | PASS |
| P10 | Cashier cannot manage supplier payments / inventory; ReportingUser cannot mutate | PASS |

---

## GO / NO-GO

**PILOT_GO = YES** only if:

- No open P0
- Cashier can sell with shift/register
- Inventory core works
- Purchase + supplier credit works
- Customer Utang works
- Key reports work
- Permission separation holds
- No common UI dead-end without clear next action

If P0 appears: **PILOT_GO = NO** → one repair package only.
