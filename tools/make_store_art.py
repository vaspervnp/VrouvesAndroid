#!/usr/bin/env python3
"""Makes the Google Play store graphics in store/:
  icon-512.png              the hi-res icon (512 x 512, from VrouvesPC's art/icon_512.png)
  feature-en.png, -el.png   the feature graphic (1024 x 500): the title picture with the name
The phone screenshots (store/screenshots/) come from the emulator, see store/README.md."""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
OUT = os.path.join(ROOT, "store")
FONT = os.path.join(ROOT, "Vrouves/Assets/fonts/NotoSansDisplay-Black.ttf")
GOLD, BLACK = (255, 217, 51), (0, 0, 0)

os.makedirs(OUT, exist_ok=True)

icon = Image.open(os.path.join(ROOT, "../VrouvesPC/art/icon_512.png")).convert("RGBA")
assert icon.size == (512, 512)
icon.save(os.path.join(OUT, "icon-512.png"))

title = Image.open(os.path.join(ROOT, "Vrouves/Assets/title.jpg")).convert("RGB")
# the villagers, the cauldron and the invaders on their path, with the mountains behind
crop = title.crop((0, 240, 1280, 240 + 625)).resize((1024, 500), Image.LANCZOS)

for lang, name in (("en", "Let's Go For Vrouves"), ("el", "Πάμε για Βρούβες")):
    im = crop.copy()
    d = ImageDraw.Draw(im)
    size = 92
    while True:
        f = ImageFont.truetype(FONT, size)
        w = d.textlength(name, font=f)
        if w <= 940:
            break
        size -= 2
    d.text(((1024 - w) / 2, 24), name, font=f, fill=GOLD, stroke_width=9, stroke_fill=BLACK)
    im.save(os.path.join(OUT, f"feature-{lang}.png"))
    print(lang, size)
