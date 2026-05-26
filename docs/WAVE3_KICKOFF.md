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

## Day-5 Checkpoint (Wave 3)
- [ ] PR is in final review state with blocking comments resolved.
- [ ] Traceability links are updated for delivered scope.
- [ ] Release notes draft is prepared with evidence links.
- [ ] Sprint review agenda is prepared and shared.

## Day-10 Checkpoint (Wave 3)
- [ ] Sprint 1 deliverable is merged to main.
- [ ] CI artifacts are attached to sprint review record.
- [ ] Post-merge smoke verification is complete.
- [ ] Sprint 2 candidate scope is drafted and prioritized.

## Sprint 2 Preparation Gate
- [ ] Sprint 1 retrospective actions are captured and assigned.
- [ ] Outstanding defects are triaged (must-fix vs defer) with owners.
- [ ] Sprint 2 scope is validated against capacity and risk.
- [ ] Sprint 2 kickoff note is published with dependencies and timeline.

## Sprint 2 Day-1 Checklist
- [ ] Confirm Sprint 2 feature owner and reviewer.
- [ ] Reconfirm scope boundaries based on Sprint 1 learnings.
- [ ] Publish updated acceptance criteria and delivery risks.
- [ ] Create/refresh test plan for changed endpoints and workflows.

## Sprint 2 Exit Criteria
- [ ] Sprint 2 feature slice is merged and release-tagged.
- [ ] Integration and regression evidence links are attached.
- [ ] Deferred items are documented with owner and target sprint.
- [ ] Project endgame checklist is fully reviewed before final sign-off.

## Project Endgame Checklist
- [ ] All planned Wave 3 feature slices are delivered or formally deferred.
- [ ] Final regression and smoke checks are green.
- [ ] Release notes are complete with evidence links and rollback instructions.
- [ ] Final stakeholder sign-off is recorded.

## Release Readiness Gate (Pre-Production)
- [ ] Deployment checklist is completed and approved.
- [ ] Monitoring/alert thresholds are reviewed for new feature paths.
- [ ] On-call handoff note is published with known risks.

## Post-Release Validation Window (48h)
- [ ] No Sev-1/Sev-2 incidents linked to Wave 3 scope.
- [ ] Key endpoint error-rate and latency metrics remain within baseline thresholds.
- [ ] Any rollback-trigger conditions are reviewed and closed.

## 30-Day Success Criteria
- [ ] User-facing defect rate is below agreed threshold.
- [ ] Operational metrics remain stable versus pre-release baseline.
- [ ] Top three improvement actions for next wave are documented and approved.

## 60-Day Sustainability Check
- [ ] No recurring high-severity incidents tied to Wave 3 release scope.
- [ ] Planned follow-up improvements are tracked with owners and dates.
- [ ] Stakeholder review confirms outcomes match expected business value.

## Production Experience Vision
- API access behaves predictably by role (unauthenticated users get `401`; unauthorized roles get `403` on protected operations).
- Core flows (taxpayer, property, billing, payment) are observable with clear operational evidence and release notes.
- CI evidence (unit/integration + smoke checks) is attached to each release candidate before promotion.

## Candidate Features After Current Wave 3 Baseline
1. Taxpayer self-service dashboard for bill/payment status history.
2. Property ownership timeline and change audit viewer.
3. Appeals workflow SLA tracking and escalation notifications.
4. Risk insights panel with rule explanations and reviewer actions.
5. Operations release dashboard (traceability, artifacts, and rollback readiness in one view).

## Project Completion Path (Estimate from 2026-05-25)
1. **Wave 2 final decision closeout** (1–2 days)
   - Final evidence package attached
   - GO/NO-GO decision note recorded
2. **Wave 3 Sprint 1 delivery** (2 weeks)
   - First feature slice implemented and released with evidence
3. **Wave 3 Sprint 2 delivery** (2 weeks)
   - Second prioritized feature slice and hardening pass
4. **Release stabilization + sign-off** (3–5 days)
   - Regression run, rollback check, documentation finalization

**Total remaining estimate:** ~5 to 7 weeks to project end (assuming no major scope expansion).
