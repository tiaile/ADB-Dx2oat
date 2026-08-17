$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $scriptDir

Write-Host "================================"
Write-Host " 打包 dex2oat编译检查.exe"
Write-Host "================================"

# 使用系统自带的 .NET Framework C# 编译器，免安装任何工具链
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) { throw "未找到 csc.exe，请确认 .NET Framework 4.x 已安装" }

if (-not (Test-Path "opt")) { New-Item -ItemType Directory "opt" | Out-Null }
if (Test-Path "opt\dex2oat编译检查.exe") { Remove-Item "opt\dex2oat编译检查.exe" -Force }

Write-Host "[1/1] 编译 EXE（内嵌 adb）..."
& $csc /nologo /target:exe /optimize+ /codepage:65001 `
    /resource:adb.exe,adb.exe `
    /resource:AdbWinApi.dll,AdbWinApi.dll `
    /resource:AdbWinUsbApi.dll,AdbWinUsbApi.dll `
    /resource:lib\appnames.txt,appnames.txt `
    /out:"opt\dex2oat编译检查.exe" "lib\dex2oat编译检查.cs"
if ($LASTEXITCODE -ne 0) { throw "编译失败" }

Write-Host ""
Write-Host "================================"
Write-Host " 打包完成！"
Write-Host " 输出: $scriptDir\opt\dex2oat编译检查.exe"
Write-Host "================================"
Read-Host "按 Enter 退出"
