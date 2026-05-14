# ReTax Platform — System Manual
## Real Estate Tax Intelligence Platform — Arab Republic of Egypt
### Version 2.0 | May 2026

---

## Table of Contents

1. [System Architecture](#1-system-architecture)
2. [Prerequisites](#2-prerequisites)
3. [Installation & Deployment](#3-installation--deployment)
4. [Configuration Reference](#4-configuration-reference)
5. [Accessing the System](#5-accessing-the-system)
6. [User Guide — Core Modules](#6-user-guide--core-modules)
7. [AI / Intelligence Module](#7-ai--intelligence-module)
8. [Training AI Models](#8-training-ai-models)
9. [Background Jobs (Hangfire)](#9-background-jobs-hangfire)
10. [Roles & Permissions](#10-roles--permissions)
11. [API Reference](#11-api-reference)
12. [Database Reference](#12-database-reference)
13. [Troubleshooting](#13-troubleshooting)
14. [Security Checklist](#14-security-checklist)

---

## 1. System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Browser / Mobile Client                             │
└─────────────────────────────────────┬───────────────────────────────────────┘
                                      │ HTTP :80
┌─────────────────────────────────────▼───────────────────────────────────────┐
│                     Frontend (React + Nginx) :80                             │
│                     Reverse-proxies /api/* → API                             │
└─────────────────────────────────────┬───────────────────────────────────────┘
                                      │ :8080 (internal)
┌─────────────────────────────────────▼───────────────────────────────────────┐
│                    .NET 8 Backend API (retax_api)                            │
│  ┌──────────┐ ┌───────────┐ ┌──────────────┐ ┌─────────────────────────┐   │
│  │  Core    │ │Intelligence│ │  Hangfire    │ │  Serilog + Correlation  │   │
│  │  API v1  │ │  API v2   │ │  Scheduler   │ │  ID Middleware          │   │
│  └──────────┘ └───────────┘ └──────────────┘ └─────────────────────────┘   │
└────────┬──────────────────────┬──────────────────────────────────────────────┘
         │                      │ :8001 (internal, X-Internal-Secret)
         │              ┌───────▼──────────────────────────────────────────┐
         │              │  Python ML Service (retax_ml)  FastAPI           │
         │              │  POST /predict/batch — inference                 │
         │              │  POST /train/start   — async training            │
         │              │  GET  /health                                    │
         │              └───────┬──────────────────────────────────────────┘
         │                      │
┌────────▼──────────────────────▼───────────────────────────────────────────┐
│                   PostgreSQL 16 + PostGIS 3.4 (retax_postgres)             │
│   schema: public (properties, taxpayers, bills, payments, …)              │
│   schema: intel  (feature_vectors, prediction_results, model_registry, …) │
└───────────────────────────────────────────────────────────────────────────┘
         │
┌────────▼────────────────────────────┐   ┌──────────────────────────────────┐
│  Redis 7 (retax_redis) :6379        │   │  MLflow (retax_mlflow) :5001     │
│  Job queue + caching                │   │  Experiment tracking + artifacts  │
└─────────────────────────────────────┘   └──────────────────────────────────┘
```

### Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend framework | .NET 8 Web API |
| Architecture | Clean Architecture (Domain / Application / Infrastructure / API) |
| Database | PostgreSQL 16 + PostGIS 3.4 |
| ORM | Entity Framework Core 8 |
| Authentication | JWT Bearer + Role-Based Authorization |
| Background jobs | Hangfire (PostgreSQL storage) |
| Logging | Serilog (structured, Console output) |
| Validation | FluentValidation |
| Object mapping | Mapster |
| API documentation | Swagger / OpenAPI 3 |
| Rate limiting | AspNetCoreRateLimit |
| Containerisation | Docker + Docker Compose |
| Spatial queries | NetTopologySuite + PostGIS |
| ML inference | Python FastAPI + XGBoost + scikit-learn |
| ML tracking | MLflow 2.14 |
| ML explainability | SHAP |
| Job queue | Redis 7 |

---

## 2. Prerequisites

### Server Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 4 cores | 8 cores |
| RAM | 8 GB | 16 GB |
| Disk | 50 GB SSD | 200 GB SSD |
| OS | Ubuntu 20.04 / Windows Server 2019 | Ubuntu 22.04 LTS |

### Required Software

| Software | Version | Purpose |
|----------|---------|---------|
| Docker Desktop | 24+ | Container runtime |
| Docker Compose | v2.20+ | Service orchestration |
| Git | 2.40+ | Source code management |
| Node.js | 20+ | Frontend development only |
| .NET 8 SDK | 8.0+ | Local development only |

### Network Ports (published to host)

| Port | Service | Notes |
|------|---------|-------|
| 80 | Frontend (Nginx reverse proxy) | Main entry point |
| 5001 | MLflow tracking UI | Admin access only |
| 5050 | pgAdmin (dev profile only) | `--profile dev` |

> **Note:** The .NET API runs on port 8080 *inside* the Docker network and is not published to the host directly. All browser traffic goes through the frontend Nginx on port 80, which proxies `/api/*`, `/swagger/*`, and `/hangfire/*` to the API container. The ML service runs on port 8001 internally and is never exposed to the host.

---

## 3. Installation & Deployment

### Step 1 — Clone the Repository

```bash
git clone https://github.com/helfaramawi/real_estate_tax_project.git
cd real_estate_tax_project
git checkout claude/real-estate-tax-backend-ua7ex
```

### Step 2 — Create Environment File

```bash
cp .env.example .env
```

Edit `.env` and set strong values for all secrets:

```dotenv
# PostgreSQL
POSTGRES_USER=retax_user
POSTGRES_PASSWORD=CHANGE_ME_strong_password
POSTGRES_DB=retax_db

# JWT — must be at least 32 characters, random
JWT_SECRET=CHANGE_ME_at_least_32_characters_random

# ML service shared secret — must match in API and ml_service
ML_INTERNAL_SECRET=CHANGE_ME_ml_secret

# Environment
ASPNETCORE_ENVIRONMENT=Production
JWT_EXPIRY_MINUTES=60
```

> **Security:** Never commit `.env` to source control.

### Step 3 — Start All Services

```bash
# Core services + Intelligence stack
docker compose -f docker-compose.yml -f docker-compose.intelligence.yml up -d
```

Docker will:
1. Pull/build all images (first run: ~5–10 minutes)
2. Start PostgreSQL and apply all SQL migrations from `docker-entrypoint-initdb.d/`
3. Start the API — it automatically applies any missing schema fixes at startup
4. Start the ML service, Redis, and MLflow

Monitor startup progress:
```bash
docker compose ps        # check all containers are healthy
docker compose logs -f api   # watch API startup
```

Expected API log output:
```
Schema migrations (V2 intelligence + V3 fix) applied.
Intelligence seed completed (rule-based duplicate_detector registered).
Application data seeded successfully.
Now listening on: http://[::]:8080
```

### Step 4 — Verify Installation

```bash
# API health
curl http://localhost/health

# ML service health (via API proxy or direct internal test)
docker exec retax_api curl -s http://ml_service:8001/health
```

Open in browser:

| URL | Service |
|-----|---------|
| `http://localhost` | Main application |
| `http://localhost/swagger` | API documentation (non-production only) |
| `http://localhost/hangfire` | Background jobs dashboard |
| `http://localhost:5001` | MLflow experiment tracker |

### Step 5 — Frontend Development (Optional)

Only needed when making UI changes:

```bash
cd frontend
npm install
npm run dev
# Opens at http://localhost:3001
```

### Updating an Existing Deployment

```bash
git pull origin claude/real-estate-tax-backend-ua7ex
docker compose -f docker-compose.yml -f docker-compose.intelligence.yml up -d --build api ml_service
```

Schema changes are applied automatically at API startup — no manual SQL steps required.

---

## 4. Configuration Reference

### appsettings.json — Key Sections

**File:** `src/RealEstateTax.API/appsettings.json`

#### Database

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=postgres;Port=5432;Database=retax_db;Username=retax_user;Password=YOUR_PASSWORD"
}
```

#### JWT Authentication

```json
"Jwt": {
  "Secret": "CHANGE_ME_min_32_chars",
  "Issuer": "RealEstateTaxPlatform",
  "Audience": "RealEstateTaxPlatform",
  "ExpiryMinutes": "60"
}
```

#### ML Service

```json
"MLService": {
  "BaseUrl": "http://ml_service:8001",
  "InternalSecret": "CHANGE_ME_ml_secret"
}
```

> `BaseUrl` uses the Docker service name `ml_service` — this is correct for container-to-container communication.

#### Feature Flags

```json
"FeatureManagement": {
  "GpsCapture": true,
  "OfflineSync": true,
  "GeoFencing": true,
  "RouteOptimization": false,
  "MLRiskScoring": false,
  "FraudDetection": false,
  "DuplicateDetection": false,
  "ValuationPrediction": false,
  "UnregisteredBuildingDetection": false,
  "GeoClusteringDashboard": false
}
```

| Flag | Description | Enable when |
|------|-------------|-------------|
| `GpsCapture` | GPS track recording for field inspectors | Immediately |
| `OfflineSync` | Offline data collection and sync | Immediately |
| `GeoFencing` | Monitoring zone membership | Immediately |
| `RouteOptimization` | Optimised inspection routes (requires OSRM) | After OSRM setup |
| `MLRiskScoring` | ML-based tax risk scoring | After training `risk_scorer` |
| `FraudDetection` | ML-based fraud detection | After training `fraud_detector` |
| `DuplicateDetection` | Duplicate registration detection | Works immediately (rule-based fallback active) |
| `ValuationPrediction` | Predicted market value | After training valuation model |
| `UnregisteredBuildingDetection` | Satellite/field-based detection | After model training |
| `GeoClusteringDashboard` | Geographic cluster visualisation | After feature computation |

To enable a flag, change its value to `true` and restart the API:

```bash
docker compose restart api
```

#### CORS

```json
"Cors": {
  "AllowedOrigins": ["http://localhost:3000", "https://retax.gov.eg"]
}
```

---

## 5. Accessing the System

### URLs

| Interface | URL | Available to |
|-----------|-----|-------------|
| Main application | `http://localhost` | All users |
| API Swagger docs | `http://localhost/swagger` | Development only (disabled in Production) |
| Hangfire dashboard | `http://localhost/hangfire` | Admins |
| MLflow UI | `http://localhost:5001` | Admins / Data scientists |
| pgAdmin | `http://localhost:5050` | Dev profile only (`--profile dev`) |

### Default Login Credentials

| Username | Password | Role |
|----------|----------|------|
| `superadmin` | `Admin@12345` | SuperAdmin |

> **IMPORTANT:** Change the default password immediately after first login in any environment other than local development.

### First-Login Checklist

1. Log in as `superadmin` / `Admin@12345`
2. Navigate to **Settings → Users** and change the password
3. Create role-specific user accounts (see [Section 10](#10-roles--permissions))
4. Populate tax rules in the `tax_rules` table (see [Section 12](#12-database-reference))
5. Enable feature flags as required (see [Section 4](#4-configuration-reference))

---

## 6. User Guide — Core Modules

### 6.1 Property Registry

**Path:** Sidebar → Properties

Properties go through a status lifecycle:

```
Draft → NeedsReview → Verified → Taxable
                                → Exempt
                                → Archived
```

**Key operations:**
- **Create property:** Enter type, address, area, year built, GPS coordinates
- **Link owner:** Assign taxpayer with ownership percentage and deed number
- **Verify property:** A second officer must verify (Maker-Checker — cannot be the same as the creator)
- **Spatial search:** `GET /api/properties/nearby?lat=&lng=&radius=` returns properties within the specified radius (metres)
- **Upload documents:** Supported types: PDF, JPG, PNG; max 20 MB per file

### 6.2 Taxpayer Profiles

**Path:** Sidebar → Taxpayers

- Egyptian National ID: 14 digits
- Corporate taxpayers: set `isCorporate: true`
- Multi-ownership: one property can have multiple owners with percentage tracking

### 6.3 Property Enumeration (Multi-Source Import)

**Path:** Sidebar → Enumeration

The system imports records from up to 4 government sources (electricity, water, cadastral, municipal) and automatically matches them to existing properties using:
- GPS proximity (PostGIS `ST_DWithin`)
- Address similarity (`pg_trgm` trigram matching)
- National ID matching

Each match gets a confidence score. Records with low confidence are flagged for manual review.

### 6.4 Field Surveys

**Path:** Sidebar → Field Surveys

Status flow: `Assigned → InProgress → Submitted → Approved`

Inspectors use the mobile app to:
- Record GPS coordinates and tracks
- Upload georeferenced photos
- Fill survey forms offline (synced when connection restored)

### 6.5 Valuation & Assessment

**Path:** Sidebar → Valuations / Assessments

**Supported valuation methods:**

| Code | Method |
|------|--------|
| 1 | Rental Value (standard Egyptian method) |
| 2 | Market Comparison |
| 3 | Cost |

Both valuations and assessments require **Maker-Checker approval** — the approving officer must be different from the preparing officer.

**Valuation lifecycle:** Created → Submitted → Approved → Assessment generated

### 6.6 Tax Billing

**Path:** Sidebar → Bills

Bill lifecycle:
```
Draft → Issued → Paid
              → Overdue → Cancelled
```

- Bills are generated from approved assessments
- Installment plans supported
- Taxpayers are notified automatically on bill issue

### 6.7 Payments

**Path:** Sidebar → Payments

**Supported payment methods:**

| Code | Method |
|------|--------|
| 0 | Cash |
| 1 | Bank transfer |
| 2 | Online |
| 3 | Mobile wallet |

Recording a payment automatically updates the corresponding bill status.

### 6.8 Appeals

**Path:** Sidebar → Appeals

```
Submitted → UnderReview → ReferredToCommittee → Resolved
```

60-day deadline from assessment date (verify against Egyptian Tax Law 196/2008).

### 6.9 Exemptions

**Path:** Sidebar → Exemptions

- Multiple exemption types mapped to Law 196/2008 articles
- Partial or full exemption
- Eligibility rules enforced by `IExemptionService`

### 6.10 Audit Trail

**Path:** Sidebar → Audit Logs

- Immutable append-only log of all sensitive actions
- Every request carries a Correlation ID (`X-Correlation-ID` header)
- No hard deletes — all data uses soft delete pattern

---

## 7. AI / Intelligence Module

The Intelligence module runs a nightly ML pipeline to score every property on three dimensions, plus provides geospatial analytics.

### 7.1 Architecture

```
PostgreSQL (public schema)
  ↓  nightly at 02:00 UTC (intel-feature-computation job)
intel.feature_vectors  — 35 computed features per property
  ↓  nightly 04:00–05:00 UTC (intel-ml-inference-* jobs)
Python ML Service  — XGBoost / IsolationForest inference
  ↓  results written back
intel.prediction_results  — one row per property per model
public.properties         — ml_risk_score, ml_fraud_probability, ml_duplicate_score updated
```

### 7.2 ML Models

#### Risk Scorer (`risk_scorer`)
- **Algorithm:** XGBoost gradient-boosted trees
- **Task:** Binary classification — tax non-compliance risk
- **Output:** Score 0.0–1.0 + top-5 SHAP feature importances
- **Labels:** High Risk (≥ 0.7) / Medium Risk (≥ 0.4) / Low Risk

#### Fraud Detector (`fraud_detector`)
- **Algorithm:** Isolation Forest (unsupervised anomaly detection)
- **Task:** Identify properties with abnormal patterns suggesting fraud
- **Output:** Score 0.0–1.0 + SHAP explanation
- **Assumption:** ~5% contamination rate

#### Duplicate Detector (`duplicate_detector`)
- **Algorithm:** Isolation Forest on spatial + area features
- **Task:** Identify potentially duplicate property registrations
- **Output:** Score 0.0–1.0 + label
- **Labels:** Duplicate (≥ 0.7) / Investigate (≥ 0.4) / Unique
- **Cold-start fallback:** Rule-based scoring using `nearest_neighbor_distance_m` — works immediately without a trained model:

| Distance to nearest property | Score | Label |
|------------------------------|-------|-------|
| < 5 m | 0.95 | Duplicate |
| < 15 m | 0.80 | Duplicate |
| < 30 m | 0.55 | Investigate |
| < 60 m | 0.25 | Unique |
| < 100 m | 0.10 | Unique |
| ≥ 100 m | 0.02 | Unique |

### 7.3 Feature Store — All 35 Features

| Category | Feature | Description |
|----------|---------|-------------|
| Spatial | `lat`, `lon` | GPS coordinates |
| Spatial | `has_boundary_polygon` | Whether a GIS boundary is drawn |
| Spatial | `nearest_neighbor_distance_m` | Distance to closest other property |
| Spatial | `neighbors_within_100m` | Count of properties within 100 m |
| Spatial | `neighbors_within_500m` | Count of properties within 500 m |
| Property | `built_up_area` | Built-up area (m²) |
| Property | `land_area` | Land area (m²) |
| Property | `property_type_code` | Property type enum |
| Property | `year_built` | Construction year |
| Financial | `declared_annual_value` | Owner-declared annual rental value (EGP) |
| Financial | `market_value_per_sqm` | Assessed market value per m² |
| Financial | `capitalization_rate` | Capitalisation rate used in valuation |
| Financial | `value_vs_cluster_median_pct` | Value relative to cluster median (%) |
| Financial | `value_vs_district_median_pct` | Value relative to district median (%) |
| Ownership | `ownership_chain_length` | Number of historical ownership transfers |
| Ownership | `days_since_last_transfer` | Days since last ownership change |
| Ownership | `corporate_owner_flag` | True if current owner is a company |
| Ownership | `multiple_owners_flag` | True if multiple current owners |
| Surveys | `surveys_count` | Total field surveys conducted |
| Surveys | `days_since_last_survey` | Days since most recent survey |
| Surveys | `gps_accuracy_avg` | Average GPS accuracy of surveys (m) |
| Payment | `bills_count` | Total bills issued |
| Payment | `paid_on_time_rate` | Fraction of bills paid by due date |
| Payment | `overdue_count` | Number of overdue bills |
| Payment | `total_paid_egp` | Total amount paid (EGP) |
| Payment | `total_outstanding_egp` | Total outstanding amount (EGP) |
| Risk history | `existing_risk_score` | Previous rule-based risk score |
| Risk history | `geo_verification_score` | GIS-based location quality score |
| Risk history | `fraud_flags_count` | Number of open fraud flags |
| Disputes | `appeals_count` | Number of appeals filed |
| Compliance | `exemptions_count` | Number of exemptions applied |
| Enumeration | `source_records_count` | Records from external sources |
| Enumeration | `matched_records_count` | Successfully matched records |
| Enumeration | `max_match_confidence` | Highest confidence match score |

### 7.4 Geo Analytics

**Risk Heatmap** (`GET /api/v2/geo/risk-heatmap`)

Colour-coded geographic distribution of ML risk scores:

| Colour | Risk level | Score range |
|--------|-----------|-------------|
| Red | Very High | ≥ 75% |
| Orange | High | 50–74% |
| Yellow | Medium | 25–49% |
| Green | Low | < 25% |

**Spatial Anomalies** (`GET /api/v2/geo/anomalies`)

| Anomaly type | Description |
|-------------|-------------|
| Unregistered building | Structure present on satellite/field but not in registry |
| Value outlier | Declared value deviates > 2σ from district median |
| Suspicious ownership | Rapid ownership changes or corporate shell patterns |
| Boundary overlap | Property boundary intersects another registered property |
| Duplicate coordinates | Multiple properties share exact GPS coordinates |
| Ghost property | Internally inconsistent or impossible data |
| Survey conflict | Survey findings contradict registry data |

Severity levels: **Critical** (24 h response) / **High** (1 week) / **Medium** (1 month) / **Low** (periodic review)

**Geographic Clusters** (`GET /api/v2/geo/clusters`)

DBSCAN spatial clustering groups nearby properties. Bubble size = property count; tooltip shows district, count, average value/m².

**Geo-fence Zones** (`GET/POST /api/v2/geo/fence-zones`)

Define monitoring polygons. The nightly `intel-geofence-update` job updates every property's zone membership.

### 7.5 Prediction Review

**Path:** Sidebar → Prediction Review  
**API:** `GET /api/v2/intelligence/predictions/pending-review`

For each pending prediction:
- **Confirm** — AI decision is correct; proceed with action
- **Reject** — False positive; dismiss
- **Escalate** — Refer to senior officer

> Reviewer decisions feed future training data, improving model accuracy over time.

**Reading SHAP explanations:**

```
DaysSinceLastSurvey  ████████  +0.23  ← not inspected recently (increases risk)
OverdueCount         █████     +0.15  ← overdue bills (increases risk)
PaidOnTimeRate       ████      -0.12  ← good payment history (decreases risk)
```

Red bars increase the score; green bars decrease it. Bar length = feature importance.

---

## 8. Training AI Models

### Overview

Model training is only needed once you have enough data in the feature store. The system works immediately using rule-based fallbacks — training improves accuracy further.

### Step 1 — Compute Feature Vectors

Feature computation reads all properties from `public.properties` and writes 35-feature rows to `intel.feature_vectors`.

**Run manually (via Hangfire):**
1. Open `http://localhost/hangfire`
2. Click **Recurring Jobs**
3. Find `intel-feature-computation`
4. Click **Trigger now**

**Monitor progress:**
```bash
docker logs retax_api -f | grep -i "feature"
```

Expected output:
```
Feature computation job started, version=v1
Processing 1500 properties in 3 batches
Feature computation job complete for 1500 properties
```

**Verify in database:**
```bash
docker exec retax_postgres psql -U retax_user -d retax_db \
  -c "SELECT COUNT(*), feature_version, MAX(computed_at) FROM intel.feature_vectors GROUP BY feature_version;"
```

Minimum 20 samples required to train any model.

### Step 2 — Train the Risk Scorer

```bash
curl -X POST http://localhost:8001/train/start \
  -H "X-Internal-Secret: YOUR_ML_INTERNAL_SECRET" \
  -H "Content-Type: application/json" \
  -d '{
    "model_type": "risk_scorer",
    "feature_version": "v1",
    "db_url": ""
  }'
```

> **Important:** The field is `model_type`, not `model_name`.  
> Leave `db_url` empty — the service reads `DATABASE_URL` from its environment.

**What happens during training:**
1. Features loaded from `intel.feature_vectors`
2. 80/20 train/test split
3. XGBoost trained with 5-fold cross-validation
4. AUC-ROC, Precision, Recall, F1 computed
5. SHAP values computed for all training samples
6. Model saved to `/app/models/risk_scorer_production.pkl`
7. Run logged in MLflow at `http://localhost:5001`

**Expected training duration:**

| Dataset size | Time |
|-------------|------|
| < 10,000 | 2–5 min |
| 10,000–100,000 | 15–30 min |
| > 100,000 | 1–3 hours |

**Monitor training:**
```bash
docker logs retax_ml -f | grep -E "Training|AUC|complete|error"
```

### Step 3 — Train the Fraud Detector

```bash
curl -X POST http://localhost:8001/train/start \
  -H "X-Internal-Secret: YOUR_ML_INTERNAL_SECRET" \
  -H "Content-Type: application/json" \
  -d '{
    "model_type": "fraud_detector",
    "feature_version": "v1",
    "db_url": ""
  }'
```

Uses Isolation Forest (unsupervised). Assumes ~5% of properties are anomalous.

### Step 4 — Train the Duplicate Detector

```bash
curl -X POST http://localhost:8001/train/start \
  -H "X-Internal-Secret: YOUR_ML_INTERNAL_SECRET" \
  -H "Content-Type: application/json" \
  -d '{
    "model_type": "duplicate_detector",
    "feature_version": "v1",
    "db_url": ""
  }'
```

Uses Isolation Forest on spatial + area features. Assumes ~2% contamination rate.

> The duplicate detector works **before training** using rule-based scoring. Training this model improves accuracy but is not required for the system to operate.

### Step 5 — Review in MLflow

1. Open `http://localhost:5001`
2. Select the experiment (`risk_scorer`, `fraud_detector`, or `duplicate_detector`)
3. Compare runs and review metrics

**Minimum acceptance criteria:**

| Metric | Threshold |
|--------|----------|
| AUC-ROC | > 0.75 |
| Precision | > 0.70 |
| Recall | > 0.65 |
| F1-Score | > 0.70 |

### Step 6 — Register the Model

```bash
docker exec retax_postgres psql -U retax_user -d retax_db -c "
INSERT INTO intel.model_registry
  (id, model_name, model_type, version, status, prediction_type,
   artifact_path, feature_version, trained_at, updated_at)
VALUES
  (gen_random_uuid(),
   'risk_scorer',
   'XGBoost',
   'v1.0',
   'Staged',
   'RiskScore',
   '/app/models/risk_scorer_production.pkl',
   'v1',
   NOW(), NOW());
"
```

Replace metric values with your actual MLflow results.

### Step 7 — Promote to Production

```bash
# Get a JWT token
TOKEN=$(curl -s -X POST http://localhost/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"superadmin","password":"Admin@12345"}' \
  | jq -r '.data.accessToken')

# Get the model ID
MODEL_ID=$(docker exec retax_postgres psql -U retax_user -d retax_db -t -c \
  "SELECT id FROM intel.model_registry WHERE model_name='risk_scorer' AND status='Staged' LIMIT 1;" \
  | tr -d ' \n')

# Promote
curl -X POST "http://localhost/api/v2/intelligence/models/$MODEL_ID/promote" \
  -H "Authorization: Bearer $TOKEN"
```

### Step 8 — Enable Feature Flag

```json
"FeatureManagement": {
  "MLRiskScoring": true
}
```

```bash
docker compose restart api
```

### Step 9 — Run Batch Inference

```bash
curl -X POST http://localhost/api/v2/intelligence/batch-inference \
  -H "Authorization: Bearer $TOKEN"
```

This scores all properties and writes results to both `intel.prediction_results` and the ML columns on `public.properties` (`ml_risk_score`, `ml_fraud_probability`, `ml_duplicate_score`, `ml_last_scored_at`, `ml_model_version`).

---

## 9. Background Jobs (Hangfire)

**Dashboard:** `http://localhost/hangfire`

### Registered Jobs

#### Core Business Jobs (registered in Program.cs)

| Job ID | Schedule | Description |
|--------|----------|-------------|
| `bill-reminders-daily` | Daily 08:00 UTC | Sends overdue bill reminders to taxpayers |
| `mark-overdue-bills-daily` | Daily 00:00 UTC (midnight) | Marks unpaid bills as overdue |
| `penalty-calculation-monthly` | 1st of month, 06:00 UTC | Calculates and applies late penalties |

#### Intelligence Jobs (registered by Intelligence module)

| Job ID | Schedule | Description |
|--------|----------|-------------|
| `intel-feature-computation` | Daily 02:00 UTC | Computes 35 ML features for all properties |
| `intel-ml-inference-risk` | Daily 04:00 UTC | Runs risk scorer on all properties |
| `intel-ml-inference-fraud` | Daily 04:30 UTC | Runs fraud detector on all properties |
| `intel-ml-inference-duplicate` | Daily 05:00 UTC | Runs duplicate detector on all properties |
| `intel-geo-clustering` | Weekly, Sunday 01:00 UTC | DBSCAN clustering of all properties |
| `intel-geofence-update` | Daily 03:00 UTC | Updates geo-fence zone membership |
| `intel-offline-sync` | Every 5 minutes | Processes queued offline sync payloads |

### Running a Job Manually

1. Open `http://localhost/hangfire`
2. Navigate to **Recurring Jobs**
3. Find the job by ID
4. Click **Trigger now**
5. Navigate to **Jobs** → **Processing** to watch it run

### Job Status Reference

| Status | Meaning |
|--------|---------|
| Enqueued | Waiting in queue |
| Processing | Currently running |
| Succeeded | Completed successfully |
| Failed | Error — click to see exception and stack trace |
| Scheduled | Waiting for its next trigger time |
| Retrying | Failed and will retry (Hangfire retries up to 10 times with exponential backoff) |

---

## 10. Roles & Permissions

| Role | Key Permissions |
|------|----------------|
| **SuperAdmin** | Full system access, user management, model promotion |
| **Admin** | All data operations, approval authority, model promotion |
| **Assessor** | Create/edit properties, valuations, assessments, bills |
| **FieldInspector** | Field surveys, GPS tracks, offline sync |
| **Collector** | Record payments |
| **Reviewer** | Review and action ML predictions, approve assessments |
| **ReadOnly** | View all data, no write access |

### Creating a New User

```bash
curl -X POST http://localhost/api/users \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "inspector1",
    "email": "inspector1@retax.gov.eg",
    "password": "Inspector@12345",
    "roles": ["FieldInspector"]
  }'
```

---

## 11. API Reference

**Base URL (production):** `https://api.retax.gov.eg`  
**Base URL (local):** `http://localhost`  
**Authentication:** All endpoints except `/api/auth/*` require `Authorization: Bearer {token}`

### Authentication

```http
POST /api/auth/login
POST /api/auth/refresh-token
POST /api/auth/logout
```

### Core Modules (v1)

```http
# Properties
GET    /api/properties
POST   /api/properties
GET    /api/properties/{id}
PUT    /api/properties/{id}
POST   /api/properties/{id}/verify
POST   /api/properties/{id}/link-owner
GET    /api/properties/nearby?lat=&lng=&radius=

# Taxpayers
GET    /api/taxpayers
POST   /api/taxpayers
GET    /api/taxpayers/{id}
PUT    /api/taxpayers/{id}

# Valuations
POST   /api/valuations
GET    /api/valuations/{id}
POST   /api/valuations/{id}/approve
POST   /api/valuations/{id}/reject

# Tax Assessments
POST   /api/tax-assessments/generate
GET    /api/tax-assessments/{id}
POST   /api/tax-assessments/{id}/approve

# Bills
POST   /api/bills/generate
POST   /api/bills/{id}/issue
GET    /api/bills/{id}

# Payments
POST   /api/payments
GET    /api/payments/{id}

# Appeals
POST   /api/appeals
GET    /api/appeals/{id}
POST   /api/appeals/{id}/decision

# Risk & Fraud
POST   /api/risk/recalculate/{propertyId}
POST   /api/fraud-flags
PATCH  /api/fraud-flags/{id}/resolve

# Enumeration
POST   /api/enumeration/import-source-records
POST   /api/enumeration/match

# Dashboard
GET    /api/dashboard/kpis
```

### Intelligence Module (v2)

```http
# Property predictions
GET    /api/v2/intelligence/properties/{propertyId}/predictions
POST   /api/v2/intelligence/properties/{propertyId}/predict

# Prediction review
GET    /api/v2/intelligence/predictions/pending-review
PATCH  /api/v2/intelligence/predictions/{predictionId}/review

# Dashboard
GET    /api/v2/intelligence/dashboard/summary

# Model registry
GET    /api/v2/intelligence/models
POST   /api/v2/intelligence/models/{modelId}/promote

# Batch inference (triggers nightly job on demand)
POST   /api/v2/intelligence/batch-inference

# Geospatial
GET    /api/v2/geo/risk-heatmap
GET    /api/v2/geo/clusters
GET    /api/v2/geo/anomalies
PATCH  /api/v2/geo/anomalies/{id}/status
GET    /api/v2/geo/fence-zones
POST   /api/v2/geo/fence-zones

# Route optimisation (requires RouteOptimization flag + OSRM)
POST   /api/v2/routes/optimize
GET    /api/v2/routes/assignments
PATCH  /api/v2/routes/assignments/{id}/status
GET    /api/v2/routes/assignments/{id}/track

# Offline sync
POST   /api/v2/sync/upload
GET    /api/v2/sync/status/{syncId}
POST   /api/v2/sync/tracks
```

### ML Service (internal only — not exposed to browser)

> These endpoints are called by the .NET API, not directly by clients. They require `X-Internal-Secret` header.

```http
GET    /health
POST   /predict/batch          # batch inference
POST   /train/start            # async model training
```

---

## 12. Database Reference

### Schema: `public` — Core Business Data

Key tables (all use UUID primary keys, soft delete via `is_deleted`, optimistic concurrency via `xmin`):

| Table | Description |
|-------|-------------|
| `users` | System users with hashed passwords |
| `roles` / `user_roles` | Role assignments |
| `properties` | Master property records + 5 ML score columns |
| `property_locations` | PostGIS geometry (point + polygon) |
| `taxpayers` | Individual and corporate taxpayers |
| `property_ownerships` | Property-taxpayer relationship with percentage |
| `tax_rules` | Configurable tax rates and thresholds |
| `valuations` | Property valuations with Maker-Checker |
| `tax_assessments` | Final assessed tax amounts |
| `tax_bills` | Bills issued to taxpayers |
| `payments` | Payment records |
| `appeals` | Taxpayer objections |
| `exemptions` | Exemption applications |
| `fraud_flags` | Active fraud indicators |
| `field_surveys` | Survey records with GPS |
| `audit_logs` | Immutable action history |

**ML columns on `public.properties`:**

| Column | Type | Description |
|--------|------|-------------|
| `ml_risk_score` | `float8` | Latest risk score (0.0–1.0) |
| `ml_fraud_probability` | `float8` | Latest fraud score (0.0–1.0) |
| `ml_duplicate_score` | `float8` | Latest duplicate score (0.0–1.0) |
| `ml_last_scored_at` | `timestamptz` | When last scored by ML pipeline |
| `ml_model_version` | `varchar(20)` | Version of model that produced the scores |

### Schema: `intel` — Intelligence Data

| Table | Description |
|-------|-------------|
| `feature_vectors` | 35-feature rows per property per version |
| `model_registry` | Registered ML models with status and metrics |
| `prediction_results` | Individual prediction outputs with SHAP explanations |
| `spatial_anomalies` | Detected anomalies with severity and resolution status |
| `geo_clusters` | DBSCAN cluster assignments |
| `geo_fence_zones` | Monitoring polygon zones |
| `geo_fence_memberships` | Property-to-zone membership |
| `offline_sync_batches` | Pending mobile sync payloads |
| `route_assignments` | Inspector route plans |
| `gps_tracks` | GPS track points from field inspectors |

### Useful Queries

```sql
-- Properties by ML risk level
SELECT
  CASE WHEN ml_risk_score >= 0.7 THEN 'High'
       WHEN ml_risk_score >= 0.4 THEN 'Medium'
       ELSE 'Low' END AS risk_level,
  COUNT(*)
FROM properties
WHERE ml_risk_score IS NOT NULL
GROUP BY 1 ORDER BY 2 DESC;

-- Top 20 highest-risk properties
SELECT p.id, p.property_code, p.full_address, p.ml_risk_score, p.ml_last_scored_at
FROM properties p
WHERE p.ml_risk_score >= 0.7
ORDER BY p.ml_risk_score DESC LIMIT 20;

-- Feature store status
SELECT feature_version, COUNT(*), MAX(computed_at) AS last_run
FROM intel.feature_vectors GROUP BY feature_version;

-- Pending predictions by model
SELECT model_name, COUNT(*) AS pending
FROM intel.prediction_results WHERE review_status = 'Pending'
GROUP BY model_name;

-- Anomalies by type and severity
SELECT anomaly_type, severity, status, COUNT(*)
FROM intel.spatial_anomalies
GROUP BY anomaly_type, severity, status
ORDER BY COUNT(*) DESC;

-- Properties within 500 m of a point (PostGIS)
SELECT p.property_code, p.full_address,
       ST_Distance(l.coordinates::geography,
                   ST_SetSRID(ST_MakePoint(31.2357, 30.0444), 4326)::geography) AS distance_m
FROM properties p
JOIN property_locations l ON l.property_id = p.id
WHERE ST_DWithin(l.coordinates::geography,
                 ST_SetSRID(ST_MakePoint(31.2357, 30.0444), 4326)::geography, 500)
ORDER BY distance_m;

-- Address search (Arabic text)
SELECT property_code, full_address,
       similarity(full_address, 'شارع التحرير') AS sim
FROM properties
WHERE similarity(full_address, 'شارع التحرير') > 0.3
ORDER BY sim DESC LIMIT 20;
```

### SQL Migration Files

| File | Applied when | Contents |
|------|-------------|----------|
| `V1__InitialSchema.sql` | First container start (docker-entrypoint-initdb.d) | Full `public` schema |
| `V2__Intelligence_Schema.sql` | First container start + every API startup (idempotent) | `intel` schema |
| `V3__FixTaxAssessmentsSchema.sql` | First container start + every API startup (idempotent) | Adds `prepared_at` to `tax_assessments` |

> V2 and V3 are applied automatically at every API startup using `IF NOT EXISTS` guards, so existing databases are always kept up to date without manual intervention.

---

## 13. Troubleshooting

### API returns 500 on property detail page

**Symptom:** `column t.prepared_at does not exist` in logs  
**Cause:** Database was initialised before V3 migration was added  
**Fix:** The API startup now applies V3 automatically. Restart the API:
```bash
docker compose restart api
docker logs retax_api --tail 20 | grep -i "migration\|schema"
```
You should see: `Schema migrations (V2 intelligence + V3 fix) applied.`

### API container fails to start

```bash
docker logs retax_api --tail 100
```

Common causes:
- PostgreSQL not yet ready: wait 30 s and retry
- Wrong `ConnectionStrings__DefaultConnection`: verify username/password match `.env`
- Missing `JWT_SECRET`: must be set in `.env`

### ML service not responding

```bash
docker logs retax_ml --tail 50
```

Test health from within the Docker network:
```bash
docker exec retax_api curl -s http://ml_service:8001/health
```

Common causes:
- PostgreSQL not ready when ml_service started: `docker compose restart ml_service`
- Wrong `INTERNAL_SECRET`: must match `ML_INTERNAL_SECRET` in `.env`

### Predictions not appearing in UI

1. Verify feature flag is enabled: `MLRiskScoring: true` in appsettings.json
2. Verify a `Production` model exists in `intel.model_registry`:
   ```bash
   docker exec retax_postgres psql -U retax_user -d retax_db \
     -c "SELECT model_name, status, version FROM intel.model_registry;"
   ```
3. Trigger batch inference manually from Hangfire (`intel-ml-inference-risk`)
4. Check `intel.prediction_results` for rows

### Map / heatmap shows no data

1. Verify `intel.feature_vectors` has rows (see query above)
2. Trigger `intel-feature-computation` from Hangfire
3. Verify properties have GPS coordinates:
   ```bash
   docker exec retax_postgres psql -U retax_user -d retax_db \
     -c "SELECT COUNT(*) FROM property_locations WHERE coordinates IS NOT NULL;"
   ```

### Login fails

```bash
docker exec retax_postgres psql -U retax_user -d retax_db \
  -c "SELECT username, is_active, lockout_end FROM users WHERE username='superadmin';"
```

If `is_active = false` or `lockout_end` is in the future, unlock:
```bash
docker exec retax_postgres psql -U retax_user -d retax_db \
  -c "UPDATE users SET is_active=true, failed_login_attempts=0, lockout_end=NULL WHERE username='superadmin';"
```

### Hangfire jobs failing

1. Open `http://localhost/hangfire` → **Jobs** → **Failed**
2. Click the failed job to see the full exception
3. Check logs: `docker logs retax_api --tail 100 | grep -i "error\|exception"`

### Useful Docker Commands

```bash
# View all container statuses
docker compose -f docker-compose.yml -f docker-compose.intelligence.yml ps

# Follow logs for all services
docker compose -f docker-compose.yml -f docker-compose.intelligence.yml logs -f

# Restart a single service
docker compose restart api

# Resource usage
docker stats

# Full reset (deletes ALL data including database)
docker compose -f docker-compose.yml -f docker-compose.intelligence.yml down -v
docker compose -f docker-compose.yml -f docker-compose.intelligence.yml up -d

# Access PostgreSQL
docker exec -it retax_postgres psql -U retax_user -d retax_db

# Access API shell
docker exec -it retax_api bash
```

---

## 14. Security Checklist

Before going to production, complete every item:

### Secrets
- [ ] Change `POSTGRES_PASSWORD` from default
- [ ] Set `JWT_SECRET` to a cryptographically random string ≥ 32 characters
- [ ] Change `ML_INTERNAL_SECRET` from `dev-secret`
- [ ] Change default `superadmin` password (`Admin@12345`)
- [ ] Remove or rotate all other default user accounts

### Network
- [ ] Do not expose port 5432 (PostgreSQL) to the internet
- [ ] Do not expose port 6379 (Redis) to the internet
- [ ] Do not expose port 8001 (ML service) to the internet
- [ ] Put the application behind HTTPS (TLS certificate on Nginx)
- [ ] Restrict `CORS.AllowedOrigins` to your actual domain

### Application
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production` (disables Swagger UI)
- [ ] Verify rate limiting settings (`IpRateLimiting` in appsettings.json)
- [ ] Enable account lockout (already configured: 5 failed attempts)
- [ ] Review and remove the Hangfire `HangfireAllowAllFilter` — add proper authentication

### Data
- [ ] Populate `tax_rules` table with verified rates from Law 196/2008
- [ ] Verify `ValuationRule` maintenance deduction percentage (currently 30% placeholder)
- [ ] Verify penalty rate in `TaxCalculationService`
- [ ] Verify appeal deadline (currently 60 days — confirm against Egyptian law)
- [ ] Map `ExemptionRule.LegalReference` to specific Law 196/2008 articles
- [ ] Add Egyptian National ID checksum validation

### Backups
- [ ] Configure automated PostgreSQL backups (`pg_dump` or managed backup)
- [ ] Configure ML model artefact backups (`ml_models` Docker volume)
- [ ] Test restore procedure

---

*ReTax Platform System Manual — Version 2.0*  
*Last updated: May 2026*  
*For support, contact: it@retax.gov.eg*
