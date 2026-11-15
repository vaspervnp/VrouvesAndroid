# Makes the Google Play upload key, once:
#   powershell -ExecutionPolicy Bypass -File tools\make_upload_key.ps1
# (from WSL: powershell.exe -ExecutionPolicy Bypass -File 'tools\make_upload_key.ps1')
#
# It asks for a password, then writes, outside the repository:
#   %USERPROFILE%\.android-keys\vrouves-upload.jks    the key store, alias "vrouves"
#   %USERPROFILE%\.android-keys\vrouves-upload.pass   its password, read by tools\build.cmd
# A Release build signs the bundle with this key when the two files are there. Google Play
# signs the published app with its own app signing key; this one only proves uploads come
# from you. Keep a copy of both files somewhere safe: a lost upload key can be reset through
# Play Console support, but it takes days.
$ErrorActionPreference = 'Stop'
$dir = Join-Path $env:USERPROFILE '.android-keys'
$jks = Join-Path $dir 'vrouves-upload.jks'
$passFile = Join-Path $dir 'vrouves-upload.pass'
if (Test-Path $jks) { Write-Host "$jks is already there; nothing to do."; exit 0 }

$jdk = Get-ChildItem 'C:\Program Files\Android\openjdk' -Directory -Filter 'jdk-*' | Sort-Object Name | Select-Object -Last 1
if (-not $jdk) { throw 'The Microsoft OpenJDK of Visual Studio was not found in C:\Program Files\Android\openjdk' }
$keytool = Join-Path $jdk.FullName 'bin\keytool.exe'

function Plain([Security.SecureString]$s) {
    $b = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($s)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($b) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($b) }
}
while ($true) {
    $p1 = Plain (Read-Host 'Upload key password (at least 8 characters)' -AsSecureString)
    $p2 = Plain (Read-Host 'The same password again' -AsSecureString)
    if ($p1 -ne $p2) { Write-Host 'They differ, try again.'; continue }
    if ($p1.Length -lt 8) { Write-Host 'Too short, try again.'; continue }
    break
}
New-Item -ItemType Directory -Force $dir | Out-Null
[IO.File]::WriteAllText($passFile, $p1)
& $keytool -genkeypair -v -keystore $jks -storetype PKCS12 -alias vrouves -keyalg RSA -keysize 4096 `
    -validity 10000 -dname 'CN=REVIVE8BIT, O=REVIVE8BIT, C=GR' `
    -storepass:file $passFile -keypass:file $passFile
if ($LASTEXITCODE -ne 0) { Remove-Item -ErrorAction SilentlyContinue $passFile; throw 'keytool failed' }
Write-Host ''
Write-Host "Done: $jks"
Write-Host 'Keep a copy of the .android-keys folder somewhere safe.'
