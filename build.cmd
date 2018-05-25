@echo off

set BASE=%~dp0

dotnet fake run %BASE%\build.fsx -- %*