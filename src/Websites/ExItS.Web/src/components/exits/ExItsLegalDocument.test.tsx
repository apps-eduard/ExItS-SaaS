import { render, screen } from "@testing-library/react";

import { ExItsLegalDocument } from "./ExItsLegalDocument";

describe("ExItsLegalDocument", () => {
  it("shows draft status, last updated, and does not claim final legal text", () => {
    render(
      <ExItsLegalDocument
        title="Privacy Policy"
        description="Draft description"
        lastUpdatedLabel="Pending legal review"
      >
        <p>Our privacy policy is being finalized.</p>
      </ExItsLegalDocument>,
    );

    expect(screen.getByRole("heading", { name: "Privacy Policy" })).toBeInTheDocument();
    expect(screen.getByText(/draft — pending legal review/i)).toBeInTheDocument();
    expect(screen.getByText(/last updated:/i)).toBeInTheDocument();
    expect(screen.getByText("Pending legal review")).toBeInTheDocument();
    expect(screen.getByRole("status")).toHaveTextContent(/not ExItS’s final legal documents/i);
    expect(screen.getByRole("link", { name: /contact exits/i })).toHaveAttribute(
      "href",
      "/contact",
    );
  });
});
