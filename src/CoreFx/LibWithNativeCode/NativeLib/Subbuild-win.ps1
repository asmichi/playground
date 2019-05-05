# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

param(
    [parameter()]
    [string]
    $OutRoot
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$srcRoot = $PSScriptRoot

foreach ($arch in @("x86", "x64")) {
    foreach ($configuration in @("Debug", "Release")) {
        $rid = "win-${arch}"
        $buildDir = Join-Path $OutRoot "build/$rid/${configuration}"
        $outDir = Join-Path $OutRoot "bin/$rid/${configuration}"

        New-Item -ItemType Directory -Force $buildDir | Out-Null
        Push-Location -LiteralPath $buildDir
        cmake $srcRoot -G Ninja "-DCMAKE_BUILD_TYPE=${configuration}" "-DCMAKE_TOOLCHAIN_FILE=${srcRoot}/toolchain-msvc-${arch}.cmake"
        Pop-Location

        ninja -C $buildDir

        New-Item -ItemType Directory -Force $outDir | Out-Null
        Copy-Item "$buildDir/bin/*" -Destination $outDir
    }
}
