# Runnable Project Handoff (Wave 3)

This handoff is the shortest path to run the project end-to-end in a clean environment.

## 1) Required Inputs (Must be set)
- `JWT_SECRET` (32+ chars)
- `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`
- `ML_INTERNAL_SECRET` (must match API and ML service)

## 2) Start Commands (Docker)
```bash
cp .env.example .env
# edit .env with real secrets
docker compose -f docker-compose.yml -f docker-compose.intelligence.yml up -d --build
```

## 3) Health Verification
```bash
curl http://localhost/health
curl http://localhost/swagger/index.html
curl http://localhost/hangfire
```

## 4) Test Verification (CI-equivalent)
```bash
dotnet restore
dotnet build --configuration Release
dotnet test tests/RealEstateTax.UnitTests/RealEstateTax.UnitTests.csproj --configuration Release --logger "trx;LogFileName=unit-test-results.trx" --results-directory ./test-results
dotnet test tests/RealEstateTax.IntegrationTests/RealEstateTax.IntegrationTests.csproj --configuration Release --logger "trx;LogFileName=integration-test-results.trx" --results-directory ./test-results
```

## 5) Go-Live Readiness Minimum
- CI run URL attached
- Unit + Integration TRX artifact links attached
- RC SHA attached
- Rollback owner + on-call contact recorded
- Wave 3 epic link recorded

## 6) If Environment Fails to Start
1. Verify Docker daemon is running.
2. Verify `.env` secrets are present (non-empty).
3. Check container logs:
   ```bash
   docker compose logs --tail=200 api
   docker compose logs --tail=200 postgres
   docker compose logs --tail=200 ml_service
   ```
4. Rebuild API and restart:
   ```bash
   docker compose up -d --build api
   ```

## 7) Final Completion Gate (Before Project Close)
- [ ] Wave 3 epic link is present in release notes.
- [ ] RC SHA + CI run URL + TRX links are archived in one final evidence note.
- [ ] Post-release 48h validation checks are marked complete.
- [ ] 30-day success criteria owner is assigned with review date.

## 8) Project Finish Signal (Definition)
The project is considered finished when all of the following are true at the same time:
- Wave 3 scope is either delivered or explicitly deferred with owner/date.
- Final evidence note contains RC SHA, CI URL, unit/integration TRX links, and rollback ownership.
- Post-release 48h and 30-day checks are completed and signed off.

## 9) Final Handover Package (Required Attachments)
- [ ] Final release note URL
- [ ] Wave 3 epic URL
- [ ] RC SHA + CI run URL bundle
- [ ] Unit + integration TRX artifact URLs
- [ ] Rollback owner, on-call contact, and escalation channel

## 10) Current Status (As of 2026-05-28)
- Wave 2 decision is recorded as GO with captured sign-offs in checklist.
- Remaining finish work is evidence completion: RC SHA, CI URL, TRX URLs, epic URL, and final release-note linkage.
- Wave 3 planning guardrails are documented; execution should proceed using the week-1/week-2 checkpoints.
