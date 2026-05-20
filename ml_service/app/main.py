from contextlib import asynccontextmanager
from fastapi import FastAPI, Depends, HTTPException, Header
from fastapi.middleware.cors import CORSMiddleware
import logging
import os

from app.routers import predict, train, health
from app.models.risk_scorer import RiskScorer
from app.models.fraud_detector import FraudDetector
from app.models.duplicate_detector import DuplicateDetector

logger = logging.getLogger(__name__)
MODEL_STORE: dict = {}

INTERNAL_SECRET = os.getenv("INTERNAL_SECRET", "dev-secret")


def verify_internal_secret(x_internal_secret: str = Header(...)):
    if x_internal_secret != INTERNAL_SECRET:
        raise HTTPException(status_code=403, detail="Invalid internal secret")


@asynccontextmanager
async def lifespan(app: FastAPI):
    try:
        MODEL_STORE["risk_scorer"] = RiskScorer.load_production()
        logger.info("Risk scorer model loaded")
    except Exception as e:
        logger.warning("Risk scorer not loaded (no trained model yet): %s", e)

    try:
        MODEL_STORE["fraud_detector"] = FraudDetector.load_production()
        logger.info("Fraud detector model loaded")
    except Exception as e:
        logger.warning("Fraud detector not loaded (no trained model yet): %s", e)

    try:
        MODEL_STORE["duplicate_detector"] = DuplicateDetector.load_production()
        logger.info("Duplicate detector ML model loaded")
    except FileNotFoundError:
        # No trained artefact yet — fall back to the rule-based scorer so the
        # nightly batch inference job can run immediately without training.
        MODEL_STORE["duplicate_detector"] = DuplicateDetector()
        logger.info("Duplicate detector using rule-based fallback (no trained model)")
    except Exception as e:
        MODEL_STORE["duplicate_detector"] = DuplicateDetector()
        logger.warning("Duplicate detector fallback due to load error: %s", e)

    app.state.models = MODEL_STORE
    yield
    MODEL_STORE.clear()


app = FastAPI(
    title="ReTax ML Service",
    version="1.0.0",
    lifespan=lifespan,
    docs_url="/docs" if os.getenv("ENVIRONMENT") != "production" else None,
    redoc_url=None,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://retax_api:8080"],
    allow_methods=["POST", "GET"],
    allow_headers=["*"],
)

app.include_router(health.router, prefix="/health", tags=["health"])
app.include_router(
    predict.router,
    prefix="/predict",
    tags=["predictions"],
    dependencies=[Depends(verify_internal_secret)],
)
app.include_router(
    train.router,
    prefix="/train",
    tags=["training"],
    dependencies=[Depends(verify_internal_secret)],
)
