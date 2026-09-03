import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import { ExItsFaq } from "./ExItsFaq";

describe("ExItsFaq", () => {
  it("renders questions and expands an answer", async () => {
    const user = userEvent.setup();
    render(
      <ExItsFaq
        items={[
          { question: "What is ExItS?", answer: "A multi-product SaaS platform." },
        ]}
      />,
    );

    expect(screen.getByRole("button", { name: /what is exits\?/i })).toBeVisible();
    await user.click(screen.getByRole("button", { name: /what is exits\?/i }));
    expect(screen.getByText("A multi-product SaaS platform.")).toBeVisible();
  });
});
