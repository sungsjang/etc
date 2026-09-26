@echo off
setlocal
cd /d "%~dp0"
title USD/KRW Widget - Local Build

echo ==============================================
echo   USD/KRW Widget - Local Build
echo ==============================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 goto nodotnet

for /f "tokens=1 delims=." %%v in ('dotnet --version') do set DOTNET_MAJOR=%%v
if not defined DOTNET_MAJOR goto nodotnet
if %DOTNET_MAJOR% LSS 8 goto nodotnet

echo [1/3] .NET SDK found.
echo [2/3] Building Windows executable...

if exist "dist\local" rmdir /s /q "dist\local"

dotnet publish "UsdKrwWidget.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -o "dist\local"
if errorlevel 1 goto buildfailed

echo.
echo [3/3] Build completed successfully.
echo.
echo Executable created at:
echo   %CD%\dist\local\UsdKrwWidget.exe
echo.
choice /c YN /n /m "Run the widget now? [Y/N] "
if errorlevel 2 goto done
start "" "%CD%\dist\local\UsdKrwWidget.exe"
goto done

:nodotnet
echo.
echo .NET 8 SDK is not installed or is too old.
echo.
echo This build script needs the Microsoft .NET 8 SDK one time only.
echo Press any key to open the official Microsoft download page.
pause >nul
start "" "https://dotnet.microsoft.com/en-us/download/dotnet/8.0"
goto done

:buildfailed
echo.
echo BUILD FAILED.
echo Please copy the error messages shown above and send them to ChatGPT.
echo.
pause
goto done

:done
endlocal
