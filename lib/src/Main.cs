// dex2oat 编译状态批量检查（Windows 原生版，单文件可独立运行）

// 等价移植自 bash 脚本 安卓dax2oat编译检查.sh，无需 Python / bash。

// adb.exe + AdbWinApi.dll + AdbWinUsbApi.dll 已内嵌到本 exe（见 build.ps1

// 的 /resource 参数），运行时自动解压到临时目录使用，

// 因此本 exe 单独一个文件即可运行，无需外部 adb 环境。

// 使用前确保已连接设备并开启 ADB 调试。

using System;

using System.Collections.Generic;

using System.Diagnostics;

using System.IO;

using System.Reflection;

using System.Text;

using System.Text.RegularExpressions;

using System.Windows.Forms;

using System.Drawing;

using QRCoder;



partial class Dex2OatCheck

{

    static string ADB;

    static string AdbDir;   // 解压内嵌 adb 的临时目录（为空表示未使用内嵌模式）

    static bool AdbBroken;  // adb 进程启动失败标记（路径无效等），避免无意义重试

    static string CurSerial;    // 当前命令目标设备序列号（多台设备 / 同一台设备多个 transport 时自动 -s 指定）



    // 把内嵌的 adb 工具释放到临时目录，成功返回 true；

    // 若本 exe 未内嵌这些文件（普通编译），返回 false，走外部查找。

    // 每次用独立子目录，避免上次残留的 adb 进程占用文件导致解压失败。

    static bool ExtractBundledAdb()

    {

        string[] files = { "adb.exe", "AdbWinApi.dll", "AdbWinUsbApi.dll" };

        string dir = Path.Combine(Path.GetTempPath(), "dex2oat_adb", Guid.NewGuid().ToString("N").Substring(0, 8));

        try

        {

            Directory.CreateDirectory(dir);

            var asm = Assembly.GetExecutingAssembly();

            foreach (string f in files)

            {

                using (Stream s = asm.GetManifestResourceStream(f))

                {

                    if (s == null) return false;    // 未内嵌，回退外部 adb

                    using (FileStream fs = File.Create(Path.Combine(dir, f)))

                        s.CopyTo(fs);

                }

            }

            AdbDir = dir;

            return true;

        }

        catch

        {

            return false;

        }

    }



    // 查找 adb：优先用内嵌版（已解压），否则依次查找 exe 同目录 / 上级目录 /

    // 当前目录下的 adb.exe 或 adb shell\adb.exe，最后再试 PATH 中的 adb

    static string FindAdb()

    {

        if (ExtractBundledAdb())

            return Path.Combine(AdbDir, "adb.exe");

        string exeDir = AppDomain.CurrentDomain.BaseDirectory;

        string parent = Path.GetDirectoryName(exeDir.TrimEnd('\\'));

        string[] cands = {

            Path.Combine(exeDir, "adb.exe"),

            Path.Combine(exeDir, "adb shell", "adb.exe"),

            Path.Combine(parent, "adb.exe"),

            Path.Combine(parent, "adb shell", "adb.exe"),

            Path.Combine(Directory.GetCurrentDirectory(), "adb.exe"),

            Path.Combine(Directory.GetCurrentDirectory(), "adb shell", "adb.exe"),

        };

        foreach (string c in cands)

            if (File.Exists(c)) return c;

        try

        {

            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";

            foreach (string p in pathEnv.Split(';'))

            {

                string pp = p.Trim().Trim('"');

                if (pp.Length > 0 && File.Exists(Path.Combine(pp, "adb.exe")))

                    return Path.Combine(pp, "adb.exe");

            }

        }

        catch { }

        return "adb.exe";

    }



    // 退出时清理解压的临时文件（若 adb server 仍占用则跳过，下次运行会复用/覆盖）

    static void CleanupBundled()

    {

        if (AdbDir == null) return;

        try

        {

            if (Directory.Exists(AdbDir))

                Directory.Delete(AdbDir, true);

        }

        catch

        {

            /* 文件被 adb server 占用时删除失败属正常，留给下次运行覆盖 */

        }

    }



    // 执行 adb 命令并返回输出（按 UTF-8 解码）。

    // 已锁定目标设备（CurSerial）时，为设备级命令自动附加 -s <序列号>，

    // 避免多台设备 / 同一台设备出现多个 transport（无线调试同时有 IP:端口 与

    // mDNS 两条连接）时报 "more than one device/emulator"；目标失效时自动重新解析重试。

    static string RunAdb(string args)

    {

        string so = ExecAdb(WithTarget(args));

        if (NeedsDevice(args) && (so.Contains("more than one device/emulator")

            || so.Contains("device not found") || so.Contains("no devices/emulators found")

            || so.Contains("device offline")))

        {

            ResolveDeviceTarget();   // 重新解析目标设备（断开 / 新增设备后目标可能已变化）

            if (CurSerial != null)

                so = ExecAdb("-s " + CurSerial + " " + args);

        }

        return so;

    }



