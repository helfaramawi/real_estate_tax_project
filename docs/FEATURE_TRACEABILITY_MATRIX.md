# Feature Traceability Matrix

This matrix is the immediate next execution step after the deep-study baseline. It connects business features to implementation artifacts and verification evidence.

## How to use
- One row per feature/workflow.
- Keep statuses realistic; mark unknown items as `TBD`.
- Do not mark `Ready for Expansion` until every critical field is complete.

## Status legend
- `TBD` = not yet reviewed
- `In Progress` = partially mapped or tested
- `Done` = mapped and verified

## Matrix

| Domain Feature | API Endpoint(s) | Application Service/Validator | Frontend Page(s) | Unit Tests | Integration Tests | Maturity (L0-L4) | Gaps | Owner | Status |
|---|---|---|---|---|---|---|---|---|---|
| Authentication login/refresh | `POST /api/auth/login`, `POST /api/auth/refresh` | `AuthAppService`, auth validators | `LoginPage.tsx` | `AuthAppServiceTests`, `AuthValidatorTests` | `AuthEndpointsTests` | TBD | Token rotation edge-cases | TBD | In Progress |
| Property lifecycle | `GET/POST/PUT /api/properties`, `POST /api/properties/{id}/verify` | `PropertyAppService`, property validators | `PropertiesPage.tsx`, `PropertyDetailPage.tsx` | `PropertyValidatorTests` | `PropertiesEndpointsTests` (includes verify transition checks) | L2 | Needs richer role-path + maker-checker integration scenarios | Wave1 Team | In Progress |
| Taxpayer profile management | `GET/POST/PUT /api/taxpayers` | `TaxpayerAppService`, taxpayer validators | `TaxpayersPage.tsx`, `TaxpayerDetailPage.tsx` | `TaxpayerValidatorTests` | `TaxpayersEndpointsTests` | TBD | NID checksum algorithm | TBD | In Progress |
| Billing workflow | `GET/POST /api/bills` | billing app services | `BillsPage.tsx` | TBD | TBD | TBD | Missing end-to-end settlement checks | TBD | TBD |
| Payment settlement | `GET/POST /api/payments` | payment app services | `PaymentsPage.tsx` | TBD | TBD | TBD | Reconciliation scenarios | TBD | TBD |
| Appeals workflow | `GET/POST /api/appeals` | appeal app services | `AppealsPage.tsx` | TBD | TBD | TBD | Deadline/legal rule validation | TBD | TBD |
| Exemption processing | `GET/POST /api/exemptions` | `ExemptionDomainService` | `ExemptionsPage.tsx` | `ExemptionDomainServiceTests` | TBD | TBD | Legal article mapping | TBD | TBD |
| Valuation & assessments | `GET/POST /api/valuations`, `GET/POST /api/taxassessments` | `ValuationDomainService`, `TaxCalculationService` | `ValuationsPage.tsx` | `ValuationDomainServiceTests`, `TaxCalculationServiceTests` | TBD | TBD | Approval-flow coverage | TBD | TBD |
| Risk scoring | `GET /api/risk/*` | `RiskScoringService` | `IntelligencePage.tsx`, `PredictionsPage.tsx` | `RiskScoringServiceTests` | TBD | TBD | Trigger and recalc cadence checks | TBD | TBD |

## Weekly review checklist
1. Update maturity levels with evidence links.
2. Confirm failing gaps have assigned owners and target dates.
3. Promote only features that meet DoD and quality gates.
