<#
.SYNOPSIS
    Produces an unsigned sideload-ready MSIX using dotnet publish + Windows SDK tools.
    Requires: .NET SDK, Windows SDK 10.0.17763+ (makeappx.exe, makepri.exe).
    No Visual Studio or Windows App Packaging workload required.

.PARAMETER Configuration
    Release (default) or Debug.

.PARAMETER Platform
    x64 (default).

.EXAMPLE
    .\build-msix.ps1
    .\build-msix.ps1 -Configuration Debug
#>
param(
    [string]$Configuration    = "Release",
    [string]$Platform         = "x64",
    # Path to the .pfx used to sign the MSIX. Defaults to PdfExtractToSkill.pfx
    # next to this script. Set to $null or use -SkipSigning to skip.
    [string]$CertificatePath  = "$PSScriptRoot\PdfExtractToSkill.pfx",
    [string]$CertificatePassword = "",
    [switch]$SkipSigning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root         = $PSScriptRoot
$wpfProj      = "$root\src\PdfExtractToSkill\PdfExtractToSkill.csproj"
$pkgDir       = "$root\src\PdfExtractToSkill.Packaging"
$publishDir   = "$pkgDir\bin\$Platform\$Configuration\publish"
$layoutDir    = "$pkgDir\bin\$Platform\$Configuration\layout"
$outputDir    = "$pkgDir\AppPackages"
$msixOut      = "$outputDir\PdfExtractToSkill_1.0.0.0_${Platform}_Test\PdfExtractToSkill_1.0.0.0_$Platform.msix"

# Locate Windows SDK tools (prefer latest installed version)
$sdkRoot  = "C:\Program Files (x86)\Windows Kits\10\bin"
$sdkVer   = Get-ChildItem $sdkRoot -Directory |
                Where-Object { $_.Name -match '^\d+\.' } |
                Sort-Object Name -Descending |
                Select-Object -First 1 -ExpandProperty Name
$sdkBin   = "$sdkRoot\$sdkVer\x64"
$makeappx = "$sdkBin\makeappx.exe"
$makepri  = "$sdkBin\MakePri.exe"

foreach ($tool in $makeappx, $makepri) {
    if (-not (Test-Path $tool)) { throw "Required tool not found: $tool" }
}

Write-Host "==> Using Windows SDK $sdkVer"
Write-Host "==> Configuration: $Configuration | Platform: $Platform"

# ── 1. Publish WPF app (self-contained, win-x64) ─────────────────────────────
Write-Host "`n==> Publishing WPF app..."
& dotnet publish $wpfProj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# ── 2. Assemble package layout ────────────────────────────────────────────────
Write-Host "`n==> Assembling MSIX layout..."
if (Test-Path $layoutDir) { Remove-Item $layoutDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $layoutDir | Out-Null

# Copy all published files
Copy-Item -Path "$publishDir\*" -Destination $layoutDir -Recurse -Force

# Bundle extract.py
Copy-Item -Path "$root\extract.py" -Destination "$layoutDir\extract.py" -Force

# Manifest (makeappx requires the file named AppxManifest.xml)
Copy-Item -Path "$pkgDir\Package.appxmanifest" -Destination "$layoutDir\AppxManifest.xml" -Force

# Brand assets
New-Item -ItemType Directory -Force -Path "$layoutDir\Assets" | Out-Null
Copy-Item -Path "$pkgDir\Assets\*" -Destination "$layoutDir\Assets\" -Force

# ── 3. Generate resources.pri ─────────────────────────────────────────────────
Write-Host "`n==> Generating resources.pri..."
$priConfig = "$layoutDir\priconfig.xml"
Push-Location $layoutDir
try {
    & $makepri createconfig /cf $priConfig /dq en-US /o
    if ($LASTEXITCODE -ne 0) { throw "makepri createconfig failed" }

    & $makepri new /pr $layoutDir /cf $priConfig /of "$layoutDir\resources.pri" /o
    if ($LASTEXITCODE -ne 0) { throw "makepri new failed" }
} finally {
    Pop-Location
}
Remove-Item $priConfig -ErrorAction SilentlyContinue

# ── 4. Pack MSIX ─────────────────────────────────────────────────────────────
Write-Host "`n==> Packing MSIX..."
$msixDir = Split-Path $msixOut -Parent
New-Item -ItemType Directory -Force -Path $msixDir | Out-Null

& $makeappx pack /v /d $layoutDir /p $msixOut /o
if ($LASTEXITCODE -ne 0) { throw "makeappx pack failed" }

Write-Host "`n==> MSIX created: $msixOut"

# ── 5. Sign MSIX ──────────────────────────────────────────────────────────────
if (-not $SkipSigning) {
    if (-not (Test-Path $CertificatePath)) {
        Write-Warning "Certificate not found at '$CertificatePath' — package is unsigned. Place your PFX there or pass -CertificatePath."
    } else {
        $signtool = "$sdkBin\signtool.exe"
        if (-not (Test-Path $signtool)) { throw "signtool.exe not found at: $signtool" }

        Write-Host "`n==> Signing MSIX..."
        $signArgs = @("sign", "/fd", "SHA256", "/a", "/f", $CertificatePath)
        if ($CertificatePassword) { $signArgs += "/p", $CertificatePassword }
        $signArgs += $msixOut

        & $signtool @signArgs
        if ($LASTEXITCODE -ne 0) { throw "signtool failed (exit $LASTEXITCODE)" }

        Write-Host "==> Signed: $msixOut"
    }
}
