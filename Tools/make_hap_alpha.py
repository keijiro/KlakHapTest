#!/usr/bin/env python3
import math
import shutil
import subprocess
import sys
import zlib
from pathlib import Path
from struct import pack

OUT_DIR = Path("Assets/StreamingAssets/HapAlpha")
SIZE = 256

MOV_NAME = "HapAlpha.mov"
EXTRACTED_NAME = "000001.png"


def run(cmd):
    result = subprocess.run(cmd, check=False)
    if result.returncode != 0:
        sys.exit(result.returncode)


def ensure_ffmpeg():
    if shutil.which("ffmpeg") is None:
        print("ffmpeg not found in PATH.", file=sys.stderr)
        sys.exit(1)


def hsv_to_rgb(h, s, v):
    h = h % 1.0
    i = int(h * 6.0)
    f = h * 6.0 - i
    p = v * (1.0 - s)
    q = v * (1.0 - f * s)
    t = v * (1.0 - (1.0 - f) * s)
    i = i % 6

    if i == 0:
        return v, t, p
    if i == 1:
        return q, v, p
    if i == 2:
        return p, v, t
    if i == 3:
        return p, q, v
    if i == 4:
        return t, p, v
    return v, p, q


def write_png_rgba(path, width, height, pixels):
    def chunk(tag, data):
        return pack(">I", len(data)) + tag + data + pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    raw = bytearray()
    stride = width * 4
    for y in range(height):
        row_start = y * stride
        raw.append(0)
        raw.extend(pixels[row_start:row_start + stride])

    compressed = zlib.compress(bytes(raw), level=9)

    header = b"\x89PNG\r\n\x1a\n"
    ihdr = pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    data = header + chunk(b"IHDR", ihdr) + chunk(b"IDAT", compressed) + chunk(b"IEND", b"")

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)


def make_gradient_png(path, size):
    width = height = size
    pixels = bytearray(width * height * 4)

    for y in range(height):
        alpha = y / (height - 1)
        for x in range(width):
            hue = x / (width - 1)
            r, g, b = hsv_to_rgb(hue, 1.0, 1.0)
            i = (y * width + x) * 4
            pixels[i + 0] = int(r * 255 + 0.5)
            pixels[i + 1] = int(g * 255 + 0.5)
            pixels[i + 2] = int(b * 255 + 0.5)
            pixels[i + 3] = int(alpha * 255 + 0.5)

    write_png_rgba(path, width, height, pixels)


def main():
    ensure_ffmpeg()

    work_dir = OUT_DIR / "_work"
    source_png = work_dir / "source.png"

    if work_dir.exists():
        shutil.rmtree(work_dir)
    work_dir.mkdir(parents=True, exist_ok=True)

    make_gradient_png(source_png, SIZE)

    OUT_DIR.mkdir(parents=True, exist_ok=True)

    run([
        "ffmpeg",
        "-y",
        "-i",
        str(source_png),
        "-frames:v",
        "1",
        "-c:v",
        "hap",
        "-format",
        "hap_alpha",
        "-pix_fmt",
        "rgba",
        str(OUT_DIR / MOV_NAME),
    ])

    run([
        "ffmpeg",
        "-y",
        "-i",
        str(OUT_DIR / MOV_NAME),
        "-frames:v",
        "1",
        "-update",
        "1",
        "-pix_fmt",
        "rgba",
        "-color_range",
        "pc",
        str(OUT_DIR / EXTRACTED_NAME),
    ])

    if work_dir.exists():
        shutil.rmtree(work_dir)

    print(f"Done. Files written to {OUT_DIR}")


if __name__ == "__main__":
    main()
