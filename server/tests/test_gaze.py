import math

import pytest

from server import gaze


def rot_y(deg):
    c, s = math.cos(math.radians(deg)), math.sin(math.radians(deg))
    return [[c, 0, s, 0], [0, 1, 0, 0], [-s, 0, c, 0], [0, 0, 0, 1]]


def rot_x(deg):
    c, s = math.cos(math.radians(deg)), math.sin(math.radians(deg))
    return [[1, 0, 0, 0], [0, c, -s, 0], [0, s, c, 0], [0, 0, 0, 1]]


def test_facing_camera_is_zero():
    yaw, pitch = gaze.head_pose_from_matrix(rot_y(0))
    assert abs(yaw) < 1e-6 and abs(pitch) < 1e-6


@pytest.mark.parametrize("deg", [-40, -10, 25])
def test_yaw_and_pitch(deg):
    assert gaze.head_pose_from_matrix(rot_y(deg))[0] == pytest.approx(deg)
    assert gaze.head_pose_from_matrix(rot_x(deg))[1] == pytest.approx(deg)


def test_scale_is_ignored():
    m = [[v * 3 if i < 3 and j < 3 else v for j, v in enumerate(row)] for i, row in enumerate(rot_y(30))]
    assert gaze.head_pose_from_matrix(m)[0] == pytest.approx(30)


def test_majority_smoothing():
    assert not gaze.majority([])
    assert not gaze.majority([True, False, False])
    assert gaze.majority([True, True, False])


def test_real_detector_on_blank_frame():
    pytest.importorskip("mediapipe")
    cv2 = pytest.importorskip("cv2")
    np = pytest.importorskip("numpy")
    if not gaze.MODEL_PATH.exists():
        pytest.skip("face model not downloaded")
    ok, jpeg = cv2.imencode(".jpg", np.zeros((240, 320, 3), dtype=np.uint8))
    result = gaze.GazeDetector().analyse(jpeg.tobytes())
    assert result["face_found"] is False and result["looking_at_screen"] is False


def test_real_detector_rejects_non_jpeg():
    pytest.importorskip("mediapipe")
    if not gaze.MODEL_PATH.exists():
        pytest.skip("face model not downloaded")
    with pytest.raises(ValueError):
        gaze.GazeDetector().analyse(b"definitely not a jpeg")
