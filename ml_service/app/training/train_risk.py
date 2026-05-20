"""
Train XGBoost risk scoring model on feature store data.
Usage: python -m app.training.train_risk
"""
import os
import joblib
import pandas as pd
import xgboost as xgb
import mlflow
import mlflow.xgboost
import psycopg2
import shap
import numpy as np
from sklearn.model_selection import StratifiedKFold


def train_risk_model(db_url: str, feature_version: str = "v1") -> dict:
    conn = psycopg2.connect(db_url)

    df = pd.read_sql("""
        SELECT
            f.*,
            CASE WHEN r.score >= 70 THEN 1 ELSE 0 END AS high_risk_label
        FROM intel.feature_vectors f
        JOIN public.risk_scores r ON r.property_id = f.property_id
        WHERE f.feature_version = %(version)s
          AND r.score IS NOT NULL
    """, conn, params={"version": feature_version})

    conn.close()

    if len(df) < 50:
        raise ValueError(f"Insufficient training data: {len(df)} samples (need >= 50)")

    from app.features.feature_schema import FEATURE_COLUMNS
    available = [c for c in FEATURE_COLUMNS if c in df.columns]
    X = df[available].fillna(-1).astype(float)
    y = df["high_risk_label"]

    mlflow_uri = os.getenv("MLFLOW_TRACKING_URI", "http://mlflow:5000")
    mlflow.set_tracking_uri(mlflow_uri)
    mlflow.set_experiment("risk_scorer")

    with mlflow.start_run(run_name=f"risk_scorer_{feature_version}") as run:
        mlflow.log_param("feature_version", feature_version)
        mlflow.log_param("n_samples", len(X))
        mlflow.log_param("n_features", len(available))

        pos_weight = float((y == 0).sum()) / max(float((y == 1).sum()), 1)
        params = {
            "objective": "binary:logistic",
            "eval_metric": "auc",
            "max_depth": 6,
            "eta": 0.05,
            "subsample": 0.8,
            "colsample_bytree": 0.8,
            "scale_pos_weight": pos_weight,
            "tree_method": "hist",
            "seed": 42,
        }
        mlflow.log_params(params)

        cv = StratifiedKFold(n_splits=5, shuffle=True, random_state=42)
        dtrain = xgb.DMatrix(X, label=y, feature_names=available)
        cv_results = xgb.cv(
            params, dtrain, num_boost_round=500,
            folds=cv, early_stopping_rounds=20, verbose_eval=False
        )

        best_rounds = int(cv_results["test-auc-mean"].idxmax())
        best_auc = float(cv_results["test-auc-mean"].max())
        mlflow.log_metric("cv_auc", best_auc)

        final_model = xgb.train(params, dtrain, num_boost_round=best_rounds)

        # SHAP feature importance
        explainer = shap.TreeExplainer(final_model)
        sample = X.sample(min(500, len(X)), random_state=42)
        sv = explainer.shap_values(sample)
        importance = dict(zip(available, np.abs(sv).mean(axis=0).tolist()))
        mlflow.log_dict(importance, "feature_importance.json")

        mlflow.xgboost.log_model(final_model, "model")

        # Save production artifact locally
        artifact_path = os.path.join(
            os.getenv("MODEL_ARTIFACT_PATH", "/app/models"),
            "risk_scorer_production.pkl"
        )
        joblib.dump(final_model, artifact_path)

        return {
            "mlflow_run_id": run.info.run_id,
            "metrics": {"auc": best_auc},
            "feature_importance": importance,
            "best_rounds": best_rounds,
            "artifact_path": artifact_path,
        }


if __name__ == "__main__":
    result = train_risk_model(os.environ["DATABASE_URL"])
    print("Training complete:", result)
