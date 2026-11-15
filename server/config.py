"""Settings from environment variables, then server/.env (gitignored). See .env.example."""
import os
from dataclasses import dataclass
from pathlib import Path

try:
    from dotenv import load_dotenv

    # Real environment variables win over .env (load_dotenv never overrides them).
    load_dotenv(Path(__file__).with_name(".env"))
except ImportError:
    pass


def _str(name, default=""):
    return os.environ.get(name, default).strip()


def _flag(name):
    return _str(name, "0").lower() in ("1", "true", "yes", "on")


def _float(name, default):
    return float(_str(name, str(default)) or default)


@dataclass
class Settings:
    fake: bool
    gemini_api_key: str
    gemini_model: str
    gemini_thinking_budget: str
    groq_api_key: str
    groq_stt_model: str
    openai_api_key: str
    openai_stt_model: str
    request_budget_s: float
    stt_timeout_s: float
    llm_timeout_s: float
    gaze_yaw_max: float
    gaze_pitch_max: float
    gaze_smooth_frames: int


def load():
    return Settings(
        fake=_flag("FAKE"),
        gemini_api_key=_str("GEMINI_API_KEY"),
        gemini_model=_str("GEMINI_MODEL", "gemini-flash-latest") or "gemini-flash-latest",
        gemini_thinking_budget=_str("GEMINI_THINKING_BUDGET"),
        groq_api_key=_str("GROQ_API_KEY"),
        groq_stt_model=_str("GROQ_STT_MODEL", "whisper-large-v3-turbo") or "whisper-large-v3-turbo",
        openai_api_key=_str("OPENAI_API_KEY"),
        openai_stt_model=_str("OPENAI_STT_MODEL", "whisper-1") or "whisper-1",
        request_budget_s=_float("REQUEST_BUDGET_S", 7.5),
        stt_timeout_s=_float("STT_TIMEOUT_S", 4),
        llm_timeout_s=_float("LLM_TIMEOUT_S", 8),
        gaze_yaw_max=_float("GAZE_YAW_MAX", 20),
        gaze_pitch_max=_float("GAZE_PITCH_MAX", 20),
        gaze_smooth_frames=int(_float("GAZE_SMOOTH_FRAMES", 3)),
    )


settings = load()
