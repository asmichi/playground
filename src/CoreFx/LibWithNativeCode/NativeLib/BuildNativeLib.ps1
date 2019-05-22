# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#
# WARNING: You need a Visual Studio 2017 license to build MSVC binaries.
#

# Build native runtimes of LibWithNativeCode.
param(
    [parameter()]
    [switch]
    $AlwaysBuildImage,
    [parameter()]
    [string]
    $ImageNamePrefix = "asmichi"
)

function Test-Image {
    param(
        [parameter()]
        [string]
        $ImageName
    )

    try {
        docker image inspect $ImageName 2>&1 | Out-Null
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $False
    }
}

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$thisDir = $PSScriptRoot
$workTreeRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")

$winBaseImageName = "mcr.microsoft.com/dotnet/framework/runtime:4.7.2-windowsservercore-ltsc2019"
$linuxImageName = "${ImageNamePrefix}/ubuntu-x64/gcc:18.04"
$msvcImageName = "${ImageNamePrefix}/win/buildtools2017native:latest"
$linuxContainerName = "${ImageNamePrefix}-BuildNativeLib-linux"
$msvcContainerName = "${ImageNamePrefix}-BuildNativeLib-win"

if ($AlwaysBuildImage -or -not (Test-Image $linuxImageName)) {
    docker build -t $linuxImageName -f "${thisDir}\ubuntu-x64-gcc-18.04.Dockerfile" ${thisDir}
}
if ($AlwaysBuildImage -or -not (Test-Image $msvcImageName)) {
    docker build -t $msvcImageName -f "${thisDir}\win-buildtools2017.Dockerfile" --build-arg FROM_IMAGE=$winBaseImageName ${thisDir}
}

# Build Linux binaries.
docker run --mount "type=bind,readonly,source=${workTreeRoot}/src/CoreFx/LibWithNativeCode/NativeLib,target=/home/proj/root" --name $linuxContainerName $linuxImageName `
    bash /home/proj/root/Subbuild-linux.sh

New-Item -ItemType Directory -Force "${workTreeRoot}\bin\NativeLib\linux-x64" | Out-Null
docker cp "${linuxContainerName}:/home/proj/bin/linux-x64/." "${workTreeRoot}\bin\NativeLib\linux-x64"
docker rm $linuxContainerName | Out-Null

# Build Windows binaries.
docker run --mount "type=bind,readonly,source=${workTreeRoot}/src/CoreFx/LibWithNativeCode/NativeLib,target=c:/proj/root" --name $msvcContainerName $msvcImageName `
    powershell -File c:\proj\root\Subbuild-win.ps1 -OutRoot c:\proj

New-Item -ItemType Directory -Force "${workTreeRoot}\bin\NativeLib\win-x86" | Out-Null
New-Item -ItemType Directory -Force "${workTreeRoot}\bin\NativeLib\win-x64" | Out-Null
# `/.` doesn't work: Error response from daemon: GetFileAttributesEx \\?\Volume...\proj\bin\NativeLib\win-x86\.: The filename, directory name, or volume label syntax is incorrect.
docker cp "${msvcContainerName}:c:/proj/bin/win-x86" "${workTreeRoot}/bin/NativeLib"
docker cp "${msvcContainerName}:c:/proj/bin/win-x64" "${workTreeRoot}/bin/NativeLib"
docker rm $msvcContainerName | Out-Null
