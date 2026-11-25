"""Internal HTTP adapter for the frozen Văn Úc historical forecast bundle."""

from contextlib import asynccontextmanager
from dataclasses import dataclass
from datetime import datetime, timedelta
from hashlib import sha256
import json
from math import isfinite
import os
from pathlib import Path
import sys

from fastapi import FastAPI, HTTPException, Request
import pandas as pd
from pydantic import BaseModel, ConfigDict, Field, FiniteFloat
from pydantic.alias_generators import to_camel
from xgboost import XGBRegressor, __version__ as xgboost_version


VERSION = "backtest-2022-h2"
HORIZONS = (1, 3, 6)
HISTORY_HOURS = 48
INPUTS = (
    "water_level_trung_trang_raw",
    "water_level_hon_dau_raw",
    "discharge_trung_trang_raw",
)


class WireModel(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, extra="forbid")


class Hour(WireModel):
    timestamp_local: datetime
    water_level_trung_trang_raw: FiniteFloat
    water_level_hon_dau_raw: FiniteFloat
    discharge_trung_trang_raw: FiniteFloat


class PredictionRequest(WireModel):
    issue_time_local: datetime
    manifest_hash: str
    history: list[Hour] = Field(min_length=HISTORY_HOURS, max_length=HISTORY_HOURS)


@dataclass(frozen=True)
class HorizonModel:
    horizon: int
    artifact: str
    rounds: int
    estimator: XGBRegressor


class Bundle:
    def __init__(self, ml_root: Path):
        root = ml_root / "runs" / "van_uc_trung_trang" / VERSION
        manifest_bytes = (root / "manifest.json").read_bytes()
        manifest = json.loads(manifest_bytes)
        self.config = manifest["config"]
        self.manifest_hash = sha256(manifest_bytes).hexdigest()
        self.threshold = float(manifest["high_water_threshold_raw"])
        self.features = [
            f"{column}_lag_{lag}h"
            for column in INPUTS
            for lag in range(HISTORY_HOURS)
        ]
        if (self.config["station_id"] != "van_uc_trung_trang"
                or tuple(self.config["inputs"]) != INPUTS
                or self.config["history_hours"] != HISTORY_HOURS
                or tuple(self.config["horizons_hours"]) != HORIZONS
                or manifest["features_in_order"] != self.features
                or manifest["xgboost_version"] != xgboost_version
                or self.threshold != 142.0):
            raise ValueError("Unsupported Văn Úc manifest or XGBoost runtime")

        sys.path.insert(0, str(ml_root))
        from forecast_data import lag_features

        self.lag_features = lag_features
        self.models = [self._load_model(root, manifest, horizon) for horizon in HORIZONS]

    def _load_model(self, root: Path, manifest: dict, horizon: int) -> HorizonModel:
        run = manifest["runs"][str(horizon)]
        artifact = f"xgboost_{horizon}h.ubj"
        if run["model_file"] != artifact:
            raise ValueError(f"Unexpected artifact for horizon {horizon}")
        model_bytes = (root / artifact).read_bytes()
        if sha256(model_bytes).hexdigest() != run["model_sha256"]:
            raise ValueError(f"Artifact checksum mismatch for horizon {horizon}")

        estimator = XGBRegressor(n_jobs=1)
        estimator.load_model(bytearray(model_bytes))
        rounds = run["xgboost_selected"]["best_rounds"]
        if (estimator.get_booster().feature_names != self.features
                or estimator.best_iteration + 1 != rounds):
            raise ValueError(f"Artifact feature or iteration mismatch for horizon {horizon}")
        return HorizonModel(horizon, artifact, rounds, estimator)

    def metadata(self) -> dict:
        return {
            "version": VERSION,
            "manifestHash": self.manifest_hash,
            "thresholdValueRaw": self.threshold,
            "models": [
                {"horizonHours": model.horizon, "artifactPath": model.artifact}
                for model in self.models
            ],
        }

    def predict(self, request: PredictionRequest) -> dict:
        if request.manifest_hash != self.manifest_hash:
            raise HTTPException(status_code=409, detail="Forecast bundle has changed")
        origin = request.issue_time_local
        if origin.tzinfo is not None or origin != origin.replace(minute=0, second=0, microsecond=0):
            raise HTTPException(status_code=422, detail="Expected a source-local issue hour")
        if origin < datetime.min + timedelta(hours=HISTORY_HOURS - 1):
            raise HTTPException(status_code=422, detail="Issue hour is outside the supported range")
        for index, row in enumerate(request.history):
            expected = origin - timedelta(hours=HISTORY_HOURS - 1 - index)
            if row.timestamp_local != expected:
                raise HTTPException(status_code=422, detail="Expected 48 consecutive source-local hours")

        hourly = pd.DataFrame([{name: getattr(row, name) for name in INPUTS} for row in request.history])
        features = self.lag_features(hourly, self.config).iloc[[-1]]
        if not all(isfinite(value) for value in features.iloc[0]):
            raise HTTPException(status_code=422, detail="Input exceeds the supported feature scale")
        predictions = []
        for model in self.models:
            value = float(model.estimator.predict(features, iteration_range=(0, model.rounds))[0])
            if not isfinite(value):
                raise HTTPException(status_code=503, detail="Non-finite model result")
            predictions.append({"horizonHours": model.horizon, "predictedWaterLevelRaw": value})
        return {
            "version": VERSION,
            "manifestHash": self.manifest_hash,
            "predictions": predictions,
        }


@asynccontextmanager
async def lifespan(app: FastAPI):
    ml_root = Path(os.environ.get("FLOODAI_ML_DIR", Path(__file__).resolve().parents[2] / "ml")).resolve()
    app.state.bundle = Bundle(ml_root)
    yield


app = FastAPI(lifespan=lifespan, docs_url=None, redoc_url=None, openapi_url=None)


@app.get("/bundle")
def get_bundle(request: Request):
    return request.app.state.bundle.metadata()


@app.post("/predict")
def predict(payload: PredictionRequest, request: Request):
    return request.app.state.bundle.predict(payload)
