"""
Train Isolation Forest fraud detector on feature store data (unsupervised cold start).
Usage: python -m app.training.train_fraud
"""
import os
import joblib
import pandas as pd
import mlflow
import mlflow.sklearn
import psycopg2
import numpy as np
from sklearn.ensemble import IsolationForest
from sklearn.preprocessing import StandardScaler
from sklearn.pipeline import Pipeline


def train_fraud_model(db_url: str, feature_version: str = "v1") -> dict:
    conn = psycopg2.connect(db_url)

    df = pd.read_sql("""
        SELECT f.*
        FROM intel.feature_vectors f
        WHERE f.feature_version = %(version)s
    """, conn, params={"version": feature_version})

    conn.close()

    if len(df) < 20:
        raise ValueError(f"Insufficient data: {len(df)} samples (need >= 20)")

    from app.features.feature_schema import FEATURE_COLUMNS
    available = [c for c in FEATURE_COLUMNS if c in df.columns]
    X = df[available].fillna(-1).astype(float)

    mlflow_uri = os.getenv("MLFLOW_TRACKING_URI", "http://mlflow:5000")
    mlflow.set_tracking_uri(mlflow_uri)
    mlflow.set_experiment("fraud_detector")

    with mlflow.start_run(run_name=f"fraud_detector_iforest_{feature_version}") as run:
        mlflow.log_param("feature_version", feature_version)
        mlflow.log_param("n_samples", len(X))
        mlflow.log_param("algorithm", "IsolationForest")

        contamination = 0.05  # Assume ~5% of properties are fraudulent
        model = Pipeline([
            ("scaler", StandardScaler()),
            ("iforest", IsolationForest(
                n_estimators=200,
                contamination=contamination,
                random_state=42,
                n_jobs=-1,
            ))
        ])
        model.fit(X)

        scores = model.decision_function(X)
        anomaly_count = int((model.predict(X) == -1).sum())
        mlflow.log_metric("anomaly_count", anomaly_count)
        mlflow.log_metric("anomaly_rate", anomaly_count / len(X))
        mlflow.sklearn.log_model(model, "model")

        artifact_path = os.path.join(
            os.getenv("MODEL_ARTIFACT_PATH", "/app/models"),
            "fraud_detector_production.pkl"
        )
        joblib.dump(model, artifact_path)

        return {
            "mlflow_run_id": run.info.run_id,
            "metrics": {"anomaly_count": anomaly_count, "anomaly_rate": anomaly_count / len(X)},
            "artifact_path": artifact_path,
        }


if __name__ == "__main__":
    result = train_fraud_model(os.environ["DATABASE_URL"])
    print("Training complete:", result)
