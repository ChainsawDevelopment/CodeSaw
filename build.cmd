@echo off
set BASE=%~dp0

REM IF NOT EXIST "%BUILD_PACKAGES%\fake.exe" (
REM   dotnet tool install fake-cli ^
REM     --tool-path %BUILD_PACKAGES% ^
REM     --version 5.0.0-rc*
REM )

rem dotnet restore %BASE%\dotnet-fake.csproj

"%BASE%\.paket\paket.bootstrapper.exe"

set DOTNET=dotnet

if not "%DOTNETCORE_SDK_PATH%" == "" (
    set DOTNET=%DOTNETCORE_SDK_PATH%\%DOTNET%
    set DOTNET_ROOT=%DOTNETCORE_SDK_PATH%
)

%DOTNET% tool install "--tool-path=%BASE%.fake-tools" fake-cli
"%BASE%\.fake-tools\fake.exe" run %BASE%\build.fsx -- %*