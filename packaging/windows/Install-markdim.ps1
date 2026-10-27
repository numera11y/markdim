$ErrorActionPreference = 'Stop'

$portableApp = Join-Path $PSScriptRoot 'markdim'
$portableExecutable = Join-Path $portableApp 'markdim.exe'
$portableAssembly = Join-Path $portableApp 'markdim.dll'
$associationScript = Join-Path $PSScriptRoot 'register-file-association.ps1'
$payloadManifest = Join-Path $PSScriptRoot 'markdim.sha256'

if ((-not (Test-Path -LiteralPath $portableExecutable -PathType Leaf)) -or (-not (Test-Path -LiteralPath $portableAssembly -PathType Leaf))) {
    throw 'markdim application files were not found. Extract the complete release archive first.'
}
if (-not (Test-Path -LiteralPath $associationScript -PathType Leaf)) {
    throw 'The file-association helper is missing from this release archive.'
}
if (-not (Test-Path -LiteralPath $payloadManifest -PathType Leaf)) {
    throw 'The payload integrity manifest is missing from this release archive.'
}
foreach ($line in Get-Content -LiteralPath $payloadManifest) {
    if ($line -notmatch '^([0-9a-fA-F]{64})  (.+)$') { throw 'The payload integrity manifest is invalid.' }
    $relativePath = $Matches[2]
    if ([System.IO.Path]::IsPathRooted($relativePath) -or $relativePath.Contains('..')) { throw 'The payload integrity manifest contains an unsafe path.' }
    $filePath = Join-Path $portableApp ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) { throw "A packaged application file is missing: $relativePath" }
    $actualHash = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash
    if ($actualHash -ine $Matches[1]) { throw "A packaged application file failed its SHA-256 check: $relativePath" }
}

$productVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($portableAssembly).ProductVersion
$version = ($productVersion -split '\+')[0]
if ($version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$') {
    throw "Could not determine a stable application version from '$productVersion'."
}

$installRoot = Join-Path $env:LOCALAPPDATA 'Programs\markdim'
$versionRoot = Join-Path $installRoot "v$version"
$installedApp = Join-Path $versionRoot 'markdim'
$installedExecutable = Join-Path $installedApp 'markdim.exe'
$runningTarget = Get-CimInstance -ClassName Win32_Process -Filter "Name = 'markdim.exe'" -ErrorAction SilentlyContinue |
    Where-Object { $_.ExecutablePath -ieq $installedExecutable }
if ($runningTarget) {
    throw 'Close this version of markdim before reinstalling it, then run the installer again.'
}

New-Item -ItemType Directory -Path $versionRoot -Force | Out-Null
if (Test-Path -LiteralPath $installedApp) {
    Remove-Item -LiteralPath $installedApp -Recurse -Force
}
New-Item -ItemType Directory -Path $installedApp -Force | Out-Null
Copy-Item -Path (Join-Path $portableApp '*') -Destination $installedApp -Recurse -Force

# Update the shared per-user ProgID to this version. Existing Open With choices
# and the optional Windows default-app choice use the same ProgID and keep working.
& $associationScript -ApplicationPath $installedExecutable

$startMenu = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
New-Item -ItemType Directory -Path $startMenu -Force | Out-Null
$shortcutPath = Join-Path $startMenu 'markdim.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $installedExecutable
$shortcut.WorkingDirectory = $installedApp
$shortcut.IconLocation = "$installedExecutable,0"
$shortcut.Description = 'Read, edit, and export Markdown documents'
$shortcut.Save()

Write-Host "markdim $version has been installed for the current Windows user."
Write-Host 'The Start Menu shortcut and .md/.markdown Open With command now point to this version.'
Write-Host 'Windows Default Apps can separately make markdim the system default.'
