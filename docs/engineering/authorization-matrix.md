# Authorization Matrix

[Security](security.md) | [Home](../index.md)

## Platform roles

| Capability | Platform Admin | Billing Admin | Support Agent |
|---|---:|---:|---:|
| View organizations | Yes | Yes | Yes |
| Manage products/plans | Yes | No | No |
| Activate subscription | Yes | Yes | No |
| Suspend organization | Yes | Conditional | No |
| View platform audit | Yes | Billing scope | Support scope |

## PinoyBusinessPOS roles

| Capability | Owner | Manager | Cashier | Inventory Staff |
|---|---:|---:|---:|---:|
| Manage subscription | Yes | No | No | No |
| Record Utang/payment | Yes | Yes | Yes | No |
| Manage products | Yes | Yes | Conditional | Yes |
| View profit | Yes | Yes | No | No |
| Refund completed sale | Yes | Yes | No | No |
| Adjust inventory | Yes | Yes | No | Yes |

Exact permissions are finalized in product phases and enforced by API tests.
