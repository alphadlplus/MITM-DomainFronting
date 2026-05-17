param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$Publish
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $projectRoot "src\MITMDomainFronting.Windows\MITMDomainFronting.Windows.csproj"

$sdkList = & dotnet --list-sdks
if (!$sdkList) {
    throw "No .NET SDK is installed. Install the .NET 8 SDK, then run this script again: https://dotnet.microsoft.com/download/dotnet/8.0"
}

if ($Publish) {
    $output = Join-Path $projectRoot "publish\win-x64"
    dotnet publish $project -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=false -o $output
    Write-Host "Published to $output"
    exit
}

dotnet build $project -c $Configuration

