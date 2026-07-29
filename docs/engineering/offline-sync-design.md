# PinoyBusinessPOS Offline Synchronization

[Home](../index.md) | [Security](security.md)

MAUI stores approved offline data in SQLite and queues commands with OperationId, DeviceId and idempotency key.

Supported first:

- Customer creation
- Remarks-based credit
- Payment on existing credit
- Later: sales and inventory movements

Financial records are append-only. Retry must not duplicate balances or inventory. Offline operations use the last valid entitlement snapshot with a controlled grace policy.
