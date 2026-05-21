# Next Sprint Plan — Professional Readiness Sprint

**Sprint window:** 2026-05-22 to 2026-06-04  
**Objective:** Complete Wave 2 trust/compliance gates and remove blockers to Wave 3 go/no-go.

## 1) Sprint Outcomes (Definition of Success)
- Core revenue modules (Auth, Property, Assessment, Billing, Payment) have documented L3 evidence.
- Top 5 critical workflows have deterministic integration coverage.
- Wave 2 checklist sections A–E have evidence links and owner/date assignments.
- All legal-rule placeholders have owner + due date (or implemented approved values).

## 2) Workstreams and Owners

| Workstream | Owner | Backup | Deliverables | Due Date |
|---|---|---|---|---|
| Traceability completion | Eng Lead | Senior QA | Fully populated rows, no `TBD` in critical wave items | 2026-05-26 |
| Integration test expansion | QA Automation | Backend Lead | 5 workflow tests added + deterministic setup notes | 2026-05-30 |
| Authorization/audit verification | Security Reviewer | Backend Lead | Endpoint permission matrix + negative auth evidence + audit evidence | 2026-05-30 |
| Data consistency controls | Backend Lead | DBA | Ownership %, status transition, soft-delete, concurrency evidence | 2026-06-02 |
| Legal placeholders governance | Product + Legal | Eng Lead | Owner/date/ticket for every placeholder | 2026-06-03 |
| Quality gate execution | QA Lead | DevOps | Build/unit/integration/lint/smoke runbook with run logs | 2026-06-04 |

## 3) Story Backlog (Sprint-ready)

### SP-01 Complete Feature Traceability Matrix
- Update `docs/FEATURE_TRACEABILITY_MATRIX.md` rows for critical workflows with:
  - explicit endpoints,
  - app service/validator mapping,
  - unit/integration test references,
  - maturity level and rationale,
  - gap owner and target date.
- **Acceptance:** No `TBD` in critical Wave 2 rows; weekly checklist items link to evidence.

### SP-02 Add Deterministic Integration Tests (Top 5)
Add/extend integration tests for:
1. Property lifecycle transition negatives.
2. Assessment approval + recalculation branch.
3. Bill generation + partial/full settlement status updates.
4. Appeals + exemptions combined payable-balance scenario.
5. Auth negative/refresh token edge-case path.
- **Acceptance:** Tests pass in local compose and are deterministic across reruns.

### SP-03 Trust & Compliance Evidence Pack
- Build endpoint-to-role matrix for protected APIs.
- Add positive/negative auth test evidence.
- Add audit event evidence for sensitive actions.
- **Acceptance:** Wave 2 section B gate rows all have evidence links.

### SP-04 Data Consistency and Concurrency Controls
- Verify and test:
  - ownership percentage sums,
  - illegal status transitions,
  - soft-delete behavior consistency,
  - idempotency/concurrency behavior on update-heavy paths.
- **Acceptance:** Wave 2 section A3 and B row complete with sign-off notes.

### SP-05 Legal-Rule Placeholder Closure
- For each placeholder in `README.md` business-rule TODO list, record:
  - decision owner,
  - due date,
  - tracking ticket,
  - temporary default (if any).
- **Acceptance:** No unowned/undated legal placeholders.

## 4) Daily Execution Cadence
- **Daily standup:** risk/blocker review + checklist progression.
- **Mid-sprint checkpoint (2026-05-29):** freeze scope, evaluate slip risk.
- **Final review (2026-06-04):** run Wave 2 go/no-go checklist and collect signatures.

## 5) Risk Register
| Risk | Impact | Mitigation |
|---|---|---|
| Flaky integration environment | Delays go/no-go evidence | Deterministic seed + isolated test data reset per suite |
| Legal approval delays | Placeholder closure blocked | Escalate by 2026-05-30; capture interim owner/date commitments |
| Incomplete permission mapping | Compliance gate failure | Prioritize protected endpoint inventory in first 2 sprint days |

## 6) End-of-Sprint Exit Review
Use `docs/WAVE2_COMPLETION_CHECKLIST.md` and proceed to Wave 3 only if all mandatory gates are met.
