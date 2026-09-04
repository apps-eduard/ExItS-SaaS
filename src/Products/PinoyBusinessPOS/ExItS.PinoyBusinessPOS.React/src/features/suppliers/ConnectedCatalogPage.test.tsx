import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { ConnectedCatalogPage } from "@/features/suppliers/ConnectedCatalogPage";

const orgId = "11111111-1111-1111-1111-111111111111";
const branchId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const supplierId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const relationshipId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const exposureNew = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const exposureReview = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const exposureConflict = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const exposureLinked = "99999999-9999-4999-8999-999999999999";
const candidateA = "10101010-1010-4010-8010-101010101010";
const candidateB = "20202020-2020-4020-8020-202020202020";

const workspaceMock = {
  boundWorkspace: {
    organizationId: orgId,
    organizationDisplayName: "Paul store",
    branchId,
    branchName: "Main Branch",
    experience: "operations" as const,
  },
  sessionGrant: {
    productAccessAllowed: true,
    membershipRole: "OrganizationOwner",
    productLocalRoleCode: "Owner",
    mappedPosRoleCode: "Owner",
  } as Record<string, unknown>,
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceMock,
}));

const getSupplier = vi.fn();
const classifyCatalogReadiness = vi.fn();
const autoLinkExactMatches = vi.fn();
const linkProduct = vi.fn();
const createBuyerProductAndLink = vi.fn();

vi.mock("@/api/pos/pos-suppliers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-suppliers-client")>();
  return {
    ...actual,
    getSupplier: (...args: unknown[]) => getSupplier(...args),
  };
});

vi.mock("@/api/pos/pos-connected-suppliers-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/pos/pos-connected-suppliers-client")>();
  return {
    ...actual,
    classifyCatalogReadiness: (...args: unknown[]) => classifyCatalogReadiness(...args),
    autoLinkExactMatches: (...args: unknown[]) => autoLinkExactMatches(...args),
    linkProduct: (...args: unknown[]) => linkProduct(...args),
    createBuyerProductAndLink: (...args: unknown[]) => createBuyerProductAndLink(...args),
  };
});

function readinessPayload() {
  return {
    relationshipId,
    ready: 1,
    new: 1,
    review: 1,
    conflict: 1,
    items: [
      {
        exposureId: exposureLinked,
        supplierProductId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1",
        supplierName: "Linked Rice",
        supplierSku: "L-1",
        supplierBarcode: null,
        unitOfMeasureCode: "Kilogram",
        poPrice: 40,
        status: "AlreadyLinked",
        canAutoLink: false,
        candidateBuyerProductId: candidateA,
        candidateBuyerProductName: "My Rice",
        nameMatched: true,
        skuMatched: true,
        barcodeMatched: true,
        unitCompatible: true,
        matchDetails: "Already linked",
        linkedBuyerProductId: candidateA,
        conflictCandidates: [],
      },
      {
        exposureId: exposureNew,
        supplierProductId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa2",
        supplierName: "Brand New Snack",
        supplierSku: "N-1",
        supplierBarcode: null,
        unitOfMeasureCode: "Piece",
        poPrice: 12,
        status: "New",
        canAutoLink: false,
        candidateBuyerProductId: null,
        candidateBuyerProductName: null,
        nameMatched: false,
        skuMatched: false,
        barcodeMatched: false,
        unitCompatible: false,
        matchDetails: "No credible buyer product match.",
        linkedBuyerProductId: null,
        conflictCandidates: [],
      },
      {
        exposureId: exposureReview,
        supplierProductId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa3",
        supplierName: "Review Oil",
        supplierSku: "R-1",
        supplierBarcode: null,
        unitOfMeasureCode: "Liter",
        poPrice: 80,
        status: "Review",
        canAutoLink: false,
        candidateBuyerProductId: candidateA,
        candidateBuyerProductName: "Cooking Oil",
        nameMatched: true,
        skuMatched: true,
        barcodeMatched: false,
        unitCompatible: true,
        matchDetails: "Name and SKU match",
        linkedBuyerProductId: null,
        conflictCandidates: [],
      },
      {
        exposureId: exposureConflict,
        supplierProductId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa4",
        supplierName: "Conflict Soap",
        supplierSku: "C-1",
        supplierBarcode: "4006381333931",
        unitOfMeasureCode: "Piece",
        poPrice: 20,
        status: "Conflict",
        canAutoLink: false,
        candidateBuyerProductId: null,
        candidateBuyerProductName: null,
        nameMatched: false,
        skuMatched: true,
        barcodeMatched: true,
        unitCompatible: true,
        matchDetails: "Identifiers point to different buyer products.",
        linkedBuyerProductId: null,
        conflictCandidates: [
          {
            productId: candidateA,
            name: "Soap A",
            sku: "SKU-A",
            unitOfMeasureCode: "Piece",
          },
          {
            productId: candidateB,
            name: "Soap B",
            sku: "SKU-B",
            unitOfMeasureCode: "Piece",
          },
        ],
      },
    ],
  };
}

