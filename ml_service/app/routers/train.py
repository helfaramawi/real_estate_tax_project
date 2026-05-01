from fastapi import APIRouter, BackgroundTasks, HTTPException
from pydantic import BaseModel
import logging
import os

router = APIRouter()
logger = logging.getLogger(__name__)


class TrainRequest(BaseModel):
    model_type: str   # "risk_scorer" | "fraud_detector"
    feature_version: str = "v1"
    db_url: str = ""  # overridden from env if empty


@router.post("/start")
async def start_training(request: TrainRequest, background_tasks: BackgroundTasks):
    """Trigger async model training. Returns immediately; check MLflow for progress."""
    allowed = {"risk_scorer", "fraud_detector"}
    if request.model_type not in allowed:
        raise HTTPException(400, f"model_type must be one of {allowed}")

    db_url = request.db_url or os.getenv("DATABASE_URL", "")
    if not db_url:
        raise HTTPException(400, "DATABASE_URL not configured")

    background_tasks.add_task(_run_training, request.model_type, request.feature_version, db_url)
    return {"status": "training_started", "model_type": request.model_type}


async def _run_training(model_type: str, feature_version: str, db_url: str):
    try:
        if model_type == "risk_scorer":
            from app.training.train_risk import train_risk_model
            result = train_risk_model(db_url, feature_version)
        elif model_type == "fraud_detector":
            from app.training.train_fraud import train_fraud_model
            result = train_fraud_model(db_url, feature_version)
        else:
            return
        logger.info("Training complete for %s: %s", model_type, result)
    except Exception as e:
        logger.error("Training failed for %s: %s", model_type, e, exc_info=True)
