import numpy as np
from app.models.base_model import BaseMLModel


class RiskScorer(BaseMLModel):
    MODEL_NAME = "risk_scorer"

    def predict_with_explanation(self, features_list: list[dict]) -> list[dict]:
        df = self.prepare_features(features_list)

        try:
            import xgboost as xgb
            dmatrix = xgb.DMatrix(df)
            scores = self.model.predict(dmatrix)
        except Exception:
            scores = self.model.predict_proba(df)[:, 1]

        results = []
        for i, score in enumerate(scores):
            explanation = {}
            if self.explainer is not None:
                try:
                    sv = self.explainer.shap_values(df.iloc[[i]])
                    if isinstance(sv, list):
                        sv = sv[1]
                    explanation = self.get_top_shap_features(df, sv[0])
                except Exception:
                    pass

            results.append({
                "score": float(np.clip(score, 0.0, 1.0)),
                "label": _score_to_label(float(score)),
                "confidence": float(min(abs(score - 0.5) * 2, 1.0)),
                "explanation": explanation,
            })
        return results


def _score_to_label(score: float) -> str:
    if score >= 0.75:
        return "High"
    if score >= 0.45:
        return "Medium"
    return "Low"
