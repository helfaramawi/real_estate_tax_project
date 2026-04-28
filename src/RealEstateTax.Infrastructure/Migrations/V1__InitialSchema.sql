-- =============================================================================
-- Real Estate Tax Intelligence Platform — Initial Schema
-- Database: PostgreSQL 16 + PostGIS 3.4
-- =============================================================================

-- Extensions
CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS pg_trgm;       -- trigram similarity for address search
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";   -- gen_random_uuid() fallback

-- =============================================================================
-- USERS & RBAC
-- =============================================================================

CREATE TABLE users (
    id                    UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    username              VARCHAR(100) NOT NULL,
    email                 VARCHAR(200) NOT NULL,
    password_hash         VARCHAR(500) NOT NULL,
    phone_number          VARCHAR(20),
    national_id           VARCHAR(14),
    first_name            VARCHAR(100) NOT NULL,
    last_name             VARCHAR(100) NOT NULL,
    is_active             BOOLEAN     NOT NULL DEFAULT TRUE,
    is_email_verified     BOOLEAN     NOT NULL DEFAULT FALSE,
    last_login_at         TIMESTAMPTZ,
    refresh_token         VARCHAR(500),
    refresh_token_expiry  TIMESTAMPTZ,
    password_reset_token  VARCHAR(200),
    password_reset_expiry TIMESTAMPTZ,
    failed_login_attempts INTEGER     NOT NULL DEFAULT 0,
    locked_until          TIMESTAMPTZ,
    -- BaseEntity
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100),
    xmin        XID         -- used as row version (EF Core UseXminAsConcurrencyToken)
);

CREATE UNIQUE INDEX uq_users_username ON users (username) WHERE NOT is_deleted;
CREATE UNIQUE INDEX uq_users_email    ON users (email)    WHERE NOT is_deleted;
CREATE INDEX ix_users_national_id     ON users (national_id);

CREATE TABLE roles (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100),
    xmin        XID
);

CREATE UNIQUE INDEX uq_roles_name ON roles (name) WHERE NOT is_deleted;

CREATE TABLE permissions (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(200) NOT NULL,
    resource    VARCHAR(100) NOT NULL,
    action      VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE UNIQUE INDEX uq_permissions_resource_action ON permissions (resource, action) WHERE NOT is_deleted;

CREATE TABLE user_roles (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     UUID        NOT NULL REFERENCES users (id),
    role_id     UUID        NOT NULL REFERENCES roles (id),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE INDEX ix_user_roles_user_role ON user_roles (user_id, role_id) WHERE NOT is_deleted;

CREATE TABLE role_permissions (
    id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id       UUID        NOT NULL REFERENCES roles (id),
    permission_id UUID        NOT NULL REFERENCES permissions (id),
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by    VARCHAR(100) NOT NULL DEFAULT '',
    updated_at    TIMESTAMPTZ,
    updated_by    VARCHAR(100),
    is_deleted    BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at    TIMESTAMPTZ,
    deleted_by    VARCHAR(100)
);

CREATE UNIQUE INDEX uq_role_permissions ON role_permissions (role_id, permission_id) WHERE NOT is_deleted;

-- =============================================================================
-- TAXPAYERS
-- =============================================================================

CREATE TABLE taxpayers (
    id                               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    taxpayer_code                    VARCHAR(30)  NOT NULL,
    national_id                      VARCHAR(14)  NOT NULL,
    tax_registration_number          VARCHAR(50),
    commercial_registration_number   VARCHAR(50),
    first_name                       VARCHAR(100) NOT NULL,
    last_name                        VARCHAR(100) NOT NULL,
    father_name                      VARCHAR(100),
    date_of_birth                    DATE,
    gender                           VARCHAR(10),
    nationality                      VARCHAR(10) DEFAULT 'EGY',
    is_corporate                     BOOLEAN     NOT NULL DEFAULT FALSE,
    company_name                     VARCHAR(200),
    legal_form                       VARCHAR(100),
    email                            VARCHAR(200),
    phone_number                     VARCHAR(20),
    alternative_phone                VARCHAR(20),
    address                          VARCHAR(500),
    governorate                      VARCHAR(100),
    city                             VARCHAR(100),
    postal_code                      VARCHAR(10),
    is_verified                      BOOLEAN     NOT NULL DEFAULT FALSE,
    verified_at                      TIMESTAMPTZ,
    verified_by                      VARCHAR(100),
    is_deceased                      BOOLEAN     NOT NULL DEFAULT FALSE,
    deceased_at                      TIMESTAMPTZ,
    linked_user_id                   UUID        REFERENCES users (id),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100),
    xmin        XID
);

CREATE UNIQUE INDEX uq_taxpayers_code ON taxpayers (taxpayer_code) WHERE NOT is_deleted;
CREATE INDEX ix_taxpayers_national_id ON taxpayers (national_id);
CREATE INDEX ix_taxpayers_tax_reg     ON taxpayers (tax_registration_number);

-- =============================================================================
-- PROPERTIES
-- =============================================================================

CREATE TABLE properties (
    id                      UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    property_code           VARCHAR(30)  NOT NULL,
    parcel_number           VARCHAR(50),
    cadastral_reference     VARCHAR(100),
    -- Enum stored as int (EF Core default)
    type                    INTEGER     NOT NULL DEFAULT 0,
    status                  INTEGER     NOT NULL DEFAULT 0,
    sub_type                VARCHAR(100),
    usage_description       VARCHAR(500),
    -- Physical
    built_up_area           NUMERIC(12,2) NOT NULL DEFAULT 0,
    land_area               NUMERIC(12,2),
    number_of_floors        INTEGER,
    floor_number            INTEGER,
    number_of_units         INTEGER,
    year_built              INTEGER,
    year_last_renovated     INTEGER,
    building_condition      VARCHAR(100),
    -- Address
    street_address          VARCHAR(300),
    building_number         VARCHAR(20),
    neighbourhood           VARCHAR(100),
    district                VARCHAR(100),
    city                    VARCHAR(100),
    governorate             VARCHAR(100),
    postal_code             VARCHAR(10),
    full_address            VARCHAR(600),
    -- Quality
    confidence_score        DOUBLE PRECISION NOT NULL DEFAULT 1.0,
    has_duplicate_suspicion BOOLEAN     NOT NULL DEFAULT FALSE,
    merged_into_property_id UUID,
    -- Maker-Checker
    verified_by             VARCHAR(100),
    verified_at             TIMESTAMPTZ,
    verification_notes      VARCHAR(1000),
    review_notes            VARCHAR(1000),
    -- BaseEntity
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100),
    xmin        XID
);

