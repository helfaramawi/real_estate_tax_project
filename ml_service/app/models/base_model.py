import os
import joblib
import shap
import numpy as np
import pandas as pd
from abc import ABC, abstractmethod
from app.features.feature_schema import FEATURE_COLUMNS


class BaseMLModel(ABC):
    MODEL_DIR = os.getenv("MODEL_ARTIFACT_PATH", "/app/models")

    def __init__(self, model, model_name: str):
        self.model = model
        self.model_name = model_name
        self.explainer = None

    @classmethod
    def load_production(cls):
        path = os.path.join(cls.MODEL_DIR, f"{cls.MODEL_NAME}_production.pkl")
        model_obj = joblib.load(path)
        instance = cls.__new__(cls)
        instance.model = model_obj
        instance.model_name = cls.MODEL_NAME
        instance._init_explainer()
        return instance

    def _init_explainer(self):
        try:
            self.explainer = shap.TreeExplainer(self.model)
        except Exception:
            self.explainer = None

    def prepare_features(self, features_list: list[dict]) -> pd.DataFrame:
        df = pd.DataFrame(features_list)
        for col in FEATURE_COLUMNS:
            if col not in df.columns:
                df[col] = np.nan
        df["corporate_owner_flag"] = df.get("corporate_owner_flag", False).astype(float)
        df["multiple_owners_flag"] = df.get("multiple_owners_flag", False).astype(float)
        df["has_boundary_polygon"] = df.get("has_boundary_polygon", False).astype(float)
        return df[FEATURE_COLUMNS].fillna(-1)

    def get_top_shap_features(self, features: pd.DataFrame, shap_values: np.ndarray,
                               k: int = 5) -> dict[str, float]:
        abs_shap = np.abs(shap_values)
        top_idx = np.argsort(abs_shap)[-k:][::-1]
        return {
            features.columns[i]: float(shap_values[i])
            for i in top_idx
        }

    @abstractmethod
    def predict_with_explanation(self, features: list[dict]) -> list[dict]:
        pass
