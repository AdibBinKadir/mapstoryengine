"""WSGI proxy for the Unity Oracle. Run locally with python -m server.main."""

from collections import deque
import json
from threading import BoundedSemaphore, Lock
from time import monotonic
from wsgiref.simple_server import WSGIRequestHandler, make_server

from .oracle import FACTS, REFUSAL, OracleUnavailable, answer, classify, gemini


class OracleBudget:
    def __init__(self):
        self.calls = deque()
        self.lock = Lock()

    def reserve(self, client):
        with self.lock:
            now = monotonic()
            while self.calls and self.calls[0][0] <= now - 60:
                self.calls.popleft()
            if len(self.calls) >= 30 or sum(ip == client for _, ip in self.calls) >= 12:
                return False
            self.calls.append((now, client))
            return True


def create_app(generate=gemini):
    budget = OracleBudget()
    slots = BoundedSemaphore(4)

    def application(environ, start_response):
        def respond(status, payload, headers=()):
            body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
            start_response(status, [("Content-Type", "application/json; charset=utf-8"),
                                    ("Cache-Control", "no-store"), ("Content-Length", str(len(body))), *headers])
            return [body]

        if environ["PATH_INFO"] != "/oracle":
            return respond("404 Not Found", {"error": "not_found"})
        if environ["REQUEST_METHOD"] != "POST":
            return respond("405 Method Not Allowed", {"error": "method"}, [("Allow", "POST")])
        if environ.get("CONTENT_TYPE", "").split(";")[0].strip().lower() != "application/json":
            return respond("415 Unsupported Media Type", {"error": "content_type"})
        try:
            length = int(environ.get("CONTENT_LENGTH") or 0)
        except ValueError:
            length = 0
        if not 0 < length <= 4096:
            return respond("400 Bad Request", {"error": "invalid_length"})
        try:
            data = json.loads(environ["wsgi.input"].read(length))
        except (ValueError, UnicodeError):
            return respond("400 Bad Request", {"error": "invalid_json"})
        if not isinstance(data, dict) or set(data) - {"message", "context"}:
            return respond("400 Bad Request", {"error": "invalid_fields"})
        message, context = data.get("message"), data.get("context", "")
        if (not isinstance(message, str) or not 0 < len(message.strip()) <= 280
                or not isinstance(context, str) or (context and context not in FACTS)):
            return respond("400 Bad Request", {"error": "invalid_message_or_context"})
        intent = classify(message, context)
        if intent is None:
            return respond("200 OK", {"reply": REFUSAL, "context": context})

        retry = [("Retry-After", "60")]
        if not slots.acquire(blocking=False):
            return respond("429 Too Many Requests", {"error": "busy"}, retry)
        try:
            if not budget.reserve(environ.get("REMOTE_ADDR", "")):
                return respond("429 Too Many Requests", {"error": "busy"}, retry)
            topic, focus = intent
            try:
                reply = answer(topic, focus, context, generate)
            except OracleUnavailable:
                return respond("503 Service Unavailable", {"error": "unavailable"})
            return respond("200 OK", {"reply": reply, "context": topic})
        finally:
            slots.release()

    return application


app = create_app()


class QuietHandler(WSGIRequestHandler):
    def log_message(self, *args):
        pass


if __name__ == "__main__":
    with make_server("127.0.0.1", 8787, app, handler_class=QuietHandler) as server:
        print("Oracle developer endpoint: http://127.0.0.1:8787/oracle")
        server.serve_forever()
