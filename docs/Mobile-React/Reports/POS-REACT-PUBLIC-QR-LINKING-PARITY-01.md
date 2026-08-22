# POS React â€” Personal + Business QR / Public ID Linking Parity

**Status:** `AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
**Branch:** `feat/pos-react-client`  
**Starting HEAD:** `a38ee5d62e67f7d86973bd91d470af2785c2e43a`

## What existed before

- Platform public identity + resolve + CustomerLinkRequest APIs (Phase 19)
- MAUI My QR / Business QR / customer create-with-personal-link
- React: text-only My ExItS ID, Personal Accept/Decline, Linked merchants (RMAP-22F)
- React device-registration code UX **removed** (prior package) â€” unchanged

## Delivered in this package

| Surface | Status |
|---|---|
| Personal My QR (visual QR, copy/share, safe payload) | Added |
| Organization Business QR | Added (`/org/business-qr`) |
| TS ExItS QR envelope + purpose guard | Added |
| Still-image scan + manual ID entry | Added |
| Customer create â†’ resolve â†’ confirm â†’ `with-personal-link` | Added |
| Personal Accept/Decline consent | Preserved |
| Device-registration QR reintroduction | **Not done** (correct) |
| RMAP-B04 / B05 | **NOT STARTED** |

## Canonical rules preserved

- Scan/resolve never activates LinkedCustomerAppUser
- Pending CustomerLinkRequest â†’ Personal Accept
- No email/phone/token/UUID in QR payload
- Exact-match resolve only

## Flags

`REACT_PERSONAL_PUBLIC_QR=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
`REACT_BUSINESS_PUBLIC_QR=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
`REACT_CUSTOMER_LINK_ENTRY_FLOW=AWAITING_PRODUCT_OWNER_CHATGPT_REVIEW`  
`RMAP_B04_AUTHORIZED=NO`  
`RMAP_B05_AUTHORIZED=NO`  
`RMAP_23_AUTHORIZED=NO`  
`PRODUCTION_CUTOVER=NO`
