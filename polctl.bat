@echo off
:: Setlocal ensures variables don't bleed into your open cmd session environment
setlocal enabledelayedexpansion

:: Base structural layout configurations
set "BINARY_DIR=%~dp0"
set "TARGET_MODULE=%~1"

:: If the first argument is completely empty, jump straight to the usage guide.
if "%TARGET_MODULE%"=="" (
    goto :PrintUsage
)

:: Map target keywords into dynamic service profile definitions
if "%TARGET_MODULE%"=="auth" (
    set "SERVICE_NAME=POLAuth"
    set "DISPLAY_NAME=Project Crystal: PlayOnline Authentication Server"
    set "DESCRIPTION=Main authentication server for PlayOnline. Part of the Project Crystal PlayOnline Server Emulator suite."
    set "HAS_PASSTHROUGH=true"
    goto :ShiftAndRoute
)
if "%TARGET_MODULE%"=="profile" (
    set "SERVICE_NAME=POLProfile"
    set "DISPLAY_NAME=Project Crystal: PlayOnline Profile Server"
    set "DESCRIPTION=Profile server for PolPro data requests. Part of the Project Crystal PlayOnline Server Emulator suite."
    set "HAS_PASSTHROUGH=false"
    goto :ShiftAndRoute
)
if "%TARGET_MODULE%"=="patch" (
    set "SERVICE_NAME=POLPatch"
    set "DISPLAY_NAME=Project Crystal: PlayOnline Patch Server"
    set "DESCRIPTION=Patch server for version verification and client updating. Part of the Project Crystal PlayOnline Server Emulator suite."
    set "HAS_PASSTHROUGH=false"
    goto :ShiftAndRoute
)

:: Intercept management commands missing their program type prefix and route to usage guide
if "%TARGET_MODULE%"=="install-service" goto :PrintUsage
if "%TARGET_MODULE%"=="remove-service"  goto :PrintUsage
if "%TARGET_MODULE%"=="start"           goto :PrintUsage
if "%TARGET_MODULE%"=="stop"            goto :PrintUsage
if "%TARGET_MODULE%"=="restart"         goto :PrintUsage

:: Safe Implicit Passthrough Setup (POLAuth fallback)
set "SERVICE_NAME=POLAuth"
set "DISPLAY_NAME=Project Crystal: PlayOnline Authentication Server"
set "DESCRIPTION=Main authentication server for the Project Crystal PlayOnline Server Emulator suite."
set "HAS_PASSTHROUGH=true"
set "COMMAND=%~1"
goto :EvaluateCommand

:ShiftAndRoute
:: Pop the targeted module identifier string from the execution parameter stack
shift
set "COMMAND=%~1"

:EvaluateCommand
if "%COMMAND%"=="" (
    goto :PrintUsage
)

set "BINARY_PATH=%BINARY_DIR%%SERVICE_NAME%.exe"

:: Route to the functional script labels based on selected target
if "%COMMAND%"=="install-service" goto :InstallService
if "%COMMAND%"=="remove-service"  goto :RemoveService
if "%COMMAND%"=="start"           goto :StartService
if "%COMMAND%"=="stop"            goto :StopService
if "%COMMAND%"=="restart"         goto :RestartService
goto :PassThroughCommand

:VerifyAdmin
:: Native Windows trick to check if running from an elevated administrator command prompt
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Please re-run this command from an Elevated Administrator Command Prompt.
    exit /b 1
)
exit /b 0

:InstallService
echo [INFO] Initializing Windows Service installation layout for %SERVICE_NAME%...
call :VerifyAdmin
if %errorLevel% neq 0 exit /b 1

if not exist "%BINARY_PATH%" (
    echo [ERROR] Executable binary not found at %BINARY_PATH%. Please build the project first.
    exit /b 1
)

echo [INFO] Registering service container record...
sc.exe create "%SERVICE_NAME%" binPath= "%BINARY_PATH%" DisplayName= "%DISPLAY_NAME%" start= auto type= own >nul
sc.exe description "%SERVICE_NAME%" "%DESCRIPTION%" >nul

echo [INFO] Starting Windows Service...
sc.exe start "%SERVICE_NAME%" >nul

echo [INFO] Service successfully created and initiated!
goto :End

:RemoveService
echo [INFO] Tearing down service allocations for %SERVICE_NAME%...
call :VerifyAdmin
if %errorLevel% neq 0 exit /b 1

echo [INFO] Halting background processing hooks...
sc.exe stop "%SERVICE_NAME%" >nul 2>&1

echo [INFO] Purging service container record...
sc.exe delete "%SERVICE_NAME%" >nul

echo [INFO] Service clean-up complete.
goto :End

:StartService
call :VerifyAdmin
if %errorLevel% neq 0 exit /b 1
sc.exe start "%SERVICE_NAME%" >nul
goto :End

:StopService
call :VerifyAdmin
if %errorLevel% neq 0 exit /b 1
sc.exe stop "%SERVICE_NAME%" >nul
goto :End

:RestartService
call :VerifyAdmin
if %errorLevel% neq 0 exit /b 1
sc.exe stop "%SERVICE_NAME%" >nul 2>&1
timeout /t 2 /nobreak >nul
sc.exe start "%SERVICE_NAME%" >nul
goto :End

:PassThroughCommand
:: Security Check: Validate whether the targeted profile permissions allow commands
if "%HAS_PASSTHROUGH%"=="false" (
    echo [ERROR] Unknown command "%COMMAND%" for module %SERVICE_NAME%.
    echo Usage: polcontrol.bat %TARGET_MODULE% [install-service ^| remove-service ^| start ^| stop ^| restart]
    exit /b 1
)

if not exist "%BINARY_PATH%" (
    echo [ERROR] Cannot process control execution block. Binary missing at %BINARY_PATH%
    exit /b 1
)

:: Reconstruct remaining execution arguments string safely
if "%TARGET_MODULE%"=="auth" (
    REM Use a sub-parsing block to pull out parameters past the first shifted position
    set "PASSTHROUGH_ARGS="
    for /f "tokens=1* delims= " %%a in ("%*") do set "PASSTHROUGH_ARGS=%%b"
    "%BINARY_PATH%" --control !PASSTHROUGH_ARGS!
) else (
    "%BINARY_PATH%" --control %*
)
goto :End

:PrintUsage
echo Usage: polcontrol.bat [auth ^| profile ^| patch] [install-service ^| remove-service ^| start ^| stop ^| restart]
echo        polcontrol.bat [auth-server-passthrough-command]
exit /b 1

:End
endlocal