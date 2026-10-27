param(
    [string] $OutputDirectory,
    [string] $ScreenshotPath
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $PSScriptRoot 'ListingAssets' }
if (-not (Test-Path -LiteralPath $OutputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
if ([string]::IsNullOrWhiteSpace($ScreenshotPath)) {
    $ScreenshotPath = Join-Path $PSScriptRoot 'ListingAssets/Store-Screenshot-Source.png'
} else {
    $ScreenshotPath = (Resolve-Path -LiteralPath $ScreenshotPath).Path
}
$screenshot = $ScreenshotPath
$logo = Join-Path $PSScriptRoot 'Assets/Square150x150Logo.png'
$fontRegular = Join-Path $repositoryRoot 'MarkdownDesk/Assets/Fonts/IBMPlexSans.ttf'
$fontSemiBold = Join-Path $repositoryRoot 'MarkdownDesk/Assets/Fonts/IBMPlexSans-SemiBold.ttf'
$ffmpegCommand = Get-Command ffmpeg -ErrorAction SilentlyContinue
$ffmpeg = if ($ffmpegCommand) { $ffmpegCommand.Source } else { $null }

foreach ($requiredFile in @($screenshot, $logo, $fontRegular, $fontSemiBold)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "A source file required for Store artwork is missing: $requiredFile"
    }
}
if (-not $ffmpeg) { throw 'ffmpeg was not found in PATH.' }
if (-not (Test-Path -LiteralPath $OutputDirectory -PathType Container)) {
    throw "The artwork output directory does not exist: $OutputDirectory"
}

$escapeFilterPath = {
    param([string] $Path)
    $Path.Replace('\', '/').Replace(':', '\:')
}
$regularFontPath = & $escapeFilterPath $fontRegular
$semiBoldFontPath = & $escapeFilterPath $fontSemiBold
$upperE = [string][char]0x00C9
$lowerE = [string][char]0x00E9
$posterHeadline = "Lire. ${upperE}crire. Exporter."
$featureOne = "${upperE}diteur et aper${lowerE}u en direct"
$featureTwo = "Biblioth${lowerE}que et onglets r${lowerE}ordonnables"
$featureThree = "Export PDF int${lowerE}gr${lowerE}"
$squareFeatureOne = "Source + aper${lowerE}u en direct"
$squareFeatureTwo = "Onglets et biblioth${lowerE}que"
$footerText = "B${lowerE}ta publique Windows  |  Gratuit  |  Open source MIT"
$posterPath = Join-Path $OutputDirectory 'Store-Poster-9x16.png'
$squarePath = Join-Path $OutputDirectory 'Store-Hero-1x1.png'
$artworkMetadata = 'Generated for the markdim Microsoft Store listing from the official markdim icon and a real UI screenshot. UI screenshot captured from markdim 0.3.0-beta.6 in dark theme; no user document or private path is shown.'
$textDirectory = Join-Path $OutputDirectory 'artwork-text'
New-Item -ItemType Directory -Path $textDirectory -Force | Out-Null
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$textFiles = @{
    posterHeadline = $posterHeadline
    featureOne = $featureOne
    featureTwo = $featureTwo
    featureThree = $featureThree
    squareFeatureOne = $squareFeatureOne
    squareFeatureTwo = $squareFeatureTwo
    footer = $footerText
}
$textPaths = @{}
foreach ($name in $textFiles.Keys) {
    $textPath = Join-Path $textDirectory "$name.txt"
    [System.IO.File]::WriteAllText($textPath, $textFiles[$name], $utf8NoBom)
    $textPaths[$name] = & $escapeFilterPath $textPath
}

$posterFilter = @"
[1:v]scale=1260:-2:flags=lanczos[screen];
[2:v]scale=128:128:flags=lanczos[logo];
[0:v]drawbox=x=0:y=0:w=iw:h=8:color=0xA3B8FF:t=fill[base];
[base][logo]overlay=96:88:format=auto[brandmark];
[brandmark]drawtext=fontfile='$semiBoldFontPath':text='markdim':fontcolor=0xE6EAF0:fontsize=100:x=250:y=106[brand];
[brand]drawtext=fontfile='$semiBoldFontPath':textfile='$($textPaths.posterHeadline)':fontcolor=0xE6EAF0:fontsize=82:x=96:y=360[headline];
[headline]drawtext=fontfile='$regularFontPath':text='Vos documents Markdown. Du brouillon au PDF.':fontcolor=0xD0D6E0:fontsize=42:x=100:y=485[subhead];
[subhead]drawbox=x=76:y=630:w=1288:h=685:color=0x343C49:t=2[frame];
[frame][screen]overlay=90:642:format=auto[screenframe];
[screenframe]drawbox=x=96:y=1380:w=1248:h=2:color=0x343C49:t=fill[rule1];
[rule1]drawtext=fontfile='$semiBoldFontPath':textfile='$($textPaths.featureOne)':fontcolor=0xE6EAF0:fontsize=48:x=110:y=1430[feature1];
[feature1]drawbox=x=96:y=1510:w=1248:h=1:color=0x343C49:t=fill[rule2];
[rule2]drawtext=fontfile='$semiBoldFontPath':textfile='$($textPaths.featureTwo)':fontcolor=0xE6EAF0:fontsize=48:x=110:y=1550[feature2];
[feature2]drawbox=x=96:y=1630:w=1248:h=1:color=0x343C49:t=fill[rule3];
[rule3]drawtext=fontfile='$semiBoldFontPath':textfile='$($textPaths.featureThree)':fontcolor=0xE6EAF0:fontsize=48:x=110:y=1670[feature3];
[feature3]drawbox=x=96:y=1830:w=1248:h=2:color=0x343C49:t=fill[footerRule];
[footerRule]drawtext=fontfile='$regularFontPath':textfile='$($textPaths.footer)':fontcolor=0xA3B8FF:fontsize=34:x=100:y=1910[out]
"@ -replace "`r?`n", ''

$squareFilter = @"
[1:v]scale=1880:-2:flags=lanczos[screen];
[2:v]scale=160:160:flags=lanczos[logo];
[0:v]drawbox=x=0:y=0:w=iw:h=8:color=0xA3B8FF:t=fill[base];
[base][logo]overlay=130:105:format=auto[brandmark];
[brandmark]drawtext=fontfile='$semiBoldFontPath':text='markdim':fontcolor=0xE6EAF0:fontsize=120:x=340:y=125[brand];
[brand]drawtext=fontfile='$semiBoldFontPath':textfile='$($textPaths.posterHeadline)':fontcolor=0xE6EAF0:fontsize=110:x=140:y=405[headline];
[headline]drawtext=fontfile='$regularFontPath':text='Votre espace Markdown.':fontcolor=0xD0D6E0:fontsize=58:x=145:y=560[subhead];
[subhead]drawbox=x=120:y=700:w=1920:h=1018:color=0x343C49:t=2[frame];
[frame][screen]overlay=140:716:format=auto[screenframe];
[screenframe]drawbox=x=140:y=1800:w=1880:h=2:color=0x343C49:t=fill[rule1];
[rule1]drawtext=fontfile='$semiBoldFontPath':textfile='$($textPaths.squareFeatureOne)':fontcolor=0xE6EAF0:fontsize=46:x=145:y=1850[feature1];
[feature1]drawtext=fontfile='$semiBoldFontPath':textfile='$($textPaths.squareFeatureTwo)':fontcolor=0xE6EAF0:fontsize=46:x=850:y=1850[feature2];
[feature2]drawtext=fontfile='$semiBoldFontPath':text='Export PDF':fontcolor=0xE6EAF0:fontsize=46:x=1550:y=1850[feature3];
[feature3]drawbox=x=140:y=1975:w=1880:h=2:color=0x343C49:t=fill[footerRule];
[footerRule]drawtext=fontfile='$regularFontPath':textfile='$($textPaths.footer)':fontcolor=0xA3B8FF:fontsize=38:x=145:y=2040[out]
"@ -replace "`r?`n", ''

$posterArguments = @('-hide_banner', '-loglevel', 'error', '-y', '-f', 'lavfi', '-i', 'color=c=0x141820:s=1440x2160:d=1', '-i', $screenshot, '-i', $logo, '-filter_complex', $posterFilter, '-map', '[out]', '-frames:v', '1', '-map_metadata', '-1', '-compression_level', '9', '-metadata', "comment=$artworkMetadata", $posterPath)
& $ffmpeg @posterArguments
if ($LASTEXITCODE -ne 0) { throw "ffmpeg failed to create the 9:16 poster (exit code $LASTEXITCODE)." }

$squareArguments = @('-hide_banner', '-loglevel', 'error', '-y', '-f', 'lavfi', '-i', 'color=c=0x141820:s=2160x2160:d=1', '-i', $screenshot, '-i', $logo, '-filter_complex', $squareFilter, '-map', '[out]', '-frames:v', '1', '-map_metadata', '-1', '-compression_level', '9', '-metadata', "comment=$artworkMetadata", $squarePath)
& $ffmpeg @squareArguments
if ($LASTEXITCODE -ne 0) { throw "ffmpeg failed to create the 1:1 hero image (exit code $LASTEXITCODE)." }
Remove-Item -LiteralPath $textDirectory -Recurse -Force

Write-Host "Poster created: $posterPath (1440 x 2160)"
Write-Host "Square hero image created: $squarePath (2160 x 2160)"
