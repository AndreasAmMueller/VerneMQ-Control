@echo off

cd /d "%~dp0"
cd ..

powershell Write-Host -fore Blue "Restoring solution packages..."
dotnet restore -v q || goto error
dotnet tool restore -v q || goto error

powershell Write-Host -fore Blue "Building binary for amd64..."
dotnet clean -v m -c Release -nologo || goto error
dotnet publish -c Release -r linux-x64 --self-contained || goto error

powershell Write-Host -fore Blue "Building setup for amd64..."
dotnet make-deb setup/systemd/vernemq-control-amd64.debspec -vf bin/Release/linux-x64/publish/VerneMQ-Control.dll || goto error


powershell Write-Host -fore Blue "Building binary for armhf..."
dotnet clean -v m -c Release -nologo || goto error
dotnet publish -c Release -r linux-arm --self-contained || goto error

powershell Write-Host -fore Blue "Building setup for armhf..."
dotnet make-deb setup/systemd/vernemq-control-armhf.debspec -vf bin/Release/linux-arm/publish/VerneMQ-Control.dll || goto error

powershell Write-Host -fore Green "Build succeeded"
exit /b

:error
pause