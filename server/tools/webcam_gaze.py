"""Webcam test for Medusa's detector: shows yaw/pitch/looking live. Press Q to quit.

  python -m server.tools.webcam_gaze                          # runs the detector locally
  python -m server.tools.webcam_gaze --url http://127.0.0.1:8000  # posts frames to a running server's /frame

Frames are shrunk to 320x240 JPEG at ~5 fps, like the phone sends them.
"""
import argparse
import time

import cv2


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--url", help="server base URL; omit to run the detector in-process")
    parser.add_argument("--camera", type=int, default=0)
    parser.add_argument("--fps", type=float, default=5)
    args = parser.parse_args()

    if args.url:
        import httpx

        def analyse(jpeg):
            r = httpx.post(args.url.rstrip("/") + "/frame", content=jpeg,
                           headers={"Content-Type": "image/jpeg"}, timeout=5)
            r.raise_for_status()
            return r.json()
    else:
        from server import config
        from server.gaze import GazeDetector

        s = config.settings
        analyse = GazeDetector(s.gaze_yaw_max, s.gaze_pitch_max, s.gaze_smooth_frames).analyse

    cam = cv2.VideoCapture(args.camera)
    if not cam.isOpened():
        raise SystemExit("could not open webcam %d" % args.camera)
    result, last_sent = None, 0.0
    while True:
        ok, frame = cam.read()
        if not ok:
            break
        if time.time() - last_sent >= 1.0 / args.fps:
            last_sent = time.time()
            ok, jpeg = cv2.imencode(".jpg", cv2.resize(frame, (320, 240)), [cv2.IMWRITE_JPEG_QUALITY, 70])
            try:
                result = analyse(jpeg.tobytes())
            except Exception as e:
                result = {"error": str(e)}
        if result is not None:
            if "error" in result:
                text, colour = result["error"][:60], (0, 0, 255)
            elif not result["face_found"]:
                text, colour = "no face", (200, 200, 200)
            else:
                text = "yaw %+.0f  pitch %+.0f  %s" % (
                    result["yaw"], result["pitch"], "LOOKING" if result["looking_at_screen"] else "away")
                colour = (0, 0, 255) if result["looking_at_screen"] else (0, 200, 0)
            cv2.putText(frame, text, (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.8, colour, 2)
        cv2.imshow("Medusa gaze test (Q quits)", frame)
        if cv2.waitKey(1) & 0xFF in (ord("q"), 27):
            break
    cam.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    main()
