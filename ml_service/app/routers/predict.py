from fastapi import APIRouter, Request, HTTPException
from pydantic import BaseModel
from typing import Optional

router = APIRouter()


class FeatureRow(BaseModel):
    property_id: str
    lat: Optional[float] = None
    lon: Optional[float] = None
    built_up_area: Optional[float] = None
    land_area: Optional[float] = None
    property_type_code: Optional[int] = None
    year_built: Optional[int] = None
    declared_annual_value: Optional[float] = None
    market_value_per_sqm: Optional[float] = None
    capitalization_rate: Optional[float] = None
    paid_on_time_rate: Optional[float] = None
    overdue_count: Optional[int] = None
    bills_count: Optional[int] = None
    total_paid_egp: Optional[float] = None
    total_outstanding_egp: Optional[float] = None
    nearest_neighbor_distance_m: Optional[float] = None
    neighbors_within100m: Optional[int] = None
    neighbors_within500m: Optional[int] = None
    surveys_count: Optional[int] = None
    days_since_last_survey: Optional[int] = None
    gps_accuracy_avg: Optional[float] = None
    existing_risk_score: Optional[int] = None
    geo_verification_score: Optional[int] = None
    fraud_flags_count: Optional[int] = None
    appeals_count: Optional[int] = None
    exemptions_count: Optional[int] = None
    source_records_count: Optional[int] = None
    matched_records_count: Optional[int] = None
    max_match_confidence: Optional[float] = None
    ownership_chain_length: Optional[int] = None
    days_since_last_transfer: Optional[int] = None
    corporate_owner_flag: Optional[bool] = None
    multiple_owners_flag: Optional[bool] = None
    value_vs_cluster_median_pct: Optional[float] = None
    value_vs_district_median_pct: Optional[float] = None
    has_boundary_polygon: Optional[bool] = None


class BatchPredictionRequest(BaseModel):
    model_name: str
    model_version: str
    features: list[FeatureRow]


class PredictionResult(BaseModel):
    property_id: str
    score: float
    label: str
    confidence: float
    explanation: dict[str, float]


@router.post("/batch", response_model=list[PredictionResult])
async def predict_batch(request_body: BatchPredictionRequest, request: Request):
    models = getattr(request.app.state, "models", {})

    model_key = request_body.model_name.split("_")[0]  # e.g. "risk_scorer" -> "risk"
    model = models.get(request_body.model_name) or models.get(model_key + "_scorer") or models.get(model_key + "_detector")

    if model is None:
        raise HTTPException(
            status_code=503,
            detail=f"Model '{request_body.model_name}' not loaded. Train and register a model first."
        )

    features_dicts = [f.model_dump(exclude={"property_id"}) for f in request_body.features]
    property_ids = [f.property_id for f in request_body.features]

    raw_results = model.predict_with_explanation(features_dicts)

    return [
        PredictionResult(
            property_id=pid,
            score=r["score"],
            label=r["label"],
            confidence=r["confidence"],
            explanation=r["explanation"],
        )
        for pid, r in zip(property_ids, raw_results)
    ]
