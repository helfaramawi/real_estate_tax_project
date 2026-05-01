-- ============================================================
-- V2__Intelligence_Schema.sql
-- Additive migration: creates intel schema + new tables
-- ZERO impact on existing public schema tables or queries
-- ============================================================

-- PostGIS already enabled in V1 — no need to repeat
CREATE SCHEMA IF NOT EXISTS intel;

-- ─── Feature Store ────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS intel.feature_vectors (
    id                              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id                     UUID NOT NULL REFERENCES public.properties(id),
    computed_at                     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    feature_version                 VARCHAR(20) NOT NULL,

    -- Spatial features
    lat                             DOUBLE PRECISION,
    lon                             DOUBLE PRECISION,
    has_boundary_polygon            BOOLEAN NOT NULL DEFAULT FALSE,
    boundary_area_sqm               DOUBLE PRECISION,
    nearest_neighbor_distance_m     DOUBLE PRECISION,
    neighbors_within100m            INTEGER,
    neighbors_within500m            INTEGER,
    in_geo_cluster                  BOOLEAN NOT NULL DEFAULT FALSE,
    cluster_id                      UUID,
    distance_to_cluster_center_m    DOUBLE PRECISION,

    -- Property attributes
    built_up_area                   DOUBLE PRECISION,
    land_area                       DOUBLE PRECISION,
    property_type_code              INTEGER,
    year_built                      INTEGER,
    floors_count                    INTEGER,
    units_count                     INTEGER,

    -- Valuation features
    declared_annual_value           NUMERIC(18,2),
    market_value_per_sqm            NUMERIC(18,2),
    capitalization_rate             DOUBLE PRECISION,
    value_vs_cluster_median_pct     DOUBLE PRECISION,
    value_vs_district_median_pct    DOUBLE PRECISION,

    -- Ownership features
    ownership_chain_length          INTEGER,
    days_since_last_transfer        INTEGER,
    transfer_count3y                INTEGER,
    multiple_owners_flag            BOOLEAN NOT NULL DEFAULT FALSE,
    corporate_owner_flag            BOOLEAN NOT NULL DEFAULT FALSE,

    -- Survey features
    surveys_count                   INTEGER,
    days_since_last_survey          INTEGER,
    gps_accuracy_avg                DOUBLE PRECISION,
    measured_vs_declared_area_delta_pct DOUBLE PRECISION,

    -- Payment / billing features
    bills_count                     INTEGER,
    paid_on_time_rate               DOUBLE PRECISION,
    overdue_count                   INTEGER,
    total_paid_egp                  NUMERIC(18,2),
    total_outstanding_egp           NUMERIC(18,2),

    -- Risk / compliance features
    existing_risk_score             INTEGER,
    geo_verification_score          INTEGER,
    fraud_flags_count               INTEGER,
    appeals_count                   INTEGER,
    exemptions_count                INTEGER,

    -- Source matching features
    source_records_count            INTEGER,
    matched_records_count           INTEGER,
    max_match_confidence            DOUBLE PRECISION,
    source_systems_count            INTEGER,

    CONSTRAINT uq_feature_property_version UNIQUE (property_id, feature_version)
);

CREATE INDEX IF NOT EXISTS idx_fv_property   ON intel.feature_vectors(property_id);
CREATE INDEX IF NOT EXISTS idx_fv_computed   ON intel.feature_vectors(computed_at DESC);

-- ─── Model Registry ───────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS intel.model_registry (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    model_name          VARCHAR(100) NOT NULL,
    model_type          VARCHAR(50)  NOT NULL,
    version             VARCHAR(20)  NOT NULL,
    status              VARCHAR(20)  NOT NULL DEFAULT 'Staged',
    prediction_type     VARCHAR(50)  NOT NULL,
    artifact_path       TEXT,
    mlflow_run_id       VARCHAR(100),
    feature_version     VARCHAR(20),
    trained_at          TIMESTAMPTZ,
    promoted_at         TIMESTAMPTZ,
    retired_at          TIMESTAMPTZ,
    training_sample_size INTEGER,
    metrics             JSONB,
    feature_importance  JSONB,
    notes               TEXT,
    created_by          UUID,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_model_name_version UNIQUE (model_name, version)
);

