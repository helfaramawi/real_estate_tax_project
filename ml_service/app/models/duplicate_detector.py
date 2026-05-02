import os
import logging
from pathlib import Path

import joblib
import numpy as np

from app.models.base_model import BaseMLModel

logger = logging.getLogger(__name__)

ARTIFACT_PATH = Path(os.getenv("MODEL_ARTIFACT_PATH", "/app/models"))


class DuplicateDetector(BaseMLModel):
    """
    Detects potentially duplicate property registrations.

    Two modes:
      1. Rule-based (default / cold-start): scores properties solely on
         nearest-neighbour proximity — properties < 30 m from another record
         are flagged as probable duplicates.
      2. ML-based: an Isolation Forest trained on spatial + area features;
         loaded from disk when a trained artefact is present.
    """

    ARTIFACT_FILE = ARTIFACT_PATH / "duplicate_detector_production.pkl"

    def __init__(self, pipeline=None):
        self._pipeline = pipeline   # sklearn Pipeline or None → rule-based mode

    # ── Lifecycle ──────────────────────────────────────────────────────────────

    @classmethod
    def load_production(cls) -> "DuplicateDetector":
        if not cls.ARTIFACT_FILE.exists():
            raise FileNotFoundError(f"No trained artefact at {cls.ARTIFACT_FILE}")
        pipeline = joblib.load(cls.ARTIFACT_FILE)
        logger.info("Duplicate detector loaded from %s", cls.ARTIFACT_FILE)
        return cls(pipeline=pipeline)

    # ── Inference ──────────────────────────────────────────────────────────────

    def predict_with_explanation(self, features_list: list[dict]) -> list[dict]:
        if self._pipeline is not None:
            return self._ml_predict(features_list)
        return self._rule_based_predict(features_list)

    # ── Rule-based scoring ─────────────────────────────────────────────────────

    @staticmethod
    def _rule_based_predict(features_list: list[dict]) -> list[dict]:
        results = []
        for f in features_list:
            nn_dist = f.get("nearest_neighbor_distance_m")

            if nn_dist is None or nn_dist < 0:
                # No spatial data — cannot assess; return neutral score
                score, confidence = 0.1, 0.4
            elif nn_dist < 5:
                score, confidence = 0.95, 0.90
            elif nn_dist < 15:
                score, confidence = 0.80, 0.85
            elif nn_dist < 30:
                score, confidence = 0.55, 0.75
            elif nn_dist < 60:
                score, confidence = 0.25, 0.70
            elif nn_dist < 100:
                score, confidence = 0.10, 0.65
            else:
                score, confidence = 0.02, 0.80

            if score >= 0.7:
                label = "Duplicate"
            elif score >= 0.35:
                label = "Investigate"
            else:
                label = "Unique"

            results.append({
                "score": score,
                "label": label,
                "confidence": confidence,
                "explanation": {
                    "nearest_neighbor_distance_m": float(nn_dist) if nn_dist is not None else -1.0,
                    "mode": 0.0,   # 0 = rule-based, 1 = ml
                },
            })
        return results

    # ── ML-based scoring ───────────────────────────────────────────────────────

    # Features that correlate with duplicate registrations:
    # properties with abnormally small nearest-neighbour distance and similar
    # built-up area compared to their immediate neighbours are duplicates.
    ML_FEATURES = [
        "nearest_neighbor_distance_m",
        "built_up_area",
        "land_area",
        "neighbors_within100m",
    ]

    def _ml_predict(self, features_list: list[dict]) -> list[dict]:
        X = self.prepare_features(features_list, self.ML_FEATURES)

        # IsolationForest: decision_function returns negative for anomalies.
        # We negate and normalise to [0, 1].
        raw = self._pipeline.decision_function(X)
        # Typical range is roughly [-0.5, 0.5]; clip and normalise
        normed = np.clip(-raw, -0.5, 0.5)
        scores = (normed + 0.5)           # shift to [0, 1]

        results = []
        for i, f in enumerate(features_list):
            score = float(scores[i])
            label = "Duplicate" if score >= 0.7 else ("Investigate" if score >= 0.4 else "Unique")
            nn_dist = f.get("nearest_neighbor_distance_m", -1) or -1
            results.append({
                "score": score,
                "label": label,
                "confidence": 0.75,
                "explanation": {
                    "nearest_neighbor_distance_m": float(nn_dist),
                    "anomaly_raw": float(raw[i]),
                    "mode": 1.0,   # 1 = ml
                },
            })
        return results
