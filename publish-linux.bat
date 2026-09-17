@echo off
setlocal
cd /d "%~dp0"

set "RID=linux-x64"
set "OUT=%~dp0Build\Publish"

call :publish POLAuth polauth || goto :failed
call :publish POLProfile polprofile || goto :failed
call :publish POLPatch polpatch || goto :failed

echo.
echo All three servers published to %OUT%
exit /b 0

:publish
echo.
echo ======= Publishing %1 (%RID%) =======
dotnet publish "%~dp0%1\%1.csproj" -c Release -r %RID% --self-contained true -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false -o "%OUT%\%2"
exit /b %errorlevel%

:failed
echo.
echo Publish FAILED.
exit /b 1
