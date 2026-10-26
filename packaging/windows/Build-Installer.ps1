param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$')]
    [string] $Version
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$releaseDirectory = Join-Path $repositoryRoot "release/v$Version"
$ridDirectory = Join-Path $releaseDirectory 'windows/x64'
$applicationDirectory = Join-Path $ridDirectory 'markdim'
$payloadManifest = Join-Path $ridDirectory 'markdim.sha256'
$installerDefinition = Join-Path $PSScriptRoot 'markdim.iss'

$isccCommand = Get-Command ISCC.exe -ErrorAction SilentlyContinue
$iscc = if ($isccCommand) { $isccCommand.Source } else {
    Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)) 'Inno Setup 6/ISCC.exe'
}
if (-not (Test-Path -LiteralPath $iscc -PathType Leaf)) {
    throw 'Inno Setup 6 is required to build the Windows installer. Install it or add ISCC.exe to PATH.'
}
if (-not (Test-Path -LiteralPath $payloadManifest -PathType Leaf)) {
    throw 'The Windows payload manifest is missing; run Publish-Release.ps1 first.'
}

foreach ($line in Get-Content -LiteralPath $payloadManifest) {
    if ($line -notmatch '^([0-9a-fA-F]{64})  (.+)$') { throw 'The payload integrity manifest is invalid.' }
    $relativePath = $Matches[2]
    if ([System.IO.Path]::IsPathRooted($relativePath) -or $relativePath.Contains('..')) { throw 'The payload integrity manifest contains an unsafe path.' }
    $filePath = Join-Path $applicationDirectory ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) { throw "A packaged application file is missing: $relativePath" }
    $actualHash = (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash
    if ($actualHash -ine $Matches[1]) { throw "A packaged application file failed its SHA-256 check: $relativePath" }
}

Push-Location $PSScriptRoot
$isccExitCode = 0
try {
    & $iscc "/DAppVersion=$Version" "/DAppSource=$applicationDirectory" "/DOutputDir=$ridDirectory" $installerDefinition
    $isccExitCode = $LASTEXITCODE
} finally {
    Pop-Location
}
if ($isccExitCode -ne 0) { exit $isccExitCode }

$installerName = "markdim-Setup-v$Version-windows-x64.exe"
$installerPath = Join-Path $ridDirectory $installerName
$installerHash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
[System.IO.File]::WriteAllText("$installerPath.sha256", "$installerHash  $installerName`n", [System.Text.Encoding]::ASCII)

$ridReadme = Join-Path $ridDirectory 'README.txt'
if (Test-Path -LiteralPath $ridReadme) {
    [System.IO.File]::AppendAllText($ridReadme, "Windows installer: $installerName`r`nSHA-256 file: $installerName.sha256`r`n")
}
$releaseReadme = Join-Path $releaseDirectory 'README.txt'
if (Test-Path -LiteralPath $releaseReadme) {
    [System.IO.File]::AppendAllText($releaseReadme, "Windows x64 installer: windows/x64/$installerName`r`n")
}

Write-Host "Windows installer created: $installerPath"
Write-Host "SHA-256: $installerHash"
