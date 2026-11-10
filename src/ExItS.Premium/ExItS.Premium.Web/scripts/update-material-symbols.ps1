# Regenerates the self-hosted Material Symbols Rounded subset from src/icons/registry.ts.
# Run from ExItS.Premium.Web. This is a build-time download, not a runtime dependency.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$registry = Get-Content (Join-Path $root "src\icons\registry.ts") -Raw
$names = [regex]::Matches($registry, '^\s+"([a-z0-9_]+)",\s*$', "Multiline") | ForEach-Object { $_.Groups[1].Value }
if ($names.Count -lt 1) { throw "No icon names found in registry.ts" }
$joined = $names -join ","
$url = "https://fonts.googleapis.com/css2?family=Material+Symbols+Rounded:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200&icon_names=$joined"
$headers = @{ "User-Agent" = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36" }
$css = (Invoke-WebRequest -Uri $url -Headers $headers -UseBasicParsing).Content
$fontUrl = [regex]::Match($css, "url\((https://[^)]+)\)").Groups[1].Value
if (-not $fontUrl) { throw "Google Fonts did not return a subset. Check the icon names." }
$dest = Join-Path $root "src\icons\material-symbols-rounded.woff2"
Invoke-WebRequest -Uri $fontUrl -OutFile $dest -UseBasicParsing
Write-Host "Wrote $($names.Count) icons to $dest ($((Get-Item $dest).Length) bytes)"
