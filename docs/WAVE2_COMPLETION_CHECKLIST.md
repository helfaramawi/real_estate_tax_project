# Wave 2 Completion Checklist (Go/No-Go for Wave 3)

This checklist turns the Wave 2 goals and professionalization exit criteria into a concrete review artifact.
Use it during sprint review to decide whether the team can start Wave 3.

## Source Alignment
Wave 2 scope (Trust & Compliance):
1. Audit trail completeness checks
2. Role/permission enforcement matrix validation
3. Data consistency rules (ownership %, statuses, soft-delete behavior)
4. Legal-rule placeholders replacement with approved values

Exit criteria baseline:
- Core revenue modules are at L3 or above.
- Definition of Done (DoD) and quality gates are enforced in PR review.
- Critical workflows have deterministic integration coverage.
- Open legal-rule placeholders have decision owner + due date.

---

## A) Scope Completion (Wave 2 Trust & Compliance Coverage)
Mark each line: `Done` / `In Progress` / `Blocked`.

### A1. Audit trail completeness
- [ ] Sensitive create/update/delete operations emit audit events.
- [ ] Audit records include actor, timestamp, action, entity, and key identifiers.
- [ ] Failure-path actions (authorization denied, validation failed) are logged where policy requires.
- [ ] Audit retrieval/reporting path is validated for review workflows.

### A2. Role/permission enforcement matrix
- [ ] All protected endpoints are mapped to required roles/permissions.
- [ ] Positive authorization paths are integration-tested.
- [ ] Negative authorization paths return expected status and error contract.
- [ ] Privilege-escalation and cross-tenant/cross-user access checks are covered.

### A3. Data consistency rules
- [ ] Property ownership percentages enforce 100% aggregate constraints.
- [ ] Status-transition rules are validated for illegal transitions.
- [ ] Soft-delete behavior is consistent across read/query/report endpoints.
- [ ] Concurrency/idempotency behavior is defined for update-heavy workflows.

### A4. Legal-rule placeholders replacement
- [ ] All Wave 2 legal/business placeholders are inventoried.
- [ ] Placeholder logic is replaced with approved policy values/rules.
- [ ] Any unavoidable temporary defaults have owner + due date.
- [ ] Change history links to legal/compliance approval artifacts.

---

## B) Trust & Compliance Readiness Gate
For each row below, all checks must be `Yes`.

| Area | Evidence documented | Unit tests cover edge cases | Integration tests deterministic | Reviewer sign-off captured | Gate Met? |
|---|---|---|---|---|---|
| Audit trail completeness | [ ] | [ ] | [ ] | [ ] | [ ] |
| Authorization matrix | [ ] | [ ] | [ ] | [ ] | [ ] |
| Data consistency rules | [ ] | [ ] | [ ] | [ ] | [ ] |
| Legal-rule replacement | [ ] | [ ] | [ ] | [ ] | [ ] |

**Go rule:** No area can remain below gate threshold.

---

## C) DoD Enforcement Checklist (Per Wave 2 Story)
For each Wave 2 story/PR, confirm all are present:
- [ ] API contract update (Swagger + DTO docs)
- [ ] Input validation + authorization checks
- [ ] Audit logging for sensitive actions
- [ ] Unit tests (happy path + edge cases)
- [ ] Integration test for endpoint workflow
- [ ] UI success/error states verified
- [ ] Migration/data-change safety reviewed
- [ ] Operational note (monitoring + rollback)

**Go rule:** 100% of Wave 2 PRs meet DoD.

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

## E) Compliance Evidence Pack (Must Be Current)
For each Wave 2 workflow, provide:
- [ ] Feature row updated in `docs/FEATURE_TRACEABILITY_MATRIX.md`
- [ ] Endpoint-to-permission mapping verified
- [ ] Service/validator + audit mapping verified
- [ ] Unit test references updated
- [ ] Integration test references updated
- [ ] Maturity level updated with evidence
- [ ] Compliance reviewer notes linked
- [ ] Gaps assigned owner + target date

**Go rule:** No unresolved `TBD` in critical Wave 2 rows without owner/date.

---

## Final Go/No-Go Decision
- [ ] **GO to Wave 3**
- [ ] **NO-GO (remain in Wave 2)**

### Required sign-off
- Product owner: __________________ Date: __________
- Engineering lead: _______________ Date: __________
- QA lead: ________________________ Date: __________
- Compliance/legal reviewer: _______ Date: __________

### If NO-GO, list blockers
1. ______________________________________________
2. ______________________________________________
3. ______________________________________________
