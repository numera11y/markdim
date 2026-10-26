param(
    [Parameter(Mandatory = $true)]
    [string] $ApplicationPath
)

$exe = (Resolve-Path -LiteralPath $ApplicationPath).Path
$progId = 'markdim.Markdown'
$classes = 'HKCU:\Software\Classes'
New-Item -Path "$classes\$progId" -Force | Out-Null
Set-Item -Path "$classes\$progId" -Value 'markdim Markdown document'
New-Item -Path "$classes\$progId\DefaultIcon" -Force | Out-Null
Set-Item -Path "$classes\$progId\DefaultIcon" -Value "`"$exe`",0"
New-Item -Path "$classes\$progId\shell\open\command" -Force | Out-Null
Set-Item -Path "$classes\$progId\shell\open\command" -Value "`"$exe`" `"%1`""
$applicationKey = "$classes\Applications\markdim.exe"
New-Item -Path $applicationKey -Force | Out-Null
New-ItemProperty -Path $applicationKey -Name FriendlyAppName -Value 'markdim' -PropertyType String -Force | Out-Null
foreach ($extension in @('.md', '.markdown')) {
    New-Item -Path "$classes\$extension\OpenWithProgids" -Force | Out-Null
    New-ItemProperty -Path "$classes\$extension\OpenWithProgids" -Name $progId -Value '' -PropertyType String -Force | Out-Null
    New-Item -Path "$applicationKey\SupportedTypes" -Force | Out-Null
    New-ItemProperty -Path "$applicationKey\SupportedTypes" -Name $extension -Value '' -PropertyType String -Force | Out-Null
}
Write-Host 'markdim is available in Windows Open with. Choose it as the default app in Settings if desired.'
