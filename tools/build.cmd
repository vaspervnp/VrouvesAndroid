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
rem a Release is signed with the Google Play upload key, if tools\make_upload_key.ps1 made one
set "KEYS=%USERPROFILE%\.android-keys"
set SIGN=
if /i "%CFG%"=="Release" if exist "%KEYS%\vrouves-upload.jks" set SIGN=-p:AndroidKeyStore=true "-p:AndroidSigningKeyStore=%KEYS%\vrouves-upload.jks" -p:AndroidSigningKeyAlias=vrouves "-p:AndroidSigningStorePass=file:%KEYS%\vrouves-upload.pass" "-p:AndroidSigningKeyPass=file:%KEYS%\vrouves-upload.pass"
if defined SIGN (echo Signing with the upload key in %KEYS%) else (echo Signing with the debug key)
rem an unchanged build would keep the bundle signed with the other key
if /i "%CFG%"=="Release" (
    del /q "Vrouves\bin\Release\net10.0-android\*-Signed.*" 2>nul
    del /q "Vrouves\obj\Release\net10.0-android\android\bin\*.aab" "Vrouves\obj\Release\net10.0-android\android\bin\*.apk" 2>nul
)
dotnet build Vrouves\Vrouves.csproj -c %CFG% -nologo -v:minimal -p:EmbedAssembliesIntoApk=true %SIGN%
set ERR=%ERRORLEVEL%
popd
exit /b %ERR%
