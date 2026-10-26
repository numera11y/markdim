param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?$')]
    [string] $Version,

    [ValidateSet('win-x64', 'osx-x64', 'osx-arm64', 'linux-x64', 'linux-arm64')]
    [string] $RuntimeIdentifier = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repositoryRoot 'MarkdownDesk/MarkdownDesk.csproj'
$solution = Join-Path $repositoryRoot 'Marginalia.slnx'
$windowsInstallerSource = Join-Path $repositoryRoot 'packaging/windows/Install-Marginalia.ps1'
$windowsAssociationSource = Join-Path $repositoryRoot 'packaging/windows/register-file-association.ps1'
$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnet = if ($dotnetCommand) { $dotnetCommand.Source } else { $null }
if (-not $dotnet -and [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
    $dotnet = Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'
}
if (-not $dotnet -or -not (Test-Path -LiteralPath $dotnet)) {
    throw 'The .NET SDK executable was not found. Install the .NET 10 SDK or add dotnet to PATH.'
}

$platform, $architecture = switch -Regex ($RuntimeIdentifier) {
    '^win-(.+)$' { 'windows', $Matches[1]; break }
    '^osx-(.+)$' { 'macos', $Matches[1]; break }
    '^linux-(.+)$' { 'linux', $Matches[1]; break }
}

$ridDirectory = Join-Path $repositoryRoot "release/v$Version/$platform/$architecture"
$applicationDirectory = Join-Path $ridDirectory 'Marginalia'
$archiveExtension = if ($platform -eq 'windows') { 'zip' } else { 'tar.gz' }
$archiveName = "Marginalia-v$Version-$platform-$architecture.$archiveExtension"
$archivePath = Join-Path $ridDirectory $archiveName

Write-Host "Verifying solution for release v$Version..."
& $dotnet restore $solution --locked-mode
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $dotnet test $solution --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (Test-Path -LiteralPath $applicationDirectory) {
    Remove-Item -LiteralPath $applicationDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $ridDirectory -Force | Out-Null

Write-Host "Publishing $RuntimeIdentifier to $applicationDirectory..."
& $dotnet publish $project --configuration Release --runtime $RuntimeIdentifier `
    --self-contained true --output $applicationDirectory `
    "-p:Version=$Version" "-p:InformationalVersion=$Version" '-p:RestoreLockedMode=true'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}
if ($platform -eq 'windows') {
    $installerPath = Join-Path $ridDirectory 'Install-Marginalia.ps1'
    $associationPath = Join-Path $ridDirectory 'register-file-association.ps1'
    Copy-Item -LiteralPath $windowsInstallerSource -Destination $installerPath -Force
    Copy-Item -LiteralPath $windowsAssociationSource -Destination $associationPath -Force
    $payloadManifest = Join-Path $ridDirectory 'Marginalia.sha256'
    $applicationPrefix = $applicationDirectory.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $manifestLines = Get-ChildItem -LiteralPath $applicationDirectory -File -Recurse | Sort-Object FullName | ForEach-Object {
        $relativePath = $_.FullName.Substring($applicationPrefix.Length).Replace('\', '/')
        $fileHash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$fileHash  $relativePath"
    }
    [System.IO.File]::WriteAllLines($payloadManifest, $manifestLines, [System.Text.Encoding]::ASCII)
    Compress-Archive -Path @($applicationDirectory, $installerPath, $associationPath, $payloadManifest) -DestinationPath $archivePath -CompressionLevel Optimal
} else {
    & tar -czf $archivePath -C $ridDirectory 'Marginalia'
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
[System.IO.File]::WriteAllText("$archivePath.sha256", "$hash  $archiveName`n", [System.Text.Encoding]::ASCII)
$releaseDirectory = Join-Path $repositoryRoot "release/v$Version"
$ridReadme = Join-Path $ridDirectory 'README.txt'
$ridReadmeText = @"
Marginalia release v$Version

Platform: $platform / $architecture
Run: Marginalia/Marginalia$(if ($platform -eq 'windows') { '.exe' })
Portable archive: $archiveName
SHA-256 file: $archiveName.sha256

The Marginalia folder contains the full self-contained application. Keep all its files together.
"@
[System.IO.File]::WriteAllText($ridReadme, $ridReadmeText, [System.Text.Encoding]::ASCII)
$releaseReadme = Join-Path $releaseDirectory 'README.txt'
$releaseReadmeText = @"
Marginalia release v$Version

Portable, self-contained builds are organized by platform and architecture:
- windows/x64, macos/x64, macos/arm64, linux/x64, linux/arm64
- Each directory contains its portable archive and SHA-256 checksum.
- The Windows ZIP contains the portable payload and integrity manifest; the release workflow also builds a graphical Setup executable.
- Linux beta releases also include installable Debian packages for Debian/Ubuntu.

For Windows, run windows/x64/Marginalia-Setup-v$Version-windows-x64.exe, or extract the portable ZIP and run Install-Marginalia.ps1. On Linux, install the matching .deb on Debian/Ubuntu or use the portable TAR.GZ.
"@
[System.IO.File]::WriteAllText($releaseReadme, $releaseReadmeText, [System.Text.Encoding]::ASCII)

Write-Host "Release files created under release/v$Version/$platform/$architecture"
Write-Host "SHA-256: $hash"
