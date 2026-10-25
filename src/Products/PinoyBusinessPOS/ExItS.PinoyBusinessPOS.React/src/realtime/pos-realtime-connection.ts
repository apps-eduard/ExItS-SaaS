import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { getPosAccessToken } from "@/api/platform/pos-access-token";
import { posApiUrl } from "@/api/pos/pos-http";
import {
  normalizePosRealtimeEnvelope,
  type PosRealtimeEventEnvelope,
} from "@/realtime/pos-realtime-event-types";

const HUB_PATH = "/hubs/pos-realtime";

type EnvelopeHandler = (envelope: PosRealtimeEventEnvelope) => void;

function orderKey(organizationId: string, orderId: string): string {
  return `${organizationId}:${orderId}`;
}

/**
 * Single shared POS SignalR connection per browser tab.
 */
class PosRealtimeConnectionManager {
  private connection: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private readonly handlers = new Set<EnvelopeHandler>();
  private readonly joinedOrganizations = new Set<string>();
  private readonly joinedOrders = new Set<string>();
  private organizationHeaderId: string | null = null;
  private refCount = 0;

  subscribe(handler: EnvelopeHandler): () => void {
    this.handlers.add(handler);
    this.refCount += 1;
    void this.ensureStarted();
    return () => {
      this.handlers.delete(handler);
      this.refCount = Math.max(0, this.refCount - 1);
      if (this.refCount === 0) {
        void this.stop();
      }
    };
  }

  setOrganizationHeader(organizationId: string | null): void {
    this.organizationHeaderId = organizationId;
  }

  async joinOrganization(organizationId: string): Promise<void> {
    if (!organizationId) return;
    this.setOrganizationHeader(organizationId);
    await this.ensureStarted();
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) return;
    if (this.joinedOrganizations.has(organizationId)) return;
    try {
      await this.connection.invoke("JoinOrganization", organizationId);
      this.joinedOrganizations.add(organizationId);
    } catch {
      /* auth or network — authoritative REST still applies */
    }
  }

  async leaveOrganization(organizationId: string): Promise<void> {
    if (!organizationId || !this.connection) return;
    this.joinedOrganizations.delete(organizationId);
    if (this.connection.state !== HubConnectionState.Connected) return;
    try {
      await this.connection.invoke("LeaveOrganization", organizationId);
    } catch {
      /* ignore */
    }
  }

  async joinOrder(organizationId: string, orderId: string): Promise<void> {
    if (!organizationId || !orderId) return;
    await this.ensureStarted();
    if (!this.connection || this.connection.state !== HubConnectionState.Connected) return;
    const key = orderKey(organizationId, orderId);
    if (this.joinedOrders.has(key)) return;
    try {
      await this.connection.invoke("JoinOrder", organizationId, orderId);
      this.joinedOrders.add(key);
    } catch {
      /* ignore */
    }
  }

  async leaveOrder(organizationId: string, orderId: string): Promise<void> {
    if (!organizationId || !orderId || !this.connection) return;
    const key = orderKey(organizationId, orderId);
    this.joinedOrders.delete(key);
    if (this.connection.state !== HubConnectionState.Connected) return;
    try {
      await this.connection.invoke("LeaveOrder", organizationId, orderId);
    } catch {
      /* ignore */
    }
  }

  private buildHeaders(): Record<string, string> | undefined {
    if (!this.organizationHeaderId) return undefined;
    return { "X-Pos-Organization-Id": this.organizationHeaderId };
  }

  private async ensureStarted(): Promise<void> {
    if (import.meta.env.MODE === "test") return;
    if (this.connection?.state === HubConnectionState.Connected) return;
    if (this.startPromise) return this.startPromise;

    this.connection ??= new HubConnectionBuilder()
      .withUrl(posApiUrl(HUB_PATH), {
        accessTokenFactory: () => getPosAccessToken() ?? "",
        headers: this.buildHeaders(),
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.off("PosRealtimeEvent");
    this.connection.on("PosRealtimeEvent", (raw: unknown) => {
      const envelope = normalizePosRealtimeEnvelope(raw);
      if (!envelope) return;
      for (const handler of this.handlers) {
        try {
          handler(envelope);
        } catch {
          /* isolate handler failures */
        }
      }
    });

    this.connection.onreconnected(() => {
      const orgs = [...this.joinedOrganizations];
      const orders = [...this.joinedOrders];
      this.joinedOrganizations.clear();
      this.joinedOrders.clear();
      for (const id of orgs) void this.joinOrganization(id);
      for (const key of orders) {
        const [orgId, ordId] = key.split(":");
        if (orgId && ordId) void this.joinOrder(orgId, ordId);
      }
    });

    this.startPromise = this.connection
      .start()
      .catch(() => undefined)
      .finally(() => {
        this.startPromise = null;
      });
    return this.startPromise;
  }

  private async stop(): Promise<void> {
    const conn = this.connection;
    this.connection = null;
    this.joinedOrganizations.clear();
    this.joinedOrders.clear();
    this.organizationHeaderId = null;
    if (!conn) return;
    try {
      await conn.stop();
    } catch {
      /* ignore */
    }
  }
}

export const posRealtimeConnection = new PosRealtimeConnectionManager();
