param(
    [string] $OutputDirectory = (Join-Path $PSScriptRoot 'ListingAssets')
)

$ErrorActionPreference = 'Stop'
$iconPath = Join-Path $PSScriptRoot '../../../MarkdownDesk/Assets/markdim.ico'
if (-not (Test-Path -LiteralPath $iconPath -PathType Leaf)) {
    throw "The markdim application icon was not found: $iconPath"
}
if (-not (Test-Path -LiteralPath $OutputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path

Add-Type -AssemblyName System.Drawing

function Write-StoreListingLogo([int] $Size, [string] $FileName) {
    $icon = [System.Drawing.Icon]::new($iconPath, $Size, $Size)
    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.DrawIcon($icon, [System.Drawing.Rectangle]::new(0, 0, $Size, $Size))
        $bitmap.Save((Join-Path $OutputDirectory $FileName), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
        $icon.Dispose()
    }
}

Write-StoreListingLogo 300 'Store-Listing-Logo-300x300.png'
Write-StoreListingLogo 150 'Store-Listing-Logo-150x150.png'
Write-StoreListingLogo 71 'Store-Listing-Logo-71x71.png'
Write-Host "Store listing logos generated from the existing markdim icon: $OutputDirectory"
