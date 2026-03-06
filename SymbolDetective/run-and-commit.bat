@echo off
setlocal

REM ============================================================
REM  SymbolDetective — client-side run-and-commit helper
REM  Usage: run-and-commit.bat [extra SymbolDetective args]
REM
REM  Workflow:
REM    1. git pull  (get latest exe from dev machine)
REM    2. Run SymbolDetective.exe against the TC server
REM    3. git add output/  +  git commit  (push results back)
REM
REM  Common overrides:
REM    run-and-commit.bat -symbol forklift_truck_symbol_1 -rev A
REM    run-and-commit.bat -symbol some_other_symbol -rev B
REM ============================================================

set SCRIPT_DIR=%~dp0
cd /d "%SCRIPT_DIR%"

echo.
echo ========================================
echo  Step 1: git pull
echo ========================================
git pull
if errorlevel 1 (
    echo [ERROR] git pull failed. Aborting.
    exit /b 1
)

echo.
echo ========================================
echo  Step 2: Run SymbolDetective
echo ========================================
bin\Debug\SymbolDetective.exe -host https://tcweb03.dev.rolls-royce-smr.com:3000/tc %*
if errorlevel 1 (
    echo [WARN] SymbolDetective exited with an error code — output may be incomplete.
)

echo.
echo ========================================
echo  Step 3: Commit output to git
echo ========================================
git add output\
git status --short output\

REM Only commit if there is something staged
git diff --cached --quiet
if errorlevel 1 (
    REM Build a commit message that includes the timestamp
    for /f "tokens=1-5 delims=/ " %%a in ("%DATE%") do set DATESTAMP=%%c-%%b-%%a
    for /f "tokens=1-2 delims=: " %%a in ("%TIME%") do set TIMESTAMP=%%a%%b
    git commit -m "SymbolDetective output %DATESTAMP% %TIMESTAMP%"
    echo.
    echo [OK] Output committed. Push when ready:  git push
) else (
    echo [INFO] No changes to output — nothing committed.
)

endlocal
