param(
    [string] $OutputDirectory = (Join-Path $PSScriptRoot 'Assets')
)

$ErrorActionPreference = 'Stop'
$iconPath = Join-Path $PSScriptRoot '../../../MarkdownDesk/Assets/markdim.ico'
if (-not (Test-Path -LiteralPath $iconPath -PathType Leaf)) {
    throw "The markdim application icon was not found: $iconPath"
}

Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

function Write-StoreLogo([string] $FileName, [int] $CanvasWidth, [int] $CanvasHeight, [int] $LogoSize) {
    $icon = [System.Drawing.Icon]::new($iconPath, $LogoSize, $LogoSize)
    $bitmap = [System.Drawing.Bitmap]::new($CanvasWidth, $CanvasHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $left = [int][Math]::Floor(($CanvasWidth - $LogoSize) / 2)
        $top = [int][Math]::Floor(($CanvasHeight - $LogoSize) / 2)
        $graphics.DrawIcon($icon, [System.Drawing.Rectangle]::new($left, $top, $LogoSize, $LogoSize))
        $bitmap.Save((Join-Path $OutputDirectory $FileName), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
        $icon.Dispose()
    }
}

Write-StoreLogo 'StoreLogo.png' 50 50 50
Write-StoreLogo 'Square44x44Logo.png' 44 44 44
Write-StoreLogo 'Square150x150Logo.png' 150 150 150
Write-StoreLogo 'Wide310x150Logo.png' 310 150 126
Write-Host "Store logo assets generated from the existing markdim icon: $OutputDirectory"
