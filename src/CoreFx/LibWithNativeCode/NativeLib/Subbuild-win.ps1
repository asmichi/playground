# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.

param(
    [parameter()]
    [string]
    $OutRoot
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$srcRoot = $PSScriptRoot
$makefilePath = Join-Path $srcRoot "Makefile.win"

foreach ($rid in @("win-x86", "win-x64")) {
    foreach ($configuration in @("Debug", "Release")) {
        nmake /nologo /f $makefilePath RID=$rid CONFIGURATION=$configuration SRCROOT=$srcRoot OUTROOT=$OutRoot
    }
}
