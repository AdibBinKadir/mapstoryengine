"""Odyssey AR laptop server. API: docs/CONTRACTS.md section A.

Run: python -m server [--fake]
"""
import base64
import json
import logging
import time
from typing import Optional

from fastapi import APIRouter, FastAPI, File, Form, HTTPException, Request, UploadFile
from pydantic import BaseModel

from . import config, sphinx, stt, tts
from .gaze import GazeDetector, GazeUnavailable

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(name)s %(levelname)s %(message)s")
log = logging.getLogger("server")

MAX_FRAME_BYTES = 2 * 1024 * 1024
MAX_AUDIO_BYTES = 4 * 1024 * 1024  # 8 s of 16 kHz mono 16-bit is ~256 KB

router = APIRouter()
_gaze = None


class SphinxState(BaseModel):
    event: str = "intro"
    hp: int = 100
    max_hp: int = 100
    deaths: int = 0
    last_event: Optional[str] = None
    riddle_index: int = -1      # the current riddle in sphinx.RIDDLES; -1 before the intro
    correct_count: int = 0      # correct answers so far (two -> the reveal)
    riddle_answer: str = ""     # legacy, ignored


def _detector():
    global _gaze
    if _gaze is None:
        s = config.settings
        _gaze = GazeDetector(s.gaze_yaw_max, s.gaze_pitch_max, s.gaze_smooth_frames)
    return _gaze


def _deadline():
    return time.monotonic() + config.settings.request_budget_s


def _with_voice(reply, deadline):
    """Adds `audio`: base64 WAV of what she says (line, then the riddle), or None."""
    reply["speechInput"] = bool(stt.providers()) or config.settings.fake
    if reply.get("reveal"):
        wav = tts.speak_wav(reply["line"], deadline, cache=True)
    else:
        wav = tts.speak_wav(" ".join(t for t in (reply.get("line"), reply.get("riddle")) if t), deadline)
    reply["audio"] = base64.b64encode(wav).decode("ascii") if wav else None
    return reply


@router.get("/health")
def health():
    return {"ok": True, "fake": config.settings.fake}


@router.post("/frame")
async def frame(request: Request):
    body = await request.body()
    if not body:
        raise HTTPException(400, "send raw JPEG bytes as the body (Content-Type: image/jpeg)")
    if len(body) > MAX_FRAME_BYTES:
        raise HTTPException(413, "frame too large; send ~320x240 JPEG")
    if config.settings.fake:
        # Looks at the screen for 1 s out of every 5 s, so Medusa's STONED path can be tested end to end.
        now = time.time()
        looking = now % 5 < 1
        return {"face_found": True, "looking_at_screen": looking,
                "yaw": 0.0 if looking else 35.0, "pitch": 0.0, "ts": round(now, 2)}
    try:
        return _detector().analyse(body)
    except ValueError as e:
        raise HTTPException(400, str(e))
    except GazeUnavailable as e:
        raise HTTPException(503, str(e))


@router.post("/sphinx")
def sphinx_speak(state: SphinxState):
    started = time.monotonic()
    data = state.model_dump()
    deadline = _deadline()
    reply = sphinx.fake_speak(data) if config.settings.fake else sphinx.speak(data, deadline)
    _with_voice(reply, deadline)
    log.info("/sphinx %s -> %r (%.1fs)", state.event, reply["line"], time.monotonic() - started)
    return reply


@router.post("/sphinx/talk")
def sphinx_talk(
    state: str = Form(...),
    audio: Optional[UploadFile] = File(None),
    text: Optional[str] = Form(None),
):
    """`text` (optional, not in the contract yet) skips speech-to-text: the typed debug-box fallback."""
    started = time.monotonic()
    try:
        data = SphinxState(**json.loads(state)).model_dump()
    except (ValueError, TypeError) as e:
        raise HTTPException(400, "`state` must be the /sphinx JSON as a string: %s" % e)
    wav = audio.file.read(MAX_AUDIO_BYTES + 1) if audio is not None else b""
    if len(wav) > MAX_AUDIO_BYTES:
        raise HTTPException(413, "audio too long; send <= ~8 s of 16 kHz mono WAV")
    if text is None and not wav:
        raise HTTPException(400, "send `audio` (WAV) or `text`")
    if text is not None and len(text.strip()) > 280:
        raise HTTPException(400, "send an answer of at most 280 characters")

    deadline = _deadline()
    if config.settings.fake:
        reply = sphinx.fake_talk(data, text)
    else:
        heard = text
        if heard is None:
            try:
                heard = stt.transcribe(wav, deadline)
            except stt.SttError as e:
                log.warning("speech-to-text failed: %s", e)
                heard = ""
        reply = sphinx.talk(heard, data, deadline)
    _with_voice(reply, deadline)
    log.info("/sphinx/talk verdict=%s reveal=%s (%.1fs)", reply["verdict"], reply["reveal"], time.monotonic() - started)
    return reply


app = FastAPI(title="Odyssey AR server")
app.include_router(router)
