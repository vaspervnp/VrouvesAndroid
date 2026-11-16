#!/bin/bash
# Takes the Google Play phone screenshots (exactly 1920 x 1080) on the emulator, in English:
#   tools/store_screenshots.sh          take the raw shots into build/shots/, then crop them
#   tools/store_screenshots.sh crop     only crop the raw shots already there
# The emulator (Pixel 7) has a camera cutout on its left in landscape, which the game keeps
# clear, so the display is made 109 px wider than 1920 and that strip is cropped off.
# The stages are played by the bot (the plans in Vrouves/Assets/data/plans.json) at speed x3,
# a shot every 20 seconds; PICK says which of them go to the store.
set -e
cd "$(dirname "$0")/.."
CUT=109
OUT=store/screenshots
RAW=build/shots
STAGES="10 13 16 19"
SHOTS=13
# store name = raw shot
PICK="1-title=title 2-dairy=s10_4 3-monastery=s13_6 4-defenders=defenders 5-windmills=s16_10
      6-summit=s19_8 7-invaders=invaders 8-ending=ending"

if [ "$1" != crop ]; then
    LA=$(wslpath "$(cmd.exe /c 'echo %LOCALAPPDATA%' 2>/dev/null | tr -d '\r')")
    ADB=$LA/Android/Sdk/platform-tools/adb.exe
    A=com.revive8bit.vrouves/crc64a8875a36a102a997.MainActivity
    mkdir -p "$RAW"
    "$ADB" devices | tr -d "\r" | grep -q "emulator-.*device$" || { echo "start the emulator first (tools/run_emulator.sh)"; exit 1; }
    # a build signed with another key (debug / upload) cannot update the installed one
    "$ADB" install -r "$(wslpath -w build/Vrouves-Release.apk)" >/dev/null 2>&1 ||
        { "$ADB" uninstall com.revive8bit.vrouves >/dev/null; "$ADB" install "$(wslpath -w build/Vrouves-Release.apk)" >/dev/null; }
    "$ADB" shell wm size 1080x$((1920 + CUT))
    shot() { "$ADB" exec-out screencap -p > "$RAW/$1.png"; }
    start() { "$ADB" shell am force-stop com.revive8bit.vrouves; "$ADB" shell am start -n $A "$@" >/dev/null; }

    "$ADB" shell pm clear com.revive8bit.vrouves >/dev/null      # English, default settings, no saved game
    start; sleep 10; shot title
    "$ADB" shell input tap 1015 441; sleep 2                        # How to play
    "$ADB" shell input tap 1640 264; sleep 2; shot defenders
    "$ADB" shell input tap 1640 264; sleep 2; shot invaders
    start --ei ending 31416; sleep 18; shot ending
    for s in $STAGES; do
        start --ei stage $s --ez bot true; sleep 5
        "$ADB" shell input keyevent 34; "$ADB" shell input keyevent 34  # F F: speed x3
        for i in $(seq 1 $SHOTS); do sleep 20; shot s${s}_$i; done
    done
    "$ADB" shell am force-stop com.revive8bit.vrouves
    "$ADB" shell wm size reset
fi

rm -f "$OUT"/en-*.png
mkdir -p "$OUT"
python3 - "$RAW" "$OUT" $CUT $PICK <<'EOF'
import sys
from PIL import Image
raw, out, cut = sys.argv[1], sys.argv[2], int(sys.argv[3])
for pair in sys.argv[4:]:
    dst, src = pair.split("=")
    im = Image.open(f"{raw}/{src}.png").convert("RGB").crop((cut, 0, cut + 1920, 1080))
    im.save(f"{out}/en-{dst}.png")
    print(dst, im.size)
EOF
