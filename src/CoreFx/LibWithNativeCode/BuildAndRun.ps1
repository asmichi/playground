# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

#
# WARNING: You need a Visual Studio 2017 license to build MSVC binaries.
#

# Build native runtimes of LibWithNativeCode.
param(
    [parameter()]
    [string]
    $ImageNamePrefix = "asmichi"
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$thisDir = $PSScriptRoot
$workTreeRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")

& "${thisDir}\NativeLib\BuildNativeLib.ps1" -ImageNamePrefix $ImageNamePrefix

dotnet build -nologo -v:quiet -c Release "${thisDir}\LibWithNativeCode\LibWithNativeCode.csproj"

$feedDir = "${workTreeRoot}\bin\NuGetFeed\libwithnativecode"
$cacheDir = Join-Path ${Env:USERPROFILE} ".nuget\packages\libwithnativecode"
if (Test-Path $feedDir) {
    Remove-Item -recurse $feedDir
}
if (Test-Path $cacheDir) {
    Remove-Item -recurse $cacheDir
}
nuget add -Source "${workTreeRoot}\bin\NuGetFeed" "${workTreeRoot}\bin\LibWithNativeCode\AnyCPU\Release\LibWithNativeCode.0.0.0-localbuild.nupkg"

# Run the FDD binary.
dotnet publish -nologo -c Release -v:quiet "${thisDir}\LibUser\LibUser.csproj"

dotnet "${workTreeRoot}\bin\LibUser\AnyCPU\Release\netcoreapp2.2\publish\LibUser.dll"

docker pull mcr.microsoft.com/dotnet/core/runtime:2.2.4-alpine3.9
docker run --rm --mount "type=bind,readonly,source=${workTreeRoot}/bin/LibUser/AnyCPU/Release/netcoreapp2.2/publish,target=/home/proj/app" `
    mcr.microsoft.com/dotnet/core/runtime:2.2.4-alpine3.9 dotnet /home/proj/app/LibUser.dll

