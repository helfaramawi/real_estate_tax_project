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
| Authentication login/refresh | `POST /api/auth/login`, `POST /api/auth/refresh` | `AuthAppService`, auth validators | `LoginPage.tsx` | `AuthAppServiceTests`, `AuthValidatorTests` | `AuthEndpointsTests` | L2 | Token rotation edge-cases + negative refresh scenarios | Security Reviewer | In Progress |
| Property lifecycle | `GET/POST/PUT /api/properties` | `PropertyAppService`, property validators | `PropertiesPage.tsx`, `PropertyDetailPage.tsx` | `PropertyValidatorTests` | `PropertiesEndpointsTests` | L2 | Status transition and concurrency coverage | Backend Lead | In Progress |
| Taxpayer profile management | `GET/POST/PUT /api/taxpayers` | `TaxpayerAppService`, taxpayer validators | `TaxpayersPage.tsx`, `TaxpayerDetailPage.tsx` | `TaxpayerValidatorTests` | `TaxpayersEndpointsTests` | L2 | NID checksum algorithm + legal validation alignment | Product + Legal | In Progress |
| Billing workflow | `GET/POST /api/bills` | billing app services | `BillsPage.tsx` | TBD | TBD | L1 | Missing end-to-end settlement checks | QA Automation | In Progress |
| Payment settlement | `GET/POST /api/payments` | payment app services | `PaymentsPage.tsx` | TBD | TBD | L1 | Reconciliation and duplicate-payment scenarios | QA Automation | In Progress |
| Appeals workflow | `GET/POST /api/appeals` | appeal app services | `AppealsPage.tsx` | TBD | TBD | L1 | Deadline/legal rule validation | Product + Legal | In Progress |
| Exemption processing | `GET/POST /api/exemptions` | `ExemptionDomainService` | `ExemptionsPage.tsx` | `ExemptionDomainServiceTests` | TBD | L1 | Legal article mapping + combined payable effects | Product + Legal | In Progress |
| Valuation & assessments | `GET/POST /api/valuations`, `GET/POST /api/taxassessments` | `ValuationDomainService`, `TaxCalculationService` | `ValuationsPage.tsx` | `ValuationDomainServiceTests`, `TaxCalculationServiceTests` | TBD | L2 | Approval-flow + recalculation branch coverage | Backend Lead | In Progress |
| Risk scoring | `GET /api/risk/*` | `RiskScoringService` | `IntelligencePage.tsx`, `PredictionsPage.tsx` | `RiskScoringServiceTests` | TBD | L1 | Trigger and recalc cadence checks | Data Science Lead | In Progress |

## Weekly review checklist
1. Update maturity levels with evidence links.
2. Confirm failing gaps have assigned owners and target dates.
3. Promote only features that meet DoD and quality gates.
