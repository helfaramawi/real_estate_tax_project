# Real Estate Tax Intelligence Platform – Egypt
## Production Backend — .NET 8 Clean Architecture

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         API Layer                                    │
│  Controllers · Middleware · JWT Auth · Rate Limiting · Swagger      │
├─────────────────────────────────────────────────────────────────────┤
│                      Application Layer                               │
│  Service Interfaces · DTOs · Validators · Mapster Mappings          │
├─────────────────────────────────────────────────────────────────────┤
│                       Domain Layer                                   │
│  Entities · Enums · Domain Service Interfaces · Domain Events       │
├─────────────────────────────────────────────────────────────────────┤
│                    Infrastructure Layer                              │
│  EF Core · PostgreSQL/PostGIS · JWT · Hangfire · Serilog · Files    │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Tech Stack

| Concern            | Technology                              |
|--------------------|-----------------------------------------|
| Framework          | .NET 8 Web API                          |
| Architecture       | Clean Architecture (4-layer)            |
| Database           | PostgreSQL 16 + PostGIS 3.4             |
| ORM                | Entity Framework Core 8                 |
| Authentication     | JWT Bearer + Role-Based Authorization   |
| Background Jobs    | Hangfire (PostgreSQL storage)           |
| Logging            | Serilog (Console + extensible)          |
| Validation         | FluentValidation                        |
| Mapping            | Mapster                                 |
| API Docs           | Swagger / OpenAPI 3                     |
| Rate Limiting      | AspNetCoreRateLimit                     |
| Containerization   | Docker + Docker Compose                 |
| Spatial            | NetTopologySuite + PostGIS              |

---

## Quick Start

### Option A: Docker Compose (recommended)

```bash
git clone <repo>
cd real_estate_tax_project

# Copy and configure environment
cp docker-compose.yml docker-compose.override.yml
# Edit docker-compose.override.yml: set strong passwords and JWT secret

docker compose up -d postgres  # Wait for PostgreSQL to be ready
docker compose up -d api

# API will be available at: http://localhost:8080
# Swagger UI:              http://localhost:8080/swagger
# Hangfire Dashboard:      http://localhost:8080/hangfire
# Health check:            http://localhost:8080/health
```

### Option B: Local Development

**Prerequisites:**
- .NET 8 SDK
- PostgreSQL 16 with PostGIS 3.x
- dotnet-ef tool: `dotnet tool install --global dotnet-ef`

**Steps:**
```bash
# 1. Clone and setup database (see docs/MIGRATIONS.md)
psql -c "CREATE DATABASE retax_dev;"
psql -d retax_dev -c "CREATE EXTENSION IF NOT EXISTS postgis;"
psql -d retax_dev -c "CREATE EXTENSION IF NOT EXISTS pg_trgm;"

# 2. Configure connection string
# Edit: src/RealEstateTax.API/appsettings.Development.json

# 3. Apply migrations
dotnet ef database update \
  --project src/RealEstateTax.Infrastructure \
  --startup-project src/RealEstateTax.API

# 4. Run the API
cd src/RealEstateTax.API
dotnet run

# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
# Health: http://localhost:5000/health
```

---

## Default Credentials

| Role       | Username    | Password      |
|------------|-------------|---------------|
| SuperAdmin | superadmin  | Admin@12345   |

**IMPORTANT:** Change the default password immediately after first login.

---

## Module Overview

### 1. Property Registry
- Master property records with full lifecycle management
- Status machine: Draft → NeedsReview → Verified → Taxable / Exempt / Archived
- PostGIS-backed spatial queries (nearby, boundary)

### 2. Taxpayer / Owner Profiles
- Egyptian National ID (14-digit) validation
- Corporate and individual taxpayers
- Multi-ownership support with percentage tracking

### 3. Property Enumeration
- Multi-source import (electricity, water, cadastral, municipal)
- Automatic duplicate detection via GIS + address similarity + National ID
- Confidence scoring for matched records
- Data quality issue tracking

### 4. Field Surveys
- Assignment to FieldInspectors
- GPS-tagged photo/document upload
- Status workflow: Assigned → InProgress → Submitted → Approved

### 5. Valuation & Assessment
- Pluggable valuation methods (Rental Value, Market Comparison, Cost)
- Maker-Checker approval for valuations and assessments
- Configurable tax rules (rates NOT hard-coded)

### 6. Tax Billing
- Automatic bill generation from approved assessments
- Installment plan support
- Automated taxpayer notifications on issue

### 7. Payments
- Multi-channel payment registration (cash, bank, online, mobile wallet)
- Automatic bill status update on payment
- Payment confirmation notifications

### 8. Appeals & Objections
- Full appeal lifecycle with hearing scheduling
- Document upload with secure validation
- Maker-Checker decision workflow

