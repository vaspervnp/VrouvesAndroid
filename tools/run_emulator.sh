#!/bin/bash
# Starts the Android emulator (if it is not running), installs the game and opens it:
#   tools/run_emulator.sh [Debug|Release] [adb am start extras, e.g. --ei stage 8]
# The emulator is the AVD Vrouves_Pixel7 (Pixel 7, Android 16, x86_64, WHPX) in the user SDK
# %LOCALAPPDATA%\Android\Sdk; tools/build.sh makes the APK first if it is missing.
set -e
cd "$(dirname "$0")/.."
CFG=${1:-Release}
shift || true
LA=$(wslpath "$(cmd.exe /c 'echo %LOCALAPPDATA%' 2>/dev/null | tr -d '\r')")
SDK=$LA/Android/Sdk
ADB=$SDK/platform-tools/adb.exe
APK=build/Vrouves-$CFG.apk
[ -f "$APK" ] || tools/build.sh "$CFG"
if ! "$ADB" devices | tr -d "\r" | grep -q "emulator-.*device$"; then
    (cd "$LA/Temp" && nohup "$SDK/emulator/emulator.exe" -avd Vrouves_Pixel7 -no-boot-anim -no-snapshot-save -gpu host >/dev/null 2>&1 &)
    "$ADB" wait-for-device
    until [ "$("$ADB" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" = 1 ]; do sleep 2; done
fi
# a build signed with another key (debug / upload) cannot update the installed one
"$ADB" install -r "$(wslpath -w "$APK")" || { "$ADB" uninstall com.revive8bit.vrouves; "$ADB" install "$(wslpath -w "$APK")"; }
"$ADB" shell am force-stop com.revive8bit.vrouves
"$ADB" shell am start -n com.revive8bit.vrouves/crc64a8875a36a102a997.MainActivity "$@"
