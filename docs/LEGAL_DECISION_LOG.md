# Legal & Business Rule Decision Log

This log tracks legal/business-rule placeholders that must be confirmed with authority policy before Wave 3 go/no-go.

## Status Legend
- `Open` = decision pending
- `In Review` = submitted for legal/compliance review
- `Approved` = approved value/rule confirmed
- `Implemented` = approved rule is in code/config and validated

## Decision Register

| ID | Placeholder | Source Reference | Current Default | Decision Owner | Due Date | Tracking Ticket | Status | Notes |
|---|---|---|---|---|---|---|---|---|
| LEG-001 | Tax rates population in `TaxRule` table | `README.md` business rules TODO | Not finalized | Product + Legal | 2026-06-03 | RETAX-401 | In Review | Requires confirmation against Law 196/2008 and annual decree updates |
| LEG-002 | Maintenance deduction % in `ValuationRule` | `README.md` business rules TODO | 30% placeholder | Product + Legal | 2026-06-03 | RETAX-402 | In Review | Validate whether percentage varies by property class |
| LEG-003 | `TaxRule.MinTaxableValue` floor | `README.md` business rules TODO | Not finalized | Product + Legal | 2026-06-03 | RETAX-403 | Open | Must include effective-date versioning strategy |
| LEG-004 | Monthly penalty rate in `CalculatePenaltyAsync` | `README.md` business rules TODO | Not finalized | Product + Legal | 2026-06-03 | RETAX-404 | Open | Finance and legal joint sign-off required |
| LEG-005 | Appeal submission deadline in `AppealAppService.SubmitAsync` | `README.md` business rules TODO | 60-day assumption | Product + Legal | 2026-06-03 | RETAX-405 | In Review | Confirm exact statutory timing and exception handling |
| LEG-006 | Exemption legal article mapping in `ExemptionRule.LegalReference` | `README.md` business rules TODO | Partial mapping | Product + Legal | 2026-06-03 | RETAX-406 | Open | Requires full article-to-rule matrix |
| LEG-007 | Egyptian NID checksum algorithm in `TaxpayerValidators` | `README.md` business rules TODO | Format-only validation | Product + Legal | 2026-06-03 | RETAX-407 | Open | Legal confirm + engineering feasibility spike |

## Governance Rules
1. No row can move to `Implemented` without legal approval artifact reference.
2. Any row still `Open` after due date must include escalation note and revised date.
3. Wave 2 cannot be marked GO if any critical row lacks owner or due date.
