import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { AppProviders } from "@/app/providers";
import { PersonalStoreIdentity } from "@/features/customer-ordering/PersonalStoreIdentity";

describe("PersonalStoreIdentity", () => {
  it("matches the store-card identity: avatar, name, connected, ordering badge", () => {
    render(
      <AppProviders>
        <PersonalStoreIdentity storeName="Mica Org" canCustomerOrder={false} />
      </AppProviders>,
    );

    const identity = screen.getByTestId("personal-store-identity");
    expect(identity).toHaveTextContent("M");
    expect(identity).toHaveTextContent("Mica Org");
    expect(identity).toHaveTextContent("Connected");
    expect(identity).toHaveTextContent("Ordering unavailable");
    expect(screen.queryByText(/Linked as/i)).not.toBeInTheDocument();
  });

  it("shows Linked as only for a distinct merchant-assigned name", () => {
    render(
      <AppProviders>
        <PersonalStoreIdentity
          storeName="Mica Org"
          relationshipLabel="Ana Reyes"
          canCustomerOrder
        />
      </AppProviders>,
    );

    expect(screen.getByText("Linked as Ana Reyes")).toBeInTheDocument();
    expect(screen.getByText("Ordering available")).toBeInTheDocument();
  });
});