async function renderPage() {
  render(
    <AppProviders>
      <MemoryRouter initialEntries={[`/suppliers/${supplierId}/catalog`]}>
        <Routes>
          <Route path="/suppliers/:supplierId/catalog" element={<ConnectedCatalogPage />} />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
  await waitFor(() => expect(screen.getByTestId("connected-catalog-page")).toBeInTheDocument());
}

describe("ConnectedCatalogPage readiness UX", () => {
  beforeEach(() => {
    getSupplier.mockResolvedValue({
      supplierId,
      organizationId: orgId,
      name: "Mica Store",
      status: "Active",
      connectionType: "ConnectedOrganization",
      connectedRelationshipId: relationshipId,
    });
    autoLinkExactMatches.mockResolvedValue({
      relationshipId,
      linkedNow: 0,
      alreadyReady: 0,
      review: 0,
      new: 0,
      conflict: 0,
      linkedExposureIds: [],
    });
    classifyCatalogReadiness.mockResolvedValue(readinessPayload());
    linkProduct.mockResolvedValue({ linkId: "l1" });
    createBuyerProductAndLink.mockResolvedValue({
      buyerProductId: candidateB,
      createdNewProduct: true,
      alreadyLinked: false,
    });
  });

  it("shows counters that match classified rows and Linked has no action buttons", async () => {
    await renderPage();
    await waitFor(() => expect(screen.getByTestId("connected-ready-all")).toHaveTextContent("All (4)"));
    expect(screen.getByTestId("connected-ready-newProduct")).toHaveTextContent("New products (1)");
    expect(screen.getByTestId("connected-ready-checkMatch")).toHaveTextContent("Check match (1)");
    expect(screen.getByTestId("connected-ready-attention")).toHaveTextContent("Attention (1)");
    expect(screen.getByTestId("connected-ready-linked")).toHaveTextContent("Linked (1)");

    const linkedCard = screen.getByTestId(`connected-catalog-item-${exposureLinked}`);
    expect(within(linkedCard).getByText("Linked")).toBeInTheDocument();
    expect(within(linkedCard).queryByRole("button")).not.toBeInTheDocument();
  });

  it("offers New product and Check match / Attention actions only", async () => {
    const user = userEvent.setup();
    await renderPage();
    await waitFor(() => screen.getByTestId(`connected-catalog-item-${exposureNew}`));

    expect(screen.getByTestId(`connected-create-link-${exposureNew}`)).toBeInTheDocument();
    expect(screen.getByTestId(`connected-new-help-${exposureNew}`)).toHaveTextContent(
      "No credible matching product was found in your catalog. Add it as a new product.",
    );
    expect(screen.queryByTestId(`connected-find-existing-${exposureNew}`)).not.toBeInTheDocument();
    expect(screen.queryByTestId("connected-link-picker")).not.toBeInTheDocument();
    expect(screen.getByTestId(`connected-confirm-match-${exposureReview}`)).toBeInTheDocument();
    expect(screen.getByTestId(`connected-add-as-new-${exposureReview}`)).toBeInTheDocument();
    expect(screen.queryByText("Choose another product")).not.toBeInTheDocument();
    expect(screen.queryByText("Search my products")).not.toBeInTheDocument();

    await user.click(screen.getByTestId(`connected-conflict-pick-${candidateB}`));
    await user.click(screen.getByTestId(`connected-link-selected-${exposureConflict}`));
    await waitFor(() =>
      expect(linkProduct).toHaveBeenCalledWith(
        expect.anything(),
        relationshipId,
        expect.objectContaining({ exposureId: exposureConflict, buyerProductId: candidateB }),
      ),
    );
  });

  it("runs AutoLinkExactMatches then refreshes readiness", async () => {
    autoLinkExactMatches.mockResolvedValue({
      relationshipId,
      linkedNow: 1,
      alreadyReady: 0,
      review: 0,
      new: 0,
      conflict: 0,
      linkedExposureIds: [exposureLinked],
    });
    await renderPage();
    await waitFor(() => expect(autoLinkExactMatches).toHaveBeenCalled());
    await waitFor(() =>
      expect(screen.getByTestId("connected-catalog-message")).toHaveTextContent(
        "Automatically linked 1 exact match(es).",
      ),
    );
    expect(classifyCatalogReadiness.mock.calls.length).toBeGreaterThanOrEqual(1);
  });
});
