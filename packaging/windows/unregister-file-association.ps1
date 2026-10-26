param(
    [Parameter(Mandatory = $true)]
    [string] $ApplicationPath
)

$ErrorActionPreference = 'Stop'
$progId = 'Marginalia.Markdown'
$classes = 'HKCU:\Software\Classes'
$commandKey = Join-Path $classes "$progId\shell\open\command"
$commandEntry = Get-Item -LiteralPath $commandKey -ErrorAction SilentlyContinue
$registeredCommand = if ($commandEntry) { $commandEntry.GetValue('') } else { $null }
if ([string]::IsNullOrWhiteSpace($registeredCommand)) { exit 0 }

$registeredExecutable = $null
$value = $registeredCommand.TrimStart()
if ($value.StartsWith('"')) {
    $endQuote = $value.IndexOf('"', 1)
    if ($endQuote -gt 1) { $registeredExecutable = $value.Substring(1, $endQuote - 1) }
} else {
    $registeredExecutable = ($value -split '\s+', 2)[0]
}

$targetExecutable = [System.IO.Path]::GetFullPath($ApplicationPath)
if (-not [string]::Equals($registeredExecutable, $targetExecutable, [StringComparison]::OrdinalIgnoreCase)) {
    Write-Host 'A newer Marginalia installation owns the file association; leaving it in place.'
    exit 0
}

foreach ($extension in @('.md', '.markdown')) {
    $openWithKey = Join-Path $classes "$extension\OpenWithProgids"
    Remove-ItemProperty -LiteralPath $openWithKey -Name $progId -ErrorAction SilentlyContinue
}
Remove-Item -LiteralPath (Join-Path $classes $progId) -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $classes 'Applications\Marginalia.exe') -Recurse -Force -ErrorAction SilentlyContinue
