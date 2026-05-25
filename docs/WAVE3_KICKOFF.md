# Wave 3 Kickoff Plan (Started: 2026-05-25)

This document marks the immediate start of Wave 3 after Wave 2 closeout review.

## Wave 3 Objectives
1. Deliver first Wave 3 feature slice behind existing authorization boundaries.
2. Preserve Wave 2 quality gates (unit + integration + artifact evidence).
3. Track rollout risk with explicit owners and target dates.

## Entry Conditions (Confirmed)
- Wave 2 closeout checklist reviewed.
- GO/NO-GO decision recorded by approvers.
- Wave 3 kickoff epic created and linked in project tracking.

## First Execution Batch
1. Prioritize top Wave 3 feature candidates and select one for Sprint 1.
2. Define API contract + acceptance criteria for selected feature.
3. Implement with unit + integration tests and update traceability docs.
4. Run CI, collect artifacts, and publish sprint review notes.

## Ownership
- Product: define priority and acceptance criteria.
- Engineering: implementation + test coverage + rollout safety.
- QA: integration verification and regression validation.
- Compliance: ensure controls remain intact during Wave 3 delivery.

## Sprint 1 Starter Backlog (Wave 3)
1. Select one Wave 3 feature candidate and freeze scope for Sprint 1.
2. Add API contract draft and acceptance criteria to the feature ticket.
3. Implement backend slice with unit + integration tests.
4. Update `FEATURE_TRACEABILITY_MATRIX.md` with Wave 3 status/evidence links.
5. Demo in sprint review and capture rollback/monitoring notes.

## Done Criteria for Sprint 1
- Feature ticket has approved acceptance criteria.
- CI build, unit tests, and integration tests are green for merge commit.
- Evidence links (tests + traceability update) are attached to the sprint review note.

## Sprint 1 Exit Artifacts
- Pull request link for selected Wave 3 feature slice.
- CI run URL with unit/integration evidence.
- Updated `FEATURE_TRACEABILITY_MATRIX.md` entry link.
- Short retrospective note: what blocked delivery and what to improve in Sprint 2.

## Sprint 1 Risks and Mitigations
- Risk: Scope creep from carrying unresolved Wave 2 items into Wave 3 delivery.
  - Mitigation: Keep a strict sprint scope freeze after acceptance criteria approval.
- Risk: Regression in authorization guard behavior while adding new feature slice.
  - Mitigation: Keep role-based integration tests updated for touched endpoints in same PR.
- Risk: Merge conflicts on high-churn docs/test files.
  - Mitigation: Apply `docs/MERGE_CONFLICT_PLAYBOOK.md` ownership and batching rules.

## Two-Week Delivery Cadence (Wave 3)
- Week 1:
  - Finalize Sprint 1 scope and contract.
  - Implement first feature slice + unit tests.
  - Open draft PR early for review.
- Week 2:
  - Complete integration tests and traceability updates.
  - Close review comments and merge.
  - Publish sprint review evidence package and retrospective note.

## Sprint 1 Communication Cadence
- Daily async status update in team channel (scope, blockers, next 24h plan).
- Mid-sprint checkpoint with Product + QA to confirm acceptance criteria alignment.
- End-of-sprint review includes demo, evidence links, and rollback notes.

## Sprint 1 Review Template (Required)
- Delivered scope vs planned scope:
- CI evidence links (build, unit, integration):
- Authorization/regression notes:
- Rollback and monitoring readiness:
- Decisions for Sprint 2:

## Day-1 Execution Checklist (Wave 3)
- [ ] Confirm Sprint 1 feature owner and reviewer.
- [ ] Create feature branch and link kickoff epic.
- [ ] Publish acceptance criteria and API contract draft.
- [ ] Add initial unit test skeleton and integration test placeholders.

## Day-3 Checkpoint (Wave 3)
- [ ] Scope is still within Sprint 1 freeze (no unapproved expansion).
- [ ] Core implementation is merged or in review-ready state.
- [ ] Unit tests are passing for the implemented slice.
- [ ] Integration test placeholders are converted to executable cases.
