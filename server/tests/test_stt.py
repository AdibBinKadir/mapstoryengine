import time

import pytest

from server import config, stt


def test_no_keys_is_an_error():
    with pytest.raises(stt.SttError):
        stt.transcribe(b"wav", time.monotonic() + 5)


def test_groq_first_then_openai_fallback(monkeypatch):
    monkeypatch.setattr(config.settings, "groq_api_key", "g")
    monkeypatch.setattr(config.settings, "openai_api_key", "o")
    calls = []

    def fake_post(url, key, model, wav, timeout):
        calls.append(key)
        if key == "g":
            raise RuntimeError("groq down")
        return "central library"

    monkeypatch.setattr(stt, "_post", fake_post)
    assert stt.transcribe(b"wav", time.monotonic() + 5) == "central library"
    assert calls == ["g", "o"]


def test_groq_alone_is_enough(monkeypatch):
    monkeypatch.setattr(config.settings, "groq_api_key", "g")
    monkeypatch.setattr(stt, "_post", lambda url, key, model, wav, timeout: "hi")
    assert stt.transcribe(b"wav", time.monotonic() + 5) == "hi"


def test_provider_order_is_elevenlabs_groq_openai(monkeypatch):
    monkeypatch.setattr(config.settings, "elevenlabs_api_key", "e")
    monkeypatch.setattr(config.settings, "groq_api_key", "g")
    monkeypatch.setattr(config.settings, "openai_api_key", "o")
    assert [p[0] for p in stt.providers()] == ["elevenlabs", "groq", "openai"]


def test_elevenlabs_request_shape(monkeypatch):
    monkeypatch.setattr(config.settings, "elevenlabs_api_key", "e")
    seen = {}

    class Resp:
        status_code = 200

        def raise_for_status(self):
            pass

        def json(self):
            return {"text": " a man "}

    def fake_post(url, headers=None, files=None, data=None, timeout=None):
        seen.update(url=url, headers=headers, files=files, data=data)
        return Resp()

    monkeypatch.setattr(stt.httpx, "post", fake_post)
    assert stt.transcribe(b"RIFFwav", time.monotonic() + 5) == "a man"
    assert seen["url"] == "https://api.elevenlabs.io/v1/speech-to-text"
    assert seen["headers"] == {"xi-api-key": "e"}
    assert seen["data"] == {"model_id": "scribe_v1", "language_code": "en"}
    assert seen["files"]["file"][1] == b"RIFFwav"


def test_elevenlabs_failure_falls_back_to_groq(monkeypatch):
    monkeypatch.setattr(config.settings, "elevenlabs_api_key", "e")
    monkeypatch.setattr(config.settings, "groq_api_key", "g")

    def broken(url, key, model, wav, timeout):
        raise RuntimeError("elevenlabs down")

    monkeypatch.setattr(stt, "_post_elevenlabs", broken)
    monkeypatch.setattr(stt, "_post", lambda url, key, model, wav, timeout: "from groq")
    assert stt.transcribe(b"wav", time.monotonic() + 5) == "from groq"
