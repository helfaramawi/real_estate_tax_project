# Merge Conflict Playbook (Wave 2 / Wave 3)

If we keep getting repeated conflicts, the root cause is usually **parallel edits to the same files** (`docs/WAVE2_COMPLETION_CHECKLIST.md`, large integration test files, and `ci.yml`).

Use this playbook to reduce conflicts immediately.

## 1) Single-Owner “Hot Files”
- Assign one temporary owner per hot file for the sprint:
  - `docs/WAVE2_COMPLETION_CHECKLIST.md`
  - `.github/workflows/ci.yml`
  - `tests/RealEstateTax.IntegrationTests/Properties/PropertiesEndpointsTests.cs`
  - `tests/RealEstateTax.IntegrationTests/Bills/BillsEndpointsTests.cs`
- Other contributors must not edit these directly in parallel PRs.

## 2) PR Scope Rule
- One PR should touch **one domain slice** only (example: `Properties` tests only, or docs only).
- Do not bundle docs + CI + multiple test domains in the same PR.

## 3) Rebase Policy
- Rebase your branch on latest `main` **right before** opening PR and again before merge.
- If conflicts appear, resolve immediately and push the updated branch once (avoid long-lived stale branches).

## 4) Checklist Update Policy
- Update `docs/WAVE2_COMPLETION_CHECKLIST.md` only in:
  1) the daily closeout PR, or
  2) the final GO/NO-GO PR.
- All other PRs reference checklist items but do not edit that file.

## 5) Test File Symmetry Batching
- When adding role-symmetry tests, batch them per endpoint in a single PR (not one test per PR).
- This minimizes repeated edits at nearby lines and reduces merge collisions.

## 6) Merge Order
- Merge order should be:
  1) CI/workflow fixes
  2) shared test-factory changes
  3) domain test batches
  4) docs/checklist closeout

Following this order usually removes most recurring conflicts.

## 7) 10-Minute Conflict Triage
- If a PR hits conflicts, do this immediately:
  1) Rebase branch on latest `main`.
  2) Resolve conflicts in hot files first (`ci.yml`, checklist, high-churn test files).
  3) Run focused smoke validation for touched domain.
  4) Push once and request re-review (avoid repeated partial pushes).

## 8) Fast Conflict-Prevention Defaults (Use Immediately)
- Default branch lifetime target: under 24 hours for docs-only and test-only PRs.
- Run `git fetch origin && git rebase origin/main` before requesting review, not after review starts.
- If a file is modified by 2+ open PRs, pause the newer PR and re-scope to non-overlapping files.
- Prefer additive test files over repeatedly editing the same large endpoint test file.
