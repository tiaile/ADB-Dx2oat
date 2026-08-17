// ADB toolbox - partial class source (compiled together with other files under lib\src)

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
    // 功能2：dex2oat 编译状态管理（编译 / 还原编译 / 黑名单管理）
    static bool RunDex2OatCheck()
    {
        while (true)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("       dex2oat 编译状态管理");
            Console.WriteLine("========================================");
            Console.WriteLine("  [1] 编译（检查并编译未处理应用）");
            Console.WriteLine("  [2] 还原编译（已编译应用恢复为轻量验证）");
            Console.WriteLine("  [3] 黑名单管理（查看/移出）");
            Console.WriteLine("  [0] 返回主菜单");
            Console.WriteLine("========================================");
            Console.Write("请输入序号：");
            string choice = Console.ReadLine();
            if (choice == null) return true;
            choice = choice.Trim();
            if (choice == "0") return true;
            else if (choice == "1") { Dex2OatCompile(); PauseBack(); }
            else if (choice == "2") { Dex2OatRestore(); PauseBack(); }
            else if (choice == "3") { ManageBlacklist(); PauseBack(); }
            else Console.WriteLine("无效序号：" + choice);
        }
    }

    // 扫描所有用户应用的编译状态，返回记录（状态 | 包名 | 显示名）
    static List<string[]> ScanDex2Oat()
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

        var entries = new List<string[]>();
        int total = pkgs.Count;
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
        return entries;
    }

    // dex2oat 菜单-功能1：检查并编译未处理应用
    static void Dex2OatCompile()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  检查并编译未处理应用");
        Console.WriteLine("========================================");
        var entries = ScanDex2Oat();
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
        Console.WriteLine(" 总计检查用户应用：" + entries.Count + " 个");
        Console.WriteLine("========================================");

        // 已处理列表（仅展示，还原请到菜单功能 2）
        Console.WriteLine();
        Console.WriteLine("已处理应用列表（" + compiledApps.Count + " 个）：");
        Console.WriteLine("========================================");
        foreach (string[] e in compiledApps)
            Console.WriteLine("  " + (e[2] == e[1] ? e[1] : e[2]));
        Console.WriteLine("========================================");

        // 未处理列表（无 OAT + 仅轻量验证），剔除黑名单后进入交互菜单
        ShowUncompiledMenu(verifyApps, uncompiledApps);
    }

    // dex2oat 菜单-功能2：还原已编译应用为轻量验证（编译前状态）
    static void Dex2OatRestore()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  还原编译状态（恢复为轻量验证）");
        Console.WriteLine("========================================");
        var entries = ScanDex2Oat();
        var compiledApps = entries.FindAll(e => e[0] == "compiled");

        if (compiledApps.Count == 0)
        {
            Console.WriteLine("没有已完整编译的应用，无需还原。");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("已处理应用列表（" + compiledApps.Count + " 个）：");
        Console.WriteLine("========================================");
        for (int i = 0; i < compiledApps.Count; i++)
        {
            string[] e = compiledApps[i];
            Console.WriteLine("  [" + (i + 1) + "] " + (e[2] == e[1] ? e[1] : e[2]));
        }
        Console.WriteLine("========================================");
        Console.WriteLine("输入序号还原该应用编译状态（恢复为轻量验证）；A=全部还原；直接回车返回");
        Console.Write(">>> ");
        string act = Console.ReadLine();
        if (act == null) return;
        act = act.Trim();
        if (act.Length == 0) return;

        var targets = new List<string[]>();
        string lower = act.ToLowerInvariant();
        if (lower == "a" || lower == "all")
            targets.AddRange(compiledApps);
        else
        {
            int idx;
            if (int.TryParse(act, out idx) && idx >= 1 && idx <= compiledApps.Count)
                targets.Add(compiledApps[idx - 1]);
            else
            {
                Console.WriteLine("无效输入：" + act);
                return;
            }
        }

        Console.WriteLine();
        Console.WriteLine("开始还原编译状态...");
        Console.WriteLine("========================================");
        int ok = 0, fail = 0;
        foreach (string[] e in targets)
        {
            string pkg = e[1];
            Console.Write("正在还原: " + (e[2] == e[1] ? e[1] : e[2]) + "... ");
            string detail;
            if (RestoreOne(pkg, out detail))
            {
                Console.WriteLine("成功（已恢复为轻量验证）");
                ok++;
            }
            else
            {
                Console.WriteLine("失败（状态未变化）");
                Console.WriteLine("  " + detail);
                fail++;
            }
        }
        Console.WriteLine("========================================");
        Console.WriteLine(" 还原成功: " + ok + " 个");
        Console.WriteLine(" 还原失败: " + fail + " 个");
        Console.WriteLine("========================================");
        if (fail > 0)
            Console.WriteLine("提示：已尝试 verify 与 extract 两种模式仍无法还原，多为系统 dexopt 策略限制；"
                + "完全删除 OAT 需 root，且还原后可能需要重启设备状态才刷新。");
    }

    // 还原单个应用：强制 verify（仅验证不 AOT），若仍未脱离编译状态则改用 extract（最接近无 OAT）。
    // 每次执行后用 dumpsys 复核，返回是否真正还原成功。
    static bool RestoreOne(string pkg, out string detail)
    {
        detail = "";
        string lastResult = "";
        string[] modes = { "verify", "extract" };
        foreach (string mode in modes)
        {
            // -f 强制重新 dexopt，否则已编译的应用会被当作"无需处理"而跳过
            lastResult = RunAdb("shell cmd package compile -f -m " + mode + " " + pkg);
            string info = RunAdb("shell dumpsys package " + pkg);
            if (!IsCompiledState(info))
            {
                detail = "adb 输出：" + lastResult.Trim();
                return true;
            }
        }
        detail = "verify/extract 均执行后仍为编译状态；最后 adb 输出：" + lastResult.Trim()
            + "。多为系统 dexopt 策略限制。";
        return false;
    }

    // 判断 dumpsys 输出是否仍处于 speed/odex 编译状态（与扫描判定口径一致）
    static bool IsCompiledState(string info)
    {
        return Regex.IsMatch(info, "status=speed|status=odex|compileFilter=speed|compileFilter=speed-profile");
    }

    // 未处理应用交互菜单：一键编译 / 输入序号加入黑名单
    static void ShowUncompiledMenu(List<string[]> verifyApps, List<string[]> uncompiledApps)
    {
        while (true)
        {
            var notDone = new List<string[]>();
            notDone.AddRange(verifyApps);
            notDone.AddRange(uncompiledApps);
            var filtered = notDone.FindAll(e => !Blacklist.Contains(e[1]));

            Console.WriteLine();
            if (filtered.Count == 0)
            {
                Console.WriteLine("未处理应用已全部处理或已加入黑名单，无需操作。");
                return;
            }

            Console.WriteLine("未处理应用列表（" + filtered.Count + " 个）：");
            Console.WriteLine("========================================");
            for (int i = 0; i < filtered.Count; i++)
            {
                string pkg = filtered[i][1];
                Console.WriteLine("  [" + (i + 1) + "] " + (filtered[i][2] == pkg ? pkg : filtered[i][2]));
            }
            Console.WriteLine("========================================");
            Console.WriteLine("  Y     一键编译未处理应用（黑名单除外）");
            Console.WriteLine("  序号  加入黑名单（可逗号分隔，如 1,3,5）");
            Console.WriteLine("  回车  返回菜单");
            Console.Write(">>> ");
            string input = Console.ReadLine();
            if (input == null) return;
            input = input.Trim();
            if (input.Length == 0) return;

            string lower = input.ToLowerInvariant();
            if (lower == "y" || lower == "yes")
            {
                // 一键编译：仅无 OAT 且不在黑名单中的应用
                var toCompile = uncompiledApps.FindAll(e => !Blacklist.Contains(e[1]));
                if (toCompile.Count == 0)
                {
                    Console.WriteLine("没有可编译的应用（未处理的已在黑名单中）。");
                    return;
                }
                Console.WriteLine();
                Console.WriteLine("开始编译未处理的应用...");
                Console.WriteLine("========================================");
                int ok = 0, fail = 0;
                foreach (string[] e in toCompile)
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
                return;
            }

            // 解析序号列表，加入黑名单
            bool any = false;
            var added = new List<string>();
            foreach (string part in input.Split(new char[] { ',', '，', ' ', '、', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int n;
                if (int.TryParse(part, out n) && n >= 1 && n <= filtered.Count)
                {
                    string pkg = filtered[n - 1][1];
                    if (AddToBlacklist(pkg)) { added.Add(pkg); any = true; }
                }
            }
            if (any)
            {
                Console.WriteLine("已加入黑名单：" + string.Join(", ", added));
                Console.WriteLine("（黑名单保存于 config\\blacklist.txt，可手动编辑移除）");
                // 继续循环，列表刷新后重新显示
            }
            else
            {
                Console.WriteLine("无效输入：" + input);
            }
        }
    }
}
