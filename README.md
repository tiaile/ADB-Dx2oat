# ADB-Dx2oat 安卓工具箱

> 通过 adb 批量检查/编译 Android 应用的 dex2oat 状态，附带应用列表与中文名管理。
> 单文件 exe、免安装环境，无需 root、无需解锁。

## 功能

启动后输入序号进入对应功能：

| 序号 | 功能 | 说明 |
| --- | --- | --- |
| 1 | dex2oat 编译状态检查 | 批量检查第三方应用的编译状态（speed/speed-profile / verify / 无 OAT），可选一键 `cmd package compile -m speed` 编译未处理应用 |
| 2 | 列出用户安装应用 | 列出应用名+包名，输入序号可直接改名/补名 |
| 3 | 列出系统应用 | 同上，针对系统预装应用 |
| 0 | 退出 | |

应用列表按包名 A-Z 排序，未命名的应用会显示原始包名。

## 快速使用

1. 运行 `build.ps1` 构建（或使用 Releases 提供的成品），得到 `opt\dex2oat编译检查.exe`（**单文件，已内嵌 adb，无需安装任何环境**）。
2. 手机开启「USB 调试」并连接电脑。
3. 双击 exe，按菜单输入序号即可。

## 应用名管理

- exe 首次运行会在**自身所在目录**自动生成 `config\appnames.txt`，可直接编辑补全（一行 `包名=显示名`，支持中文，保持 A-Z 排序）。
- 也可以在功能 2 / 3 的列表里输入序号直接改名，程序自动写回 `config\appnames.txt`。
- 输入 `0` 可切换「只看未命名应用」，方便批量补名。
- 想恢复默认，删除 `config\appnames.txt` 重新运行即可。

## 常见问题：ADB 无法识别设备时可以尝试的方法

`adb devices` 检测不到手机（或 fastboot 模式无设备）时，多数是缺少 **Google USB 驱动**，可尝试：

1. 到 Google 官网下载官方 USB 驱动：https://developer.android.com/studio/run/win-usb
2. 解压得到 `usb_driver` 文件夹
3. 打开「设备管理器」→ 右键未识别设备 →「更新驱动程序」→「浏览我的电脑以查找驱动程序」→ 选择解压出的 `usb_driver` 文件夹安装

> 驱动文件体积较大，不随本仓库分发，需要时请从官网下载。

## 构建（可选）

需要 Windows 自带的 .NET Framework（csc.exe），无需其他工具链：

```
.\build.ps1
```

- 源码：`lib\dex2oat编译检查.cs`（C#，编译时把 adb.exe + 两个 DLL + 名字表内嵌进 exe）
- 内置默认名字表：`lib\appnames.txt`
- 构建输入（内嵌用）：`adb shell\adb.exe`、`adb shell\AdbWinApi.dll`、`adb shell\AdbWinUsbApi.dll`
- 产物输出：`opt\dex2oat编译检查.exe`

## 目录结构

```
.
├── build.ps1                 # 一键打包脚本（根目录）
├── lib/
│   ├── dex2oat编译检查.cs     # C# 源码
│   └── appnames.txt          # 内置默认应用名对照表
├── opt/                    # 构建产物（本地生成，不入库）
│   ├── dex2oat编译检查.exe    # 成品（自包含，单文件可用）
│   └── config/appnames.txt   # 运行时生成的可编辑名字表
├── adb shell/                # adb 工具链及辅助文件（构建输入）
```
（USB 驱动不随仓库分发，需用时从官网下载，见上方「常见问题」一节。）

## 说明

- 原理：通过 adb 读取 `dumpsys package` 判断各应用的 dex2oat 编译状态，`pm list packages -3`（用户应用）/ `-s`（系统应用）获取应用列表。
- HyperOS/adb 不提供解析后的应用名，因此内置「包名=中文名」对照表，未收录的应用显示原始包名，可自行补充。
- 脚本移植自同目录的 `安卓dax2oat编译检查.sh`（bash 版）。
