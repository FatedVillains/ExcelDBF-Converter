#Requires -Version 5.1
<#
.SYNOPSIS
    ExcelDBF Converter test runner
.EXAMPLE
    .\run-tests.ps1
    .\run-tests.ps1 -SkipPublish
    .\run-tests.ps1 -Configuration Debug
#>
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipPublish,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

$totalStart = Get-Date
$exitCode = 0

function Write-Step {
    param([string]$Name, [string]$Color = "Cyan")
    Write-Host ""
    Write-Host "===============================================================" -ForegroundColor $Color
    Write-Host "  $Name" -ForegroundColor $Color
    Write-Host "===============================================================" -ForegroundColor $Color
}

function Invoke-Step {
    param([string]$Name, [string]$Command)
    Write-Host ""
    Write-Host "  >> $Name" -ForegroundColor Yellow
    Write-Host "     $Command" -ForegroundColor DarkGray
    Invoke-Expression $Command
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [FAIL] $Name" -ForegroundColor Red
        $script:exitCode = 1
        return $false
    }
    Write-Host "  [OK] $Name" -ForegroundColor Green
    return $true
}

Write-Step "ExcelDBF Converter Test Runner" "Green"
Write-Host "  Directory: $scriptDir"
Write-Host "  Config: $Configuration"
Write-Host "  Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

# Step 1: Check .NET SDK
Write-Step "Step 1/5: Environment Check"
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "  [FAIL] dotnet not found. Install .NET 8 SDK." -ForegroundColor Red
    exit 1
}
$dotnetVersion = dotnet --version
Write-Host "  .NET SDK: $dotnetVersion" -ForegroundColor Green

# Step 2: Build
Write-Step "Step 2/5: Build"
if (-not $SkipBuild) {
    $restoreOk = Invoke-Step "Restore NuGet" "dotnet restore ExcelDbfConverter.sln"
    if (-not $restoreOk) {
        Write-Step "Build failed, aborting" "Red"
        exit 1
    }
    $buildOk = Invoke-Step "Build solution" "dotnet build ExcelDbfConverter.sln -c $Configuration --no-restore"
    if (-not $buildOk) {
        Write-Step "Build failed, aborting" "Red"
        exit 1
    }
} else {
    Write-Host "  >> Skipping build" -ForegroundColor DarkYellow
}

# Step 3: Unit tests
Write-Step "Step 3/5: Unit Tests"
$unitTestPassed = Invoke-Step "Infrastructure.Tests" "dotnet test tests/ExcelDbfConverter.Infrastructure.Tests/ExcelDbfConverter.Infrastructure.Tests.csproj -c $Configuration --no-build -v minimal"

# Step 4: Integration tests
Write-Step "Step 4/5: Integration Tests"
$integrationTestPassed = Invoke-Step "Integration.Tests" "dotnet test tests/ExcelDbfConverter.Integration.Tests/ExcelDbfConverter.Integration.Tests.csproj -c $Configuration --no-build -v minimal"

# Step 5: Publish
if (-not $SkipPublish) {
    Write-Step "Step 5/5: Publish (win-x64 self-contained)"
    $publishOk = Invoke-Step "Publish WPF app" "dotnet publish src/ExcelDbfConverter.Desktop/ExcelDbfConverter.Desktop.csproj -c $Configuration -r win-x64 --self-contained true -o publish -v minimal"

    if ($LASTEXITCODE -eq 0 -and (Test-Path "publish\ExcelDBFConverter.exe")) {
        $exeSize = (Get-Item "publish\ExcelDBFConverter.exe").Length
        $totalSize = (Get-ChildItem publish -Recurse | Measure-Object -Property Length -Sum).Sum
        Write-Host "  [OK] Published successfully" -ForegroundColor Green
        Write-Host "       EXE size: $([math]::Round($exeSize / 1MB, 1)) MB"
        Write-Host "       Total:   $([math]::Round($totalSize / 1MB, 1)) MB"
    }
} else {
    Write-Step "Step 5/5: Publish Skipped" "DarkYellow"
    Write-Host "  >> Use -SkipPublish to skip this step" -ForegroundColor DarkYellow
}

# Summary
Write-Step "Test Results Summary" "White"
$elapsed = (Get-Date) - $totalStart
Write-Host "  Elapsed:  $($elapsed.ToString('hh\:mm\:ss'))" -ForegroundColor White
$uStatus = if ($unitTestPassed) { "[PASS]" } else { "[FAIL]" }
$iStatus = if ($integrationTestPassed) { "[PASS]" } else { "[FAIL]" }
Write-Host "  Unit:       $uStatus" -ForegroundColor $(if ($unitTestPassed) { 'Green' } else { 'Red' })
Write-Host "  Integration: $iStatus" -ForegroundColor $(if ($integrationTestPassed) { 'Green' } else { 'Red' })
if ($SkipPublish) {
    Write-Host "  Publish:     [SKIP]" -ForegroundColor DarkYellow
} else {
    $pStatus = if ($LASTEXITCODE -eq 0) { "[PASS]" } else { "[FAIL]" }
    Write-Host "  Publish:     $pStatus" -ForegroundColor $(if ($LASTEXITCODE -eq 0) { 'Green' } else { 'Red' })
}
Write-Host ""

exit $exitCode
