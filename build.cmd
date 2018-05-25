@echo off
set BASE=%~dp0

REM IF NOT EXIST "%BUILD_PACKAGES%\fake.exe" (
REM   dotnet tool install fake-cli ^
REM     --tool-path %BUILD_PACKAGES% ^
REM     --version 5.0.0-rc*
REM )

dotnet restore %BASE%\dotnet-fake.csproj

dotnet fake run %BASE%\build.fsx -- %*