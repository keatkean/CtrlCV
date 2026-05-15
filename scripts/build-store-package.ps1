<#
.SYNOPSIS
    Builds the CtrlCV Microsoft Store .msixupload bundle (x64 + ARM64, ReleaseStore).

.DESCRIPTION
    Wraps the MSBuild invocation that produces a Partner Center-ready .msixupload
    from CtrlCV.Package.wapproj. Use this instead of the Visual Studio
    "Create App Packages" wizard, which has a known ordering bug on .NET 8 + WAP
    where obj\wappublish\<RID>\project.assets.json is not pre-populated and the
    build fails with "Assets file not found. Run a NuGet package restore...".

    Key behaviours:
      - Locates MSBuild via vswhere (no hard-coded path).
      - Cleans stale obj\wappublish to avoid half-built per-RID restore state.
      - Builds with Configuration=ReleaseStore so the STORE compile constant
        strips the GitHub UpdateChecker from the binary (Store Policy 10.8.1).
      - Bundles x64 + ARM64 into a single .msixupload (x86 and 32-bit ARM are
        intentionally omitted; the net8.0-windows10.0.19041.0 TFM already
        requires Windows 10 20H1+).
      - Leaves AppxPackageSigningEnabled=false; the Microsoft Store re-signs
        the package server-side with the publisher certificate.
      - Optionally bumps the manifest revision (-BumpRevision).
      - Optionally launches the Windows App Certification Kit on the bundle
        (-RunWack) and writes the report next to the package.

.PARAMETER BumpRevision
    Increment the fourth component of <Identity Version> in
    CtrlCV.Package\Package.appxmanifest before building. The Store rejects
    re-uploads of an already-submitted version.

.PARAMETER RunWack
    After a successful build, run the Windows App Certification Kit against
    the produced .msixbundle and write a report to AppPackages\wack-report.xml.
    Requires the WACK to be installed (ships with the Windows SDK).

.EXAMPLE
    pwsh .\scripts\build-store-package.ps1
    Build with the manifest's current version. Use for the first submission of
    a given version, or to rebuild without bumping.

.EXAMPLE
    pwsh .\scripts\build-store-package.ps1 -BumpRevision -RunWack
    Bump the revision (e.g. 1.4.2.0 -> 1.4.2.1), build, then run WACK.
#>
[CmdletBinding()]
param(
    [switch]$BumpRevision,
    [switch]$RunWack
)

$ErrorActionPreference = 'Stop'

# Resolve the repo root (one level above this script's folder).
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repoRoot

$wapProj    = Join-Path $repoRoot 'CtrlCV.Package\CtrlCV.Package.wapproj'
$manifest   = Join-Path $repoRoot 'CtrlCV.Package\Package.appxmanifest'
$appPackages = Join-Path $repoRoot 'AppPackages'

if (-not (Test-Path $wapProj))   { throw "Cannot find $wapProj" }
if (-not (Test-Path $manifest))  { throw "Cannot find $manifest" }

# ---------------------------------------------------------------------------
# 1. Locate MSBuild (any installed VS edition >= 2019 with MSBuild component).
# ---------------------------------------------------------------------------
$vsWhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vsWhere)) {
    throw "vswhere.exe not found at $vsWhere. Install Visual Studio 2019+ or Build Tools."
}

