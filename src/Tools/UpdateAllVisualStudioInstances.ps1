# Copyright (c) @asmichi (https://github.com/asmichi). Licensed under the MIT License. See LICENCE in the project root for details.
#
# すべての Visual Studio インスタンスを更新する。管理者権限で実行する。

#Requires -Version 5.1 -RunAsAdministrator

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$vswhere = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
$vs_installer = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vs_installer.exe"
$instances = & $vswhere -prerelease -format json -sort | ConvertFrom-Json

if ($instances.Length -eq 0) {
    Write-Host "No Visual Studio instance found."
}

# 古いバージョンから更新する
[array]::Reverse($instances)

$rebootRequired = $false
$failed = $false

foreach ($instance in $instances) {
    Write-Host
    Write-Host "******************************"
    Write-Host "*** Updating $($instance.installationName)"
    Write-Host

    # GUI を表示したい場合は、 --quiet ではなく --passive にする
    $updateArgs = "update --quiet --norestart --installPath `"$($instance.installationPath)`""
    $p = Start-Process -FilePath $vs_installer -ArgumentList $updateArgs -Wait -PassThru

    # https://docs.microsoft.com/en-us/visualstudio/install/use-command-line-parameters-to-install-visual-studio?view=vs-2019#error-codes
    switch ($p.ExitCode) {
        0 { }
        3010 { $rebootRequired = $true }
        Default { $failed = $true }
    }

    if ($failed) {
        break
    }
}

Write-Host

if ($failed) {
    Write-Host "******************************"
    Write-Host "***         ERROR          ***"
    Write-Host "******************************"
    exit 1
}
elseif ($rebootRequired) {
    Write-Host "******************************"
    Write-Host "***    REBOOT REQUIRED     ***"
    Write-Host "******************************"
    exit 3010
}
else {
    Write-Host "SUCCESS!"
    exit 0
}
