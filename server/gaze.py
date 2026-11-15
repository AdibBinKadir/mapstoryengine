"""Medusa's detector: JPEG from the phone's hidden front camera -> head yaw/pitch -> looking_at_screen.

Head pose comes from MediaPipe Face Landmarker's facial transformation matrix.
The ~0.5 s "kept looking" rule before STONED lives in Unity, not here.
"""
import math
import threading
import time
import urllib.request
from collections import deque
from pathlib import Path

MODEL_URL = (
    "https://storage.googleapis.com/mediapipe-models/face_landmarker/"
    "face_landmarker/float16/1/face_landmarker.task"
)
MODEL_PATH = Path(__file__).parent / "models" / "face_landmarker.task"

# A gap longer than this between frames means a new Medusa encounter: forget old frames.
HISTORY_RESET_S = 1.5


class GazeUnavailable(RuntimeError):
    """MediaPipe or its model can't be loaded."""


def head_pose_from_matrix(m):
    """(yaw, pitch) in degrees from a 4x4 face transform. (0, 0) = facing the camera."""
    r = [[float(m[i][j]) for j in range(3)] for i in range(3)]
    # The transform can include scale; normalise each column to get a pure rotation.
    for j in range(3):
        n = math.sqrt(sum(r[i][j] ** 2 for i in range(3))) or 1.0
        for i in range(3):
            r[i][j] /= n
    yaw = math.degrees(math.atan2(r[0][2], r[2][2]))
    pitch = math.degrees(math.atan2(-r[1][2], math.hypot(r[0][2], r[2][2])))
    return yaw, pitch


def majority(history):
    """True if more than half of the recent raw 'looking' results are True."""
    return len(history) > 0 and sum(history) * 2 > len(history)


def ensure_model():
    if not MODEL_PATH.exists():
        MODEL_PATH.parent.mkdir(parents=True, exist_ok=True)
        tmp = MODEL_PATH.with_suffix(".part")
        urllib.request.urlretrieve(MODEL_URL, str(tmp))
        tmp.replace(MODEL_PATH)
    return MODEL_PATH


class GazeDetector:
    def __init__(self, yaw_max=20.0, pitch_max=20.0, smooth_frames=3):
        self.yaw_max = yaw_max
        self.pitch_max = pitch_max
        self._history = deque(maxlen=max(1, smooth_frames))
        self._last_frame_at = 0.0
        self._landmarker = None
        self._cv2 = None
        self._mp = None
        # Phone camera frames often arrive rotated; remember which rotation last found a face.
        self._rotation = 0
        self._lock = threading.Lock()

    def _load(self):
        if self._landmarker is not None:
            return
        try:
            import cv2
            import mediapipe as mp
            from mediapipe.tasks.python import BaseOptions, vision
        except ImportError as e:
            raise GazeUnavailable(
                "mediapipe is not installed (needs Python 3.10-3.12): %s" % e
            )
        try:
            model = ensure_model()
        except Exception as e:
            raise GazeUnavailable("could not download the face model: %s" % e)
        options = vision.FaceLandmarkerOptions(
            base_options=BaseOptions(model_asset_path=str(model)),
            running_mode=vision.RunningMode.IMAGE,
            num_faces=1,
            output_facial_transformation_matrixes=True,
        )
        self._landmarker = vision.FaceLandmarker.create_from_options(options)
        self._cv2 = cv2
        self._mp = mp

    def _rotate(self, img, degrees):
        cv2 = self._cv2
        if degrees == 90:
            return cv2.rotate(img, cv2.ROTATE_90_CLOCKWISE)
        if degrees == 180:
            return cv2.rotate(img, cv2.ROTATE_180)
        if degrees == 270:
            return cv2.rotate(img, cv2.ROTATE_90_COUNTERCLOCKWISE)
        return img

    def _detect(self, rgb):
        """(yaw, pitch) of the first face, or None."""
        image = self._mp.Image(image_format=self._mp.ImageFormat.SRGB, data=rgb)
        result = self._landmarker.detect(image)
        if not result.facial_transformation_matrixes:
            return None
        return head_pose_from_matrix(result.facial_transformation_matrixes[0])

    def analyse(self, jpeg_bytes):
        """Raises ValueError if the bytes aren't an image, GazeUnavailable if MediaPipe isn't usable."""
        with self._lock:
            self._load()
            import numpy as np

            buf = np.frombuffer(jpeg_bytes, dtype=np.uint8)
            bgr = self._cv2.imdecode(buf, self._cv2.IMREAD_COLOR)
            if bgr is None:
                raise ValueError("body is not a decodable JPEG")
            rgb = np.ascontiguousarray(self._cv2.cvtColor(bgr, self._cv2.COLOR_BGR2RGB))

            pose = None
            tries = [self._rotation] + [d for d in (0, 90, 270, 180) if d != self._rotation]
            for degrees in tries:
                pose = self._detect(np.ascontiguousarray(self._rotate(rgb, degrees)))
                if pose is not None:
                    self._rotation = degrees
                    break

            now = time.time()
            if now - self._last_frame_at > HISTORY_RESET_S:
                self._history.clear()
            self._last_frame_at = now

            face_found = pose is not None
            yaw, pitch = pose if face_found else (0.0, 0.0)
            raw_looking = face_found and abs(yaw) < self.yaw_max and abs(pitch) < self.pitch_max
            self._history.append(raw_looking)
            return {
                "face_found": face_found,
                "looking_at_screen": face_found and majority(self._history),
                "yaw": round(yaw, 1),
                "pitch": round(pitch, 1),
                "ts": round(now, 2),
            }
