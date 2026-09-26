$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'UsdKrwWidget.csproj'
$output = Join-Path $PSScriptRoot 'dist\win-x64'

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}

Write-Host 'Publishing USD/KRW Widget...'
dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -o $output

Write-Host ''
Write-Host "Done: $output\UsdKrwWidget.exe"
