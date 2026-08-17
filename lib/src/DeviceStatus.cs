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

    // 判断 adb devices 输出中的第一个设备状态：device / unauthorized / offline / ""（无设备）

    static string DeviceStatus(string devicesOut)

    {

        foreach (string line in devicesOut.Split('\n'))

        {

            string t = line.Trim();

            if (t.Length == 0 || t.StartsWith("List") || t.StartsWith("*")) continue;

            string[] cols = t.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (cols.Length >= 2)

                return cols[1];

        }

        return "";

    }



    // 显示宽度：全角字符（中文、全角符号）按 2 列计，用于对齐冒号
    static int DisplayWidth(string s)
    {
        int w = 0;
        foreach (char c in s)
            w += c > 0x7E ? 2 : 1;
        return w;
    }

    // 标签补齐空格到统一宽度，使各行的冒号对齐
    static string PadLabel(string label, int width)
    {
        int pad = width - DisplayWidth(label);
        return pad > 0 ? label + new string(' ', pad) : label;
    }

    // ============ 多设备 / 同一设备重复连接（IP:端口 + mDNS）处理 ============

    // 解析 adb devices -l 输出，返回所有状态为 device 的设备：{serial, model}

    static List<string[]> GetDeviceList()

    {

        var list = new List<string[]>();

        foreach (string line in RunAdb("devices -l").Split('\n'))

        {

            string t = line.Trim();

            if (t.Length == 0 || t.StartsWith("List") || t.StartsWith("*")) continue;

            string[] cols = t.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (cols.Length < 2 || cols[1] != "device") continue;

            string model = "";

            for (int i = 2; i < cols.Length; i++)

                if (cols[i].StartsWith("model:")) model = cols[i].Substring("model:".Length);

            list.Add(new string[] { cols[0], model });

        }

        return list;

    }



    // mDNS 无线调试连接（adb-xxxx._adb-tls-connect._tcp），与 IP:端口 是同一台设备的两个 transport

    static bool IsMdnsSerial(string serial)

    {

        return serial.StartsWith("adb-") && serial.EndsWith("._tcp");

    }



    // mDNS 条目是否与某 IP:端口 条目指向同一台设备（型号相同视为同一台）

    static bool HasIpTwin(List<string[]> ready, string[] mdns)

    {

        foreach (var e in ready)

            if (e != mdns && e[0].Contains(":") && e[0].Contains(".")

                && mdns[1].Length > 0 && e[1] == mdns[1])

                return true;

        return false;

    }



    // 统计实际设备台数：mDNS 重复条目与同型号的 IP:端口 条目合并为一台

    static int CountPhysicalDevices(List<string[]> ready)

    {

        int n = 0;

        foreach (var d in ready)

            if (!IsMdnsSerial(d[0]) || !HasIpTwin(ready, d)) n++;

        return n;

    }



    // 从设备列表中挑选命令目标：优先 IP:端口 直连条目（可读性好），其次任意已连接设备

    static string PickSerial(List<string[]> devs)

    {

        if (devs.Count == 0) return null;

        foreach (var d in devs)

            if (d[0].Contains(":") && d[0].Contains(".")) return d[0];

        return devs[0][0];

    }



    // 重新解析命令目标设备序列号（存入 CurSerial），无设备时置空

    static string ResolveDeviceTarget()

    {

        CurSerial = PickSerial(GetDeviceList());

        return CurSerial;

    }



    // 获取连接状态摘要。level：2=已连接，1=未授权/离线，0=未连接

    static string GetAdbStatus(out int level, out string text)

    {

        var ready = new List<string[]>();

        var busy = new List<string>();

        foreach (string line in RunAdb("devices -l").Split('\n'))

        {

            string t = line.Trim();

            if (t.Length == 0 || t.StartsWith("List") || t.StartsWith("*")) continue;

            string[] cols = t.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (cols.Length < 2) continue;

            if (cols[1] == "device")

            {

                string model = "";

                for (int i = 2; i < cols.Length; i++)

                    if (cols[i].StartsWith("model:")) model = cols[i].Substring("model:".Length);

                ready.Add(new string[] { cols[0], model });

            }

            else busy.Add(cols[0] + "（" + cols[1] + "）");

        }



        if (ready.Count > 0)

        {

            int n = CountPhysicalDevices(ready);

            string primary = PickSerial(ready), model = "";

            foreach (var d in ready)

                if (d[1].Length > 0) { model = d[1]; break; }

            CurSerial = primary;   // 同步命令目标，后续设备命令自动 -s 指定

            level = 2;

            if (n == 1)

            {

                if (model.Length > 0)

                    text = "已连接 " + model + (IsMdnsSerial(primary) ? "" : "（" + primary + "）");

                else

                    text = "已连接 " + primary;

            }

            else

                text = "已连接 " + n + " 台（" + primary + " 等）";

        }

        else if (busy.Count > 0)

        {

            level = 1;

            text = "设备未就绪：" + string.Join("、", busy);

        }

        else

        {

            CurSerial = null;

            level = 0;

            text = "未连接（无设备）";

        }

        return text;

    }



    // 在主菜单打印连接状态行（带颜色），并同步窗口标题

    static void PrintAdbStatusLine()

    {

        int level; string text;

        GetAdbStatus(out level, out text);

        ConsoleColor old = Console.ForegroundColor;

        Console.ForegroundColor = level == 2 ? ConsoleColor.Green

            : level == 1 ? ConsoleColor.Yellow : ConsoleColor.Red;

        Console.WriteLine("  ADB 状态: " + text);

        Console.ForegroundColor = old;

        try { Console.Title = "安卓工具箱 - " + text; } catch { }

    }



    // ADB 连接状态-功能1：检查连接（未检测到时自动重启 adb 服务重新枚举）

    static void CheckAdbConnection()

    {

        Console.WriteLine("========================================");

        Console.WriteLine("  检查 ADB 连接状态");

        Console.WriteLine("========================================");

        Console.WriteLine("正在执行 adb devices -l ...");

        string devices = RunAdb("devices -l");

        Console.WriteLine(devices.Trim());

        Console.WriteLine("----------------------------------------");



        string status = DeviceStatus(devices);

        if (status == "unauthorized")

            Console.WriteLine("检测到设备但尚未授权，请在手机上点击「允许 USB 调试」后重试。");



        if (status != "device")

        {

            // 刚在手机上点击“允许”后，adb 服务需要重启才能重新枚举到设备

            Console.WriteLine("未检测到已连接的设备，正在重启 adb 服务重新枚举...");

            RunAdb("kill-server");

            devices = RunAdb("devices -l");

            Console.WriteLine(devices.Trim());

            Console.WriteLine("----------------------------------------");

            status = DeviceStatus(devices);

        }



        if (status != "device")

        {

            Console.WriteLine("! 仍未检测到已连接的设备。");

            if (status == "unauthorized")

                Console.WriteLine("  请点击手机上弹出的「允许 USB 调试」对话框中的“允许”。");

            else if (status == "offline")

                Console.WriteLine("  设备状态为 offline，请重新插拔 USB 数据线。");

            else

            {

                Console.WriteLine("  请确认：手机已开启「USB 调试」并授权，或用 adb connect <IP:端口> 无线连接。");

                Console.WriteLine("  若 USB 无法识别，可参考 README「ADB 无法识别设备」一节安装 Google USB 驱动。");

            }

            return;

        }



        // 选定命令目标：多台设备 / 同一台设备的重复连接（IP:端口 + mDNS）时自动 -s 指定

        var ready = GetDeviceList();

        string target = PickSerial(ready);

        CurSerial = target;

        int phys = CountPhysicalDevices(ready);

        Console.WriteLine("已检测到 " + ready.Count + " 个传输（实际 " + phys + " 台设备），命令目标: " + target);

        if (ready.Count > phys)

            Console.WriteLine("提示：同一台设备同时存在 IP:端口 与 mDNS（adb-*. _tcp）两条连接，"

                + "已自动选用 " + target + " 执行命令，不影响使用。");



        Console.WriteLine("设备已连接，正在测试 adb shell 连通性...");
        string test = RunAdb("shell echo ADB_OK").Trim();
        if (test.Contains("ADB_OK"))
        {
            Console.WriteLine("ADB 连接成功：adb shell 可正常执行。");

            // ---- 设备状态检查（只读，不重启设备）----
            Console.WriteLine();
            Console.WriteLine("设备状态检查：");

            // 收集状态行（标签, 值），稍后统一对齐冒号
            var lines = new List<string[]>();

            // 1) OEM / Bootloader 解锁状态
            string vbmeta = RunAdb("shell getprop ro.boot.vbmeta.device_state").Trim();
            string flashLocked = RunAdb("shell getprop ro.boot.flash.locked").Trim();
            string oemAllowed = RunAdb("shell getprop sys.oem_unlock_allowed").Trim();
            string oemSupported = RunAdb("shell getprop ro.oem_unlock_supported").Trim();
            string bootState = RunAdb("shell getprop ro.boot.verifiedbootstate").Trim();

            string lockText;
            if (vbmeta.Equals("unlocked", StringComparison.OrdinalIgnoreCase) || flashLocked == "0")
                lockText = "已解锁（unlocked）";
            else if (vbmeta.Equals("locked", StringComparison.OrdinalIgnoreCase) || flashLocked == "1")
                lockText = "已锁定（locked）";
            else if (vbmeta.Length > 0 || flashLocked.Length > 0)
                lockText = vbmeta.Length > 0 ? vbmeta : flashLocked;
            else
                lockText = "无法读取";
            lines.Add(new string[] { "OEM/Bootloader 状态", lockText });

            if (oemAllowed.Length > 0)
                lines.Add(new string[] { "「OEM 解锁」开关", oemAllowed == "1" ? "已开启（允许解锁）" : "未开启" });
            if (oemSupported.Length > 0 && oemSupported != "1" && oemSupported.ToLowerInvariant() != "true")
                lines.Add(new string[] { "OEM 解锁支持", "不支持（ro.oem_unlock_supported=" + oemSupported + "）" });
            if (bootState.Length > 0)
            {
                string bst = bootState.ToLowerInvariant();
                string bstText = bst == "green" ? "green（未解锁/原厂）"
                    : bst == "orange" ? "orange（已解锁）"
                    : bst == "red" ? "red（系统被篡改）" : bootState;
                lines.Add(new string[] { "验证启动状态", bstText });
            }

            // 2) USB 安装状态（是否允许通过 USB 安装应用）
            string usbConfirm = RunAdb("shell settings get global adb_install_need_confirm").Trim();
            string nonMarket = RunAdb("shell settings get secure install_non_market_apps").Trim();
            if (usbConfirm.Length > 0 && usbConfirm != "null")
            {
                // Android 11+：0=无需确认（已开启），1=需确认（未开启）
                string usbState = usbConfirm == "0"
                    ? "已开启（adb 安装无需确认）"
                    : "未开启（安装需在手机上确认）";
                lines.Add(new string[] { "「通过 USB 安装」", usbState + "（adb_install_need_confirm=" + usbConfirm + "）" });
            }
            else if (nonMarket.Length > 0 && nonMarket != "null")
            {
                string usbState = nonMarket == "1"
                    ? "已允许安装非商店应用"
                    : "未允许安装非商店应用";
                lines.Add(new string[] { "允许未知来源应用", usbState });
            }
            else
            {
                lines.Add(new string[] { "「通过 USB 安装」", "无法读取（未提供此设置项）" });
            }

            // 3) SELinux 模式（宽容模式 / 强制模式）
            string selinux = RunAdb("shell getenforce").Trim();
            if (selinux.Length > 0 && !selinux.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                string seText = selinux.StartsWith("Permissive", StringComparison.OrdinalIgnoreCase)
                    ? "Permissive（宽容模式）"
                    : selinux.StartsWith("Enforcing", StringComparison.OrdinalIgnoreCase)
                        ? "Enforcing（强制模式）"
                        : selinux;
                lines.Add(new string[] { "SELinux 模式", seText });
            }

            // 统一按最长标签对齐冒号
            int maxW = 0;
            foreach (var ln in lines)
            {
                int w = DisplayWidth(ln[0]);
                if (w > maxW) maxW = w;
            }
            foreach (var ln in lines)
                Console.WriteLine("  " + PadLabel(ln[0], maxW) + ": " + ln[1]);
        }
        else
        {
            Console.WriteLine("! adb 能识别设备，但 shell 执行异常：" + (test.Length > 0 ? test : "(无输出)"));
        }

    }

}

