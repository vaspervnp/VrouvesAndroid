#!/bin/bash
# Takes the Google Play phone screenshots (exactly 1920 x 1080) on the emulator, in English:
#   tools/store_screenshots.sh
# The emulator (Pixel 7) has a camera cutout on its left in landscape, which the game keeps
# clear, so the display is made 109 px wider than 1920 and that strip is cropped off.
# The stages are played by the bot with the CPC's build plans at speed x3.
set -e
cd "$(dirname "$0")/.."
LA=$(wslpath "$(cmd.exe /c 'echo %LOCALAPPDATA%' 2>/dev/null | tr -d '\r')")
ADB=$LA/Android/Sdk/platform-tools/adb.exe
A=com.revive8bit.vrouves/crc64a8875a36a102a997.MainActivity
CUT=109
OUT=store/screenshots
TMP=$(mktemp -d)
mkdir -p "$OUT"
"$ADB" devices | tr -d "\r" | grep -q "emulator-.*device$" || { echo "start the emulator first (tools/run_emulator.sh)"; exit 1; }
# a build signed with another key (debug / upload) cannot update the installed one
"$ADB" install -r "$(wslpath -w build/Vrouves-Release.apk)" >/dev/null 2>&1 ||
    { "$ADB" uninstall com.revive8bit.vrouves >/dev/null; "$ADB" install "$(wslpath -w build/Vrouves-Release.apk)" >/dev/null; }
"$ADB" shell wm size 1080x$((1920 + CUT))
shot() { "$ADB" exec-out screencap -p > "$TMP/$1.png"; }
start() { "$ADB" shell am force-stop com.revive8bit.vrouves; "$ADB" shell am start -n $A "$@" >/dev/null; }

"$ADB" shell pm clear com.revive8bit.vrouves >/dev/null      # English, default settings
start; sleep 10; shot title
"$ADB" shell input tap 1015 441; sleep 2                        # How to play
"$ADB" shell input tap 1640 264; sleep 2; shot defenders
"$ADB" shell input tap 1640 264; sleep 2; shot invaders
start --ei ending 31416; sleep 9; shot ending
start --ei stage 4 --ez bot true; sleep 5
"$ADB" shell input keyevent 34; "$ADB" shell input keyevent 34  # F F: speed x3
for i in $(seq 1 9); do sleep 20; shot s4_$i; done
start --ei stage 8 --ez bot true; sleep 5
"$ADB" shell input keyevent 34; "$ADB" shell input keyevent 34
for i in $(seq 1 9); do sleep 22; shot s8_$i; done
"$ADB" shell am force-stop com.revive8bit.vrouves
"$ADB" shell wm size reset

python3 - "$TMP" "$OUT" $CUT <<'EOF'
import sys
from PIL import Image
tmp, out, cut = sys.argv[1], sys.argv[2], int(sys.argv[3])
for src, dst in [("title", "1-title"), ("s4_7", "2-bakery"), ("s8_7", "3-sheepfold-tanks"),
                 ("defenders", "4-defenders"), ("s4_9", "5-final-wave"), ("invaders", "6-invaders"),
                 ("ending", "7-ending")]:
    im = Image.open(f"{tmp}/{src}.png").convert("RGB")
    im = im.crop((cut, 0, cut + 1920, 1080))
    im.save(f"{out}/en-{dst}.png")
    print(dst, im.size)
EOF
echo "raw shots in $TMP"
