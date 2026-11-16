#!/bin/bash
# Copies the art, sounds and stage data of the Godot version (../VrouvesPC, whose stage data
# comes from the CPC original ../VrouvesCPC by tools/export_stages.py) into the Android app:
#   Vrouves/Assets/data/stages.json      the CPC's ten stages: maps, roads, flight paths, waves,
#                                        with this version's ten (tools/make_stages.py) before
#                                        the wedding, which becomes stage 20
#   Vrouves/Assets/sprites/*.png         the defenders, invaders, sacks, shots and effects
#   Vrouves/Assets/stages/stageN.jpg     each stage's map picture (JPEG: it has no alpha); stages
#                                        10-19 and the Shepherd and the Priest come from build/art,
#                                        where tools/blender/render_extra.py renders them
#   Vrouves/Assets/title.jpg, ending.jpg the title and ending pictures
#   Vrouves/Assets/audio/                the sound effects (WAV) and the tune (Ogg Vorbis)
#   Vrouves/Assets/fonts/                Noto Sans (SIL OFL)
#   Vrouves/Resources/mipmap-*/          the launcher icon
# Needs python3 with Pillow, and ffmpeg with libvorbis.
set -e
cd "$(dirname "$0")/.."
SRC=${1:-../VrouvesPC}
A=Vrouves/Assets
R=Vrouves/Resources

mkdir -p $A/data $A/sprites $A/stages $A/audio $A/fonts
cp $SRC/data/stages.json $A/data/
python3 tools/make_stages.py >/dev/null
cp $SRC/art/sprites/*.png $A/sprites/
cp $SRC/art/audio/sfx_*.wav $A/audio/
ffmpeg -loglevel error -y -i $SRC/art/audio/music.wav -c:a libvorbis -q:a 5 $A/audio/music.ogg
cp $SRC/art/fonts/*.ttf $SRC/art/fonts/LICENSE-noto.txt $A/fonts/

python3 - "$SRC" "$A" "$R" <<'EOF'
import sys, os, glob
from PIL import Image
src, a, r = sys.argv[1:4]
for f in sorted(glob.glob(f"{src}/art/stages/stage*.png")):
    n = int(os.path.basename(f)[5:-4])
    n = 20 if n == 10 else n            # the wedding comes after this version's ten stages
    Image.open(f).convert("RGB").save(f"{a}/stages/stage{n}.jpg", quality=90, optimize=True)
for n in range(10, 20):
    f = f"build/art/stage{n}.png"
    if os.path.exists(f):
        Image.open(f).convert("RGB").resize((1920, 1344), Image.LANCZOS).save(f"{a}/stages/stage{n}.jpg", quality=90,
                                                                             optimize=True)
for n in ("shepherd", "priest"):
    if os.path.exists(f"build/art/{n}.png"):
        Image.open(f"build/art/{n}.png").save(f"{a}/sprites/{n}.png", optimize=True)
for n in ("title", "ending"):
    Image.open(f"{src}/art/{n}.png").convert("RGB").save(f"{a}/{n}.jpg", quality=90, optimize=True)
icon = Image.open(f"{src}/art/icon_1024.png").convert("RGBA")
for d, s in (("mdpi", 48), ("hdpi", 72), ("xhdpi", 96), ("xxhdpi", 144), ("xxxhdpi", 192)):
    os.makedirs(f"{r}/mipmap-{d}", exist_ok=True)
    icon.resize((s, s), Image.LANCZOS).save(f"{r}/mipmap-{d}/icon.png", optimize=True)
EOF
du -sh $A/*
