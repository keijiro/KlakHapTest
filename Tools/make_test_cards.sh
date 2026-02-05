#!/usr/bin/env bash
set -euo pipefail

OUT_DIR="${1:-Assets/StreamingAssets/Tests/TestCards}"
SIZE="${SIZE:-512x512}"
FPS="${FPS:-1}"

WIDTH="${SIZE%x*}"
HEIGHT="${SIZE#*x}"

make_png() {
  local index="$1"
  local filter="$2"
  local out_path
  out_path="$(printf "%06d.png" "${index}")"

  ffmpeg -y -f lavfi -i "${filter},scale=${WIDTH}:${HEIGHT}" \
    -frames:v 1 \
    -update 1 \
    -color_range pc \
    -pix_fmt rgba \
    "${out_path}"
}

make_png 1 "allrgb"
make_png 2 "haldclutsrc"
make_png 3 "rgbtestsrc"
make_png 4 "smptehdbars"
make_png 5 "yuvtestsrc"

mkdir -p "${OUT_DIR}"

ffmpeg -y -framerate "${FPS}" -i "%06d.png" \
  -c:v hap -pix_fmt rgba \
  "${OUT_DIR}/TestCards.mov"

ffmpeg -y -i "${OUT_DIR}/TestCards.mov" \
  -pix_fmt rgba -color_range pc \
  "${OUT_DIR}/%06d.png"

rm -f 000001.png 000002.png 000003.png 000004.png 000005.png

echo "Done: ${OUT_DIR}"
