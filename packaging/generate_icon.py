"""Generate the Windows multi-resolution icon from the Marginalia book mark."""

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
ASSET_DIR = ROOT / "MarkdownDesk" / "Assets"
SIZE = 1024
SCALE = SIZE / 256


def point(x: float, y: float) -> tuple[int, int]:
    return round(x * SCALE), round(y * SCALE)


def draw_curve(draw: ImageDraw.ImageDraw, points: list[tuple[float, float]], color: str, width: int) -> None:
    draw.line([point(x, y) for x, y in points], fill=color, width=round(width * SCALE), joint="curve")


image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
draw = ImageDraw.Draw(image)
draw.rounded_rectangle(
    (point(8, 8), point(248, 248)),
    radius=round(56 * SCALE),
    fill="#3867D6",
)

left_page = [(128, 79), (114, 68), (98, 61), (81, 62), (62, 67), (45, 69), (45, 185), (64, 179), (83, 176), (101, 178), (116, 183), (128, 197)]
right_page = [(256 - x, y) for x, y in left_page]
draw.polygon([point(x, y) for x, y in left_page], fill="#243754")
draw.polygon([point(x, y) for x, y in right_page], fill="#243754")
draw_curve(draw, left_page + [left_page[0]], "#F5F8FF", 9)
draw_curve(draw, right_page + [right_page[0]], "#F5F8FF", 9)
draw_curve(draw, [(128, 80), (128, 110), (128, 150), (128, 196)], "#B8CCF5", 7)
draw_curve(draw, [(65, 102), (82, 98), (99, 100), (113, 107)], "#AFC5F2", 7)
draw_curve(draw, [(65, 130), (82, 126), (99, 128), (113, 135)], "#AFC5F2", 7)
draw_curve(draw, [(143, 107), (157, 100), (174, 98), (191, 102)], "#AFC5F2", 7)
draw_curve(draw, [(143, 135), (157, 128), (174, 126), (191, 130)], "#AFC5F2", 7)

image.save(ASSET_DIR / "marginalia.ico", format="ICO", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
