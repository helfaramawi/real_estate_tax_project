import numpy as np
from sklearn.ensemble import IsolationForest
from app.models.base_model import BaseMLModel


class FraudDetector(BaseMLModel):
    MODEL_NAME = "fraud_detector"

    def predict_with_explanation(self, features_list: list[dict]) -> list[dict]:
        df = self.prepare_features(features_list)

        # IsolationForest returns -1 (anomaly) or 1 (normal)
        # decision_function returns the anomaly score (lower = more anomalous)
        raw_scores = self.model.decision_function(df)
        # Normalize to [0, 1] where 1 = high fraud probability
        min_s, max_s = raw_scores.min(), raw_scores.max()
        if max_s > min_s:
            normalized = 1.0 - (raw_scores - min_s) / (max_s - min_s)
        else:
            normalized = np.full(len(raw_scores), 0.5)

        results = []
        for i, score in enumerate(normalized):
            results.append({
                "score": float(np.clip(score, 0.0, 1.0)),
                "label": "Suspect" if score >= 0.6 else "Clean",
                "confidence": float(abs(score - 0.5) * 2),
                "explanation": {},  # IsolationForest has no per-feature SHAP
            })
        return results
