import os

# Before server.config is imported: fake mode, and blank keys so a developer's server/.env
# never makes the tests call real APIs (load_dotenv doesn't override variables that are set).
os.environ["FAKE"] = "1"
for name in ("GEMINI_API_KEY", "GROQ_API_KEY", "OPENAI_API_KEY", "ELEVENLABS_API_KEY", "ELEVENLABS_VOICE_ID"):
    os.environ[name] = ""

import pytest  # noqa: E402
from fastapi.testclient import TestClient  # noqa: E402

from server import config, sphinx_api  # noqa: E402


@pytest.fixture
def client():
    return TestClient(sphinx_api.app)


@pytest.fixture
def real_mode(monkeypatch):
    """Non-fake mode with fake keys; tests monkeypatch the network calls themselves."""
    monkeypatch.setattr(config.settings, "fake", False)
    monkeypatch.setattr(config.settings, "gemini_api_key", "test")
    yield
