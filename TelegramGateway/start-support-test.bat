@echo off
setlocal

cd /d "%~dp0"

copy /Y "support-test.html" "index.html" >nul
if errorlevel 1 (
    echo Could not create index.html from support-test.html.
    pause
    exit /b 1
)

where py >nul 2>&1
if not errorlevel 1 (
    start "Support Test Server" cmd /k "cd /d ""%~dp0"" && py -m http.server 8080"
    goto openBrowser
)

where python >nul 2>&1
if not errorlevel 1 (
    start "Support Test Server" cmd /k "cd /d ""%~dp0"" && python -m http.server 8080"
    goto openBrowser
)

echo Python was not found. Install Python first.
pause
exit /b 1

:openBrowser
timeout /t 1 /nobreak >nul
start "" "http://localhost:8080/index.html"
echo Support test page opened at http://localhost:8080/index.html
endlocal
