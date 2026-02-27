"""
Punto de entrada principal de la API FastAPI.

Para ejecutar:
    cd APIRest
    uvicorn main:app --host 0.0.0.0 --port 8000 --reload
"""

from fastapi import FastAPI
from config import API_TITLE, API_VERSION, API_DESCRIPTION
from routes import router as orders_router

app = FastAPI(
    title=API_TITLE,
    version=API_VERSION,
    description=API_DESCRIPTION,
)

# ── Registrar routers ──────────────────────────────────────────────────────
app.include_router(orders_router)


@app.get("/", tags=["Health"])
def health_check():
    """Endpoint de verificación de estado de la API."""
    return {"status": "ok", "message": "Zhoho-SAP Integration API is running."}
