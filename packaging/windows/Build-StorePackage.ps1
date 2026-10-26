param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string] $PackageVersion,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^CN=.+')]
    [string] $Publisher,
    [Parameter(Mandatory = $true)]
    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Container })]
    [string] $LogoAssetsPath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$manifestTemplate = Join-Path $PSScriptRoot 'Store/AppxManifest.xml'
$storeAssets = (Resolve-Path -LiteralPath $LogoAssetsPath).Path
$outputDirectory = Join-Path $repositoryRoot 'release/store'
$layoutDirectory = Join-Path $outputDirectory 'layout'
$manifestPath = Join-Path $layoutDirectory 'AppxManifest.xml'
$packagePath = Join-Path $outputDirectory "markdim_$PackageVersion`_x64.msix"
$makeAppx = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86)) 'Windows Kits/10/bin/10.0.26100.0/x64/makeappx.exe'

if (-not (Test-Path -LiteralPath $makeAppx -PathType Leaf)) {
    throw "makeappx.exe not found at the expected Windows SDK path: $makeAppx"
}
if ($Publisher.Contains('&') -or $Publisher.Contains('<') -or $Publisher.Contains('>') -or $Publisher.Contains('"')) {
    throw 'Publisher contains characters that cannot be used in the package manifest.'
}

$requiredAssets = @('StoreLogo.png', 'Square150x150Logo.png', 'Square44x44Logo.png', 'Wide310x150Logo.png')
foreach ($asset in $requiredAssets) {
    if (-not (Test-Path -LiteralPath (Join-Path $storeAssets $asset) -PathType Leaf)) {
        throw "Required Microsoft Store package image is missing: $asset"
    }
}

if (Test-Path -LiteralPath $layoutDirectory) { Remove-Item -LiteralPath $layoutDirectory -Recurse -Force }
New-Item -ItemType Directory -Path $layoutDirectory | Out-Null
$publishDirectory = Join-Path $outputDirectory 'publish'
dotnet publish (Join-Path $repositoryRoot 'MarkdownDesk/MarkdownDesk.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publishDirectory
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
Copy-Item -Path (Join-Path $publishDirectory '*') -Destination $layoutDirectory -Recurse -Force
New-Item -ItemType Directory -Path (Join-Path $layoutDirectory 'Assets') | Out-Null
foreach ($asset in $requiredAssets) {
    Copy-Item -LiteralPath (Join-Path $storeAssets $asset) -Destination (Join-Path $layoutDirectory "Assets/$asset")
}

$manifest = [System.IO.File]::ReadAllText($manifestTemplate)
$manifest = $manifest.Replace('__PUBLISHER__', $Publisher).Replace('__VERSION__', $PackageVersion)
[System.IO.File]::WriteAllText($manifestPath, $manifest, [System.Text.UTF8Encoding]::new($false))
if (Test-Path -LiteralPath $packagePath) { Remove-Item -LiteralPath $packagePath -Force }
& $makeAppx pack /d $layoutDirectory /p $packagePath /o
if ($LASTEXITCODE -ne 0) { throw "makeappx pack failed with exit code $LASTEXITCODE" }
Write-Host "Unsigned Store package created: $packagePath"
Write-Host 'Upload the MSIX to Partner Center so Microsoft can sign and publish it.'
