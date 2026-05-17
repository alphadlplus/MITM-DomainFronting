param(
    [Parameter(Mandatory = $true)]
    [string]$XrayZip,

    [string]$ConfigUrl = "https://raw.githubusercontent.com/patterniha/MITM-DomainFronting/refs/heads/main/Xray-config/MITM-DomainFronting.json",

    [switch]$NoConfigDownload
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$assetsDir = Join-Path $projectRoot "src\MITMDomainFronting.Windows\assets"
$xrayDir = Join-Path $assetsDir "xray"
$tempDir = Join-Path $projectRoot "artifacts\prepare-assets"

New-Item -ItemType Directory -Force -Path $assetsDir, $xrayDir, $tempDir | Out-Null

if (!(Test-Path -LiteralPath $XrayZip)) {
    throw "Xray zip not found: $XrayZip"
}

$configPath = Join-Path $assetsDir "MITM-DomainFronting.json"
if (!$NoConfigDownload) {
    Write-Host "Downloading MITM-DomainFronting config..."
    try {
        Invoke-WebRequest -Uri $ConfigUrl -OutFile $configPath
    } catch {
        if (!(Test-Path -LiteralPath $configPath)) {
            throw
        }
        Write-Warning "Could not download config. Using existing local config: $configPath"
    }
} elseif (!(Test-Path -LiteralPath $configPath)) {
    throw "Config file is missing: $configPath"
}

Write-Host "Extracting Xray zip..."
Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
Expand-Archive -LiteralPath $XrayZip -DestinationPath $tempDir -Force

$xrayExe = Get-ChildItem -LiteralPath $tempDir -Recurse -Filter "xray.exe" | Select-Object -First 1
if (!$xrayExe) {
    throw "Could not find xray.exe inside $XrayZip"
}

Copy-Item -LiteralPath $xrayExe.FullName -Destination (Join-Path $xrayDir "xray.exe") -Force

foreach ($dataFile in @("geoip.dat", "geosite.dat")) {
    $file = Get-ChildItem -LiteralPath $tempDir -Recurse -Filter $dataFile | Select-Object -First 1
    if (!$file) {
        throw "Could not find $dataFile inside $XrayZip"
    }
    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $xrayDir $dataFile) -Force
}

Write-Host "Assets ready:"
Write-Host " - $(Join-Path $assetsDir "MITM-DomainFronting.json")"
Write-Host " - $(Join-Path $xrayDir "xray.exe")"
Write-Host " - $(Join-Path $xrayDir "geoip.dat")"
Write-Host " - $(Join-Path $xrayDir "geosite.dat")"
