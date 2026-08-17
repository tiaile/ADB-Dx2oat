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

class Dex2OatCheck
{
    static string ADB;
    static string AdbDir;   // 解压内嵌 adb 的临时目录（为空表示未使用内嵌模式）

    // 把内嵌的 adb 工具释放到 %TEMP%\dex2oat_adb，成功返回 true；
    // 若本 exe 未内嵌这些文件（普通编译），返回 false，走外部查找。
    static bool ExtractBundledAdb()
    {
        string[] files = { "adb.exe", "AdbWinApi.dll", "AdbWinUsbApi.dll" };
        string dir = Path.Combine(Path.GetTempPath(), "dex2oat_adb");
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

    // 查找 adb：优先用内嵌版（已解压），否则依次查找 exe 同目录 / 上级目录 / 当前目录
    static string FindAdb()
    {
        if (ExtractBundledAdb())
            return Path.Combine(AdbDir, "adb.exe");
        string exeDir = AppDomain.CurrentDomain.BaseDirectory;
        string[] cands = {
            Path.Combine(exeDir, "adb.exe"),
            Path.Combine(Path.GetDirectoryName(exeDir.TrimEnd('\\')) + "\\", "adb.exe"),
            Path.Combine(Directory.GetCurrentDirectory(), "adb.exe"),
        };
        foreach (string c in cands)
            if (File.Exists(c)) return c;
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

    // 应用名对照表：先加载内嵌默认表，再加载 exe 旁 config\appnames.txt（用户可补全/修改）
    static Dictionary<string, string> AppNames = null;

    static void LoadAppNames()
    {
        AppNames = new Dictionary<string, string>();
        string embedded = "";
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream("appnames.txt"))
            {
                if (s != null)
                    using (var sr = new StreamReader(s, Encoding.UTF8))
                        embedded = sr.ReadToEnd();
            }
        }
        catch { }

        // 先解析内嵌默认表
        ParseNames(embedded, AppNames, false);

        // 确保 exe 同目录下有 config\appnames.txt 供用户维护（首次运行自动生成模板）
        try
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgDir = Path.Combine(exeDir, "config");
            Directory.CreateDirectory(cfgDir);
            string cfgPath = Path.Combine(cfgDir, "appnames.txt");
            if (!File.Exists(cfgPath))
            {
                string tpl =
                    "# ============================================================\n" +
                    "# 应用名对照表（包名=显示名）\n" +
                    "# 本文件由程序首次运行自动生成，可直接编辑：\n" +
                    "#   1. 修改已有应用名\n" +
                    "#   2. 给不认识的应用补一行：包名=名字\n" +
                    "#   3. 删掉某行 = 恢复显示包名；# 开头的行为注释\n" +
                    "# 保存后重新运行程序即生效；想恢复默认就删除本文件再运行。\n" +
                    "# ============================================================\n" +
                    embedded;
                File.WriteAllText(cfgPath, tpl, new UTF8Encoding(true));
            }

            // 外部用户表覆盖内嵌表
            foreach (string line in File.ReadAllLines(cfgPath, Encoding.UTF8))
            {
                string t = line.Trim();
                if (t.Length == 0 || t.StartsWith("#")) continue;
                int eq = t.IndexOf('=');
                if (eq <= 0) continue;
                string pkg = t.Substring(0, eq).Trim();
                string name = t.Substring(eq + 1).Trim();
                if (pkg.Length > 0 && name.Length > 0)
                    AppNames[pkg] = name;   // 外部优先（覆盖内嵌）
            }
        }
        catch { /* 目录无写权限等情况时仅使用内嵌表 */ }
    }

    // 解析 "包名=名字" 文本；overwrite=false 时不覆盖已有键
    static void ParseNames(string text, Dictionary<string, string> into, bool overwrite)
    {
        if (string.IsNullOrEmpty(text)) return;
        foreach (string line in text.Split('\n'))
        {
            string t = line.Trim();
            if (t.Length == 0 || t.StartsWith("#")) continue;
            int eq = t.IndexOf('=');
            if (eq <= 0) continue;
            string pkg = t.Substring(0, eq).Trim();
            string name = t.Substring(eq + 1).Trim();
            if (pkg.Length > 0 && name.Length > 0 && (overwrite || !into.ContainsKey(pkg)))
                into[pkg] = name;
        }
    }

    // 获取应用显示名，未知返回包名本身
    static string AppName(string pkg)
    {
        string n;
        if (AppNames != null && AppNames.TryGetValue(pkg, out n))
            return n;
        return pkg;
    }

    // 执行 adb 命令并返回输出（按 UTF-8 解码）
    static string RunAdb(string args)
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
            Console.WriteLine("! 执行 adb 失败：" + ex.Message);
            return "";
        }
    }

    // 功能2：列出用户安装应用（应用名+包名，名称规则同 appnames）
    static void ListApps()
    {
        Console.WriteLine("正在获取用户应用列表...");
        string listOut = RunAdb("shell pm list packages -3");
        var pkgs = new List<string>();
        foreach (string line in listOut.Split('\n'))
        {
            string t = line.Trim();
            if (t.StartsWith("package:"))
                pkgs.Add(t.Substring("package:".Length).Trim());
        }
        pkgs.Sort(StringComparer.OrdinalIgnoreCase);
        Console.WriteLine("用户安装应用共 " + pkgs.Count + " 个：");
        Console.WriteLine("========================================");
        foreach (string pkg in pkgs)
        {
            string name = AppName(pkg);
            if (name == pkg)
                Console.WriteLine("  " + pkg);
            else
                Console.WriteLine("  " + name + " (" + pkg + ")");
        }
        Console.WriteLine("========================================");
    }

    static void Main()
    {
        ADB = FindAdb();
        LoadAppNames();

        // 主菜单：输入序号进入对应功能
        while (true)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("           安卓工具箱");
            Console.WriteLine("========================================");
            Console.WriteLine("  [1] dex2oat 编译状态检查");
            Console.WriteLine("  [2] 列出用户安装应用（应用名+包名）");
            Console.WriteLine("  [0] 退出");
            Console.WriteLine("========================================");
            Console.Write("请输入序号：");
            string choice = Console.ReadLine();
            if (choice == null) break;
            choice = choice.Trim();
            if (choice == "0") break;
            else if (choice == "1") RunDex2OatCheck();
            else if (choice == "2") ListApps();
            else Console.WriteLine("无效序号：" + choice);

            Console.WriteLine();
            Console.WriteLine("按 Enter 返回菜单...");
            Console.ReadLine();
        }
        CleanupBundled();   // 清理内嵌 adb 解压的临时文件
    }

    // 功能1：dex2oat 编译状态检查
    static void RunDex2OatCheck()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  安卓应用 dex2oat 状态批量检查工具");
        Console.WriteLine("========================================");
        Console.WriteLine("正在获取用户应用列表...");

        // 获取所有用户应用包名（过滤系统应用）
        string listOut = RunAdb("shell pm list packages -3");
        var pkgs = new List<string>();
        foreach (string line in listOut.Split('\n'))
        {
            string t = line.Trim();
            if (t.StartsWith("package:"))
                pkgs.Add(t.Substring("package:".Length).Trim());
        }

        // 每条记录：状态(compiled/verify/uncompiled) | 包名 | 显示名
        var entries = new List<string[]>();
        int total = pkgs.Count;

        // 遍历每个应用包名
        for (int i = 0; i < total; i++)
        {
            string pkg = pkgs[i];
            string name = AppName(pkg);
            Console.Write("\r[{0}/{1}] 检查应用: {2}...    ", i + 1, total, name);

            string info = RunAdb("shell dumpsys package " + pkg);

            // 判断编译状态
            if (Regex.IsMatch(info, "status=speed|status=odex|compileFilter=speed|compileFilter=speed-profile"))
                entries.Add(new string[] { "compiled", pkg, name });
            else if (Regex.IsMatch(info, "compileFilter=quicken|compileFilter=verify"))
                entries.Add(new string[] { "verify", pkg, name });
            else
                entries.Add(new string[] { "uncompiled", pkg, name });
        }

        Console.WriteLine("\n检查完成！");

        var compiledApps = entries.FindAll(e => e[0] == "compiled");
        var verifyApps = entries.FindAll(e => e[0] == "verify");
        var uncompiledApps = entries.FindAll(e => e[0] == "uncompiled");

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("              检查结果汇总              ");
        Console.WriteLine("========================================");
        Console.WriteLine(" 已完整处理（speed/speed-profile）：" + compiledApps.Count + " 个");
        Console.WriteLine(" 仅轻量验证（quicken/verify）：" + verifyApps.Count + " 个");
        Console.WriteLine(" 未处理（无 OAT）：" + uncompiledApps.Count + " 个");
        Console.WriteLine(" 总计检查用户应用：" + total + " 个");
        Console.WriteLine("========================================");

        // 已处理列表
        Console.WriteLine();
        Console.WriteLine("已处理应用列表（" + compiledApps.Count + " 个）：");
        Console.WriteLine("========================================");
        foreach (string[] e in compiledApps)
            Console.WriteLine("  " + (e[2] == e[1] ? e[1] : e[2]));   // 未识别才显示包名
        Console.WriteLine("========================================");

        // 未处理列表（无 OAT + 仅轻量验证）
        var notDone = new List<string[]>();
        notDone.AddRange(verifyApps);
        notDone.AddRange(uncompiledApps);
        Console.WriteLine();
        Console.WriteLine("未处理应用列表（" + notDone.Count + " 个）：");
        Console.WriteLine("========================================");
        foreach (string[] e in notDone)
            Console.WriteLine("  " + (e[2] == e[1] ? e[1] : e[2]));   // 未识别才显示包名
        Console.WriteLine("========================================");

        if (uncompiledApps.Count > 0)
        {
            Console.WriteLine();
            Console.Write("是否一键编译未处理（无 OAT）的应用？[Y/N] ");
            string response = Console.ReadLine();
            if (response != null && (response.Trim().ToLowerInvariant() == "y" || response.Trim().ToLowerInvariant() == "yes"))
            {
                Console.WriteLine();
                Console.WriteLine("开始编译未处理的应用...");
                Console.WriteLine("========================================");
                int ok = 0, fail = 0;
                foreach (string[] e in uncompiledApps)
                {
                    string pkg = e[1];
                    Console.Write("正在编译: " + (e[2] == e[1] ? e[1] : e[2]) + "... ");

                    // 使用 speed 模式编译
                    string result = RunAdb("shell cmd package compile -m speed " + pkg);
                    if (result.Contains("Success"))
                    {
                        Console.WriteLine("成功");
                        ok++;
                    }
                    else
                    {
                        Console.WriteLine("失败");
                        fail++;
                    }
                }
                Console.WriteLine("========================================");
                Console.WriteLine(" 编译成功: " + ok + " 个");
                Console.WriteLine(" 编译失败: " + fail + " 个");
                Console.WriteLine("========================================");
                if (fail == 0)
                    Console.WriteLine("所有应用编译完成！");
                else
                    Console.WriteLine("部分应用编译失败，请检查错误信息");
                Console.WriteLine("提示：如果某些应用反复编译不成功，可能是该应用有系统编译限制，");
                Console.WriteLine("      这种情况下应用仍以 verify 模式运行，属于正常现象。");
            }
            else
            {
                Console.WriteLine("已取消编译操作");
            }
        }
        else
        {
            Console.WriteLine("所有应用都已处理完毕！");
        }
    }
}
