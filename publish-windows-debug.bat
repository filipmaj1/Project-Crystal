@echo off
setlocal
cd /d "%~dp0"

set "RID=win-x64"
set "OUT=%~dp0Build\Publish-Windows-Debug"

call :publish POLAuth polauth || goto :failed
call :publish POLProfile polprofile || goto :failed
call :publish POLPatch polpatch || goto :failed

echo.
echo All three servers published (Debug) to %OUT%
exit /b 0

:publish
echo.
echo ======= Publishing %1 (%RID%, Debug) =======
dotnet publish "%~dp0%1\%1.csproj" -c Debug -r %RID% --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:DebugType=portable -p:DebugSymbols=true -o "%OUT%\%2"
exit /b %errorlevel%

:failed
echo.
echo Publish FAILED.
exit /b 1
