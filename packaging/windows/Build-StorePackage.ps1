param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string] $PackageVersion,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^CN=.+')]
    [string] $Publisher,
    [string] $LogoAssetsPath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$manifestTemplate = Join-Path $PSScriptRoot 'Store/AppxManifest.xml'
$storeAssets = if ([string]::IsNullOrWhiteSpace($LogoAssetsPath)) {
    Join-Path $PSScriptRoot 'Store/Assets'
} else {
    (Resolve-Path -LiteralPath $LogoAssetsPath).Path
}
$outputDirectory = Join-Path $repositoryRoot 'release/store'
$layoutDirectory = Join-Path $outputDirectory 'layout'
$manifestPath = Join-Path $layoutDirectory 'AppxManifest.xml'
$packagePath = Join-Path $outputDirectory "markdim_$PackageVersion`_x64.msix"
$sdkBinaryDirectory = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)) 'Windows Kits/10/bin'
$makeAppxCommand = Get-Command makeappx.exe -ErrorAction SilentlyContinue
$makeAppx = if ($makeAppxCommand) { $makeAppxCommand.Source } else {
    Get-ChildItem -LiteralPath $sdkBinaryDirectory -Filter makeappx.exe -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\makeappx\.exe$' } |
        Sort-Object { [version]([regex]::Match($_.FullName, 'bin\\([^\\]+)\\').Groups[1].Value) } -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

if (-not $makeAppx -or -not (Test-Path -LiteralPath $makeAppx -PathType Leaf)) {
    throw "makeappx.exe was not found in PATH or under the Windows SDK directory: $sdkBinaryDirectory"
}
$packageVersionParts = @($PackageVersion.Split('.') | ForEach-Object { [int]$_ })
if (($packageVersionParts | Where-Object { $_ -gt 65535 }).Count -gt 0 -or $packageVersionParts[0] -eq 0 -or $packageVersionParts[3] -ne 0) {
    throw 'Store package versions require a nonzero first component, components no greater than 65535, and a fourth component of 0. The package version is independent of the app display version.'
}
if ($Publisher.Contains('&') -or $Publisher.Contains('<') -or $Publisher.Contains('>') -or $Publisher.Contains('"')) {
    throw 'Publisher contains characters that cannot be used in the package manifest.'
}

$requiredAssets = @('StoreLogo.png', 'Square150x150Logo.png', 'Square44x44Logo.png', 'Wide310x150Logo.png')
if (-not (Test-Path -LiteralPath $storeAssets -PathType Container)) {
    & (Join-Path $PSScriptRoot 'Store/Create-StoreAssets.ps1') -OutputDirectory $storeAssets
}
foreach ($asset in $requiredAssets) {
    if (-not (Test-Path -LiteralPath (Join-Path $storeAssets $asset) -PathType Leaf)) {
        throw "Required Microsoft Store package image is missing: $asset"
    }
}

if (Test-Path -LiteralPath $layoutDirectory) { Remove-Item -LiteralPath $layoutDirectory -Recurse -Force }
$publishDirectory = Join-Path $outputDirectory 'publish'
if (Test-Path -LiteralPath $publishDirectory) { Remove-Item -LiteralPath $publishDirectory -Recurse -Force }
New-Item -ItemType Directory -Path $layoutDirectory | Out-Null
dotnet publish (Join-Path $repositoryRoot 'MarkdownDesk/MarkdownDesk.csproj') -c Release -r win-x64 --self-contained true -o $publishDirectory
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
Copy-Item -Path (Join-Path $publishDirectory '*') -Destination $layoutDirectory -Recurse -Force
New-Item -ItemType Directory -Path (Join-Path $layoutDirectory 'Assets') -Force | Out-Null
foreach ($asset in $requiredAssets) {
    Copy-Item -LiteralPath (Join-Path $storeAssets $asset) -Destination (Join-Path $layoutDirectory "Assets/$asset")
}
$debugSymbols = Join-Path $layoutDirectory 'markdim.pdb'
if (Test-Path -LiteralPath $debugSymbols) { Remove-Item -LiteralPath $debugSymbols -Force }

$manifest = [System.IO.File]::ReadAllText($manifestTemplate)
$escapedPublisher = [System.Security.SecurityElement]::Escape($Publisher)
$manifest = $manifest.Replace('__PUBLISHER__', $escapedPublisher).Replace('__VERSION__', $PackageVersion)
[System.IO.File]::WriteAllText($manifestPath, $manifest, [System.Text.UTF8Encoding]::new($false))
if (Test-Path -LiteralPath $packagePath) { Remove-Item -LiteralPath $packagePath -Force }
& $makeAppx pack /d $layoutDirectory /p $packagePath /o
if ($LASTEXITCODE -ne 0) { throw "makeappx pack failed with exit code $LASTEXITCODE" }
$validationDirectory = Join-Path $outputDirectory 'validation-layout'
if (Test-Path -LiteralPath $validationDirectory) { Remove-Item -LiteralPath $validationDirectory -Recurse -Force }
& $makeAppx unpack /p $packagePath /d $validationDirectory
if ($LASTEXITCODE -ne 0) { throw "makeappx unpack validation failed with exit code $LASTEXITCODE" }
Write-Host "Unsigned Store package created: $packagePath"
Write-Host 'Upload the MSIX to Partner Center so Microsoft can sign and publish it.'
