# PayMongo subscription checkout (test mode)

ExItS subscription billing is separate from merchant POS payments. Cash, GCash, Maya, card, QR, and Utang on a sale stay recorded tender types. PayMongo is used only when an organization or personal account pays ExItS for a SaaS subscription.

## Configure the test secret

From the Platform API project, store the secret outside source control:

```powershell
dotnet user-secrets set "PayMongo:SecretKey" "<YOUR_TEST_SECRET>" --project src/Platform/ExItS.Platform.Api
dotnet user-secrets set "PayMongo:WebhookSecret" "<YOUR_WEBHOOK_SECRET>" --project src/Platform/ExItS.Platform.Api
dotnet user-secrets set "PayMongo:PublicAppBaseUrl" "http://127.0.0.1:5177" --project src/Platform/ExItS.Platform.Api
```

Do not put the secret in `appsettings.json`, React, logs, or git. Test mode does not move real money.

`PublicAppBaseUrl` is optional on a local non-production host. If it is empty, the API accepts a loopback `Origin` such as `http://127.0.0.1:5177` for the PayMongo return URL. Production requires an HTTPS base URL.

## Local checkout

1. Choose a plan in ExItS. The browser sends only the plan id and billing cycle.
2. The API loads the catalog price in PHP and creates a PayMongo Hosted Checkout session.
3. The browser redirects to PayMongo. Complete a test payment there.
4. PayMongo returns to `/billing/payment/success` or `/billing/payment/cancelled`.
5. The success page shows **Verifying your payment** and asks the API to confirm the session. Returning to that URL does not activate the subscription by itself.

## Webhooks

Subscription activation is trusted only after a signed `checkout_session.payment.paid` event, or after the API reads the same paid session from PayMongo with the secret key. Amount and currency must match the stored ExItS quote. A repeated event does not activate twice. A mismatch or unknown session does not activate.

PayMongo cannot call `http://127.0.0.1` directly. To exercise the webhook later:

1. Run a temporary HTTPS tunnel to the Platform API.
2. In the PayMongo test dashboard, point the webhook to `https://<tunnel>/api/v1/platform/webhooks/paymongo` (the alias `/api/webhooks/paymongo` is the same handler).
3. Subscribe to `checkout_session.payment.paid`.
4. Set `PayMongo:WebhookSecret` to that endpoint's signing secret.

Until the tunnel is in place, local confirmation uses the server-side session lookup on the return page. That lookup still refuses amount or currency mismatches. It is not a fake paid status.
