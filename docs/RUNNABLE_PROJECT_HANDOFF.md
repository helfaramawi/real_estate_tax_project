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