### 9. Exemptions
- Multiple exemption types mapped to Egyptian law articles
- Eligibility rules engine (IExemptionService)
- Partial or full exemption support

### 10. Risk & Fraud Detection
- Multi-factor risk scoring (data completeness, valuation consistency, etc.)
- Fraud flag lifecycle management
- Automatic risk recalculation trigger

### 11. Integration Hub
- Inbound/outbound integration with government entities
- Hangfire background processing queue
- Retry mechanism with exponential backoff

### 12. Audit Trail
- Immutable append-only audit logs
- Every sensitive action recorded (CRUD, approvals, decisions)
- Correlation ID tracking across all requests

---

## Security Features

- JWT authentication with refresh token rotation
- 9 granular roles with permission-based authorization
- Maker-Checker workflow for all approval actions
- Account lockout after 5 failed login attempts
- Soft delete (no data is hard-deleted)
- Optimistic concurrency (PostgreSQL xmin)
- Rate limiting (20 req/s global, 10 req/min for auth)
- File upload type and size validation
- Correlation ID on every request
- Global exception handler (no stack traces in responses)

---

## Professional Readiness

Before expanding scope, use `docs/PROFESSIONAL_WORK_PREP.md` as the baseline deep-study checklist and maturity framework for all modules and features.

---

## Business Rules (TODO Items)

The following require verification with Egyptian Tax Authority:

- [ ] **Tax Rates**: `TaxRule` table — populate from Law 196/2008 and annual decrees
- [ ] **Deduction Rate**: `ValuationRule` table — verify maintenance deduction % (currently 30% placeholder)
- [ ] **Minimum Threshold**: `TaxRule.MinTaxableValue` — confirm annual rental value floor
- [ ] **Penalty Rate**: `TaxCalculationService.CalculatePenaltyAsync` — verify monthly penalty rate
- [ ] **Appeal Deadline**: `AppealAppService.SubmitAsync` — verify 60-day deadline under Egyptian law
- [ ] **Exemption Articles**: `ExemptionRule.LegalReference` — map to specific Law 196/2008 articles
- [ ] **National ID Checksum**: Add Egyptian NID algorithmic validation in `TaxpayerValidators`

---

## Documentation

- Documentation index: `docs/DOCS_INDEX.md`

## Project Structure

```
real_estate_tax_project/
├── src/
│   ├── RealEstateTax.Domain/           # Entities, Enums, Domain Interfaces
│   │   ├── Common/                     # BaseEntity, DomainEvent
│   │   ├── Entities/                   # 25+ domain entities
│   │   ├── Enums/                      # All domain enumerations
│   │   └── Services/                   # IPropertyMatchingService, ITaxCalculationService, etc.
│   ├── RealEstateTax.Application/      # Use Cases Layer
│   │   ├── Common/Interfaces/          # IApplicationDbContext, ICurrentUserService, etc.
│   │   ├── Common/Models/              # Result<T>, PagedResult<T>, QueryParameters
│   │   ├── DTOs/                       # All request/response DTOs by module
│   │   ├── Services/                   # Application service interfaces (14 modules)
│   │   ├── Validators/                 # FluentValidation validators
│   │   └── Mappings/                   # Mapster configuration
│   ├── RealEstateTax.Infrastructure/   # Data & External Services
│   │   ├── Persistence/                # ApplicationDbContext, EF Configurations, Seed
│   │   ├── Identity/                   # JwtTokenService
│   │   └── Services/                   # All service implementations
│   │       └── ApplicationServices/    # 14 application service implementations
│   └── RealEstateTax.API/              # Web API
│       ├── Controllers/                # 11 API controllers (50+ endpoints)
│       ├── Middleware/                 # Exception, CorrelationId
│       ├── Extensions/                 # Result → IActionResult mapping
│       ├── Program.cs
│       ├── appsettings.json
│       └── appsettings.Development.json
├── tests/
│   └── RealEstateTax.UnitTests/
├── docs/
│   ├── MIGRATIONS.md                   # DB setup & migration guide
│   └── API_EXAMPLES.md                 # Sample HTTP requests
├── Dockerfile
├── docker-compose.yml
├── RealEstateTax.sln
└── README.md
```

---

## Running Tests

```bash
dotnet test tests/RealEstateTax.UnitTests/RealEstateTax.UnitTests.csproj
```

---

## Contributing

See Egyptian Tax Authority IT governance process for code review and deployment procedures.

> **Legal Notice**: Tax rates, valuation formulas, exemption criteria, and penalty schedules are placeholder values. All must be verified against Egyptian Real Estate Tax Law 196/2008 and applicable ministerial decrees before production deployment.