    // 已锁定目标设备时，为设备级命令附加 -s <序列号>

    static string WithTarget(string args)

    {

        if (CurSerial != null && NeedsDevice(args) && !args.StartsWith("-s "))

            return "-s " + CurSerial + " " + args;

        return args;

    }



    // server 级命令（无需指定设备）返回 false，其余设备级命令返回 true

    static bool NeedsDevice(string args)

    {

        int sp = args.IndexOf(' ');

        string first = sp > 0 ? args.Substring(0, sp) : args;

        switch (first.ToLowerInvariant())

        {

            case "devices": case "kill-server": case "start-server": case "mdns":

            case "pair": case "connect": case "disconnect": case "version": case "help":

                return false;

        }

        return true;

    }



    // 实际启动 adb 进程执行命令

    static string ExecAdb(string args)

    {

        var psi = new ProcessStartInfo(ADB, args)

        {

            RedirectStandardOutput = true,

            RedirectStandardError = true,

            UseShellExecute = false,

            CreateNoWindow = true,

            StandardOutputEncoding = Encoding.UTF8,

            StandardErrorEncoding = Encoding.UTF8,

        };

        try

        {

            using (var p = Process.Start(psi))

            {

                string so = p.StandardOutput.ReadToEnd();

                string se = p.StandardError.ReadToEnd();

                p.WaitForExit();

                return so + se;

            }

        }

        catch (Exception ex)

        {

            AdbBroken = true;

            Console.WriteLine("! 执行 adb 失败（路径：" + ADB + "）：" + ex.Message);

            return "";

        }

    }



    [STAThread]

    static void Main()
    {
        // GitHub API 强制 TLS 1.2+，旧版 .NET Framework 默认不支持，需显式开启
        try { System.Net.ServicePointManager.SecurityProtocol |= (System.Net.SecurityProtocolType)3072; } catch { }

        ADB = FindAdb();

        LoadAppNames();

        LoadBlacklist();

        ResolveDeviceTarget();   // 预解析目标设备，之后所有设备命令自动 -s 指定



        // 主菜单：输入序号进入对应功能

        while (true)

        {

            Console.WriteLine("========================================");

            Console.WriteLine("           安卓工具箱");

            PrintAdbStatusLine();   // 每次回到主菜单刷新连接状态（绿=已连接 / 黄=未授权 / 红=未连接）

            Console.WriteLine("========================================");

            Console.WriteLine("  [1] ADB 链接状态");
            Console.WriteLine("  [2] dex2oat");
            Console.WriteLine("  [3] 应用管理");
            Console.WriteLine("  [4] 系统信息");
            Console.WriteLine("  [5] 小米专区");
            Console.WriteLine("  [6] 命令行");
            Console.WriteLine("  [7] 重启");

            Console.WriteLine("  [0] 退出");

            Console.WriteLine("========================================");

            Console.Write("请输入序号：");

            string choice = Console.ReadLine();

            if (choice == null) break;

            choice = choice.Trim();

            bool quick = false;   // 子菜单按 [0] 直接返回时跳过“按 Enter 返回菜单”等待
            if (choice == "0") break;
            else if (choice == "1") quick = AdbConnectMenu();
            else if (choice == "2") quick = RunDex2OatCheck();
            else if (choice == "3") quick = AdbFileMenu();
            else if (choice == "4") quick = SystemInfoMenu();
            else if (choice == "5") quick = XiaomiZoneMenu();
            else if (choice == "6") quick = AdbCommandLine();
            else if (choice == "7") quick = RebootMenu();
            else Console.WriteLine("无效序号：" + choice);

            if (!quick)
            {
                Console.WriteLine();
                Console.WriteLine("按 Enter 返回菜单...");
                Console.ReadLine();
            }

        }

        CleanupBundled();   // 清理内嵌 adb 解压的临时文件
    }

