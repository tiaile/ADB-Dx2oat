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

    // ADB 连接状态-功能1：ADB 连接状态（检查连接 / 无线调试）
    static bool AdbConnectMenu()
    {
        while (true)
        {

            Console.WriteLine("========================================");

            Console.WriteLine("           ADB 连接状态");

            Console.WriteLine("========================================");

            Console.WriteLine("  [1] 检查连接");
            Console.WriteLine("  [2] 使用无线调试");
            Console.WriteLine("  [3] 断开连接");
            Console.WriteLine("  [0] 返回主菜单");

            Console.WriteLine("========================================");

            Console.Write("请输入序号：");

            string choice = Console.ReadLine();
            if (choice == null) return true;
            choice = choice.Trim();
            if (choice == "0") return true;

            else if (choice == "1") { CheckAdbConnection(); PauseBack(); }
            else if (choice == "2") { WirelessDebug(); PauseBack(); }
            else if (choice == "3") { DisconnectAdb(); PauseBack(); }

            else Console.WriteLine("无效序号：" + choice);

        }

    }



    // ADB 连接状态-功能3：断开连接（断开无线 ADB 连接）
    static void DisconnectAdb()
    {
        Console.WriteLine("正在断开无线 ADB 连接...");
        // 不经 RunAdb，避免自动附加 -s（disconnect 为 server 级命令）
        string r = ExecAdb("disconnect");
        Console.WriteLine(r.Trim());
        CurSerial = null;
        Console.WriteLine("已断开。若为 USB 连接，请直接拔线。");
    }

    // ADB 连接状态-功能2：无线调试（WiFi 连接）

    static void WirelessDebug()

    {

        Console.WriteLine("========================================");

        Console.WriteLine("          使用无线调试（WiFi）");

        Console.WriteLine("========================================");

        Console.WriteLine("提示：手机与电脑需连接同一 WiFi / 局域网。");

        Console.WriteLine("========================================");

        Console.WriteLine("连接方式：");

        Console.WriteLine("  [1] Android 11+ 无线调试（先配对后连接，推荐）");

        Console.WriteLine("  [2] 传统 USB 转网络（先开 tcpip 端口再连接）");

        Console.WriteLine("  [3] 二维码配对（电脑显示二维码，手机扫码）");

        Console.WriteLine("  [0] 返回上级菜单");

        Console.Write(">>> ");

        string mode = Console.ReadLine();

        if (mode == null) return;

        mode = mode.Trim();

        if (mode == "0") return;

        else if (mode == "1") WirelessDebugPair();

        else if (mode == "2") WirelessDebugTcpip();

        else if (mode == "3") WirelessDebugQr();

        else Console.WriteLine("无效序号：" + mode);

    }



    // 无线调试-方式1：Android 11+ 先配对再连接

    static void WirelessDebugPair()

    {

        Console.WriteLine("请在手机「开发者选项-无线调试」中查看配对地址和配对码。");

        Console.Write("配对地址（IP:端口，如 192.168.1.5:37351）：");

        string pairAddr = Console.ReadLine();

        if (pairAddr == null) return;

        pairAddr = pairAddr.Trim();

        if (pairAddr.Length == 0) { Console.WriteLine("已取消。"); return; }



        Console.Write("配对码（如 123456）：");

        string code = Console.ReadLine();

        if (code == null) return;

        code = code.Trim();



        Console.WriteLine();

        Console.WriteLine("正在重置 adb 服务（避免旧版本服务冲突）...");

        RunAdb("kill-server");   // 结束旧服务，下一条命令会自动以当前 adb 重新启动



        Console.WriteLine("正在配对 " + pairAddr + " ...");

        string pr = RunAdb("pair " + pairAddr + " " + code);

        Console.WriteLine(pr.Trim());

        if (!pr.Contains("Successfully paired"))

        {

            Console.WriteLine("! 配对未成功。请确认：");

            Console.WriteLine("  1. 手机仍停留在「无线调试」配对页面，配对码未过期；");

            Console.WriteLine("  2. 电脑与手机在同一局域网且路由器未开启 AP 隔离；");

            Console.WriteLine("  3. 若反复失败，可改用传统 USB 转网络方式连接。");

            return;

        }



        Console.WriteLine("配对成功！请在「无线调试」页面查看连接地址（IP:端口）。");

        Console.Write("连接地址（IP:端口，如 192.168.1.5:43385）：");

        string connAddr = Console.ReadLine();

        if (connAddr == null) return;

        connAddr = connAddr.Trim();

        if (connAddr.Length == 0) { Console.WriteLine("已取消。"); return; }



        Console.WriteLine();

        Console.WriteLine("正在连接 " + connAddr + " ...");

        if (!TryConnectWireless(connAddr))

            Console.WriteLine("! 连接未成功，请检查地址是否正确、手机无线调试页面是否保持打开。");

    }



    // 无线调试-方式2：传统 USB 转 tcpip 再连接

    static void WirelessDebugTcpip()

    {

        Console.WriteLine("请先将手机用 USB 连接电脑（用于开启网络调试端口 5555）。");

        Console.Write("是否已用 USB 连接？(y/N)：");

        string yn = Console.ReadLine();

        if (yn == null) return;

        yn = yn.Trim().ToLowerInvariant();

        if (yn != "y" && yn != "yes") { Console.WriteLine("已取消。"); return; }



        Console.WriteLine();

        Console.WriteLine("正在重置 adb 服务（避免旧版本服务冲突）...");

        RunAdb("kill-server");



        Console.WriteLine("正在执行 adb tcpip 5555 ...");

        string tcp = RunAdb("tcpip 5555");

        Console.WriteLine(tcp.Trim());

        if (!tcp.Contains("5555") && !tcp.Contains("restarting"))

        {

            Console.WriteLine("! 开启端口失败，请确认 USB 调试已授权。");

            return;

        }

        Console.WriteLine("端口已开启，现在可以拔掉 USB 线了。");



        Console.Write("输入手机 IP（同一 WiFi 下的地址，如 192.168.1.5）：");

        string ip = Console.ReadLine();

        if (ip == null) return;

        ip = ip.Trim();

        if (ip.Length == 0) { Console.WriteLine("已取消。"); return; }

        string addr = ip.Contains(":") ? ip : ip + ":5555";



        Console.WriteLine();

        Console.WriteLine("正在连接 " + addr + " ...");

        if (!TryConnectWireless(addr))

            Console.WriteLine("! 连接未成功，请检查 IP 是否正确、手机是否已连接同一 WiFi。");

    }



    // 尝试连接无线设备：若 adb 服务已通过 mDNS 自动连上（设备列表已有 adb-*. _tcp），

    // 则跳过 adb connect，避免同一台设备出现 IP:端口 + mDNS 两个 transport；

    // 否则执行 adb connect。返回是否连接成功（成功时刷新命令目标）。

    static bool TryConnectWireless(string addr)

    {

        var ready = GetDeviceList();

        foreach (var d in ready)

            if (IsMdnsSerial(d[0]))

            {

                Console.WriteLine("设备已通过 mDNS 自动连接，无需重复 adb connect。");

                CurSerial = PickSerial(ready);

                return true;

            }

        string cr = RunAdb("connect " + addr);

        Console.WriteLine(cr.Trim());

        if (cr.Contains("connected to"))

        {

            CurSerial = PickSerial(GetDeviceList());

            Console.WriteLine("连接成功！");

            return true;

        }

        return false;

    }



    // 无线调试-方式3：电脑显示二维码，手机扫码配对

    static void WirelessDebugQr()

    {

        var rnd = new Random();

        string name = "adbqr-" + rnd.Next(4096, 65536).ToString("x4") + rnd.Next(4096, 65536).ToString("x4");

        int code = rnd.Next(100000, 1000000);

        string payload = "WIFI:T:ADB;S:" + name + ";P:" + code + ";;";



        Console.WriteLine("========================================");

        Console.WriteLine("          二维码配对（手机扫码）");

        Console.WriteLine("========================================");

        Console.WriteLine("手机操作：开发者选项 → 无线调试 →");

        Console.WriteLine("「使用二维码配对设备」，扫描弹出的窗口中的二维码：");

        Form qrWin = null;

        try

        {

            qrWin = ShowQrWindow(payload);

            var thr = new System.Threading.Thread(() => Application.Run(qrWin));

            thr.SetApartmentState(System.Threading.ApartmentState.STA);

            thr.IsBackground = true;

            thr.Start();

            while (!qrWin.IsHandleCreated) System.Threading.Thread.Sleep(10);

        }

        catch (Exception ex)

        {

            Console.WriteLine("! 弹窗显示失败：" + ex.Message + "（改用控制台色块显示）");

            qrWin = null;

            try { RenderQrConsole(QrEncodeLib(payload)); }

            catch (Exception ex2) { Console.WriteLine("! 生成二维码失败：" + ex2.Message); }

        }

        Console.WriteLine();

        Console.WriteLine("配对码   : " + code);

        Console.WriteLine("提示：请用手机对准屏幕上的二维码扫描，窗口可缩放。");

        Console.WriteLine("（扫码失败时也可改用“方式1”手动输入配对码）");



        // 重置服务后，等待手机出现配对服务（mDNS）

        RunAdb("kill-server");

        Console.WriteLine();

        Console.WriteLine("等待手机配对服务（最多 90 秒，Ctrl+C 可中止）...");

        string pairAddr = "";

        for (int i = 0; i < 90 && pairAddr.Length == 0; i++)

        {

            if (AdbBroken) break;

            string svc = RunAdb("mdns services");

            foreach (string line in svc.Split('\n'))

            {

                if (line.Contains("_adb-tls-pairing._tcp"))

                {

                    string[] cols = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    if (cols.Length >= 2 && cols[cols.Length - 1].Contains(":"))

                    {

                        pairAddr = cols[cols.Length - 1];

                        break;

                    }

                }

            }

            if (pairAddr.Length == 0) System.Threading.Thread.Sleep(1000);

        }



        if (pairAddr.Length == 0)

        {

            if (AdbBroken)

                Console.WriteLine("! adb 无法启动，无法等待配对服务。请确认 adb 可用（路径：" + ADB + "）。");

            else

            {

                Console.WriteLine("! 未发现手机配对服务。请确认：");

                Console.WriteLine("  1. 手机已扫描二维码并保持在该界面；");

                Console.WriteLine("  2. 电脑与手机在同一 WiFi，且路由器未开 AP 隔离。");

            }

            CloseQr(qrWin);

            return;

        }



        Console.WriteLine("发现配对服务 " + pairAddr + "，正在配对...");

        string pr = RunAdb("pair " + pairAddr + " " + code);

        Console.WriteLine(pr.Trim());

        if (!pr.Contains("Successfully paired"))

        {

            Console.WriteLine("! 配对未成功，请重试或改用配对码方式。");

            CloseQr(qrWin);

            return;

        }

        Console.WriteLine("配对成功！");



        // 自动寻找连接服务并连接

        Console.WriteLine("正在寻找连接服务并连接...");

        string connAddr = "";

        for (int i = 0; i < 30 && connAddr.Length == 0; i++)

        {

            if (AdbBroken) break;

            string svc = RunAdb("mdns services");

            foreach (string line in svc.Split('\n'))

            {

                if (line.Contains("_adb-tls-connect._tcp"))

                {

                    string[] cols = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    if (cols.Length >= 2 && cols[cols.Length - 1].Contains(":"))

                    {

                        connAddr = cols[cols.Length - 1];

                        break;

                    }

                }

            }

            if (connAddr.Length == 0) System.Threading.Thread.Sleep(1000);

        }



        if (connAddr.Length > 0)

        {

            if (!TryConnectWireless(connAddr))

                Console.WriteLine("! 连接未成功，可手动输入连接地址重试。");

        }

        else

        {

            Console.Write("未自动发现连接地址，请手动输入（IP:端口）：");

            string manual = Console.ReadLine();

            if (manual == null) { CloseQr(qrWin); return; }

            manual = manual.Trim();

            if (manual.Length > 0)

            {

                if (!TryConnectWireless(manual))

                    Console.WriteLine("! 连接未成功。");

            }

        }



        // 配对流程结束，关闭二维码窗口

        CloseQr(qrWin);

    }



    // 暂停等待用户按 Enter 返回菜单（避免子菜单输出一闪而过）

    static void PauseBack()

    {

        Console.WriteLine();

        Console.WriteLine("按 Enter 返回菜单...");

        Console.ReadLine();

    }



    // 读取系统属性（getprop），失败返回空串

    static string GetProp(string key)

    {

        return RunAdb("shell getprop " + key).Trim();

    }



    // 返回参数中第一个非空字符串

    static string FirstNonEmpty(params string[] vals)

    {

        foreach (string v in vals)

            if (v != null && v.Length > 0)

                return v;

        return "";

    }



    // 从 dumpsys 输出中按 "key: value" 提取值

    static string DumpVal(string text, string key)

    {

        string prefix = key + ":";

        foreach (string line in text.Split('\n'))

        {

            string t = line.Trim();

            if (t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))

                return t.Substring(prefix.Length).Trim();

        }

        return "";

    }



    // ADB 菜单-功能3：读取系统信息（手机名称/处理器/系统版本/电池等）

    static void ReadSystemInfo()

    {

        Console.WriteLine("========================================");

        Console.WriteLine("          读取系统信息");

        Console.WriteLine("========================================");

        Console.WriteLine("正在读取...\n");



        // ---- 基本信息 ----

        string brand = GetProp("ro.product.brand");

        string model = GetProp("ro.product.model");

        string market = GetProp("ro.product.marketname");

        string device = GetProp("ro.product.device");

        string android = GetProp("ro.build.version.release");

        string sdk = GetProp("ro.build.version.sdk");

        string ui = FirstNonEmpty(GetProp("ro.miui.ui.version.name"), GetProp("ro.mi.os.version.name"), GetProp("ro.hyper.os.version"));



        Console.WriteLine("【基本信息】");

        Console.WriteLine("  手机品牌   : " + (brand.Length > 0 ? brand : "未知"));

        Console.WriteLine("  手机型号   : " + (model.Length > 0 ? model : "未知")

            + (market.Length > 0 ? "（" + market + "）" : ""));

        Console.WriteLine("  设备代号   : " + (device.Length > 0 ? device : "未知"));

        Console.WriteLine("  Android    : " + (android.Length > 0 ? android : "未知")

            + (sdk.Length > 0 ? "（API " + sdk + "）" : ""));

        if (ui.Length > 0) Console.WriteLine("  系统 UI    : " + ui);



        // ---- 处理器 ----

        string socManu = GetProp("ro.soc.manufacturer");

        string socModel = GetProp("ro.soc.model");

        string board = GetProp("ro.board.platform");

        string hw = GetProp("ro.hardware");

        string abi = GetProp("ro.product.cpu.abi");



        Console.WriteLine();

        Console.WriteLine("【处理器】");

        Console.WriteLine("  制造商     : " + (socManu.Length > 0 ? socManu : (hw.Length > 0 ? hw : "未知")));

        Console.WriteLine("  型号/平台  : " + (socModel.Length > 0 ? socModel : (board.Length > 0 ? board : "未知")));

        if (abi.Length > 0) Console.WriteLine("  CPU 架构   : " + abi);

        if (socModel.Length == 0)

        {

            // 部分设备 ro.soc.* 为空，回退读取 /proc/cpuinfo 的 Hardware 字段

            string cpuinfo = RunAdb("shell cat /proc/cpuinfo").Trim();

            foreach (string line in cpuinfo.Split('\n'))

            {

                string t = line.Trim();

                if (t.StartsWith("Hardware"))

                {

                    int colon = t.IndexOf(':');

                    if (colon >= 0 && colon + 1 < t.Length)

                    {

                        string hwName = t.Substring(colon + 1).Trim();

                        if (hwName.Length > 0) Console.WriteLine("  硬件标识   : " + hwName);

                    }

                    break;

                }

            }

        }



        // ---- 系统与内核 ----

        string patch = GetProp("ro.build.version.security_patch");

        string disp = GetProp("ro.build.display.id");

        string kernel = RunAdb("shell cat /proc/version").Trim();



        Console.WriteLine();

        Console.WriteLine("【系统与内核】");

        if (patch.Length > 0) Console.WriteLine("  安全补丁   : " + patch);

        if (kernel.Length > 0)

        {

            // 例：Linux version 5.15.78-android13-8-... (gcc ...) #1 SMP PREEMPT ...

            string[] kp = kernel.Split(' ');

            if (kp.Length >= 3)

                Console.WriteLine("  内核版本   : " + kp[0] + " " + kp[1] + " " + kp[2]);

            else

                Console.WriteLine("  内核版本   : " + kernel);

        }

        if (disp.Length > 0) Console.WriteLine("  系统版本号 : " + disp);



        // ---- 屏幕 ----

        string size = RunAdb("shell wm size").Trim();

        string density = RunAdb("shell wm density").Trim();

        if (size.StartsWith("Physical size:")) size = size.Substring("Physical size:".Length).Trim();

        if (density.StartsWith("Physical density:")) density = density.Substring("Physical density:".Length).Trim();



        Console.WriteLine();

        Console.WriteLine("【屏幕】");

        if (size.Length > 0) Console.WriteLine("  分辨率     : " + size);

        if (density.Length > 0) Console.WriteLine("  屏幕密度   : " + density + " dpi");



        // ---- 电池 ----

        string bat = RunAdb("shell dumpsys battery").Trim();

        string level = DumpVal(bat, "level");

        string status = DumpVal(bat, "status");

        string health = DumpVal(bat, "health");

        string temp = DumpVal(bat, "temperature");

        string volt = DumpVal(bat, "voltage");



        Console.WriteLine();

        Console.WriteLine("【电池】");

        if (level.Length > 0) Console.WriteLine("  当前电量   : " + level + "%");

        if (status.Length > 0)

        {

            string st = status == "1" ? "未知" : status == "2" ? "充电中" : status == "3" ? "放电中"

                : status == "4" ? "未充电" : status == "5" ? "已充满" : status;

            Console.WriteLine("  充电状态   : " + st);

        }

        if (health.Length > 0)

        {

            string ht = health == "1" ? "未知" : health == "2" ? "良好" : health == "3" ? "过热"

                : health == "4" ? "损坏" : health == "5" ? "过压" : health == "6" ? "未知故障"

                : health == "7" ? "过冷" : health;

            Console.WriteLine("  电池健康   : " + ht);

        }

        if (temp.Length > 0)

        {

            double tc;

            if (double.TryParse(temp, out tc))

                Console.WriteLine("  电池温度   : " + (tc / 10.0).ToString("0.0") + " ℃");

        }

        if (volt.Length > 0) Console.WriteLine("  电池电压   : " + volt + " mV");



        // 循环次数 / 容量（部分设备才有对应 sysfs 接口，读不到属正常）

        string cycle = RunAdb("shell cat /sys/class/power_supply/battery/cycle_count").Trim();

        if (cycle.Length > 0 && !cycle.Contains("No such") && !cycle.Contains("cat:"))

            Console.WriteLine("  循环次数   : " + cycle + " 次");

        else

            Console.WriteLine("  循环次数   : 不支持读取（设备未提供该接口）");



        string full = RunAdb("shell cat /sys/class/power_supply/battery/charge_full").Trim();

        string design = RunAdb("shell cat /sys/class/power_supply/battery/charge_full_design").Trim();

        long fv, dv;

        if (long.TryParse(full, out fv) && long.TryParse(design, out dv) && dv > 0)

        {

            Console.WriteLine("  设计容量   : " + (dv / 1000.0).ToString("0.0") + " mAh");

            Console.WriteLine("  当前容量   : " + (fv / 1000.0).ToString("0.0") + " mAh");

            Console.WriteLine("  容量保持率 : " + (fv * 100.0 / dv).ToString("0.0") + "%");

        }



        // ---- 内存与存储 ----

        string meminfo = RunAdb("shell cat /proc/meminfo").Trim();

        string memTotal = "";

        foreach (string line in meminfo.Split('\n'))

        {

            string t = line.Trim();

            if (t.StartsWith("MemTotal:"))

            {

                memTotal = t.Substring("MemTotal:".Length).Trim();

                break;

            }

        }

        string df = RunAdb("shell df -h /data | tail -n 1").Trim();



        Console.WriteLine();

        Console.WriteLine("【内存与存储】");

        if (memTotal.Length > 0)

        {

            string[] mp = memTotal.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            double mb;

            if (mp.Length > 0 && double.TryParse(mp[0], out mb))

                Console.WriteLine("  运行内存   : " + (mb / 1024.0 / 1024.0).ToString("0.00") + " GB");

        }

        string[] dt = df.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        if (dt.Length >= 6 && dt[dt.Length - 1] == "/data")

        {

            Console.WriteLine("  存储已用   : " + dt[dt.Length - 4]);

            Console.WriteLine("  存储可用   : " + dt[dt.Length - 3]);

            Console.WriteLine("  存储占用率 : " + dt[dt.Length - 2]);

        }



        // 若关键字段全部为空，多半是设备未连接

        if (brand.Length == 0 && model.Length == 0 && android.Length == 0)
            Console.WriteLine("\n! 读取结果为空，请确认设备已连接并开启 ADB 调试。");
    }

    // 主菜单-功能4：系统信息二级菜单（基本信息 / 充电功率统计）
    static bool SystemInfoMenu()
    {
        while (true)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("           系统信息");
            Console.WriteLine("========================================");
            Console.WriteLine("  [1] 基本信息");
            Console.WriteLine("  [2] 充电功率统计");
            Console.WriteLine("  [0] 返回主菜单");
            Console.WriteLine("========================================");
            Console.Write("请输入序号：");
            string choice = Console.ReadLine();
            if (choice == null) return true;
            choice = choice.Trim();
            if (choice == "0") return true;
            else if (choice == "1") { ReadSystemInfo(); PauseBack(); }
            else if (choice == "2") { ChargePowerStats(); PauseBack(); }
            else Console.WriteLine("无效序号：" + choice);
        }
    }

    // 系统信息-功能2：充电功率统计（每 10 秒采样电压/电流，绘制坐标系，按任意键随时退出）
    static void ChargePowerStats()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  充电功率统计（每 10 秒采样一次）");
        Console.WriteLine("========================================");
        Console.WriteLine("提示：按任意键可随时退出。");

        // 先验证数据源是否可读
        double v, c;
        if (!ReadChargingSample(out v, out c))
        {
            Console.WriteLine("! 无法读取充电数据：已尝试 battery/main/usb 的 voltage_now、current_now、power_now");
            Console.WriteLine("  及 dumpsys battery 均不可读，该设备可能对 shell 禁用了相关电源接口。");
            return;
        }

        var powers = new List<double>();   // 功率 W（带符号，负数=放电）
        var volts = new List<double>();    // 电压 V
        var currs = new List<double>();    // 电流 A（带符号）

        while (true)
        {
            if (!ReadChargingSample(out v, out c))
            {
                Console.WriteLine("\n! 读取数据失败，停止统计。");
                break;
            }
            volts.Add(v / 1000000.0);
            currs.Add(c / 1000000.0);
            powers.Add(v * c / 1e12);

            DrawChargeChart(powers, volts, currs);

            // 等待 10 秒，期间随时响应按键退出
            bool quit = false;
            for (int i = 0; i < 50; i++)
            {
                System.Threading.Thread.Sleep(200);
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    quit = true;
                    break;
                }
            }
            if (quit) break;
        }

        Console.WriteLine();
        Console.WriteLine("已停止统计，共采样 " + powers.Count + " 次。");
        if (powers.Count > 0)
        {
            double avg = 0, mx = powers[0];
            foreach (double p in powers) { avg += p; if (p > mx) mx = p; }
            avg /= powers.Count;
            Console.WriteLine("平均功率: " + avg.ToString("0.00") + " W | 峰值功率: " + mx.ToString("0.00") + " W");
        }
    }

    // 读取一次充电电压/电流。
    // 优先 sysfs 的 battery/main/usb 电源节点（电压 µV、电流 µA），
    // 无电流接口时尝试 power_now(µW) 反推；电压再兜底 dumpsys battery（mV）。
    // 返回是否成功。
    static bool ReadChargingSample(out double voltUv, out double currUa)
    {
        voltUv = 0; currUa = 0;

        string v = ReadSysfs(new string[] {
            "/sys/class/power_supply/battery/voltage_now",
            "/sys/class/power_supply/main/voltage_now",
            "/sys/class/power_supply/battery/voltage_avg",
            "/sys/class/power_supply/usb/voltage_now" });

        string c = ReadSysfs(new string[] {
            "/sys/class/power_supply/battery/current_now",
            "/sys/class/power_supply/main/current_now",
            "/sys/class/power_supply/usb/current_now",
            "/sys/class/power_supply/battery/current_avg" });

        double vv;
        if (c.Length == 0)
        {
            // 无电流接口：尝试直接读功率 power_now(µW)，用电压反推电流
            string p = ReadSysfs(new string[] {
                "/sys/class/power_supply/battery/power_now",
                "/sys/class/power_supply/main/power_now" });
            double pw;
            if (p.Length > 0 && double.TryParse(p, out pw) && v.Length > 0 && double.TryParse(v, out vv) && vv > 0)
            {
                voltUv = vv;
                currUa = pw / vv * 1000000.0;   // µW / µV = A，再换算成 µA
                return true;
            }
            return false;
        }

        // 电压兜底：dumpsys battery 的 voltage（mV）
        if (v.Length == 0)
        {
            string mv = DumpVal(RunAdb("shell dumpsys battery"), "voltage");
            double mvV;
            if (double.TryParse(mv, out mvV) && mvV > 0)
                v = (mvV * 1000).ToString();
        }

        double cc;
        if (double.TryParse(v, out vv) && double.TryParse(c, out cc))
        {
            voltUv = vv;
            currUa = cc;
            return true;
        }
        return false;
    }

    // 依次尝试读取 sysfs 文件，返回第一个可读内容（失败/为空则试下一个）
    static string ReadSysfs(string[] paths)
    {
        foreach (string path in paths)
        {
            string r = RunAdb("shell cat " + path).Trim();
            if (r.Length > 0 && !r.Contains("No such") && !r.Contains("cat:"))
                return r;
        }
        return "";
    }

    // 在控制台绘制充电功率折线图（最多显示最近 maxN 个采样点）
    static void DrawChargeChart(List<double> powers, List<double> volts, List<double> currs)
    {
        int maxN = 36;
        int start = Math.Max(0, powers.Count - maxN);

        // Y 轴最大值：取各样本绝对值，向上取整到 0.5W
        double maxP = 0.5;
        for (int i = start; i < powers.Count; i++)
        {
            double absP = Math.Abs(powers[i]);
            if (absP > maxP) maxP = absP;
        }
        maxP = Math.Ceiling(maxP * 2) / 2.0;

        int rows = 6;
        try { Console.Clear(); } catch { }

        Console.WriteLine("========================================");
        Console.WriteLine("  充电功率统计（每格 10 秒，最多显示最近 " + maxN + " 个采样）");
        Console.WriteLine("========================================");
        Console.WriteLine("  按任意键退出    当前第 " + powers.Count + " 次采样    峰值 " + maxP.ToString("0.0") + "W");

        // 预计算每个采样点的行位置（0..rows），* = 采样点，- = 连线
        int count = powers.Count - start;
        var hs = new double[count];
        for (int i = 0; i < count; i++)
            hs[i] = Math.Abs(powers[start + i]) / maxP * rows;

        for (int r = rows; r >= 0; r--)
        {
            double level = maxP * r / rows;
            Console.Write(" " + level.ToString("0.0").PadLeft(4) + "W |");
            for (int i = 0; i < count; i++)
            {
                int cur = (int)Math.Round(hs[i]);
                if (cur == r)
                    Console.Write(" * ");
                else if (i > 0)
                {
                    int prev = (int)Math.Round(hs[i - 1]);
                    int lo = Math.Min(prev, cur), hi = Math.Max(prev, cur);
                    if (r > lo && r < hi)
                        Console.Write(" - ");   // 相邻点之间跨越本行的连线
                    else if (prev == cur && prev == r)
                        Console.Write(" - ");   // 水平线段
                    else
                        Console.Write("   ");
                }
                else
                    Console.Write("   ");
            }
            Console.WriteLine();
        }

        Console.Write("        +");
        for (int i = start; i < powers.Count; i++)
            Console.Write("---");
        Console.WriteLine();

        // X 轴刻度：每 5 格一个时间标记（每格 10 秒）
        Console.Write("        ");
        for (int i = start; i < powers.Count; i++)
        {
            if ((i - start) % 5 == 0)
                Console.Write(((i - start) * 10).ToString().PadRight(3));
            else
                Console.Write("   ");
        }
        Console.WriteLine();

        if (powers.Count > 0)
        {
            int last = powers.Count - 1;
            Console.WriteLine();
            Console.WriteLine("  最新: 电压 " + volts[last].ToString("0.000") + " V | 电流 "
                + (currs[last] >= 0 ? "+" : "") + currs[last].ToString("0.000") + " A | 功率 "
                + (powers[last] >= 0 ? "+" : "") + powers[last].ToString("0.00") + " W"
                + (currs[last] >= 0 ? "（充电中）" : "（放电中）"));
        }
    }
}

