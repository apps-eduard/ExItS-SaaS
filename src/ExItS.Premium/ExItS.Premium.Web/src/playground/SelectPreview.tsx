import { useState } from "react";
import { Button } from "../components/button/Button";
import { Select } from "../components/select/Select";
import styles from "./SelectPreview.module.scss";

const categories = (
  <>
    <option value="">Choose</option>
    <option value="groceries">Groceries</option>
    <option value="rent">Rent</option>
    <option value="utilities">Utilities</option>
  </>
);

export function SelectPreview() {
  const [category, setCategory] = useState("groceries");
  const [method, setMethod] = useState("cash");
  const [status, setStatus] = useState("");
  const [focused, setFocused] = useState(false);

  return (
    <main className={styles.page}>
      <header className={styles.intro}>
        <h1 className="type-page">Premium Select</h1>
        <p className="type-secondary">
          Isolated preview. The shared design-system navigation is unchanged. Below 768px the list opens in a bottom sheet.
        </p>
      </header>
      <section className={styles.grid}>
        <Select label="Category" value={category} onChange={(event) => setCategory(event.target.value)}>
          {categories}
        </Select>
        <div className={styles.sample}>
          <Select label="Focused category" autoFocus value={category} onChange={(event) => setCategory(event.target.value)} onFocus={() => setFocused(true)} onBlur={() => setFocused(false)}>
            {categories}
          </Select>
          <p className="type-caption">{focused ? "Focus ring is on the category field." : "Tab to the focused sample to see the ring."}</p>
        </div>
        <Select label="Payment method" value={method} onChange={(event) => setMethod(event.target.value)}>
          <option value="cash">Cash</option>
          <option value="card">Card</option>
          <option value="transfer">Transfer</option>
        </Select>
        <Select label="Disabled" value="rent" disabled onChange={() => undefined}>
          <option value="rent">Rent</option>
        </Select>
        <Select label="Error" defaultValue="" error="Choose a category.">
          {categories}
        </Select>
        <Select label="Success" defaultValue="rent" success="Category saved.">
          <option value="rent">Rent</option>
          <option value="utilities">Utilities</option>
        </Select>
        <Select label="Transaction status" value={status} onChange={(event) => setStatus(event.target.value)}>
          <optgroup label="Open">
            <option value="">Choose</option>
            <option value="due">Due</option>
            <option value="partial">Partial</option>
          </optgroup>
          <optgroup label="Closed">
            <option value="paid">Paid</option>
            <option value="void">Void</option>
            <option value="hold" disabled>
              Hold
            </option>
          </optgroup>
        </Select>
        <Select label="Search status" searchable defaultValue="">
          <option value="">Choose</option>
          <option value="paid">Paid</option>
          <option value="due">Due</option>
          <option value="partial">Partial</option>
          <option value="void">Void</option>
        </Select>
        <form
          className={styles.form}
          onSubmit={(event) => {
            event.preventDefault();
            const value = String(new FormData(event.currentTarget).get("category"));
            event.currentTarget.dataset.submitted = value;
          }}
        >
          <Select name="category" label="Form category" defaultValue="rent">
            {categories}
          </Select>
          <div className={styles.actions}>
            <Button type="submit">Save</Button>
            <Button type="reset" variant="secondary">Reset</Button>
          </div>
        </form>
      </section>
    </main>
  );
}
