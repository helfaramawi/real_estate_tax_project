# Wave 1 Completion Checklist (Go/No-Go for Wave 2)

This checklist turns the Wave 1 goals and exit criteria into a concrete review artifact.
Use it during sprint review to decide whether the team can start Wave 2.

## Source Alignment
Wave 1 scope (Core Revenue Path):
1. Property lifecycle (create/verify/classify)
2. Valuation and tax assessment approval flow
3. Bill generation and payment settlement
4. Exemptions and appeals effect on payable balances

Exit criteria baseline:
- Core revenue modules are at L3 or above.
- Definition of Done (DoD) and quality gates are enforced in PR review.
- Critical workflows have deterministic integration coverage.
- Open legal-rule placeholders have decision owner + due date.

---

## A) Scope Completion (Wave 1 Functional Coverage)
Mark each line: `Done` / `In Progress` / `Blocked`.

### A1. Property lifecycle
- [ ] Create property flow works end-to-end (API + UI + persistence).
- [ ] Verify property flow works and audit evidence is captured.
- [ ] Classification flow works and resulting state is queryable.
- [ ] Status transitions are validated and negative cases are tested.

### A2. Valuation & assessment approval
- [ ] Valuation creation and updates are functional.
- [ ] Assessment calculation path is deterministic for approved scenarios.
- [ ] Approval workflow is implemented (happy path + rejection path).
- [ ] Re-approval/recalculation behavior is defined and tested.

### A3. Billing & payment settlement
- [ ] Bill generation is correct for assessed liabilities.
- [ ] Payment posting settles balances correctly.
- [ ] Bill status updates correctly after full/partial payments.
- [ ] Overpayment/duplicate payment handling is defined and tested.

### A4. Exemptions & appeals effect on payable balances
- [ ] Exemption decision updates payable balance correctly.
- [ ] Appeal outcomes update payable balance correctly.
- [ ] Combined scenarios (appeal + exemption + prior payments) are tested.
- [ ] Effective-date behavior is defined and verified.

---

## B) Maturity Gate (L3 Minimum for Core Modules)
Required modules: Auth, Property, Assessment, Billing, Payment.

For each module below, all L3 checks must be `Yes`.

| Module | Validation/Authz/Audit implemented | Edge cases tested | Idempotency/concurrency tested | Integration workflow test deterministic | L3 Met? |
|---|---|---|---|---|---|
| Auth | [ ] | [ ] | [ ] | [ ] | [ ] |
| Property | [ ] | [ ] | [ ] | [ ] | [ ] |
| Assessment | [ ] | [ ] | [ ] | [ ] | [ ] |
| Billing | [ ] | [ ] | [ ] | [ ] | [ ] |
| Payment | [ ] | [ ] | [ ] | [ ] | [ ] |

**Go rule:** No module can remain below L3.

---

## C) DoD Enforcement Checklist (Per Wave 1 Story)
For each Wave 1 story/PR, confirm all are present:
- [ ] API contract update (Swagger + DTO docs)
- [ ] Input validation + authorization checks
- [ ] Audit logging for sensitive actions
- [ ] Unit tests (happy path + edge cases)
- [ ] Integration test for endpoint workflow
- [ ] UI success/error states verified
- [ ] Migration/data-change safety reviewed
- [ ] Operational note (monitoring + rollback)

**Go rule:** 100% of Wave 1 PRs meet DoD.

---

## D) Quality Gates (Build + Test + Static Checks)
Record run date and commit SHA during review.

- [ ] Build succeeds for solution.
- [ ] Unit tests pass with no known flaky failures.
- [ ] Integration tests pass against local compose stack.
- [ ] Lint/static checks pass for frontend and backend.
- [ ] Critical-path smoke tests pass post-startup.

**Go rule:** No failing required gate.

---

## E) Legal/Business Rule Readiness
- [ ] All legal-rule placeholders in Wave 1 paths are identified.
- [ ] Each placeholder has a named decision owner.
- [ ] Each placeholder has a due date and tracking ticket.
- [ ] Temporary defaults are explicitly documented (if any remain).

**Go rule:** No unowned or undated legal-rule placeholders.

---

## F) Traceability Evidence Pack (Must Be Current)
For each Wave 1 workflow, provide:
- [ ] Feature row updated in `docs/FEATURE_TRACEABILITY_MATRIX.md`
- [ ] Endpoint list verified
- [ ] Service/validator mapping verified
- [ ] Unit test references updated
- [ ] Integration test references updated
- [ ] Maturity level updated with evidence
- [ ] Gaps assigned owner + target date

**Go rule:** No `TBD` in critical Wave 1 rows without owner/date.

---

## Final Go/No-Go Decision
- [ ] **GO to Wave 2**
- [ ] **NO-GO (remain in Wave 1)**

### Required sign-off
- Product owner: __________________ Date: __________
- Engineering lead: _______________ Date: __________
- QA lead: ________________________ Date: __________
- Compliance/legal reviewer: _______ Date: __________

### If NO-GO, list blockers
1. ______________________________________________
2. ______________________________________________
3. ______________________________________________
