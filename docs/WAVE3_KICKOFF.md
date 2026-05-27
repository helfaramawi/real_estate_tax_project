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

## Repetition Check (Team Health)
- Feeling repeated work during closeout is normal when quality gates require evidence updates.
- If the same update appears 3+ times, convert it into a template/checklist item and stop ad-hoc repetition.
- Track repetitive tasks in sprint retrospective and assign one owner to automate or standardize them.

## Fast-Track Rule (When Team Is Fatigued)
- Prefer one consolidated weekly planning/docs update instead of many micro-updates.
- Batch related checklist edits into a single PR with one reviewer owner.
- Defer non-critical wording refinements until after the sprint review.

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

## Speed-Up Execution Mode (Immediate)
- Freeze documentation changes to one owner update every Friday unless a blocker appears.
- For each sprint, allow only one scope change request after Day-3 checkpoint.
- Merge policy: no parallel edits to `WAVE3_KICKOFF.md`; all updates via one coordinator PR.
- Keep PR size target under 400 changed lines for feature work to reduce review and conflict time.

## Timeline Refresh (As of 2026-05-26)
- Current position: Wave 2 closeout verification and GO/NO-GO evidence packaging.
- If GO decision is confirmed by 2026-05-27, expected completion window is **4 to 6 weeks** from 2026-05-26.
- If GO slips by more than 3 business days, expected completion shifts to **5 to 7 weeks**.

## App Capability Snapshot (What It Does in Production)
- Maintains a full property-tax lifecycle: property registry, taxpayer ownership, valuation/assessment, billing, payment, appeals, exemptions, and audit trail.
- Enforces role-based secure operations (`401/403` boundaries) with maker-checker approvals for sensitive decisions.
- Supports spatial and operational intelligence via PostGIS-backed data, risk scoring, and integration workflows with background jobs.
- Provides release evidence discipline: CI build/test artifacts, traceability updates, and controlled rollout/rollback checkpoints.

## Next 7-Day Forward Plan (Starting 2026-05-27)
- Day 1: finalize Wave 2 GO/NO-GO evidence package and decision note.
- Day 2: select Sprint 1 Wave 3 feature and freeze acceptance criteria.
- Day 3-4: implement first backend slice with unit tests and draft integration cases.
- Day 5: complete integration tests + traceability updates and open review-ready PR.
- Day 6-7: resolve review comments, merge, and publish sprint evidence links.

## Wave 3 Start Criteria (Post-GO Practical Gate)
- GO decision is recorded (done) and sign-offs are captured.
- Wave 3 epic link, release-candidate SHA, and CI evidence URLs are attached before Sprint 1 code merge.
- First Sprint 1 feature must include one measurable business KPI target in its acceptance criteria.

## Immediate Owner Actions (Next 24 Hours)
- Product owner: publish Wave 3 epic link and Sprint 1 KPI target.
- Engineering lead: attach release-candidate SHA and CI evidence URLs to closeout checklist.
- QA lead: confirm integration evidence references and note any flaky tests.
- Compliance/legal: confirm no blocking legal-placeholder item remains open for Sprint 1 scope.

## Week-1 Exit Conditions (Wave 3 Sprint 1)
- Wave 3 epic link is published and referenced in checklist + sprint board.
- RC SHA and CI evidence links are attached to the closeout record.
- Sprint 1 feature branch has passing unit tests and review-ready integration tests.
- One KPI baseline is captured so post-release impact can be measured.

## Sprint 1 Delivery Guardrails (Execution)
- No new feature scope accepted after Day-3 unless tied to Sev-1 risk reduction.
- Every code PR in Sprint 1 must attach at least one test evidence link before merge.
- If integration tests fail twice consecutively, pause merges and run focused stabilization for 24h.
- End-of-week review must include KPI delta snapshot vs baseline.

## Week-2 Merge Readiness Checklist
- [ ] All Sprint 1 PR comments resolved with no open blocking thread.
- [ ] Unit + integration evidence links are attached in PR description.
- [ ] Rollback note includes exact feature flag/config toggle steps.
- [ ] Release note draft reviewed by Product + QA before merge.
