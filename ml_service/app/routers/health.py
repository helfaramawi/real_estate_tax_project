from fastapi import APIRouter, Request
import os

router = APIRouter()


@router.get("")
async def health_check(request: Request):
    models = getattr(request.app.state, "models", {})
    return {
        "status": "healthy",
        "models_loaded": list(models.keys()),
        "environment": os.getenv("ENVIRONMENT", "development"),
    }
