"""Speech-to-text for /sphinx/talk: ElevenLabs Scribe first, then Groq-hosted Whisper, then OpenAI Whisper.

Each provider is used only if its key is set in server/.env.
"""
import time

import httpx

from . import config

GROQ_URL = "https://api.groq.com/openai/v1/audio/transcriptions"
OPENAI_URL = "https://api.openai.com/v1/audio/transcriptions"
ELEVENLABS_URL = "https://api.elevenlabs.io/v1/speech-to-text"
ELEVENLABS_STT_MODEL = "scribe_v1"


class SttError(RuntimeError):
    pass


def providers():
    s = config.settings
    out = []
    if s.elevenlabs_api_key:
        out.append(("elevenlabs", ELEVENLABS_URL, s.elevenlabs_api_key, ELEVENLABS_STT_MODEL))
    if s.groq_api_key:
        out.append(("groq", GROQ_URL, s.groq_api_key, s.groq_stt_model))
    if s.openai_api_key:
        out.append(("openai", OPENAI_URL, s.openai_api_key, s.openai_stt_model))
    return out


def _post_elevenlabs(url, key, model, wav, timeout):
    r = httpx.post(
        url,
        headers={"xi-api-key": key},
        files={"file": ("speech.wav", wav, "audio/wav")},
        data={"model_id": model, "language_code": "en"},
        timeout=timeout,
    )
    r.raise_for_status()
    return (r.json().get("text") or "").strip()


def _post(url, key, model, wav, timeout):
    r = httpx.post(
        url,
        headers={"Authorization": "Bearer " + key},
        files={"file": ("speech.wav", wav, "audio/wav")},
        data={"model": model, "language": "en", "temperature": "0", "response_format": "json"},
        timeout=timeout,
    )
    r.raise_for_status()
    return (r.json().get("text") or "").strip()


def transcribe(wav, deadline):
    """Transcript of a WAV, trying each configured provider until `deadline` (time.monotonic())."""
    chain = providers()
    if not chain:
        raise SttError("no speech-to-text key: set ELEVENLABS_API_KEY, GROQ_API_KEY or OPENAI_API_KEY in server/.env")
    errors = []
    for name, url, key, model in chain:
        remaining = deadline - time.monotonic()
        if remaining < 0.5:
            errors.append("%s: out of time" % name)
            break
        try:
            post = _post_elevenlabs if name == "elevenlabs" else _post
            return post(url, key, model, wav, min(config.settings.stt_timeout_s, remaining))
        except Exception as e:
            errors.append("%s: %s" % (name, e))
    raise SttError("; ".join(errors))
