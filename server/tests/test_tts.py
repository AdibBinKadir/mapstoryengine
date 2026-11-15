"""ElevenLabs text-to-speech: raw PCM wrapped as WAV, and every failure is just 'no audio'."""
import io
import struct
import time
import wave

import httpx
import pytest

from server import config, tts


def deadline():
    return time.monotonic() + 7.5


@pytest.fixture
def keyed(monkeypatch):
    monkeypatch.setattr(config.settings, "elevenlabs_api_key", "test-key")
    monkeypatch.setattr(config.settings, "elevenlabs_voice_id", "voice123")
    tts._cache.clear()
    yield
    tts._cache.clear()


class FakeResponse:
    def __init__(self, content=b"", status=200):
        self.content = content
        self.status_code = status

    def raise_for_status(self):
        if self.status_code >= 400:
            req = httpx.Request("POST", "https://example")
            raise httpx.HTTPStatusError("bad", request=req, response=httpx.Response(self.status_code, request=req))


def test_pcm_to_wav_header():
    data = tts.pcm_to_wav(b"\x01\x00\x02\x00", 16000)
    assert data[:4] == b"RIFF" and data[8:12] == b"WAVE"
    with wave.open(io.BytesIO(data)) as w:
        assert (w.getnchannels(), w.getsampwidth(), w.getframerate(), w.getnframes()) == (1, 2, 16000, 2)
    assert struct.unpack("<I", data[40:44])[0] == 4


def test_no_key_means_no_audio(monkeypatch):
    monkeypatch.setattr(config.settings, "elevenlabs_api_key", "")
    assert tts.speak_wav("Hello.", deadline()) is None


def test_speak_wav_posts_to_elevenlabs(keyed, monkeypatch):
    calls = []

    def fake_post(url, headers=None, json=None, timeout=None):
        calls.append((url, headers, json, timeout))
        return FakeResponse(b"\x00\x00" * 160)

    monkeypatch.setattr(tts.httpx, "post", fake_post)
    wav = tts.speak_wav("Wrong, little sailor.", deadline())
    url, headers, body, timeout = calls[0]
    assert url.endswith("/v1/text-to-speech/voice123?output_format=pcm_16000")
    assert headers["xi-api-key"] == "test-key"
    assert body == {"text": "Wrong, little sailor.", "model_id": config.settings.elevenlabs_model}
    assert timeout <= config.settings.tts_timeout_s
    with wave.open(io.BytesIO(wav)) as w:
        assert w.getnframes() == 160


def test_http_error_means_no_audio(keyed, monkeypatch):
    monkeypatch.setattr(tts.httpx, "post", lambda *a, **k: FakeResponse(status=401))
    assert tts.speak_wav("Hello.", deadline()) is None


def test_network_error_means_no_audio(keyed, monkeypatch):
    def boom(*a, **k):
        raise httpx.ConnectError("down")

    monkeypatch.setattr(tts.httpx, "post", boom)
    assert tts.speak_wav("Hello.", deadline()) is None


def test_out_of_time_means_no_audio(keyed, monkeypatch):
    monkeypatch.setattr(tts.httpx, "post", lambda *a, **k: pytest.fail("must not call"))
    assert tts.speak_wav("Hello.", time.monotonic() + 0.3) is None


def test_cached_text_is_spoken_once(keyed, monkeypatch):
    calls = []
    monkeypatch.setattr(tts.httpx, "post", lambda *a, **k: calls.append(1) or FakeResponse(b"\x00\x00"))
    first = tts.speak_wav("Not here.", deadline(), cache=True)
    assert tts.speak_wav("Not here.", deadline(), cache=True) == first and len(calls) == 1


def test_empty_text_means_no_audio(keyed):
    assert tts.speak_wav("  ", deadline()) is None