CREATE UNIQUE INDEX uq_properties_code   ON properties (property_code)  WHERE NOT is_deleted;
CREATE INDEX ix_properties_parcel        ON properties (parcel_number);
CREATE INDEX ix_properties_status        ON properties (status)          WHERE NOT is_deleted;
CREATE INDEX ix_properties_governorate   ON properties (governorate)     WHERE NOT is_deleted;

-- Full-text search on address using pg_trgm
CREATE INDEX ix_properties_full_address_trgm ON properties USING gin (full_address gin_trgm_ops)
    WHERE full_address IS NOT NULL AND NOT is_deleted;

CREATE TABLE property_locations (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id         UUID        NOT NULL REFERENCES properties (id),
    -- PostGIS geometry
    coordinates         geometry(Point, 4326),
    boundary            geometry(Polygon, 4326),
    -- Scalar fallback (kept in sync with coordinates)
    latitude            DOUBLE PRECISION,
    longitude           DOUBLE PRECISION,
    elevation           DOUBLE PRECISION,
    -- Administrative
    governorate         VARCHAR(100),
    markaz              VARCHAR(100),
    city                VARCHAR(100),
    village             VARCHAR(100),
    neighbourhood       VARCHAR(100),
    block               VARCHAR(50),
    plot                VARCHAR(50),
    -- Accuracy
    geocoding_source    VARCHAR(50),
    geocoding_accuracy  DOUBLE PRECISION,
    geocoded_at         TIMESTAMPTZ,
    is_verified         BOOLEAN     NOT NULL DEFAULT FALSE,
    -- BaseEntity
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE INDEX ix_property_locations_coordinates ON property_locations USING gist (coordinates)
    WHERE coordinates IS NOT NULL AND NOT is_deleted;
CREATE INDEX ix_property_locations_boundary    ON property_locations USING gist (boundary)
    WHERE boundary IS NOT NULL AND NOT is_deleted;

CREATE TABLE property_units (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id     UUID        NOT NULL REFERENCES properties (id),
    unit_code       VARCHAR(50)  NOT NULL,
    unit_number     VARCHAR(20),
    unit_type       VARCHAR(100),
    usage_type      VARCHAR(100),
    floor           INTEGER,
    area            NUMERIC(10,2),
    monthly_rent    NUMERIC(12,2),
    is_occupied     BOOLEAN     NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE UNIQUE INDEX uq_property_units_code ON property_units (unit_code) WHERE NOT is_deleted;

CREATE TABLE property_ownerships (
    id                      UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id             UUID        NOT NULL REFERENCES properties (id),
    taxpayer_id             UUID        NOT NULL REFERENCES taxpayers (id),
    property_unit_id        UUID        REFERENCES property_units (id),
    ownership_type          INTEGER     NOT NULL DEFAULT 0,
    ownership_percentage    NUMERIC(5,2) NOT NULL DEFAULT 100,
    title_deed_number       VARCHAR(100),
    registration_authority  VARCHAR(200),
    registration_date       DATE,
    notarization_reference  VARCHAR(100),
    inheritance_reference   VARCHAR(100),
    is_current              BOOLEAN     NOT NULL DEFAULT TRUE,
    start_date              DATE,
    end_date                DATE,
    is_verified             BOOLEAN     NOT NULL DEFAULT FALSE,
    verified_at             TIMESTAMPTZ,
    verified_by             VARCHAR(100),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE INDEX ix_property_ownerships_lookup ON property_ownerships (property_id, taxpayer_id, is_current)
    WHERE NOT is_deleted;

-- =============================================================================
-- DATA INTEGRATION
-- =============================================================================

CREATE TABLE property_sources (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    code        VARCHAR(30)  NOT NULL,
    name        VARCHAR(200) NOT NULL,
    description VARCHAR(500),
    api_endpoint VARCHAR(500),
    is_active   BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE UNIQUE INDEX uq_property_sources_code ON property_sources (code) WHERE NOT is_deleted;

CREATE TABLE property_source_records (
    id                      UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    source_id               UUID        NOT NULL REFERENCES property_sources (id),
    master_property_id      UUID        REFERENCES properties (id),
    source_reference_id     VARCHAR(100) NOT NULL,
    owner_name              VARCHAR(200),
    owner_national_id       VARCHAR(14),
    address                 VARCHAR(500),
    latitude                DOUBLE PRECISION,
    longitude               DOUBLE PRECISION,
    meter_number            VARCHAR(100),
    account_number          VARCHAR(100),
    property_type_from_source VARCHAR(100),
    area_from_source        NUMERIC(10,2),
    raw_data                JSONB,
    is_matched              BOOLEAN     NOT NULL DEFAULT FALSE,
    match_confidence        DOUBLE PRECISION,
    match_method            VARCHAR(100),
    is_rejected             BOOLEAN     NOT NULL DEFAULT FALSE,
    rejection_reason        VARCHAR(500),
    import_batch_id         VARCHAR(50),
    imported_at             TIMESTAMPTZ,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE UNIQUE INDEX uq_source_records_ref ON property_source_records (source_id, source_reference_id)
    WHERE NOT is_deleted;
CREATE INDEX ix_source_records_matched      ON property_source_records (is_matched)        WHERE NOT is_deleted;
CREATE INDEX ix_source_records_national_id  ON property_source_records (owner_national_id) WHERE NOT is_deleted;
CREATE INDEX ix_source_records_meter        ON property_source_records (meter_number)      WHERE NOT is_deleted;

-- =============================================================================
-- FIELD SURVEYS
-- =============================================================================

CREATE TABLE field_surveys (
    id                    UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id           UUID        NOT NULL REFERENCES properties (id),
    assigned_to_user_id   UUID        NOT NULL REFERENCES users (id),
    survey_code           VARCHAR(30)  NOT NULL,
    status                INTEGER     NOT NULL DEFAULT 0,
    scheduled_date        DATE,
    completed_date        DATE,
    measured_area         NUMERIC(10,2),
    field_notes           VARCHAR(5000),
    observed_condition    VARCHAR(200),
    occupant_info         VARCHAR(500),
    property_type_observed VARCHAR(100),
    review_notes          VARCHAR(2000),
    reviewed_by           VARCHAR(100),
    reviewed_at           TIMESTAMPTZ,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE UNIQUE INDEX uq_field_surveys_code ON field_surveys (survey_code) WHERE NOT is_deleted;
CREATE INDEX ix_field_surveys_property_status ON field_surveys (property_id, status) WHERE NOT is_deleted;

CREATE TABLE survey_attachments (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    survey_id       UUID        NOT NULL REFERENCES field_surveys (id),
    file_name       VARCHAR(300) NOT NULL,
    storage_path    VARCHAR(1000) NOT NULL,
    content_type    VARCHAR(100) NOT NULL,
    file_size_bytes BIGINT,
    attachment_type VARCHAR(50),
    description     VARCHAR(500),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

-- =============================================================================
-- VALUATION
-- =============================================================================

CREATE TABLE valuation_rules (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    code                VARCHAR(50)  NOT NULL,
    name                VARCHAR(200) NOT NULL,
    description         VARCHAR(1000),
    property_type       VARCHAR(100),
    governorate         VARCHAR(100),
    deduction_percentage NUMERIC(5,2) NOT NULL DEFAULT 30,
    min_net_value       NUMERIC(18,2),
    max_net_value       NUMERIC(18,2),
    use_rental_value    BOOLEAN     NOT NULL DEFAULT TRUE,
    standard_rent_per_sq_m NUMERIC(10,2),
    applicable_from_year INTEGER,
    is_active           BOOLEAN     NOT NULL DEFAULT TRUE,
    legal_reference     VARCHAR(500),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE UNIQUE INDEX uq_valuation_rules_code ON valuation_rules (code) WHERE NOT is_deleted;

CREATE TABLE valuations (
    id                    UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id           UUID        NOT NULL REFERENCES properties (id),
    valuation_rule_id     UUID        REFERENCES valuation_rules (id),
    valuation_code        VARCHAR(30)  NOT NULL,
    tax_year              INTEGER     NOT NULL,
    valuation_date        DATE        NOT NULL,
    status                INTEGER     NOT NULL DEFAULT 0,
    total_area            NUMERIC(12,2),
    rentable_area         NUMERIC(12,2),
    annual_rental_value   NUMERIC(18,2),
    market_value_per_sq_m NUMERIC(12,2),
    gross_value           NUMERIC(18,2),
    deduction_percentage  NUMERIC(5,2),
    net_value             NUMERIC(18,2),
    capitalization_rate   NUMERIC(6,4),
    approval_notes        VARCHAR(2000),
    rejection_reason      VARCHAR(1000),
    prepared_by           VARCHAR(100),
    approved_by           VARCHAR(100),
    approved_at           TIMESTAMPTZ,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100),
    xmin        XID
);

CREATE UNIQUE INDEX uq_valuations_code ON valuations (valuation_code) WHERE NOT is_deleted;
CREATE INDEX ix_valuations_property_year ON valuations (property_id, tax_year) WHERE NOT is_deleted;

-- =============================================================================
-- TAX RULES, ASSESSMENTS & BILLS
-- =============================================================================

CREATE TABLE tax_rules (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    code                VARCHAR(50)  NOT NULL,
    name                VARCHAR(200) NOT NULL,
    description         VARCHAR(1000),
    property_type       VARCHAR(100) NOT NULL,
    governorate         VARCHAR(100),
    tax_rate            NUMERIC(6,4) NOT NULL DEFAULT 0,
    min_taxable_value   NUMERIC(18,2),
    max_taxable_value   NUMERIC(18,2),
    flat_amount         NUMERIC(18,2),
    applicable_from_year INTEGER,
    applicable_to_year   INTEGER,
    is_active           BOOLEAN     NOT NULL DEFAULT TRUE,
    legal_reference     VARCHAR(500),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE UNIQUE INDEX uq_tax_rules_code ON tax_rules (code) WHERE NOT is_deleted;

CREATE TABLE exemption_rules (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    code                VARCHAR(50)  NOT NULL,
    name                VARCHAR(200) NOT NULL,
    description         VARCHAR(1000),
    exemption_type      VARCHAR(100) NOT NULL,
    exemption_percentage NUMERIC(5,2),
    is_full_exemption   BOOLEAN     NOT NULL DEFAULT FALSE,
    max_exempt_amount   NUMERIC(18,2),
    eligibility_criteria VARCHAR(5000),
    requires_approval   BOOLEAN     NOT NULL DEFAULT TRUE,
    max_duration_months INTEGER,
    is_active           BOOLEAN     NOT NULL DEFAULT TRUE,
    legal_reference     VARCHAR(500),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE UNIQUE INDEX uq_exemption_rules_code ON exemption_rules (code) WHERE NOT is_deleted;

CREATE TABLE exemptions (
    id                    UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id           UUID        NOT NULL REFERENCES properties (id),
    taxpayer_id           UUID        NOT NULL REFERENCES taxpayers (id),
    exemption_rule_id     UUID        REFERENCES exemption_rules (id),
    exemption_code        VARCHAR(30)  NOT NULL,
    status                INTEGER     NOT NULL DEFAULT 0,
    justification_summary VARCHAR(1000) NOT NULL,
    supporting_documents  VARCHAR(5000),
    exemption_percentage  NUMERIC(5,2),
    exempt_amount         NUMERIC(18,2),
    effective_from        DATE,
    effective_to          DATE,
    reviewed_by           VARCHAR(100),
    reviewed_at           TIMESTAMPTZ,
    approved_by           VARCHAR(100),
    approved_at           TIMESTAMPTZ,
    approval_notes        VARCHAR(2000),
    rejection_reason      VARCHAR(1000),
    revocation_reason     VARCHAR(1000),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100),
    xmin        XID
);

CREATE UNIQUE INDEX uq_exemptions_code ON exemptions (exemption_code) WHERE NOT is_deleted;
CREATE INDEX ix_exemptions_property_status ON exemptions (property_id, status) WHERE NOT is_deleted;

CREATE TABLE tax_assessments (
    id                UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id       UUID        NOT NULL REFERENCES properties (id),
    valuation_id      UUID        NOT NULL REFERENCES valuations (id),
    tax_rule_id       UUID        REFERENCES tax_rules (id),
    exemption_id      UUID        REFERENCES exemptions (id),
    assessment_code   VARCHAR(30)  NOT NULL,
    tax_year          INTEGER     NOT NULL,
    status            INTEGER     NOT NULL DEFAULT 0,
    taxable_value     NUMERIC(18,2) NOT NULL DEFAULT 0,
    tax_rate          NUMERIC(6,4)  NOT NULL DEFAULT 0,
    gross_tax_amount  NUMERIC(18,2) NOT NULL DEFAULT 0,
    exemption_amount  NUMERIC(18,2) NOT NULL DEFAULT 0,
    net_tax_amount    NUMERIC(18,2) NOT NULL DEFAULT 0,
    penalties         NUMERIC(18,2) NOT NULL DEFAULT 0,
    late_fees         NUMERIC(18,2) NOT NULL DEFAULT 0,
    total_due         NUMERIC(18,2) NOT NULL DEFAULT 0,
    prepared_by       VARCHAR(100),
    approved_by       VARCHAR(100),
    approved_at       TIMESTAMPTZ,
    approval_notes    VARCHAR(2000),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100),
    xmin        XID
);

CREATE UNIQUE INDEX uq_tax_assessments_code ON tax_assessments (assessment_code) WHERE NOT is_deleted;
CREATE INDEX ix_tax_assessments_property_year ON tax_assessments (property_id, tax_year) WHERE NOT is_deleted;

CREATE TABLE tax_bills (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id         UUID        NOT NULL REFERENCES properties (id),
    tax_assessment_id   UUID        NOT NULL REFERENCES tax_assessments (id),
    taxpayer_id         UUID        NOT NULL REFERENCES taxpayers (id),
    bill_number         VARCHAR(30)  NOT NULL,
    tax_year            INTEGER     NOT NULL,
    status              INTEGER     NOT NULL DEFAULT 0,
    total_amount        NUMERIC(18,2) NOT NULL DEFAULT 0,
    paid_amount         NUMERIC(18,2) NOT NULL DEFAULT 0,
    discount_amount     NUMERIC(18,2) NOT NULL DEFAULT 0,
    penalty_amount      NUMERIC(18,2) NOT NULL DEFAULT 0,
    issue_date          DATE,
    due_date            DATE,
    paid_date           DATE,
    issued_by           VARCHAR(100),
    cancelled_by        VARCHAR(100),
    cancellation_reason VARCHAR(500),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100),
    xmin        XID
);

CREATE UNIQUE INDEX uq_tax_bills_number ON tax_bills (bill_number) WHERE NOT is_deleted;
CREATE INDEX ix_tax_bills_property_year ON tax_bills (property_id, tax_year) WHERE NOT is_deleted;
CREATE INDEX ix_tax_bills_status        ON tax_bills (status, due_date)       WHERE NOT is_deleted;

CREATE TABLE payments (
    id                      UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tax_bill_id             UUID        NOT NULL REFERENCES tax_bills (id),
    taxpayer_id             UUID        NOT NULL REFERENCES taxpayers (id),
    payment_reference       VARCHAR(50)  NOT NULL,
    amount                  NUMERIC(18,2) NOT NULL DEFAULT 0,
    fee_amount              NUMERIC(18,2) NOT NULL DEFAULT 0,
    currency                VARCHAR(3)   NOT NULL DEFAULT 'EGP',
    payment_date            TIMESTAMPTZ,
    payment_method          INTEGER     NOT NULL DEFAULT 0,
    status                  INTEGER     NOT NULL DEFAULT 0,
    external_transaction_id VARCHAR(200),
    bank_reference          VARCHAR(200),
    receipt_number          VARCHAR(100),
    collected_by            VARCHAR(100),
    failure_reason          VARCHAR(500),
    refund_reason           VARCHAR(500),
    notes                   VARCHAR(1000),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE UNIQUE INDEX uq_payments_reference ON payments (payment_reference) WHERE NOT is_deleted;
CREATE INDEX ix_payments_status ON payments (status) WHERE NOT is_deleted;

-- =============================================================================
-- APPEALS
-- =============================================================================

CREATE TABLE appeals (
    id                        UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id               UUID        NOT NULL REFERENCES properties (id),
    taxpayer_id               UUID        NOT NULL REFERENCES taxpayers (id),
    tax_assessment_id         UUID        REFERENCES tax_assessments (id),
    tax_bill_id               UUID        REFERENCES tax_bills (id),
    assigned_to_user_id       UUID        REFERENCES users (id),
    appeal_code               VARCHAR(30)  NOT NULL,
    status                    INTEGER     NOT NULL DEFAULT 0,
    grounds_summary           VARCHAR(1000) NOT NULL,
    detailed_statement        VARCHAR(10000),
    legal_basis               VARCHAR(2000),
    requested_assessment_value NUMERIC(18,2),
    revised_assessment_value  NUMERIC(18,2),
    submitted_at              TIMESTAMPTZ,
    hearing_date              TIMESTAMPTZ,
    hearing_notes             VARCHAR(2000),
    decided_at                TIMESTAMPTZ,
    decision_by               VARCHAR(100),
    decision_notes            VARCHAR(2000),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100),
    xmin        XID
);

CREATE UNIQUE INDEX uq_appeals_code ON appeals (appeal_code) WHERE NOT is_deleted;
CREATE INDEX ix_appeals_status ON appeals (status) WHERE NOT is_deleted;

CREATE TABLE appeal_documents (
    id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    appeal_id     UUID        NOT NULL REFERENCES appeals (id),
    file_name     VARCHAR(300) NOT NULL,
    storage_path  VARCHAR(1000) NOT NULL,
    content_type  VARCHAR(100) NOT NULL,
    file_size_bytes BIGINT,
    document_type VARCHAR(100),
    description   VARCHAR(500),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

-- =============================================================================
-- NOTIFICATIONS & INTEGRATIONS
-- =============================================================================

CREATE TABLE notifications (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    taxpayer_id         UUID        NOT NULL REFERENCES taxpayers (id),
    type                INTEGER     NOT NULL DEFAULT 0,
    channel             INTEGER     NOT NULL DEFAULT 0,
    status              INTEGER     NOT NULL DEFAULT 0,
    subject             VARCHAR(300) NOT NULL,
    body                VARCHAR(5000) NOT NULL,
    template_id         VARCHAR(100),
    template_data       VARCHAR(5000),
    recipient_address   VARCHAR(200),
    sent_at             TIMESTAMPTZ,
    provider_message_id VARCHAR(200),
    failure_reason      VARCHAR(500),
    entity_type         VARCHAR(100),
    entity_id           UUID,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE INDEX ix_notifications_taxpayer_status ON notifications (taxpayer_id, status) WHERE NOT is_deleted;

CREATE TABLE external_entities (
    id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    code          VARCHAR(50)  NOT NULL,
    name          VARCHAR(200) NOT NULL,
    short_name    VARCHAR(50),
    description   VARCHAR(500),
    entity_type   INTEGER     NOT NULL DEFAULT 0,
    base_url      VARCHAR(500),
    auth_type     VARCHAR(50),
    api_key_header VARCHAR(100),
    api_key_value VARCHAR(500),
    is_active     BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE UNIQUE INDEX uq_external_entities_code ON external_entities (code) WHERE NOT is_deleted;

CREATE TABLE integration_requests (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    external_entity_id  UUID        NOT NULL REFERENCES external_entities (id),
    operation_type      VARCHAR(100) NOT NULL,
    status              INTEGER     NOT NULL DEFAULT 0,
    request_payload     JSONB,
    response_payload    JSONB,
    error_message       VARCHAR(2000),
    retry_count         INTEGER     NOT NULL DEFAULT 0,
    correlation_id      VARCHAR(100),
    related_entity_type VARCHAR(100),
    related_entity_id   UUID,
    requested_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at        TIMESTAMPTZ,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE INDEX ix_integration_requests_entity_status ON integration_requests (external_entity_id, status) WHERE NOT is_deleted;
CREATE INDEX ix_integration_requests_correlation    ON integration_requests (correlation_id);

-- =============================================================================
-- RISK & FRAUD
-- =============================================================================

CREATE TABLE risk_scores (
    id                         UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id                UUID        NOT NULL REFERENCES properties (id),
    level                      INTEGER     NOT NULL DEFAULT 0,
    score                      DOUBLE PRECISION NOT NULL DEFAULT 0,
    data_completeness_score    DOUBLE PRECISION NOT NULL DEFAULT 0,
    valuation_consistency_score DOUBLE PRECISION NOT NULL DEFAULT 0,
    ownership_chain_score      DOUBLE PRECISION NOT NULL DEFAULT 0,
    payment_history_score      DOUBLE PRECISION NOT NULL DEFAULT 0,
    geo_verification_score     DOUBLE PRECISION NOT NULL DEFAULT 0,
    source_consistency_score   DOUBLE PRECISION NOT NULL DEFAULT 0,
    risk_factors               VARCHAR(5000),
    recommendations            VARCHAR(5000),
    calculated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    calculated_by              VARCHAR(100),
    model_version              INTEGER     NOT NULL DEFAULT 1,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE INDEX ix_risk_scores_property_at ON risk_scores (property_id, calculated_at) WHERE NOT is_deleted;

CREATE TABLE fraud_flags (
    id                      UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id             UUID        NOT NULL REFERENCES properties (id),
    raised_by_user_id       UUID        REFERENCES users (id),
    investigated_by_user_id UUID        REFERENCES users (id),
    flag_type               INTEGER     NOT NULL DEFAULT 0,
    status                  INTEGER     NOT NULL DEFAULT 0,
    severity                INTEGER     NOT NULL DEFAULT 0,
    description             VARCHAR(2000) NOT NULL,
    evidence                VARCHAR(5000),
    raised_by_system        VARCHAR(100),
    investigation_started_at TIMESTAMPTZ,
    resolved_at             TIMESTAMPTZ,
    resolution_notes        VARCHAR(2000),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE INDEX ix_fraud_flags_property_status ON fraud_flags (property_id, status) WHERE NOT is_deleted;

CREATE TABLE data_quality_issues (
    id                  UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id         UUID        NOT NULL REFERENCES properties (id),
    issue_type          VARCHAR(100) NOT NULL,
    severity            VARCHAR(20)  NOT NULL,
    status              VARCHAR(20)  NOT NULL DEFAULT 'Open',
    description         VARCHAR(1000) NOT NULL,
    affected_field      VARCHAR(100),
    source_system_code  VARCHAR(50),
    suggested_action    VARCHAR(500),
    resolved_at         TIMESTAMPTZ,
    resolved_by         VARCHAR(100),
    resolution_notes    VARCHAR(1000),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100) NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ,
    updated_by  VARCHAR(100),
    is_deleted  BOOLEAN     NOT NULL DEFAULT FALSE,
    deleted_at  TIMESTAMPTZ,
    deleted_by  VARCHAR(100)
);

CREATE INDEX ix_data_quality_issues_property_status ON data_quality_issues (property_id, status) WHERE NOT is_deleted;

-- =============================================================================
-- AUDIT LOGS (never soft-deleted — append-only)
-- =============================================================================

CREATE TABLE audit_logs (
    id             UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id        UUID        REFERENCES users (id),
    username       VARCHAR(100),
    user_role      VARCHAR(100),
    ip_address     VARCHAR(50),
    user_agent     VARCHAR(500),
    action         VARCHAR(100) NOT NULL,
    entity_type    VARCHAR(100) NOT NULL,
    entity_id      UUID,
    entity_code    VARCHAR(50),
    old_values     JSONB,
    new_values     JSONB,
    change_summary VARCHAR(1000),
    correlation_id VARCHAR(100),
    http_method    VARCHAR(10),
    request_path   VARCHAR(500),
    is_success     BOOLEAN     NOT NULL DEFAULT TRUE,
    failure_reason VARCHAR(500),
    timestamp      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_audit_logs_entity    ON audit_logs (entity_type, entity_id);
CREATE INDEX ix_audit_logs_timestamp ON audit_logs (timestamp DESC);
CREATE INDEX ix_audit_logs_user_id   ON audit_logs (user_id);

-- =============================================================================
-- HANGFIRE SCHEMA (managed by Hangfire.PostgreSql — installed at startup)
-- Only a placeholder comment; actual tables created by the library.
-- =============================================================================

-- hangfire schema tables: hangfire.job, hangfire.state, hangfire.jobqueue, etc.
-- Created automatically by UseHangfireStorage(connectionString) at startup.

-- =============================================================================
-- SEED DATA — Roles & Permissions
-- =============================================================================

INSERT INTO roles (id, name, description, created_at, created_by) VALUES
    ('00000000-0000-0000-0001-000000000001', 'SuperAdmin',        'Full system access',                       NOW(), 'system'),
    ('00000000-0000-0000-0001-000000000002', 'Admin',             'Administrative access',                    NOW(), 'system'),
    ('00000000-0000-0000-0001-000000000003', 'DataEntry',         'Field data entry and source processing',   NOW(), 'system'),
    ('00000000-0000-0000-0001-000000000004', 'Assessor',          'Valuation and tax assessment',             NOW(), 'system'),
    ('00000000-0000-0000-0001-000000000005', 'Approver',          'Maker-checker approval authority',         NOW(), 'system'),
    ('00000000-0000-0000-0001-000000000006', 'FieldSurveyor',     'Field survey operations',                  NOW(), 'system'),
    ('00000000-0000-0000-0001-000000000007', 'TaxpayerPortal',    'Self-service taxpayer access',             NOW(), 'system'),
    ('00000000-0000-0000-0001-000000000008', 'ReadOnly',          'Read-only reporting access',               NOW(), 'system'),
    ('00000000-0000-0000-0001-000000000009', 'IntegrationService','Machine-to-machine API access',            NOW(), 'system');

-- Default SuperAdmin user (password: Admin@12345 — CHANGE IN PRODUCTION)
INSERT INTO users (id, username, email, password_hash, first_name, last_name, is_active, created_at, created_by) VALUES
    ('00000000-0000-0000-0000-000000000001',
     'superadmin',
     'admin@realestatetax.gov.eg',
     '$2a$11$K7Gy.BQxf1LWp6Q5mBj6zuJzPT0Gw1bXB.aelPH7YRm1F.eLpAz.6',  -- BCrypt of Admin@12345
     'System', 'Administrator', TRUE, NOW(), 'system');

INSERT INTO user_roles (id, user_id, role_id, created_at, created_by) VALUES
    (gen_random_uuid(),
     '00000000-0000-0000-0000-000000000001',
     '00000000-0000-0000-0001-000000000001',
     NOW(), 'system');

-- Seed exemption rules
INSERT INTO exemption_rules (id, code, name, exemption_type, exemption_percentage, is_full_exemption, requires_approval, is_active, legal_reference, created_at, created_by) VALUES
    (gen_random_uuid(), 'EX-SOCIAL', 'Social Housing Exemption',   'SocialHousing',     100, TRUE,  TRUE,  TRUE, 'Law 196/2008 Art. 18', NOW(), 'system'),
    (gen_random_uuid(), 'EX-WIDOW',  'Widow/Orphan Exemption',     'WidowOrOrphan',     100, TRUE,  TRUE,  TRUE, 'Law 196/2008 Art. 19', NOW(), 'system'),
    (gen_random_uuid(), 'EX-DISABL', 'Disabled Owner Exemption',   'DisabledOwner',      50, FALSE, TRUE,  TRUE, 'Law 196/2008 Art. 20', NOW(), 'system'),
    (gen_random_uuid(), 'EX-AGRI',   'Agricultural Land Exemption','Agricultural',      100, TRUE,  FALSE, TRUE, 'Law 196/2008 Art. 22', NOW(), 'system'),
    (gen_random_uuid(), 'EX-RELIG',  'Religious Property Exemption','Religious',        100, TRUE,  FALSE, TRUE, 'Law 196/2008 Art. 23', NOW(), 'system'),
    (gen_random_uuid(), 'EX-GOVT',   'Government Property Exemption','Government',      100, TRUE,  FALSE, TRUE, 'Law 196/2008 Art. 24', NOW(), 'system'),
    (gen_random_uuid(), 'EX-NEWBLD', 'New Construction Exemption', 'NewConstruction',   100, TRUE,  FALSE, TRUE, 'Law 196/2008 Art. 25', NOW(), 'system');
