# Endpoint-to-Permission Matrix (Wave 2 Evidence Draft)

This document is the working evidence artifact for Wave 2 A2/B checks.

## Scope
Critical protected API areas used in Wave 1 + Wave 2 readiness gates.

| Area | Endpoint(s) | Expected Access | Negative Test Evidence | Status |
|---|---|---|---|---|
| Auth | `POST /api/auth/refresh` | Authenticated user token context | `AuthEndpointsTests` (invalid/expired token paths) | In Progress |
| Properties | `GET/POST/PUT /api/properties` | TaxOfficer, Supervisor, Admin (policy-driven) | `PropertiesEndpointsTests` unauthorized/forbidden coverage | In Progress |
| Taxpayers | `GET/POST/PUT /api/taxpayers` | TaxOfficer, Supervisor, Admin (policy-driven) | `TaxpayersEndpointsTests` unauthorized/forbidden coverage | In Progress |
| Bills | `GET/POST /api/bills` | Billing roles + Admin (policy-driven) | `BillsEndpointsTests` unauthorized/forbidden coverage | In Progress |
| Payments | `GET/POST /api/payments` | Cashier/Finance roles + Admin (policy-driven) | `PaymentsEndpointsTests` unauthorized/forbidden coverage | In Progress |

## Notes
- Role labels above are expected business-role intents and must be reconciled against effective `[Authorize]` policy configuration in API controllers before sign-off.
- Replace “In Progress” with “Done” only after both positive and negative path integration tests are linked.
