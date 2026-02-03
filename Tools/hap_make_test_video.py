#!/usr/bin/env python3
import argparse
import math
import shutil
import subprocess
import sys
from pathlib import Path
from fractions import Fraction

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
    parser = argparse.ArgumentParser(
        description="Generate HAP codec test videos with RGB frame cycling."
    )
    parser.add_argument(
        "--out-dir",
        default="HapTestVideos",
        help="Output directory for videos and assets.",
    )
    parser.add_argument(
        "--size",
        default="256x256",
        help="Frame size, e.g. 1920x1080.",
    )
    parser.add_argument(
        "--duration",
        type=float,
        default=3.0,
        help="Duration in seconds for each video.",
    )
    args = parser.parse_args()

    ensure_ffmpeg()

    out_dir = Path(args.out_dir)
    work_dir = out_dir / "_work"
    png_dir = work_dir / "png"

    size = parse_size(args.size)

    png_dir.mkdir(parents=True, exist_ok=True)
    make_color_pngs(png_dir, size)

    fps_list = [
        "24",
        "24000/1001",
        "25",
        "30",
        "30000/1001",
        "50",
        "60",
        "60000/1001",
    ]

    for fps in fps_list:
        fps_label = sanitize_fps_label(fps)
        frames_dir = work_dir / f"frames_{fps_label}"
        if frames_dir.exists():
            shutil.rmtree(frames_dir)

        fps_value = float(Fraction(str(fps)))
        frame_count = max(1, int(math.ceil(fps_value * args.duration)))
        make_frame_sequence(png_dir, frames_dir, frame_count)

        output_path = out_dir / f"rgb_cycle_{fps_label}.mov"
        output_path.parent.mkdir(parents=True, exist_ok=True)
        encode_video(frames_dir, fps, output_path)

    print(f"Done. Videos written to {out_dir}")


if __name__ == "__main__":
    main()
