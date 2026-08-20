# Platform Commercial Integration

**Status:** Planning baseline (PSP-00)  
**Implementation present:** No  
**Related:** D-P12-03, R-091, PSP-D-00-01

## Required intersection

```text
trusted actor
+ trusted organization
+ valid Platform product access
+ allowed commercial state
+ required entitlement
+ active PinoyServicePro product-local role/assignment
+ required product-local grant
+ resource/workflow authorization
= operation allowed
```

## Rules

- Platform subscription/entitlement controls **product entry**
- ServicePro product-local authorization controls **operations**
- ServicePro operational money remains separate from Platform SaaS billing
- No direct Platform table reads
- Catalog registration of `pinoy-service-pro` is not done (PSP-D-00-01)
- Do not invent final commercial-state transport (D-P12-03)
- Do not claim production-secure authentication (R-091)
