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
