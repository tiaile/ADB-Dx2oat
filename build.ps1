$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $scriptDir

Write-Host "================================"
Write-Host " 打包 ADB工具箱.exe"
Write-Host "================================"

# 使用系统自带的 .NET Framework C# 编译器，免安装任何工具链
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) { throw "未找到 csc.exe，请确认 .NET Framework 4.x 已安装" }

if (-not (Test-Path "opt")) { New-Item -ItemType Directory "opt" | Out-Null }
if (Test-Path "opt\ADB工具箱.exe") { Remove-Item "opt\ADB工具箱.exe" -Force }

# 源码按功能拆分在 lib\src\ 下（多个 partial class 文件，全部参与编译）
$srcDir = Join-Path $scriptDir "lib\src"
$sources = [System.IO.Directory]::GetFiles($srcDir, "*.cs") | Sort-Object
if ($sources.Count -eq 0) { throw "lib\src\ 下未找到 .cs 源码" }

Write-Host "[1/1] 编译 EXE（内嵌 adb）..."
& $csc /nologo /target:exe /optimize+ /codepage:65001 `
    /reference:System.Windows.Forms.dll `
    /reference:System.Drawing.dll `
    /reference:System.Web.Extensions.dll `
    /reference:"lib\QRCoder.dll" `
    /resource:"adb shell\adb.exe,adb.exe" `
    /resource:"adb shell\AdbWinApi.dll,AdbWinApi.dll" `
    /resource:"adb shell\AdbWinUsbApi.dll,AdbWinUsbApi.dll" `
    /resource:lib\appnames.txt,appnames.txt `
    /out:"opt\ADB工具箱.exe" $sources
if ($LASTEXITCODE -ne 0) { throw "编译失败" }

# QRCoder 为外部 DLL，需与 exe 同目录
Copy-Item "lib\QRCoder.dll" "opt\QRCoder.dll" -Force

Write-Host ""
Write-Host "================================"
Write-Host " 打包完成！"
Write-Host " 输出: $scriptDir\opt\ADB工具箱.exe"
Write-Host "================================"
Read-Host "按 Enter 退出"
