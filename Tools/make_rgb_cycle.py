#!/usr/bin/env python3
import math
import shutil
import subprocess
import sys
from pathlib import Path
from fractions import Fraction

# Configuration
OUT_DIR = "Assets/StreamingAssets/RGBCycle"
SIZE = "256x256"
DURATION = 3.0

FPS_LIST = [
    "24",
    "24000/1001",
    "25",
    "30",
    "30000/1001",
    "50",
    "60",
    "60000/1001",
]

def run(cmd):
    result = subprocess.run(cmd, check=False)
    if result.returncode != 0:
        sys.exit(result.returncode)

def ensure_ffmpeg():
    if shutil.which("ffmpeg") is None:
        print("ffmpeg not found in PATH.", file=sys.stderr)
        sys.exit(1)

def make_color_pngs(out_dir, size):
    width, height = size
    colors = {
        "red": "0xFF0000",
        "green": "0x00FF00",
        "blue": "0x0000FF",
    }
    for name, color in colors.items():
        out_path = out_dir / f"{name}.png"
        cmd = [
            "ffmpeg",
            "-y",
            "-f",
            "lavfi",
            "-i",
            f"color=c={color}:s={width}x{height},format=rgba",
            "-color_range",
            "pc",
            "-frames:v",
            "1",
            str(out_path),
        ]
        run(cmd)


def make_frame_sequence(src_dir, frames_dir, frame_count):
    frames_dir.mkdir(parents=True, exist_ok=True)
    cycle = ["red.png", "green.png", "blue.png"]
    for i in range(frame_count):
        src = src_dir / cycle[i % 3]
        dst = frames_dir / f"{i + 1:06d}.png"
        shutil.copyfile(src, dst)


def encode_video(frames_dir, fps, output_path):
    cmd = [
        "ffmpeg",
        "-y",
        "-framerate",
        str(fps),
        "-i",
        str(frames_dir / "%06d.png"),
        "-c:v",
        "hap",
        "-pix_fmt",
        "rgba",
        str(output_path),
    ]
    run(cmd)


def sanitize_fps_label(fps):
    return str(fps).replace("/", "-")


def parse_size(value):
    if "x" not in value:
        raise ValueError("size must be like 1920x1080")
    w, h = value.split("x", 1)
    return int(w), int(h)


def main():
    ensure_ffmpeg()

    out_dir = Path(OUT_DIR)
    work_dir = out_dir / "_work"
    png_dir = work_dir / "png"

    size = parse_size(SIZE)

    png_dir.mkdir(parents=True, exist_ok=True)
    make_color_pngs(png_dir, size)

    for fps in FPS_LIST:
        fps_label = sanitize_fps_label(fps)
        frames_dir = work_dir / f"frames_{fps_label}"
        if frames_dir.exists():
            shutil.rmtree(frames_dir)

        fps_value = float(Fraction(str(fps)))
        frame_count = max(1, int(math.ceil(fps_value * DURATION)))
        make_frame_sequence(png_dir, frames_dir, frame_count)

        output_path = out_dir / f"RGBCycle{fps_label}.mov"
        output_path.parent.mkdir(parents=True, exist_ok=True)
        encode_video(frames_dir, fps, output_path)

    if work_dir.exists():
        shutil.rmtree(work_dir)

    print(f"Done. Videos written to {out_dir}")


if __name__ == "__main__":
    main()
