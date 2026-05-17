param(
    [string]$InnoCompilerPath = ""
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $projectRoot "publish\win-x64"
$installerScript = Join-Path $projectRoot "installer\MITMDomainFrontingWindows.iss"
$defaultInnoPaths = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
)

if (!(Test-Path -LiteralPath $publishDir)) {
    Write-Host "Publish folder is missing. Publishing Windows app first..."
    & (Join-Path $PSScriptRoot "Build-Windows.ps1") -Publish
}

if ($InnoCompilerPath -eq "") {
    $InnoCompilerPath = $defaultInnoPaths | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

if (!$InnoCompilerPath -or !(Test-Path -LiteralPath $InnoCompilerPath)) {
    throw "Inno Setup compiler not found. Install Inno Setup 6, or pass -InnoCompilerPath 'C:\Path\To\ISCC.exe'."
}

& $InnoCompilerPath $installerScript

Write-Host ""
Write-Host "Installer ready in:"
Write-Host (Join-Path $projectRoot "artifacts\installer")

