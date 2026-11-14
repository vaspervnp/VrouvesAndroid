#!/bin/bash
# Builds the Android app from WSL with the Windows .NET SDK of Visual Studio 2026:
#   tools/build.sh [Debug|Release]
# The Release build cannot strip the native libraries on a \\wsl.localhost path, so the
# sources are mirrored to a folder on the Windows side (%LOCALAPPDATA%\Temp\VrouvesAndroid-build,
# which keeps its own bin/ and obj/ between builds) and built there with tools\build.cmd.
# The APK (and, for Release, the Google Play bundle) is copied to build/.
set -e
cd "$(dirname "$0")/.."
CFG=${1:-Debug}
WIN_TEMP=$(wslpath "$(cmd.exe /c 'echo %LOCALAPPDATA%' 2>/dev/null | tr -d '\r')")/Temp
W=$WIN_TEMP/VrouvesAndroid-build
mkdir -p "$W"
rsync -a --delete --exclude=.git --exclude=bin --exclude=obj --exclude=build ./ "$W/"
(cd "$W" && cmd.exe /c "tools\\build.cmd $CFG")
OUT="$W/Vrouves/bin/$CFG/net10.0-android"
mkdir -p build
cp "$OUT/com.revive8bit.vrouves-Signed.apk" "build/Vrouves-$CFG.apk"
if [ "$CFG" = Release ]; then cp "$OUT/com.revive8bit.vrouves-Signed.aab" build/Vrouves-Release.aab; fi
ls -la build/