CREATE INDEX IF NOT EXISTS idx_mr_status          ON intel.model_registry(status);
CREATE INDEX IF NOT EXISTS idx_mr_type_status     ON intel.model_registry(prediction_type, status);

-- ─── Prediction Results ───────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS intel.prediction_results (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id         UUID NOT NULL REFERENCES public.properties(id),
    model_id            UUID NOT NULL REFERENCES intel.model_registry(id),
    prediction_type     VARCHAR(50)  NOT NULL,
    predicted_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    score               DOUBLE PRECISION NOT NULL,
    label               VARCHAR(50),
    confidence          DOUBLE PRECISION NOT NULL DEFAULT 0,
    explanation         JSONB,
    feature_snapshot    JSONB,
    is_reviewed         BOOLEAN NOT NULL DEFAULT FALSE,
    reviewed_by         UUID,
    reviewed_at         TIMESTAMPTZ,
    review_outcome      VARCHAR(20),
    review_notes        TEXT
);

CREATE INDEX IF NOT EXISTS idx_pr_property_type   ON intel.prediction_results(property_id, prediction_type);
CREATE INDEX IF NOT EXISTS idx_pr_type_date       ON intel.prediction_results(prediction_type, predicted_at DESC);
CREATE INDEX IF NOT EXISTS idx_pr_score_unreviewed ON intel.prediction_results(score DESC) WHERE is_reviewed = FALSE;

-- ─── Geo Clusters ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS intel.geo_clusters (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cluster_algorithm   VARCHAR(30)  NOT NULL DEFAULT 'DBSCAN',
    cluster_label       INTEGER      NOT NULL,
    centroid            geometry(Point, 4326),
    convex_hull         geometry(Polygon, 4326),
    property_count      INTEGER,
    median_value_sqm    NUMERIC(18,2),
    p25_value_sqm       NUMERIC(18,2),
    p75_value_sqm       NUMERIC(18,2),
    computed_at         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    district_code       VARCHAR(50),
    governorate         VARCHAR(100)
);

CREATE INDEX IF NOT EXISTS idx_gc_centroid    ON intel.geo_clusters USING GIST(centroid);
CREATE INDEX IF NOT EXISTS idx_gc_hull        ON intel.geo_clusters USING GIST(convex_hull);
CREATE INDEX IF NOT EXISTS idx_gc_computed    ON intel.geo_clusters(computed_at DESC);

-- ─── Spatial Anomalies ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS intel.spatial_anomalies (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    anomaly_type        VARCHAR(50)  NOT NULL,
    severity            VARCHAR(20)  NOT NULL,
    status              VARCHAR(20)  NOT NULL DEFAULT 'Open',
    property_id         UUID REFERENCES public.properties(id),
    location            geometry(Point, 4326),
    affected_area       geometry(Polygon, 4326),
    detected_at         TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    detection_model     VARCHAR(100),
    description         TEXT,
    evidence            JSONB,
    assigned_to         UUID,
    resolved_at         TIMESTAMPTZ,
    resolution_notes    TEXT
);

CREATE INDEX IF NOT EXISTS idx_sa_location    ON intel.spatial_anomalies USING GIST(location);
CREATE INDEX IF NOT EXISTS idx_sa_status_sev  ON intel.spatial_anomalies(status, severity);
CREATE INDEX IF NOT EXISTS idx_sa_property    ON intel.spatial_anomalies(property_id);
CREATE INDEX IF NOT EXISTS idx_sa_detected    ON intel.spatial_anomalies(detected_at DESC);

