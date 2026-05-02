"""
Training script for the duplicate property detector.

Uses Isolation Forest on spatial + area features.
Properties with abnormally small nearest-neighbour distances are treated
as anomalies (potential duplicate registrations).

Run via POST /train/start  { "model_name": "duplicate_detector" }
"""
import logging
import os
from pathlib import Path

import joblib
import mlflow
import numpy as np
import pandas as pd
import psycopg2
from sklearn.ensemble import IsolationForest
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler

logger = logging.getLogger(__name__)

ARTIFACT_PATH = Path(os.getenv("MODEL_ARTIFACT_PATH", "/app/models"))
DATABASE_URL   = os.getenv("DATABASE_URL", "")
MLFLOW_URI     = os.getenv("MLFLOW_TRACKING_URI", "http://mlflow:5000")

FEATURES = [
    "nearest_neighbor_distance_m",
    "built_up_area",
    "land_area",
    "neighbors_within100m",
]

MIN_SAMPLES = 20


def load_features() -> pd.DataFrame:
    conn = psycopg2.connect(DATABASE_URL)
    try:
        cols = ", ".join(FEATURES)
        query = f"""
            SELECT {cols}
            FROM intel.feature_vectors
            WHERE feature_version = 'v1'
              AND nearest_neighbor_distance_m IS NOT NULL
        """
        df = pd.read_sql(query, conn)
    finally:
        conn.close()
    return df


def train() -> None:
    mlflow.set_tracking_uri(MLFLOW_URI)
    mlflow.set_experiment("duplicate_detector")

    df = load_features()
    logger.info("Loaded %d feature rows for duplicate detector training", len(df))

    if len(df) < MIN_SAMPLES:
        logger.warning(
            "Only %d samples available (minimum %d). Skipping training.",
            len(df), MIN_SAMPLES
        )
        return

    X = df[FEATURES].fillna(-1).values

    # Contamination: assume 2 % of properties are potential duplicates
    contamination = 0.02

    pipeline = Pipeline([
        ("scaler", StandardScaler()),
        ("iso",    IsolationForest(
            n_estimators=200,
            contamination=contamination,
            random_state=42,
            n_jobs=-1,
        )),
    ])

    with mlflow.start_run():
        pipeline.fit(X)

        # Estimate anomaly rate on training data
        preds = pipeline.predict(X)
        anomaly_count = int((preds == -1).sum())
        anomaly_rate  = anomaly_count / len(X)

        mlflow.log_params({
            "n_estimators":  200,
            "contamination": contamination,
            "n_features":    len(FEATURES),
            "n_samples":     len(X),
            "feature_list":  ",".join(FEATURES),
        })
        mlflow.log_metrics({
            "anomaly_count": anomaly_count,
            "anomaly_rate":  anomaly_rate,
        })

        # Persist artefact
        out_path = ARTIFACT_PATH / "duplicate_detector_production.pkl"
        ARTIFACT_PATH.mkdir(parents=True, exist_ok=True)
        joblib.dump(pipeline, out_path)
        mlflow.log_artifact(str(out_path))

        logger.info(
            "Duplicate detector trained: %d samples, %.1f%% anomalies → %s",
            len(X), anomaly_rate * 100, out_path
        )
