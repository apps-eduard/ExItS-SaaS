# P18-WP04 — Role routing and Start Selling

## Summary

Automatic POS role routing to Owner/Manager/Cashier homes. Start Selling opens the shared cashier selling UI without changing role.

## Delivered

- `RoleHomeResolver` + `SellingModeService`
- `/owner`, `/manager`, `/cashier` dashboards
- NavigationGate lands on role home after setup
- Selling-mode banner and exit-to-dashboard from checkout cancel path

## Tests

`RoleHomeResolverTests` (role map, inactive denial, selling-mode return route).
