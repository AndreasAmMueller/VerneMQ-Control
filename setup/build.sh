#!/bin/bash

abspath=$(cd "$(dirname "${0}")"; pwd)
rootdir=$(dirname "${abspath}")

pushd ${rootdir}

echo "Restoring solution packages..."
dotnet restore -v q || exit 1
dotnet tool restore -v q || exit 1

echo ""
echo "Building binary for amd64..."

dotnet clean -v m -c Release -nologo || exit 1
dotnet publish -c Release -r linux-x64 --self-contained || exit 1

echo ""
echo "Building setup for amd64..."

dotnet make-deb setup/systemd/vernemq-control-amd64.debspec -vf bin/Release/linux-x64/publish/VerneMQ-Control.dll || exit 1

echo ""
echo "Building binary for armhf..."

dotnet clean -v m -c Release -nologo || exit 1
dotnet publish -c Release -r linux-arm --self-contained || exit 1

echo ""
echo "Building setup for armhf..."

dotnet make-deb setup/systemd/vernemq-control-armhf.debspec -vf bin/Release/linux-arm/publish/VerneMQ-Control.dll || exit 1

popd

echo ""
echo "Build succeeded"
