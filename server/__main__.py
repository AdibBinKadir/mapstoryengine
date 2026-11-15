"""Run the shared Oracle and Sphinx server."""
import argparse
import os


def main():
    parser = argparse.ArgumentParser(description="Odyssey AR server")
    parser.add_argument("--fake", action="store_true", help="use canned Sphinx replies")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8787)
    args = parser.parse_args()
    if args.fake:
        os.environ["FAKE"] = "1"

    import uvicorn

    print(f"Odyssey local server: http://{args.host}:{args.port}", flush=True)
    uvicorn.run("server.venue:app", host=args.host, port=args.port)


if __name__ == "__main__":
    main()
