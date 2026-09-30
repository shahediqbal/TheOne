param(
    [Parameter(Mandatory=$true)][uri]$WebsiteOrigin,
    [Parameter(Mandatory=$true)][uri]$ApiOrigin
)
$ErrorActionPreference = 'Stop'
foreach ($origin in @($WebsiteOrigin, $ApiOrigin)) {
    if ($origin.Scheme -ne 'https' -or $origin.UserInfo -or $origin.AbsolutePath -ne '/' -or $origin.Query -or $origin.Fragment) {
        throw 'Supply HTTPS origins only, without credentials, paths, query strings or fragments.'
    }
}
$repo = Split-Path $PSScriptRoot -Parent
$out = Join-Path $repo ('artifacts/windows-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
if (Test-Path -LiteralPath $out) { throw 'Output directory already exists.' }
New-Item -ItemType Directory -Path $out | Out-Null
function Run-Checked([string]$Exe, [string[]]$Arguments) {
    & $Exe @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Exe failed with exit code $LASTEXITCODE" }
}
function Copy-PublicTree([string]$Source, [string]$Destination) {
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    foreach ($entry in Get-ChildItem -LiteralPath $Source -Force) {
        if ($entry.Name -match '^\.env($|\.)|^\.certs$|^appsettings\..*\.json$|\.(pfx|p12|pem|key)$') { continue }
        $target = Join-Path $Destination $entry.Name
        if ($entry.PSIsContainer) { Copy-PublicTree $entry.FullName $target }
        else { Copy-Item -LiteralPath $entry.FullName -Destination $target }
    }
}
$previousOrigin = $env:VITE_WEBSITE_PUBLIC_ORIGIN
Push-Location $repo
try {
    # Build for the destination OS; do not upload Linux node_modules to IIS.
    Run-Checked 'dotnet' @('publish','src/TheOne/TheOne.API.csproj','-c','Release','-r','win-x64','--self-contained','false','-o',"$out/api-build",'/p:UseAppHost=false')
    Copy-PublicTree "$out/api-build" "$out/api"
    # Remove only this new, verified build directory, which may contain local appsettings.
    $buildPath = [IO.Path]::GetFullPath("$out/api-build")
    if (-not $buildPath.StartsWith([IO.Path]::GetFullPath($out) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid build output path.' }
    Remove-Item -LiteralPath $buildPath -Recurse -Force
    Push-Location 'apps/website'
    try {
        Run-Checked 'npm.cmd' @('ci')
        Run-Checked 'npm.cmd' @('run','build')
        Copy-PublicTree '.next/standalone' "$out/website"
        Copy-PublicTree '.next/static' "$out/website/apps/website/.next/static"
        Copy-PublicTree 'public' "$out/website/apps/website/public"
    } finally { Pop-Location }
    Copy-Item -LiteralPath 'deployment/iis/website.web.config' -Destination "$out/website/web.config"
    $env:VITE_WEBSITE_PUBLIC_ORIGIN = $WebsiteOrigin.GetLeftPart([UriPartial]::Authority)
    Push-Location 'apps/management'
    try {
        Run-Checked 'npm.cmd' @('ci')
        Run-Checked 'npm.cmd' @('run','build')
        Copy-PublicTree 'dist' "$out/staff"
    } finally { Pop-Location }
    $staffConfig = Get-Content -LiteralPath 'deployment/iis/staff.web.config' -Raw
    $staffConfig.Replace('API_ORIGIN_PLACEHOLDER',$ApiOrigin.Authority) | Set-Content -LiteralPath "$out/staff/web.config" -Encoding UTF8
    @('Windows x64 / framework-dependent .NET 10', 'Configure runtime secrets before starting.', 'No migrations or deployment performed.', 'Website build origin: ' + $WebsiteOrigin.AbsoluteUri) | Set-Content -LiteralPath "$out/README.txt"
    Write-Host "Packages prepared in $out. Review configuration and contents before uploading."
} finally {
    $env:VITE_WEBSITE_PUBLIC_ORIGIN = $previousOrigin
    Pop-Location
}
