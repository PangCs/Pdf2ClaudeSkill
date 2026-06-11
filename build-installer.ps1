<#
.SYNOPSIS
    Publishes the WPF app then compiles an Inno Setup installer.
    Requires: .NET SDK, Inno Setup 6 (https://jrsoftware.org/isinfo.php)

.PARAMETER Configuration
    Release (default) or Debug.

.EXAMPLE
    .\build-installer.ps1
#>
param(
    [string]$Configuration = "Release",
    [string]$Platform      = "x64"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root       = $PSScriptRoot
$wpfProj    = "$root\src\PdfExtractToSkill\PdfExtractToSkill.csproj"
$publishDir = "$root\src\PdfExtractToSkill.Packaging\bin\$Platform\$Configuration\publish"
$issScript  = "$root\installer.iss"
$outputDir  = "$root\installer-output"

# -- 1. Locate ISCC.exe -------------------------------------------------------
$iscc = Get-Command "iscc" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
if (-not $iscc) {
    $iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $iscc)) {
    throw "ISCC.exe not found. Install Inno Setup 6 from https://jrsoftware.org/isinfo.php"
}
Write-Host "==> Using ISCC: $iscc"

# -- 2. Publish WPF app -------------------------------------------------------
Write-Host "`n==> Publishing WPF app ($Configuration / $Platform)..."
& dotnet publish $wpfProj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# -- 3. Compile installer -----------------------------------------------------
Write-Host "`n==> Compiling installer..."
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
& $iscc $issScript
if ($LASTEXITCODE -ne 0) { throw "ISCC compilation failed" }

Write-Host "`n==> Installer ready in: $outputDir"
Get-ChildItem $outputDir -Filter "*.exe" | ForEach-Object { Write-Host "    $($_.Name)" }
