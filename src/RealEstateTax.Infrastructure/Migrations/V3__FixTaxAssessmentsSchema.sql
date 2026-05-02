-- V3: Add prepared_at column that was missing from tax_assessments in V1
-- The TaxAssessment entity has PreparedAt but V1__InitialSchema.sql omitted it.
ALTER TABLE tax_assessments ADD COLUMN IF NOT EXISTS prepared_at TIMESTAMPTZ;
