# Database Migration Guide

## Prerequisites
- PostgreSQL 14+ with PostGIS 3.x extension
- .NET 8 SDK
- EF Core CLI: `dotnet tool install --global dotnet-ef`

## Initial Setup

### 1. Create the database and enable PostGIS
```sql
CREATE DATABASE retax_db;
\c retax_db
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS postgis_topology;
CREATE EXTENSION IF NOT EXISTS pg_trgm;       -- For address similarity search
CREATE EXTENSION IF NOT EXISTS unaccent;      -- For Arabic text normalization
```

### 2. Create the application user
```sql
CREATE USER retax_user WITH ENCRYPTED PASSWORD 'your_strong_password';
GRANT ALL PRIVILEGES ON DATABASE retax_db TO retax_user;
```

### 3. Generate and apply migrations
```bash
cd src/RealEstateTax.Infrastructure

# Generate initial migration
dotnet ef migrations add InitialCreate \
  --project . \
  --startup-project ../RealEstateTax.API \
  --output-dir Migrations

# Apply migration
dotnet ef database update \
  --project . \
  --startup-project ../RealEstateTax.API \
  --connection "Host=localhost;Port=5432;Database=retax_db;Username=retax_user;Password=your_password"
```

## PostGIS Geometry Columns
The following columns use PostGIS geometry types and require the postgis extension:

| Table               | Column      | Type                      |
|---------------------|-------------|---------------------------|
| property_locations  | coordinates | geometry(Point, 4326)     |
| property_locations  | boundary    | geometry(Polygon, 4326)   |

SRID 4326 = WGS84 geographic coordinate system (latitude/longitude).

## Spatial Indexes
PostGIS GiST indexes are created automatically via EF configuration:
```sql
CREATE INDEX idx_property_locations_coords ON property_locations USING gist(coordinates);
CREATE INDEX idx_property_locations_boundary ON property_locations USING gist(boundary);
```

## Useful Spatial Queries

### Find properties within 500m of a point
```sql
SELECT p.property_code, p.full_address,
       ST_Distance(
           l.coordinates::geography,
           ST_SetSRID(ST_MakePoint(31.2357, 30.0444), 4326)::geography
       ) AS distance_m
FROM properties p
JOIN property_locations l ON l.property_id = p.id
WHERE ST_DWithin(
    l.coordinates::geography,
    ST_SetSRID(ST_MakePoint(31.2357, 30.0444), 4326)::geography,
    500  -- meters
)
ORDER BY distance_m;
```

### Address similarity search using pg_trgm
```sql
SELECT property_code, full_address,
       similarity(full_address, 'شارع التحرير') AS sim
FROM properties
WHERE similarity(full_address, 'شارع التحرير') > 0.3
ORDER BY sim DESC
LIMIT 20;
```

## Adding Future Migrations
```bash
dotnet ef migrations add <MigrationName> \
  --project src/RealEstateTax.Infrastructure \
  --startup-project src/RealEstateTax.API
```

## Rolling Back
```bash
dotnet ef database update <PreviousMigrationName> \
  --project src/RealEstateTax.Infrastructure \
  --startup-project src/RealEstateTax.API
```
