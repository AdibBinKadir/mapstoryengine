"""The Sphinx's voice: ElevenLabs text-to-speech, returned as a WAV the phone can always play.

Any failure (no key, HTTP error, out of time) returns None: the phone then shows subtitles only.
"""
import io
import logging
import time
import wave

import httpx

from . import config

log = logging.getLogger("tts")

ELEVENLABS_TTS = "https://api.elevenlabs.io/v1/text-to-speech/%s?output_format=pcm_%d"
RATE = 16000

_cache = {}  # text -> WAV bytes, for fixed lines such as the reveal


def pcm_to_wav(pcm, rate=RATE):
    """16-bit little-endian mono PCM -> WAV bytes."""
    buf = io.BytesIO()
    with wave.open(buf, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(pcm)
    return buf.getvalue()


def speak_wav(text, deadline, cache=False):
    """WAV bytes of `text` in the Sphinx's voice, or None. `cache` keeps the result for identical text."""
    text = (text or "").strip()
    s = config.settings
    if not text or not s.elevenlabs_api_key or not s.elevenlabs_voice_id:
        return None
    if cache and text in _cache:
        return _cache[text]
    timeout = min(s.tts_timeout_s, deadline - time.monotonic() - 0.1)
    if timeout < 0.5:
        log.warning("tts: out of time")
        return None
    try:
        r = httpx.post(
            ELEVENLABS_TTS % (s.elevenlabs_voice_id, RATE),
            headers={"xi-api-key": s.elevenlabs_api_key},
            json={"text": text, "model_id": s.elevenlabs_model},
            timeout=timeout,
        )
        r.raise_for_status()
    except httpx.HTTPStatusError as e:
        log.warning("tts: ElevenLabs HTTP %s", e.response.status_code)
        return None
    except httpx.HTTPError as e:
        log.warning("tts: %r", e)
        return None
    wav = pcm_to_wav(r.content)
    if cache:
        _cache[text] = wav
    return wav
