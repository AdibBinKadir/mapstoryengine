"""One HTTPS origin for the Oracle and the Sphinx."""
import os
from threading import BoundedSemaphore

from a2wsgi import WSGIMiddleware
from fastapi import FastAPI
from starlette.responses import JSONResponse

from . import sphinx_api
from .main import OracleBudget, app as oracle_app

app = FastAPI(title="Odyssey Venue Server", docs_url=None, redoc_url=None, openapi_url=None)
app.include_router(sphinx_api.router)
sphinx_budget = OracleBudget()
sphinx_slots = BoundedSemaphore(4)


@app.middleware("http")
async def venue_requests(request, call_next):
    path = request.url.path.rstrip("/")
    if path == "/frame" and os.environ.get("ODYSSEY_ENABLE_FRAME") != "1":
        return JSONResponse({"error": "not_found"}, status_code=404)
    if request.method != "POST" or path not in ("/sphinx", "/sphinx/talk"):
        return await call_next(request)

    if not sphinx_slots.acquire(blocking=False):
        return JSONResponse({"error": "busy"}, status_code=429, headers={"Retry-After": "60"})
    try:
        client = request.client.host if request.client else ""
        if not sphinx_budget.reserve(client):
            return JSONResponse({"error": "busy"}, status_code=429, headers={"Retry-After": "60"})
        return await call_next(request)
    finally:
        sphinx_slots.release()


app.mount("/", WSGIMiddleware(oracle_app))
