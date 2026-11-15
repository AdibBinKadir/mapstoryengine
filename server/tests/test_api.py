"""The HTTP contract (docs/CONTRACTS.md section A), in FAKE mode and with the network mocked."""
import io
import json
import wave

import base64

from server import sphinx, stt, tts

STATE = {"event": "intro", "hp": 40, "max_hp": 100, "deaths": 2, "last_event": "stoned",
         "riddle_index": 0, "correct_count": 0}


def wav_bytes(seconds=1.0):
    buf = io.BytesIO()
    with wave.open(buf, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(16000)
        w.writeframes(b"\x00\x00" * int(16000 * seconds))
    return buf.getvalue()


def talk(client, **extra):
    data = {"state": json.dumps(STATE)}
    files = None
    if "text" in extra:
        data["text"] = extra["text"]
    else:
        files = {"audio": ("speech.wav", wav_bytes(), "audio/wav")}
    return client.post("/sphinx/talk", data=data, files=files)


def test_health(client):
    assert client.get("/health").json() == {"ok": True, "fake": True}


def test_frame_fake_shape(client):
    body = client.post("/frame", content=b"\xff\xd8 fake jpeg", headers={"Content-Type": "image/jpeg"}).json()
    assert set(body) == {"face_found", "looking_at_screen", "yaw", "pitch", "ts"}


def test_frame_rejects_empty_body(client):
    assert client.post("/frame", content=b"").status_code == 400


def test_sphinx_intro_has_riddle_and_index(client):
    body = client.post("/sphinx", json=dict(STATE, riddle_index=-1)).json()
    assert body["line"] and body["riddle"]
    assert body["riddle"] == sphinx.RIDDLES[body["riddle_index"]]["question"]


def test_sphinx_taunt_has_no_riddle(client):
    body = client.post("/sphinx", json=dict(STATE, event="taunt")).json()
    assert body["line"] and body["riddle"] is None


def test_sphinx_accepts_unity_style_state(client):
    # JsonUtility sends every field, with "" for empty strings.
    assert client.post("/sphinx", json=dict(STATE, last_event="")).status_code == 200


def test_talk_fake_cycles_through_all_three_outcomes(client):
    verdicts = [talk(client).json()["verdict"] for _ in range(3)]
    assert sorted(verdicts) == ["correct", "none", "wrong"]


def test_talk_reply_shape(client):
    body = talk(client, text="hello").json()
    assert set(body) >= {"heard", "line", "riddle", "riddle_index", "answer_correct", "verdict", "reveal"}


def test_talk_typed_text(client):
    body = talk(client, text="is it a humman").json()
    assert body["heard"] == "is it a humman" and body["verdict"] == "correct" and body["reveal"] is False


def test_talk_needs_audio_or_text(client):
    assert client.post("/sphinx/talk", data={"state": json.dumps(STATE)}).status_code == 400


def test_talk_rejects_bad_state(client):
    r = client.post("/sphinx/talk", data={"state": "not json", "text": "hi"})
    assert r.status_code == 400


def test_talk_real_mode_stt_then_llm(client, real_mode, monkeypatch):
    monkeypatch.setattr(stt, "transcribe", lambda wav, deadline: "is it a man")
    monkeypatch.setattr(sphinx, "call_gemini", lambda prompt, timeout: {
        "line": "Ugh. Fine.", "answer_attempted": True, "answer_correct": True, "repeat_riddle": False})
    body = talk(client).json()
    assert body["heard"] == "is it a man" and body["line"] == "Ugh. Fine." and body["verdict"] == "correct"
    assert body["riddle_index"] != 0 and body["riddle"] == sphinx.RIDDLES[body["riddle_index"]]["question"]


def test_talk_real_mode_survives_stt_and_llm_failure(client, real_mode, monkeypatch):
    def broken_stt(wav, deadline):
        raise stt.SttError("down")

    def broken_llm(prompt, timeout):
        raise sphinx.LlmError("down")

    monkeypatch.setattr(stt, "transcribe", broken_stt)
    monkeypatch.setattr(sphinx, "call_gemini", broken_llm)
    body = talk(client).json()
    assert body["heard"] == "" and body["line"] in sphinx.DEAF_LINES and body["verdict"] == "none"


def test_replies_carry_null_audio_without_tts(client):
    assert client.post("/sphinx", json=STATE).json()["audio"] is None
    assert talk(client, text="hello").json()["audio"] is None


def test_replies_carry_base64_wav_and_speak_line_plus_riddle(client, monkeypatch):
    spoken = []

    def fake_tts(text, deadline, cache=False):
        spoken.append((text, cache))
        return b"RIFFfake"

    monkeypatch.setattr(tts, "speak_wav", fake_tts)
    body = client.post("/sphinx", json=dict(STATE, riddle_index=-1)).json()
    assert base64.b64decode(body["audio"]) == b"RIFFfake"
    assert spoken[-1] == (body["line"] + " " + body["riddle"], False)


def test_reveal_audio_uses_the_cache(client, monkeypatch):
    spoken = []
    monkeypatch.setattr(tts, "speak_wav", lambda text, deadline, cache=False: spoken.append((text, cache)) or b"RIFF")
    state = dict(STATE, correct_count=1)
    body = client.post("/sphinx/talk", data={"state": json.dumps(state), "text": "a man"}).json()
    assert body["reveal"] is True and spoken[-1] == (sphinx.REVEAL_LINE, True)
