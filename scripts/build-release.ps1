param(
    [switch]$FrameworkDependent,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$dotnet = if (Get-Command dotnet -ErrorAction SilentlyContinue) { "dotnet" } else { "C:\Program Files\dotnet\dotnet.exe" }
$publishDir = Join-Path $repoRoot "publish\win-x64"
$appProject = Join-Path $repoRoot "src\LaraLauncher.App\LaraLauncher.App.csproj"
$solution = Join-Path $repoRoot "LaraLauncher.sln"
$issScript = Join-Path $repoRoot "installer\setup.iss"

Write-Host "=== LaraLauncher Release Pipeline ===" -ForegroundColor Magenta

if (-not $SkipTests) {
    Write-Host "`n[1/3] Ejecutando pruebas unitarias (Release)..." -ForegroundColor Cyan
    & $dotnet test $solution -c Release
    if ($LASTEXITCODE -ne 0) { throw "Fallaron las pruebas unitarias." }
}

Write-Host "`n[2/3] Publicando LaraLauncher (win-x64)..." -ForegroundColor Cyan
if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

$selfContained = if ($FrameworkDependent) { "false" } else { "true" }
& $dotnet publish $appProject `
    -c Release `
    -r win-x64 `
    --self-contained $selfContained `
    -p:PublishSingleFile=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishDir

if ($LASTEXITCODE -ne 0) { throw "Falló la publicación de LaraLauncher." }
Write-Host "Publicado en: $publishDir" -ForegroundColor Green

Write-Host "`n[3/3] Generando instalador con Inno Setup..." -ForegroundColor Cyan
$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($iscc) {
    & $iscc $issScript
    if ($LASTEXITCODE -ne 0) { throw "Falló la compilación del instalador Inno Setup." }
    Write-Host "`nInstalador generado en: $(Join-Path $repoRoot 'installer\Output')" -ForegroundColor Green
}
else {
    Write-Host "Inno Setup 6 (ISCC.exe) no está instalado todavía." -ForegroundColor Yellow
    Write-Host "Instálalo con: winget install --id JRSoftware.InnoSetup --exact" -ForegroundColor Yellow
    Write-Host "Luego vuelve a ejecutar este script para generar LaraLauncherSetup-1.0.0.exe." -ForegroundColor Yellow
}