    // 主菜单-功能6：重启设备到指定模式（系统 / Recovery / Fastboot / Fastbootd / EDL）
    static bool RebootMenu()
    {
        while (true)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("          重启到指定模式");
            Console.WriteLine("========================================");
            Console.WriteLine("  [1] 系统（正常重启）");
            Console.WriteLine("  [2] 恢复模式（Recovery）");
            Console.WriteLine("  [3] Fastboot（bootloader）");
            Console.WriteLine("  [4] Fastbootd（Android 10+）");
            Console.WriteLine("  [5] EDL 9008（深度刷机模式，仅高通）");
            Console.WriteLine("  [0] 返回主菜单");
            Console.WriteLine("========================================");
            Console.Write("请输入序号：");
            string choice = Console.ReadLine();
            if (choice == null) return true;
            choice = choice.Trim();
            if (choice == "0") return true;

            string modeName = null, modeArg = null;
            if (choice == "1") { modeName = "系统"; modeArg = ""; }
            else if (choice == "2") { modeName = "恢复模式（Recovery）"; modeArg = "recovery"; }
            else if (choice == "3") { modeName = "Fastboot（bootloader）"; modeArg = "bootloader"; }
            else if (choice == "4") { modeName = "Fastbootd"; modeArg = "fastboot"; }
            else if (choice == "5") { modeName = "EDL 9008"; modeArg = "edl"; }
            else { Console.WriteLine("无效序号：" + choice); continue; }

            // 重启会断开当前连接，执行前确认一次
            Console.WriteLine();
            Console.WriteLine("即将重启到：" + modeName);
            Console.Write("确认执行？（y/N）：");
            string yn = Console.ReadLine();
            if (yn == null) return true;
            yn = yn.Trim().ToLowerInvariant();
            if (yn != "y" && yn != "yes") { Console.WriteLine("已取消。"); continue; }

            Console.WriteLine();
            Console.WriteLine("正在重启到 " + modeName + " ...");
            string r = RunAdb("reboot" + (modeArg.Length > 0 ? " " + modeArg : ""));
            Console.WriteLine(r.Trim());
            if (r.Contains("error"))
                Console.WriteLine("! 重启命令执行异常，请确认设备已连接并授权（EDL 需高通芯片支持）。");
            else
                Console.WriteLine("已发送重启指令，设备即将断开连接。");
            return false;   // 执行了重启动作，保留一次“按 Enter 返回菜单”等待
        }
    }

    // 主菜单-功能6：ADB 命令行（直接输入命令执行，exit/quit/0 返回主菜单）
    static bool AdbCommandLine()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("          ADB 命令行");
        Console.WriteLine("========================================");
        Console.WriteLine("直接输入 adb 命令执行；输入 exit / quit / 0 返回主菜单；help 查看帮助");
        Console.WriteLine("========================================");

        while (true)
        {
            Console.Write("adb> ");
            string input = Console.ReadLine();
            if (input == null) return true;
            input = input.Trim();
            if (input.Length == 0) continue;

            // 去掉用户习惯性输入的 adb 前缀（如 "adb shell ls" → "shell ls"）
            string raw = input;
            if (raw.StartsWith("adb ", StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(4).Trim();
            else if (raw.Equals("adb", StringComparison.OrdinalIgnoreCase))
                continue;   // 单独输入 adb 无意义，忽略

            string lower = raw.ToLowerInvariant();
            if (lower == "exit" || lower == "quit" || lower == "q" || raw == "0")
                return true;
            if (lower == "help")
            {
                Console.WriteLine("  命令示例：");
                Console.WriteLine("    shell ls /sdcard      执行设备 shell 命令（也可直接输 ls /sdcard，自动补 shell）");
                Console.WriteLine("    devices               查看已连接设备");
                Console.WriteLine("    connect <IP:端口>     无线连接设备");
                Console.WriteLine("    pull <设备路径> <本地> 拉取文件");
                Console.WriteLine("    reboot                重启设备");
                Console.WriteLine("  输入 exit / quit / 0 返回主菜单");
                continue;
            }

            // 非 adb 客户端动词的命令自动补 shell 前缀（如 ls / pm / settings / getprop）。
            // 含管道/引号等特殊字符时把设备命令整体包双引号，避免 Windows adb 丢引号导致设备端误拆命令
            string cmd;
            bool shellCmd = raw.StartsWith("shell ", StringComparison.OrdinalIgnoreCase);
            string devCmd = shellCmd ? raw.Substring(6) : raw;
            if (shellCmd || !IsAdbVerb(raw.Split(' ')[0]))
            {
                if (devCmd.IndexOfAny(new char[] { '|', '&', ';', '<', '>', '"', '`', '$' }) >= 0)
                    cmd = "shell \"" + devCmd.Replace("\"", "\\\"") + "\"";
                else
                    cmd = "shell " + devCmd;
            }
            else
                cmd = raw;

            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("$ adb " + cmd);
            string result = RunAdb(cmd);
            Console.WriteLine(result.TrimEnd());
            Console.WriteLine("------------------------------------------------");
        }
    }

    // adb 客户端命令动词表：其余输入自动当作设备 shell 命令执行
    static bool IsAdbVerb(string word)
    {
        switch (word.ToLowerInvariant())
        {
            case "devices": case "shell": case "kill-server": case "start-server":
            case "mdns": case "pair": case "connect": case "disconnect": case "tcpip":
            case "reboot": case "install": case "uninstall": case "push": case "pull":
            case "logcat": case "screencap": case "screencord": case "forward":
            case "reverse": case "backup": case "restore": case "remount": case "root":
            case "unroot": case "usb": case "version": case "wait-for-device":
            case "emu": case "reconnect": case "get-state": case "get-serialno":
                return true;
        }
        return false;
    }
}

