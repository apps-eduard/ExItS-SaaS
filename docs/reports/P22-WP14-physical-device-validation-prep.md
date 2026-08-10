# P22-WP14 — Physical device validation preparation

## Status
**Not Device Verified.** This document prepares validation for Android device `R58R61E3CAZ`; no ADB
validation was run in this work package.

## Preconditions

- Release-compatible Platform and POS endpoints reachable over HTTPS.
- A disposable organization with a branch, POS entitlement, active owner/staff account, and registered device.
- Device installed with the intended signed build and able to reach the configured APIs.
- A second registered device (or an owner session able to revoke the first) for revocation checks.

## Validation path

1. Confirm the application creates and reuses one installation identifier after restart.
2. Register the device to the organization branch and complete online sign-in/PIN setup.
3. Create a cash sale, payment attempt, sale return, expense, and void; each must include device
   authorization and succeed only for an active registration.
4. Revoke the device from owner device management. Repeat an online money mutation and confirm 403
   `application.pos_device.revoked` or `application.pos_device.not_authorized`.
5. Confirm the device no longer has an offline operating grant after an explicit server rejection.
6. Disable network without revoking the device. Confirm network failure does not clear the grant and
   the offline experience remains bounded by the existing grant/PIN policy.
7. Capture only non-sensitive diagnostics: correlation ID, shortened device ID, branch/organization
   IDs, timestamp, and app version. Do not capture PINs, bearer/session tokens, or payment data.

Record actual ADB commands, build version, endpoint environment, results, and evidence in a follow-up
validation report before changing the phase to Device Verified.