$msbuild = & $vsWhere -latest -requires Microsoft.Component.MSBuild `
                      -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild -or -not (Test-Path $msbuild)) {
    throw "MSBuild.exe not found via vswhere. Install the 'MSBuild' individual component."
}
Write-Host "Using MSBuild : $msbuild" -ForegroundColor DarkGray

# ---------------------------------------------------------------------------
# 2. Optionally bump the manifest's revision (the 4th component of Version).
# ---------------------------------------------------------------------------
[xml]$manifestXml = Get-Content -Raw -Path $manifest
$identityNode = $manifestXml.Package.Identity
$currentVersion = [Version]$identityNode.Version
Write-Host "Manifest      : $manifest"
Write-Host "Version (in)  : $currentVersion"

if ($BumpRevision) {
    $newVersion = [Version]::new(
        $currentVersion.Major,
        $currentVersion.Minor,
        $currentVersion.Build,
        $currentVersion.Revision + 1
    )
    $identityNode.Version = $newVersion.ToString()
    $manifestXml.Save($manifest)
    Write-Host "Version (out) : $newVersion (bumped)" -ForegroundColor Yellow
    $effectiveVersion = $newVersion
} else {
    $effectiveVersion = $currentVersion
}

# ---------------------------------------------------------------------------
# 3. Clean stale per-RID restore state; the wapproj's wappublish target reuses
#    obj\wappublish\<RID>\ between builds and a half-built state from a failed
#    earlier run (e.g. only obj\wappublish\win-x86\) breaks subsequent builds.
# ---------------------------------------------------------------------------
$wappublishDir = Join-Path $repoRoot 'obj\wappublish'
if (Test-Path $wappublishDir) {
    Remove-Item -Recurse -Force $wappublishDir
    Write-Host "Cleaned       : $wappublishDir" -ForegroundColor DarkGray
}

# ---------------------------------------------------------------------------
# 4. Build the .msixupload bundle.
# ---------------------------------------------------------------------------
$msbuildArgs = @(
    $wapProj,
    '/restore',
    '/p:Configuration=ReleaseStore',
    '/p:Platform=x64',
    '/p:AppxBundle=Always',
    '/p:AppxBundlePlatforms="x64|arm64"',
    '/p:UapAppxPackageBuildMode=StoreUpload',
    '/p:AppxPackageSigningEnabled=false',
    "/p:AppxPackageDir=$appPackages\\",
    '/v:minimal',
    '/nologo'
)

Write-Host ''
Write-Host 'Building .msixupload...' -ForegroundColor Cyan
& $msbuild @msbuildArgs
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed with exit code $LASTEXITCODE."
}

# ---------------------------------------------------------------------------
# 5. Report the produced artefacts.
# ---------------------------------------------------------------------------
$msixUpload = Get-ChildItem -Path $appPackages -Recurse -Filter '*.msixupload' `
              -ErrorAction SilentlyContinue |
              Sort-Object LastWriteTime -Descending |
              Select-Object -First 1

$msixBundle = Get-ChildItem -Path $appPackages -Recurse -Filter '*_x64_arm64*.msixbundle' `
              -ErrorAction SilentlyContinue |
              Sort-Object LastWriteTime -Descending |
              Select-Object -First 1

Write-Host ''
Write-Host 'Build succeeded.' -ForegroundColor Green
Write-Host "Version       : $effectiveVersion"
if ($msixUpload) { Write-Host ".msixupload   : $($msixUpload.FullName)" -ForegroundColor Green }
if ($msixBundle) { Write-Host ".msixbundle   : $($msixBundle.FullName)" -ForegroundColor DarkGray }

# ---------------------------------------------------------------------------
# 6. Optional Windows App Certification Kit run.
# ---------------------------------------------------------------------------
if ($RunWack) {
    $wackCandidates = @(
        'C:\Program Files (x86)\Windows Kits\10\App Certification Kit\appcert.exe',
        'C:\Program Files\Windows Kits\10\App Certification Kit\appcert.exe'
    )
    $appcert = $wackCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $appcert) {
        Write-Warning "WACK not found. Install via Visual Studio Installer > Individual components > 'Windows App Certification Kit', or download from https://aka.ms/wack"
        return
    }
    if (-not $msixBundle) {
        Write-Warning 'No .msixbundle found to validate; skipping WACK.'
        return
    }

    $reportPath = Join-Path $appPackages 'wack-report.xml'
    Write-Host ''
    Write-Host 'Running Windows App Certification Kit...' -ForegroundColor Cyan
    & $appcert reset | Out-Null
    & $appcert test -appxpackagepath $msixBundle.FullName -reportoutputpath $reportPath
    Write-Host "WACK report   : $reportPath" -ForegroundColor Green
}

Write-Host ''
Write-Host 'Next step: drag the .msixupload above into the Microsoft Partner Center' -ForegroundColor Cyan
Write-Host '           submission > Packages page, then tick "Windows 10/11 Desktop"' -ForegroundColor Cyan
Write-Host '           under Device family availability.' -ForegroundColor Cyan
