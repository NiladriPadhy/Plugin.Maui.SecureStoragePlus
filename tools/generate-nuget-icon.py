#!/usr/bin/env python3
"""Generate a simple 128x128 NuGet icon (PNG) without third-party deps."""

from __future__ import annotations

import struct
import zlib
from pathlib import Path


def chunk(tag: bytes, data: bytes) -> bytes:
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


def lerp(a: int, b: int, t: float) -> int:
    return int(a + (b - a) * t)


def pixel(x: int, y: int, size: int) -> bytes:
    # Navy-to-teal background
    t = (x + y) / (2 * (size - 1))
    r, g, b = lerp(15, 20, t), lerp(40, 140, t), lerp(70, 150, t)

    cx, cy = size / 2, size / 2 + 4
    # Soft rounded card
    card_margin = 14
    if card_margin <= x < size - card_margin and card_margin <= y < size - card_margin:
        dx = min(x - card_margin, size - card_margin - 1 - x)
        dy = min(y - card_margin, size - card_margin - 1 - y)
        if dx >= 0 and dy >= 0 and (dx >= 12 or dy >= 12 or (dx - 12) ** 2 + (dy - 12) ** 2 <= 12 * 12):
            r, g, b = 236, 248, 247

    # Shield body
    shield_top, shield_bottom = 28, 102
    shield_left, shield_right = 36, 92
    if shield_top <= y <= shield_bottom and shield_left <= x <= shield_right:
        nx = (x - cx) / 28
        progress = (y - shield_top) / (shield_bottom - shield_top)
        half = 1.0 - progress * 0.55
        if abs(nx) <= half:
            r, g, b = 16, 122, 128

    # Lock shackle
    lock_cx, lock_cy, inner_r, outer_r = 64, 52, 8, 13
    dist = ((x - lock_cx) ** 2 + (y - lock_cy) ** 2) ** 0.5
    if y <= lock_cy + 2 and inner_r <= dist <= outer_r:
        r, g, b = 245, 250, 250

    # Lock body
    if 52 <= x <= 76 and 56 <= y <= 82:
        r, g, b = 245, 250, 250

    # Keyhole
    if ((x - 64) ** 2 + (y - 66) ** 2) ** 0.5 <= 3.2 or (62 <= x <= 66 and 66 <= y <= 76):
        r, g, b = 16, 122, 128

    return bytes((r, g, b, 255))


def write_png(path: Path, size: int = 128) -> None:
    raw = bytearray()
    for y in range(size):
        raw.append(0)
        for x in range(size):
            raw.extend(pixel(x, y, size))

    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(bytes(raw), 9))
    png += chunk(b"IEND", b"")
    path.write_bytes(png)


if __name__ == "__main__":
    output = Path(__file__).resolve().parents[1] / "nuget.png"
    write_png(output)
    print(f"Wrote {output}")
