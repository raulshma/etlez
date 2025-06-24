@echo off
echo ========================================
echo ETL Framework Pipeline Creation Tests
echo ========================================
echo.

REM Set default values
set BASE_URL=http://localhost:8080
set TEST_TYPE=all

REM Check if PowerShell is available
powershell -Command "Get-Host" >nul 2>&1
if errorlevel 1 (
    echo ERROR: PowerShell is not available or not in PATH
    echo Please install PowerShell or run the tests manually
    pause
    exit /b 1
)

echo Available test options:
echo 1. Run all tests (basic + advanced + edge cases)
echo 2. Run basic tests only
echo 3. Run advanced tests only
echo 4. Run edge case tests only
echo 5. Custom URL and test type
echo.

set /p choice="Enter your choice (1-5): "

if "%choice%"=="1" (
    set TEST_TYPE=all
    echo Running all tests...
) else if "%choice%"=="2" (
    set TEST_TYPE=basic
    echo Running basic tests...
) else if "%choice%"=="3" (
    set TEST_TYPE=advanced
    echo Running advanced tests...
) else if "%choice%"=="4" (
    set TEST_TYPE=edge
    echo Running edge case tests...
) else if "%choice%"=="5" (
    set /p BASE_URL="Enter API base URL (default: http://localhost:8080): "
    if "%BASE_URL%"=="" set BASE_URL=http://localhost:8080
    
    echo.
    echo Test type options: all, basic, advanced, edge
    set /p TEST_TYPE="Enter test type (default: all): "
    if "%TEST_TYPE%"=="" set TEST_TYPE=all
) else (
    echo Invalid choice. Running all tests with default settings...
    set TEST_TYPE=all
)

echo.
echo Configuration:
echo - Base URL: %BASE_URL%
echo - Test Type: %TEST_TYPE%
echo.

REM Check if the API is accessible
echo Checking API accessibility...
powershell -Command "try { Invoke-RestMethod -Uri '%BASE_URL%/api/pipelines' -Method GET -TimeoutSec 5 -SkipCertificateCheck | Out-Null; Write-Host 'API is accessible' -ForegroundColor Green } catch { Write-Host 'WARNING: API may not be accessible - %BASE_URL%' -ForegroundColor Yellow; Write-Host 'Error: ' + $_.Exception.Message -ForegroundColor Red }"

echo.
echo Starting tests...
echo ========================================

REM Run the PowerShell test script
powershell -ExecutionPolicy Bypass -File "test-pipeline-creation.ps1" -BaseUrl "%BASE_URL%" -TestType "%TEST_TYPE%"

echo.
echo ========================================
echo Tests completed!
echo.
echo Check the generated test results file for detailed information.
echo.
pause
