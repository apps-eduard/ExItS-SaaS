# Environment matrix

| | Development | Testing/CI | Staging/Pilot | Production |
|---|---|---|---|---|
| Purpose | Local engineering | Automated tests | Controlled internal technical pilot | Public commercial (blocked) |
| Identity | Dev/Testing headers allowed | Dev/Testing headers allowed | **No** Dev/Testing headers | Production auth required (R-091 open) |
| DB ownership | Local disposable | Disposable Testcontainers/CI | Pilot DBs (not Production) | Production DBs |
| Config source | appsettings + user secrets | test fixtures / env | env + secret store | secret store only |
| Secret source | local | CI secrets | env-owned store | env-owned store |
| Logging | Information | Warning/Information | Information | Warning+ (ops choice) |
| HTTPS | optional local | optional | required (proxy TLS) | required (open validation) |
| AllowedHosts | may be `*` in Dev only | test-specific | explicit hosts | explicit hosts (no `*`) |
| Diagnostics | available | available | disabled | disabled |
| Backup policy | optional | drill/as needed | required pre-migrate | required pre-migrate |
| Data class | synthetic | synthetic | pilot / disclosed | customer Production |
| Permitted users | developers | CI agents | authorized pilot users | customers (blocked) |

Rules:

- Staging/Pilot must not inherit Development defaults.
- Production fails startup on insecure configuration.
- No environment silently falls back to Development behavior.
- Secrets are not copied between environments without authorization.
- Pilot data is not represented as Production data.