-- ─── Inspector Tracks ─────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS intel.inspector_tracks (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    inspector_id        UUID NOT NULL,
    field_survey_id     UUID REFERENCES public.field_surveys(id),
    recorded_at         TIMESTAMPTZ NOT NULL,
    synced_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    location            geometry(Point, 4326),
    accuracy_meters     DOUBLE PRECISION,
    speed_kmh           DOUBLE PRECISION,
    heading_degrees     DOUBLE PRECISION,
    altitude_m          DOUBLE PRECISION,
    battery_level       INTEGER,
    device_id           VARCHAR(100),
    is_offline_record   BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_it_inspector   ON intel.inspector_tracks(inspector_id, recorded_at DESC);
CREATE INDEX IF NOT EXISTS idx_it_survey      ON intel.inspector_tracks(field_survey_id);
CREATE INDEX IF NOT EXISTS idx_it_location    ON intel.inspector_tracks USING GIST(location);

-- ─── Route Assignments ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS intel.route_assignments (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    inspector_id            UUID NOT NULL,
    assignment_date         DATE NOT NULL,
    status                  VARCHAR(20) NOT NULL DEFAULT 'Planned',
    waypoints               JSONB NOT NULL DEFAULT '[]',
    optimized_route         geometry(LineString, 4326),
    estimated_distance_km   DOUBLE PRECISION,
    estimated_duration_min  INTEGER,
    actual_start            TIMESTAMPTZ,
    actual_end              TIMESTAMPTZ,
    properties_visited      INTEGER NOT NULL DEFAULT 0,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_ra_inspector   ON intel.route_assignments(inspector_id, assignment_date);
CREATE INDEX IF NOT EXISTS idx_ra_route       ON intel.route_assignments USING GIST(optimized_route);

-- ─── Geo Fence Zones ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS intel.geo_fence_zones (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(200) NOT NULL,
    zone_type   VARCHAR(50)  NOT NULL,
    boundary    geometry(Polygon, 4326) NOT NULL,
    properties  JSONB,
    is_active   BOOLEAN NOT NULL DEFAULT TRUE,
    valid_from  DATE,
    valid_to    DATE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_gfz_boundary   ON intel.geo_fence_zones USING GIST(boundary);
CREATE INDEX IF NOT EXISTS idx_gfz_active     ON intel.geo_fence_zones(is_active, zone_type);

-- ─── Offline Sync Queue ───────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS intel.offline_sync_queue (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id       VARCHAR(100) NOT NULL,
    inspector_id    UUID NOT NULL,
    submitted_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    packet_type     VARCHAR(50)  NOT NULL,
    payload         JSONB NOT NULL DEFAULT '{}',
    status          VARCHAR(20)  NOT NULL DEFAULT 'Pending',
    processed_at    TIMESTAMPTZ,
    error_message   TEXT,
    retry_count     INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_osq_status     ON intel.offline_sync_queue(status, submitted_at);
CREATE INDEX IF NOT EXISTS idx_osq_inspector  ON intel.offline_sync_queue(inspector_id, submitted_at DESC);

-- ─── Additive columns on public.properties ────────────────────────────────────
ALTER TABLE public.properties
    ADD COLUMN IF NOT EXISTS ml_risk_score          DOUBLE PRECISION,
    ADD COLUMN IF NOT EXISTS ml_fraud_probability   DOUBLE PRECISION,
    ADD COLUMN IF NOT EXISTS ml_duplicate_score     DOUBLE PRECISION,
    ADD COLUMN IF NOT EXISTS ml_last_scored_at      TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS ml_model_version       VARCHAR(20);

-- ─── Additive columns on public.property_locations ───────────────────────────
ALTER TABLE public.property_locations
    ADD COLUMN IF NOT EXISTS plus_code              VARCHAR(20),
    ADD COLUMN IF NOT EXISTS h3_index_res8          VARCHAR(20),
    ADD COLUMN IF NOT EXISTS h3_index_res10         VARCHAR(20),
    ADD COLUMN IF NOT EXISTS in_geo_fence           BOOLEAN,
    ADD COLUMN IF NOT EXISTS fence_zone_id          UUID REFERENCES intel.geo_fence_zones(id),
    ADD COLUMN IF NOT EXISTS satellite_verified_at  TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS satellite_source       VARCHAR(50);

-- ─── Additive columns on public.field_surveys ────────────────────────────────
ALTER TABLE public.field_surveys
    ADD COLUMN IF NOT EXISTS route_assignment_id    UUID REFERENCES intel.route_assignments(id),
    ADD COLUMN IF NOT EXISTS device_id              VARCHAR(100),
    ADD COLUMN IF NOT EXISTS offline_sync_id        UUID REFERENCES intel.offline_sync_queue(id),
    ADD COLUMN IF NOT EXISTS gps_track_points       INTEGER NOT NULL DEFAULT 0;
