import { authorizePosDevice } from "@/api/platform/pos-devices-client";
import { getDurableInstallationDeviceId } from "@/workspace/browser-installation-identity";
import {
  INITIAL_POS_DEVICE_CONTEXT,
  authorizedPosDeviceContext,
  unavailablePosDeviceContext,
  unregisteredPosDeviceContext,
  type PosDeviceContext,
} from "@/workspace/pos-device-context";

export type HydratePosDeviceInput = {
  organizationId: string | null | undefined;
  branchId: string | null | undefined;
  signal?: AbortSignal;
};

/**
 * Resolve durable browser installation id and authorize against Platform for the bound branch.
 * Fail-closed: no ephemeral register id; money stays gated until authorized.
 */
export async function hydratePosDeviceContext(
  input: HydratePosDeviceInput,
): Promise<PosDeviceContext> {
  if (!input.organizationId) {
    return {
      ...INITIAL_POS_DEVICE_CONTEXT,
      status: "unknown",
      registrationStatus: "unknown",
      detail: "Bind an organization before authorizing this browser as a POS device.",
    };
  }

  const identity = getDurableInstallationDeviceId();
  if (!identity.ok) {
    return unavailablePosDeviceContext(
      identity.reason === "storage_unavailable"
        ? "This browser cannot store a durable POS installation id. Enable storage and reload."
        : "This browser cannot create a durable POS installation id. Money actions stay blocked.",
    );
  }

  if (!input.branchId) {
    return {
      status: "unregistered",
      durableIdentityAvailable: true,
      registrationStatus: "unregistered",
      installationDeviceId: identity.installationDeviceId,
      posDeviceId: null,
      registeredBranchId: null,
      authorizedForSelectedBranch: false,
      detail:
        "Select a branch to authorize this browser for selling. Management screens do not require a branch device.",
    };
  }

  const result = await authorizePosDevice(
    input.organizationId,
    {
      installationDeviceId: identity.installationDeviceId,
      branchId: input.branchId,
    },
    input.signal,
  );

  if (result.ok) {
    return authorizedPosDeviceContext({
      installationDeviceId: result.value.installationDeviceId,
      posDeviceId: result.value.posDeviceId,
      registeredBranchId: result.value.branchId,
    });
  }

  const code = result.errorCode ?? "";
  if (code.includes("revoked")) {
    return {
      status: "revoked",
      durableIdentityAvailable: true,
      registrationStatus: "revoked",
      installationDeviceId: identity.installationDeviceId,
      posDeviceId: null,
      registeredBranchId: null,
      authorizedForSelectedBranch: false,
      detail: result.body?.detail ?? "This POS installation was revoked. Register it again.",
    };
  }

  if (code.includes("branch_conflict") || code.includes("not_authorized")) {
    const detail = result.body?.detail ?? "";
    const wrongBranch = detail.toLowerCase().includes("branch") || code.includes("branch_conflict");
    if (wrongBranch && !detail.toLowerCase().includes("not registered")) {
      return {
        status: "wrong_branch",
        durableIdentityAvailable: true,
        registrationStatus: "wrong_branch",
        installationDeviceId: identity.installationDeviceId,
        posDeviceId: null,
        registeredBranchId: null,
        authorizedForSelectedBranch: false,
        detail:
          detail ||
          "This browser is registered to a different branch. It cannot sell here without re-registration.",
      };
    }
  }

  if (result.status === 404 || code.includes("not_authorized") || code.includes("not_found")) {
    return unregisteredPosDeviceContext(
      identity.installationDeviceId,
      result.body?.detail ??
        "This browser is not registered as a POS device for this organization and branch.",
    );
  }

  return {
    status: "error",
    durableIdentityAvailable: true,
    registrationStatus: "error",
    installationDeviceId: identity.installationDeviceId,
    posDeviceId: null,
    registeredBranchId: null,
    authorizedForSelectedBranch: false,
    detail: result.body?.detail ?? "Could not authorize this browser as a POS device.",
  };
}
