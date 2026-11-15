"""python -m server [--fake] [--port 8000]: run the server on all interfaces and print the URL for Unity."""
import argparse
import os
import socket


def lan_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.connect(("10.255.255.255", 1))  # UDP connect sends nothing; it just picks the outgoing interface
        return s.getsockname()[0]
    except OSError:
        return "127.0.0.1"
    finally:
        s.close()


def main():
    parser = argparse.ArgumentParser(description="Odyssey AR server")
    parser.add_argument("--fake", action="store_true", help="canned replies, no models or keys")
    parser.add_argument("--host", default="0.0.0.0")
    parser.add_argument("--port", type=int, default=8000)
    args = parser.parse_args()
    if args.fake:
        os.environ["FAKE"] = "1"

    import uvicorn

    print("Unity serverUrl: http://%s:%d   (phone and laptop must be on the same Wi-Fi/hotspot)"
          % (lan_ip(), args.port), flush=True)
    uvicorn.run("server.main:app", host=args.host, port=args.port)


if __name__ == "__main__":
    main()
