@echo off
rem Builds the Android app with the .NET SDK of Visual Studio 2026 (the same as Build in VS).
rem   tools\build.cmd [Debug^|Release]      from Windows, or tools/build.sh from WSL
rem The APK lands in Vrouves\bin\<config>\net10.0-android\com.revive8bit.vrouves-Signed.apk
setlocal
set CFG=%~1
if "%CFG%"=="" set CFG=Debug
pushd "%~dp0.."
if not defined AndroidSdkDirectory set "AndroidSdkDirectory=C:\Program Files (x86)\Android\android-sdk"
if not defined JavaSdkDirectory for /d %%J in ("C:\Program Files\Android\openjdk\jdk-*") do set "JavaSdkDirectory=%%J"
dotnet build Vrouves\Vrouves.csproj -c %CFG% -nologo -v:minimal -p:EmbedAssembliesIntoApk=true
set ERR=%ERRORLEVEL%
popd
exit /b %ERR%
