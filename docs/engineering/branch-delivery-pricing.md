# Branch Delivery Pricing

## V1 distance

V1 uses great-circle distance between the branch and customer coordinates with the Haversine formula:

```text
Δφ = φ2 - φ1
Δλ = λ2 - λ1
a  = sin²(Δφ / 2) + cos(φ1) × cos(φ2) × sin²(Δλ / 2)
c  = 2 × atan2(√a, √(1-a))
distanceKm = 6371.0088 × c
```

Angles are radians and coordinates are WGS84 decimal degrees. Haversine is a straight-line estimate, not road distance or a route promise.

## Fee formula

```text
extraDistanceKm = max(0, distanceKm - includedDistanceKm)
distanceCharge  = extraDistanceKm × additionalFeePerKm
deliveryFee     = baseDeliveryFee + distanceCharge
```

The quote is unavailable when the branch is not delivery-capable, coordinates or policy are missing, the merchandise subtotal is below the minimum, or distance exceeds the maximum. If a free-delivery threshold exists and the subtotal meets it, the delivery fee is zero.

The server preview is authoritative. Client-side examples in management UI are explanatory draft estimates and do not replace server validation or a customer order quote.
