// ADB toolbox - partial class source (compiled together with other files under lib\src)



using System;

using System.Collections.Generic;

using System.Diagnostics;

using System.IO;
using System.Net;
using System.Web.Script.Serialization;
using System.Reflection;

using System.Text;

using System.Text.RegularExpressions;

using System.Windows.Forms;

using System.Drawing;

using QRCoder;



partial class Dex2OatCheck

{

    // ADB 菜单-功能2：应用管理

    static bool AdbFileMenu()
    {
        while (true)
        {

            Console.WriteLine("========================================");

            Console.WriteLine("             应用管理");

            Console.WriteLine("========================================");

            Console.WriteLine("  [1] 安装 APK");
            Console.WriteLine("  [2] 卸载第三方应用");
            Console.WriteLine("  [3] 列出用户安装应用");
            Console.WriteLine("  [4] 列出系统应用");
            Console.WriteLine("  [5] 提取安装包");
            Console.WriteLine("  [6] 下载 APK");
            Console.WriteLine("  [0] 返回主菜单");

            Console.WriteLine("========================================");

            Console.Write("请输入序号：");

            string choice = Console.ReadLine();
            if (choice == null) return true;
            choice = choice.Trim();
            if (choice == "0") return true;

            else if (choice == "1") InstallApk();
            else if (choice == "2") UninstallThirdPartyApp();
            else if (choice == "3") ListApps();
            else if (choice == "4") ListSystemApps();
            else if (choice == "5") ExtractApk();
            else if (choice == "6") DownloadApkMenu();
            else Console.WriteLine("无效序号：" + choice);
        }
    }

    // 主菜单-功能5：小米专区（卸载内置应用 / 管理卸载列表）
    static bool XiaomiZoneMenu()
    {
        while (true)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("             小米专区");
            Console.WriteLine("========================================");
            Console.WriteLine("  [1] 卸载小米内置应用");
            Console.WriteLine("  [2] 管理小米卸载列表（增/删/改）");
            Console.WriteLine("  [3] 小白条（底部手势条 显示/隐藏）");
            Console.WriteLine("  [0] 返回主菜单");
            Console.WriteLine("========================================");
            Console.Write("请输入序号：");
            string choice = Console.ReadLine();
            if (choice == null) return true;
            choice = choice.Trim();
            if (choice == "0") return true;
            else if (choice == "1") UninstallXiaomiApp();
            else if (choice == "2") ManageMiUninstallList();
            else if (choice == "3") SetGestureLine();
            else Console.WriteLine("无效序号：" + choice);
        }
    }

    // 小米专区-功能3：小白条（底部手势提示条）显示/隐藏（0=显示，1=隐藏）
    static void SetGestureLine()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  小白条（底部手势提示条）");
        Console.WriteLine("========================================");
        string cur = RunAdb("shell settings get global hide_gesture_line").Trim();
        string curText;
        if (cur.Length == 0 || cur == "null")
            curText = "（未设置，默认显示）";
        else
            curText = cur == "0" ? "0 = 显示" : cur + " = 隐藏";
        Console.WriteLine("  当前值: " + curText);
        Console.WriteLine();
        Console.WriteLine("  0 = 显示小白条");
        Console.WriteLine("  1 = 隐藏小白条");
        Console.Write("请输入（直接回车取消）：");
        string v = Console.ReadLine();
        if (v == null) return;
        v = v.Trim();
        if (v.Length == 0) { Console.WriteLine("已取消。"); return; }
        if (v != "0" && v != "1")
        {
            Console.WriteLine("无效输入：" + v + "（请输入 0 或 1）");
            return;
        }
        Console.WriteLine("正在设置...");
        string r = RunAdb("shell settings put global hide_gesture_line " + v);
        Console.WriteLine(r.Trim());
        Console.WriteLine(v == "0" ? "已设置为显示小白条。" : "已设置为隐藏小白条。");
    }



    // 文件管理-功能1：弹出文件选择框选 APK，传输并安装

    static void InstallApk()

    {

        Console.WriteLine("========================================");

        Console.WriteLine("  安装 APK（从电脑选择文件）");

        Console.WriteLine("========================================");

        Console.WriteLine("请在文件管理器中选择要安装的 APK 文件...");



        string apkPath = null;

        try

        {

            using (var dlg = new OpenFileDialog())

            {

                dlg.Title = "选择要安装的 APK 文件";

                dlg.Filter = "APK 文件 (*.apk)|*.apk|所有文件 (*.*)|*.*";

                dlg.FilterIndex = 1;

                if (dlg.ShowDialog() == DialogResult.OK)

                    apkPath = dlg.FileName;

            }

        }

        catch (Exception ex)

        {

            Console.WriteLine("! 打开文件选择框失败：" + ex.Message);

            return;

        }



        if (string.IsNullOrEmpty(apkPath))

        {

            Console.WriteLine("已取消选择。");

            return;

        }



        Console.WriteLine("已选择：" + apkPath);

        Console.WriteLine();

        Console.WriteLine("请选择安装方式（直接回车默认“覆盖安装并允许降级”）：");

        Console.WriteLine("  [1] 覆盖安装并允许降级（adb install -r -d）");

        Console.WriteLine("  [2] 覆盖安装（adb install -r）");

        Console.WriteLine("  [3] 全新安装（adb install，已安装则失败）");

        string installArgs, modeName;
        string mode;
        while (true)
        {
            Console.Write(">>> ");
            mode = Console.ReadLine();
            if (mode == null) return;
            mode = mode.Trim();
            if (mode == "" || mode == "1")
            {
                installArgs = "install -r -d";
                modeName = "覆盖安装并允许降级";
                break;
            }
            if (mode == "2")
            {
                installArgs = "install -r";
                modeName = "覆盖安装";
                break;
            }
            if (mode == "3")
            {
                installArgs = "install";
                modeName = "全新安装";
                break;
            }
            Console.WriteLine("! 无效选项“" + mode + "”，请输入 1、2 或 3（直接回车默认“覆盖安装并允许降级”）。");
        }



        Console.WriteLine("正在" + modeName + "（adb " + installArgs + "）...");

        string result = RunAdb(installArgs + " \"" + apkPath + "\"");

        Console.WriteLine(result.Trim());

        if (result.Contains("Success"))

            Console.WriteLine("APK 安装成功。");

        else

            Console.WriteLine("! APK 安装失败，请检查上面的输出。");

    }



    // 获取包名列表（args 如 -3、-s；空串表示全部）

    static List<string> GetPackages(string pkgArgs)

    {

        string listOut = RunAdb("shell pm list packages" + (pkgArgs.Length > 0 ? " " + pkgArgs : ""));

        var pkgs = new List<string>();

        foreach (string line in listOut.Split('\n'))

        {

            string t = line.Trim();

            if (t.StartsWith("package:"))

                pkgs.Add(t.Substring("package:".Length).Trim());

        }

        pkgs.Sort(StringComparer.OrdinalIgnoreCase);

        return pkgs;

    }



    // 解析用户输入的序号列表（逗号/空格/顿号/分号分隔），结果存入 indexes（0 基）

    static bool ParseIndexes(string input, int count, List<int> indexes)

    {

        indexes.Clear();

        bool invalid = false;

        foreach (string part in input.Split(new char[] { ',', '，', ' ', '、', ';' }, StringSplitOptions.RemoveEmptyEntries))

        {

            int n;

            if (int.TryParse(part, out n) && n >= 1 && n <= count)

                indexes.Add(n - 1);

            else

                invalid = true;

        }

        return invalid;

    }



    // 应用管理-功能2：卸载第三方应用（列出序号供用户选择）

    static void UninstallThirdPartyApp()

    {

        while (true)

        {

            Console.WriteLine("正在获取第三方应用列表...");

            var pkgs = GetPackages("-3");

            if (pkgs.Count == 0)

            {

                Console.WriteLine("未检测到已安装的第三方应用。");

                return;

            }



            Console.WriteLine("========================================");

            Console.WriteLine("  已安装的第三方应用（" + pkgs.Count + " 个）");

            Console.WriteLine("========================================");

            for (int i = 0; i < pkgs.Count; i++)

            {

                string pkg = pkgs[i], name = AppName(pkg);

                Console.WriteLine("  [" + (i + 1) + "] " + (name == pkg ? pkg : name + " (" + pkg + ")"));

            }

            Console.WriteLine("========================================");

            Console.WriteLine("  序号  卸载对应应用（可逗号分隔，如 1,3,5）");

            Console.WriteLine("  回车  返回上级菜单");

            Console.Write(">>> ");

            string input = Console.ReadLine();

            if (input == null) return;

            input = input.Trim();

            if (input.Length == 0) return;



            var idxs = new List<int>();

            bool invalid = ParseIndexes(input, pkgs.Count, idxs);

            if (idxs.Count == 0)

            {

                Console.WriteLine("无效输入：" + input);

                continue;

            }

            if (invalid)

                Console.WriteLine("部分序号无效，已忽略。");



            Console.WriteLine();

            Console.WriteLine("将卸载以下应用（应用数据将被删除，不可恢复）：");

            foreach (int i in idxs)

            {

                string pkg = pkgs[i], name = AppName(pkg);

                Console.WriteLine("  - " + (name == pkg ? pkg : name + " (" + pkg + ")"));

            }

            Console.Write("确认卸载？(y/N)：");

            string confirm = Console.ReadLine();

            if (confirm == null) return;

            confirm = confirm.Trim().ToLowerInvariant();

            if (confirm != "y" && confirm != "yes")

            {

                Console.WriteLine("已取消卸载。");

                continue;

            }



            Console.WriteLine();

            Console.WriteLine("开始卸载...");

            Console.WriteLine("========================================");

            int ok = 0, fail = 0;

            foreach (int i in idxs)

            {

                string pkg = pkgs[i], name = AppName(pkg);

                Console.Write("正在卸载: " + (name == pkg ? pkg : name) + "... ");

                string result = RunAdb("uninstall \"" + pkg + "\"");

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

            Console.WriteLine(" 卸载成功: " + ok + " 个");

            Console.WriteLine(" 卸载失败: " + fail + " 个");

            Console.WriteLine("========================================");

            // 循环刷新列表，可继续卸载其他应用

        }

    }



    // 加载 config\miuninstall.txt 的小米卸载条目，返回 {包名, 显示名}（显示名可为空串）

    // 文件不存在则自动生成模板；条目中的显示名同步进 AppNames 供列表展示

    static List<string[]> LoadMiEntries()

    {

        var list = new List<string[]>();

        try

        {

            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string cfgDir = Path.Combine(exeDir, "config");

            Directory.CreateDirectory(cfgDir);

            string path = Path.Combine(cfgDir, "miuninstall.txt");

            if (!File.Exists(path))

            {

                File.WriteAllText(path,

                    "# 小米系统应用卸载列表（每行一个包名，# 开头为注释）\n" +

                    "# 卸载系统应用有风险，可能导致部分功能异常，请自行甄别后再填写。\n" +

                    "# 格式：包名 或 包名=显示名（显示名仅用于列表展示）\n" +

                    "# 本文件可在程序内管理（应用管理-功能4），也可手动编辑。\n" +

                    "# 取消下面示例行的注释即可加入卸载列表：\n" +

                    "# com.miui.analytics\n" +

                    "# com.miui.bugreport\n" +

                    "# com.miui.cleanmaster\n" +

                    "# com.miui.gallery\n" +

                    "# com.miui.player\n" +

                    "# com.miui.systemAdSolution\n" +

                    "# com.miui.videoplayer\n" +

                    "# com.xiaomi.market\n",

                    new UTF8Encoding(true));

            }

            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))

            {

                string t = line.Trim();

                if (t.Length == 0 || t.StartsWith("#")) continue;

                int eq = t.IndexOf('=');

                if (eq > 0)

                {

                    string pkg = t.Substring(0, eq).Trim();

                    string name = t.Substring(eq + 1).Trim();

                    if (pkg.Length > 0)

                    {

                        list.Add(new string[] { pkg, name });

                        if (name.Length > 0 && !AppNames.ContainsKey(pkg)) AppNames[pkg] = name;

                    }

                }

                else

                {

                    list.Add(new string[] { t, "" });

                }

            }

        }

        catch (Exception ex)

        {

            Console.WriteLine("! 读取 config\\miuninstall.txt 失败：" + ex.Message);

        }

        return list;

    }



    // 返回 config\miuninstall.txt 中的包名列表（供“卸载小米应用”使用）

    static List<string> LoadMiUninstallList()

    {

        return LoadMiEntries().ConvertAll(e => e[0]);

    }



    // 保存小米卸载列表（保留原注释，按包名 A-Z 排序重写文件）

    static bool SaveMiUninstallList(List<string[]> entries)

    {

        try

        {

            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            string cfgDir = Path.Combine(exeDir, "config");

            Directory.CreateDirectory(cfgDir);

            string path = Path.Combine(cfgDir, "miuninstall.txt");

            var comments = new List<string>();

            if (File.Exists(path))

            {

                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))

                {

                    string t = line.Trim();

                    if (t.Length == 0 || t.StartsWith("#")) comments.Add(line);

                }

            }



            var sb = new StringBuilder();

            foreach (string c in comments) sb.AppendLine(c);

            if (comments.Count > 0) sb.AppendLine();

            var sorted = new List<string[]>(entries);

            sorted.Sort((a, b) => string.CompareOrdinal(a[0], b[0]));

            foreach (string[] e in sorted)

                sb.AppendLine(e[1].Length > 0 ? e[0] + "=" + e[1] : e[0]);

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));

            return true;

        }

        catch (Exception ex)

        {

            Console.WriteLine("! 写入 config\\miuninstall.txt 失败：" + ex.Message);

            return false;

        }

    }



    // 应用管理-功能4：管理小米卸载列表（新增/修改/删除条目）

    static void ManageMiUninstallList()

    {

        while (true)

        {

            var entries = LoadMiEntries();

            entries.Sort((a, b) => string.CompareOrdinal(a[0], b[0]));



            Console.WriteLine("========================================");

            Console.WriteLine("  小米卸载列表管理（" + entries.Count + " 个）");

            Console.WriteLine("========================================");

            if (entries.Count == 0)

            {

                Console.WriteLine("  （列表为空）");

            }

            for (int i = 0; i < entries.Count; i++)

            {

                string pkg = entries[i][0], name = entries[i][1];

                Console.WriteLine("  [" + (i + 1) + "] " + (name.Length == 0 ? pkg : name + " (" + pkg + ")"));

            }

            Console.WriteLine("========================================");

            Console.WriteLine("  序号  修改该条目（可逗号分隔，如 1,3,5）");

            Console.WriteLine("  a    新增条目");

            Console.WriteLine("  d    删除条目");

            Console.WriteLine("  回车  返回上级菜单");

            Console.Write(">>> ");

            string input = Console.ReadLine();

            if (input == null) return;

            input = input.Trim();

            if (input.Length == 0) return;



            string lower = input.ToLowerInvariant();

            if (lower == "a" || lower == "add" || lower == "+")

            {

                Console.Write("输入要添加的包名：");

                string pkg = Console.ReadLine();

                if (pkg == null) continue;

                pkg = pkg.Trim();

                if (pkg.Length == 0) { Console.WriteLine("已取消。"); continue; }

                if (entries.Exists(e => e[0].Equals(pkg, StringComparison.OrdinalIgnoreCase)))

                {

                    Console.WriteLine("该包名已在列表中。");

                    continue;

                }

                Console.Write("输入显示名（可留空）：");

                string name = Console.ReadLine();

                if (name == null) continue;

                name = name.Trim();

                entries.Add(new string[] { pkg, name });

                if (SaveMiUninstallList(entries))

                    Console.WriteLine("已添加：" + pkg + (name.Length > 0 ? " = " + name : ""));

                continue;

            }



            if (lower == "d" || lower == "del" || lower == "-")

            {

                Console.Write("输入要删除的序号（可逗号分隔，如 1,3,5）：");

                string del = Console.ReadLine();

                if (del == null) continue;

                del = del.Trim();

                if (del.Length == 0) continue;

                var idxs = new List<int>();

                ParseIndexes(del, entries.Count, idxs);

                if (idxs.Count == 0) { Console.WriteLine("无效输入：" + del); continue; }

                idxs.Sort();

                idxs.Reverse();

                var removed = new List<string>();

                foreach (int i in idxs)

                {

                    removed.Add(entries[i][0]);

                    entries.RemoveAt(i);

                }

                if (SaveMiUninstallList(entries))

                    Console.WriteLine("已删除：" + string.Join(", ", removed));

                continue;

            }



            // 修改条目

            var mods = new List<int>();

            bool invalid = ParseIndexes(input, entries.Count, mods);

            if (mods.Count == 0) { Console.WriteLine("无效输入：" + input); continue; }

            if (invalid) Console.WriteLine("部分序号无效，已忽略。");

            foreach (int i in mods)

            {

                string pkg = entries[i][0], name = entries[i][1];

                Console.WriteLine("当前：包名=" + pkg + (name.Length > 0 ? "，名称=" + name : "（无显示名）"));

                Console.Write("输入新包名（直接回车保持不变）：");

                string np = Console.ReadLine();

                if (np == null) return;

                np = np.Trim();

                Console.Write("输入新显示名（直接回车保持不变，输入 0 清除）：");

                string nn = Console.ReadLine();

                if (nn == null) return;

                nn = nn.Trim();

                string newPkg = np.Length > 0 ? np : pkg;

                string newName = nn.Length > 0 ? (nn == "0" ? "" : nn) : name;

                if (!newPkg.Equals(pkg, StringComparison.OrdinalIgnoreCase))

                {

                    bool conflict = false;

                    for (int k = 0; k < entries.Count; k++)

                        if (k != i && entries[k][0].Equals(newPkg, StringComparison.OrdinalIgnoreCase))

                            { conflict = true; break; }

                    if (conflict) { Console.WriteLine("新包名已在列表中，修改失败。"); continue; }

                }

                entries[i][0] = newPkg;

                entries[i][1] = newName;

                if (SaveMiUninstallList(entries))

                    Console.WriteLine("已修改：" + pkg + " → " + newPkg + (newName.Length > 0 ? " = " + newName : ""));

            }

        }

    }



    // 应用管理-功能3：卸载小米系统应用（包名在 config\miuninstall.txt 维护）

    static void UninstallXiaomiApp()

    {

        while (true)

        {

            var miList = LoadMiUninstallList();



            // 只列出当前用户（user 0）已安装的包：

            // pm list packages 默认列出所有用户的包，会把“已从当前用户卸载”的系统应用

            // 也算作已安装，因此这里必须用 --user 0 过滤，只保留当前用户可见的应用

            var installed = new HashSet<string>(GetPackages("--user 0"), StringComparer.OrdinalIgnoreCase);



            // 可卸载：名单内且当前用户可见

            var pkgs = new List<string>();

            foreach (string pkg in miList)

                if (installed.Contains(pkg))

                    pkgs.Add(pkg);



            // 可找回：已从当前用户卸载、但仍存在于设备上的系统应用

            var curSys = new HashSet<string>(GetPackages("-s --user 0"), StringComparer.OrdinalIgnoreCase);

            var allSys = new HashSet<string>(GetPackages("-s -u --user 0"), StringComparer.OrdinalIgnoreCase);

            var gone = new List<string>();

            foreach (string pkg in allSys)

                if (!curSys.Contains(pkg))

                    gone.Add(pkg);

            gone.Sort(StringComparer.OrdinalIgnoreCase);



            Console.WriteLine("========================================");

            Console.WriteLine("  小米系统应用卸载（" + pkgs.Count + " 个已安装）");

            Console.WriteLine("========================================");

            for (int i = 0; i < pkgs.Count; i++)

            {

                string pkg = pkgs[i], name = AppName(pkg);

                Console.WriteLine("  [" + (i + 1) + "] " + (name == pkg ? pkg : name + " (" + pkg + ")"));

            }

            if (pkgs.Count == 0)

            {

                if (miList.Count == 0)

                    Console.WriteLine("  （卸载列表为空，可在「管理小米卸载列表」中添加）");

                else

                    Console.WriteLine("  （名单中的应用均已卸载或未安装）");

            }

            Console.WriteLine("========================================");

            if (pkgs.Count > 0)

                Console.WriteLine("  序号  卸载对应应用（可逗号分隔，如 1,3,5）");

            Console.WriteLine("  r    找回已卸载的系统应用（" + gone.Count + " 个）");

            Console.WriteLine("  回车  返回上级菜单");

            Console.Write(">>> ");

            string input = Console.ReadLine();

            if (input == null) return;

            input = input.Trim();

            if (input.Length == 0) return;



            if (input.ToLowerInvariant() == "r")

            {

                RestoreMiApps(gone);

                continue;

            }



            var idxs = new List<int>();

            bool invalid = ParseIndexes(input, pkgs.Count, idxs);

            if (idxs.Count == 0)

            {

                Console.WriteLine("无效输入：" + input);

                continue;

            }

            if (invalid)

                Console.WriteLine("部分序号无效，已忽略。");



            Console.WriteLine();

            Console.WriteLine("将卸载：");

            foreach (int i in idxs)

            {

                string pkg = pkgs[i], name = AppName(pkg);

                Console.WriteLine("  - " + (name == pkg ? pkg : name + " (" + pkg + ")"));

            }



            Console.WriteLine();

            Console.WriteLine("开始卸载（--user 0 仅对当前用户生效，可恢复）...");

            Console.WriteLine("========================================");

            int ok = 0, fail = 0;

            foreach (int i in idxs)

            {

                string pkg = pkgs[i], name = AppName(pkg);

                Console.Write("正在卸载: " + (name == pkg ? pkg : name) + "... ");

                string result = RunAdb("uninstall --user 0 \"" + pkg + "\"");

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

            Console.WriteLine(" 卸载成功: " + ok + " 个");

            Console.WriteLine(" 卸载失败: " + fail + " 个");

            Console.WriteLine("========================================");

        }

    }



    // 找回（恢复）已从当前用户卸载的系统应用：pm install-existing

    static void RestoreMiApps(List<string> gone)

    {

        if (gone.Count == 0)

        {

            Console.WriteLine("当前没有已从当前用户卸载、可找回的系统应用。");

            return;

        }



        while (true)

        {

            Console.WriteLine("========================================");

            Console.WriteLine("  找回已卸载的系统应用（" + gone.Count + " 个）");

            Console.WriteLine("========================================");

            for (int i = 0; i < gone.Count; i++)

            {

                string pkg = gone[i], name = AppName(pkg);

                Console.WriteLine("  [" + (i + 1) + "] " + (name == pkg ? pkg : name + " (" + pkg + ")"));

            }

            Console.WriteLine("========================================");

            Console.WriteLine("  序号  恢复对应应用（可逗号分隔，如 1,3,5）");

            Console.WriteLine("  回车  返回上级菜单");

            Console.Write(">>> ");

            string input = Console.ReadLine();

            if (input == null) return;

            input = input.Trim();

            if (input.Length == 0) return;



            var idxs = new List<int>();

            bool invalid = ParseIndexes(input, gone.Count, idxs);

            if (idxs.Count == 0)

            {

                Console.WriteLine("无效输入：" + input);

                continue;

            }

            if (invalid)

                Console.WriteLine("部分序号无效，已忽略。");



            Console.WriteLine();

            Console.WriteLine("将找回：");

            foreach (int i in idxs)

            {

                string pkg = gone[i], name = AppName(pkg);

                Console.WriteLine("  - " + (name == pkg ? pkg : name + " (" + pkg + ")"));

            }



            Console.WriteLine();

            Console.WriteLine("开始恢复（pm install-existing）...");

            Console.WriteLine("========================================");

            int ok = 0, fail = 0;

            foreach (int i in idxs)

            {

                string pkg = gone[i], name = AppName(pkg);

                Console.Write("正在恢复: " + (name == pkg ? pkg : name) + "... ");

                // pm install-existing 成功时输出形如 "Package xxx installed for user: 0"，

                // 并不包含大写 Success（那是 pm install 的输出），需两种都判为成功

                string result = RunAdb("shell pm install-existing \"" + pkg + "\"");

                string resultLow = result.ToLowerInvariant();

                if (resultLow.Contains("success") || resultLow.Contains("installed for user"))

                {

                    Console.WriteLine("成功");

                    ok++;

                }

                else

                {

                    Console.WriteLine("失败");

                    Console.WriteLine("    " + result.Trim().Replace("\n", "\n    "));

                    fail++;

                }

            }

            Console.WriteLine("========================================");
            Console.WriteLine(" 恢复成功: " + ok + " 个");
            Console.WriteLine(" 恢复失败: " + fail + " 个");
            Console.WriteLine("========================================");
            return;
        }
    }

    // 应用管理-功能7：提取安装包（选择应用，完整提取 APK 到电脑）
    static void ExtractApk()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  提取安装包（APK）到电脑");
        Console.WriteLine("========================================");
        Console.WriteLine("正在获取用户应用列表...");
        var pkgs = GetPackages("-3");

        // 构建显示项 {包名, 名称}
        var items = new List<string[]>();
        foreach (string pkg in pkgs)
        {
            string name = AppName(pkg);
            items.Add(new string[] { pkg, name });
        }

        Console.WriteLine("用户应用列表（共 " + items.Count + " 个）：");
        Console.WriteLine("========================================");
        for (int i = 0; i < items.Count; i++)
        {
            string pkg = items[i][0], name = items[i][1];
            if (name == pkg)
                Console.WriteLine("  [" + (i + 1) + "] " + pkg);
            else
                Console.WriteLine("  [" + (i + 1) + "] " + name + " (" + pkg + ")");
        }
        Console.WriteLine("========================================");
        Console.WriteLine("输入序号选择要提取的应用（可逗号分隔，如 1,3,5）；A=全部；直接回车取消");
        Console.Write(">>> ");
        string sel = Console.ReadLine();
        if (sel == null) return;
        sel = sel.Trim();
        if (sel.Length == 0) return;

        var targets = new List<string[]>();
        string lower = sel.ToLowerInvariant();
        if (lower == "a" || lower == "all")
            targets.AddRange(items);
        else
        {
            foreach (string part in sel.Split(new char[] { ',', '，', ' ', '、', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int n;
                if (int.TryParse(part, out n) && n >= 1 && n <= items.Count)
                    targets.Add(items[n - 1]);
                else
                    Console.WriteLine("无效序号：" + part);
            }
        }
        if (targets.Count == 0)
        {
            Console.WriteLine("未选择任何应用。");
            return;
        }

        // 输出目录：默认 exe 旁的 提取的APK\<包名>\，可自定义
        string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string defDir = Path.Combine(exeDir, "提取的APK");
        Console.WriteLine();
        Console.WriteLine("提取目录（直接回车用默认）：" + defDir);
        Console.Write(">>> ");
        string outDir = Console.ReadLine();
        if (outDir == null) return;
        outDir = outDir.Trim();
        if (outDir.Length == 0) outDir = defDir;
        try { Directory.CreateDirectory(outDir); }
        catch (Exception ex) { Console.WriteLine("! 创建目录失败：" + ex.Message); return; }

        Console.WriteLine();
        Console.WriteLine("开始提取 " + targets.Count + " 个应用...");
        Console.WriteLine("========================================");
        int ok = 0, fail = 0;
        foreach (string[] it in targets)
        {
            string pkg = it[0];
            Console.Write("正在提取: " + (it[1] == pkg ? pkg : it[1]) + "... ");

            // 查询该应用的所有安装路径（base + split，AAB 安装会返回多个）
            string paths = RunAdb("shell pm path " + pkg);
            var apkPaths = new List<string>();
            foreach (string line in paths.Split('\n'))
            {
                string t = line.Trim();
                if (t.StartsWith("package:"))
                    apkPaths.Add(t.Substring("package:".Length).Trim());
            }
            if (apkPaths.Count == 0)
            {
                Console.WriteLine("失败（未找到 APK 路径）");
                fail++;
                continue;
            }

            string pkgDir = Path.Combine(outDir, pkg);
            try { Directory.CreateDirectory(pkgDir); }
            catch (Exception ex) { Console.WriteLine("失败（" + ex.Message + "）"); fail++; continue; }

            bool allOk = true;
            foreach (string ap in apkPaths)
            {
                string fname = Path.GetFileName(ap);   // base.apk / split_config.xxx.apk
                Console.Write(".");
                string pull = RunAdb("pull \"" + ap + "\" \"" + Path.Combine(pkgDir, fname) + "\"");
                // 成功输出形如 "1 file pulled, 0 files skipped (...)"；失败含 error 或 0 files pulled
                if (pull.Contains("error") || pull.Contains("0 files pulled") || !pull.Contains("pulled"))
                {
                    allOk = false;
                    Console.WriteLine();
                    Console.WriteLine("  提取失败: " + ap + " -> " + pull.Trim());
                }
            }
            if (allOk)
            {
                Console.WriteLine("成功（" + apkPaths.Count + " 个文件）");
                ok++;
            }
            else
            {
                Console.WriteLine("完成但部分失败");
                fail++;
            }
        }
        Console.WriteLine("========================================");
        Console.WriteLine(" 提取成功: " + ok + " 个应用");
        Console.WriteLine(" 提取失败: " + fail + " 个应用");
        Console.WriteLine(" 输出目录: " + outDir);
        Console.WriteLine("========================================");
    }

    // 应用管理-功能6：下载 APK（GitHub Release）子菜单
    static bool DownloadApkMenu()
    {
        while (true)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("      下载 APK（GitHub Release）");
            Console.WriteLine("========================================");
            Console.WriteLine("  [1] 检查更新（对比本机已装版本）");
            Console.WriteLine("  [2] 下载安装");
            Console.WriteLine("  [3] 软件源管理（增/删/改）");
            Console.WriteLine("  [4] 设置");
            Console.WriteLine("  [0] 返回上级菜单");
            Console.WriteLine("========================================");
            Console.Write("请输入序号：");
            string choice = Console.ReadLine();
            if (choice == null) return true;
            choice = choice.Trim();
            if (choice == "0") return true;
            else if (choice == "1") { CheckApkUpdates(); PauseBack(); }
            else if (choice == "2") { DownloadApk(); PauseBack(); }
            else if (choice == "3") ManageApkSources();
            else if (choice == "4") { EditGithubToken(); PauseBack(); }
            else Console.WriteLine("无效序号：" + choice);
        }
    }

    // 下载 APK-方式2：选择软件源直接下载安装
    static void DownloadApk()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  下载安装 APK（GitHub Release）");
        Console.WriteLine("========================================");
        var sources = LoadApkSources();
        if (sources.Count == 0)
        {
            Console.WriteLine("软件源为空，请先在「软件源管理」中添加。");
            return;
        }
        Console.WriteLine("可用软件源（config\\apk_sources.txt 可增删）：");
        Console.WriteLine("========================================");
        for (int i = 0; i < sources.Count; i++)
            Console.WriteLine("  [" + (i + 1) + "] " + sources[i][0] + "（" + sources[i][1] + "）");
        Console.WriteLine("========================================");
        Console.Write("输入序号选择（直接回车取消）：");
        string sel = Console.ReadLine();
        if (sel == null) return;
        sel = sel.Trim();
        int idx;
        if (!int.TryParse(sel, out idx) || idx < 1 || idx > sources.Count)
        {
            Console.WriteLine("已取消。");
            return;
        }
        Console.WriteLine();
        DownloadAndInstall(sources[idx - 1][0], sources[idx - 1][1]);
    }

    // 下载 APK-方式1：检查更新（对比 GitHub 最新版本与本机已装版本）
    static void CheckApkUpdates()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  检查更新（GitHub Release vs 本机已装版本）");
        Console.WriteLine("========================================");
        var sources = LoadApkSources();
        if (sources.Count == 0)
        {
            Console.WriteLine("软件源为空，请先在「软件源管理」中添加。");
            return;
        }

        // {名称, 最新版本, 已装版本, 状态, 仓库, 包名}
        var rows = new List<string[]>();
        foreach (var s in sources)
        {
            string name = s[0], repo = s[1], pkg = s[2];
            Console.Write("正在检查: " + name + "... ");
            string latest = GetGithubReleaseTag(repo);
            string installed = "";
            string status;
            if (pkg.Length == 0)
            {
                status = "未填包名";
            }
            else
            {
                installed = GetInstalledVersion(pkg);
                if (installed.Length == 0)
                    status = "未安装";
                else if (latest.Length == 0)
                    status = "获取失败";
                else if (CompareVersions(latest, installed) > 0)
                    status = "有更新";
                else
                    status = "已最新";
            }
            Console.WriteLine(status);
            rows.Add(new string[] { name, latest, installed, status, repo, pkg });
        }

        // 表格（标签按显示宽度对齐）
        int wName = 8, wLatest = 8, wInst = 8;
        foreach (var r in rows)
        {
            if (DisplayWidth(r[0]) > wName) wName = DisplayWidth(r[0]);
            if (DisplayWidth(r[1]) > wLatest) wLatest = DisplayWidth(r[1]);
            if (DisplayWidth(r[2]) > wInst) wInst = DisplayWidth(r[2]);
        }
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  " + PadLabel("名称", wName) + " " + PadLabel("最新版本", wLatest) + " " + PadLabel("已装版本", wInst) + " 状态");
        foreach (var r in rows)
            Console.WriteLine("  " + PadLabel(r[0], wName) + " " + PadLabel(r[1], wLatest) + " " + PadLabel(r[2], wInst) + " " + r[3]);
        Console.WriteLine("========================================");

        // 可操作项（有更新 / 未安装 / 未填包名 / 获取失败）
        var upd = new List<int>();
        for (int i = 0; i < rows.Count; i++)
            if (rows[i][3] == "有更新" || rows[i][3] == "未安装" || rows[i][3] == "未填包名" || rows[i][3] == "获取失败")
                upd.Add(i);
        if (upd.Count == 0)
        {
            Console.WriteLine("所有已填包名的软件源均为最新版本。");
            return;
        }
        Console.WriteLine("可操作项（" + upd.Count + " 个）：");
        for (int j = 0; j < upd.Count; j++)
        {
            int i = upd[j];
            Console.WriteLine("  [" + (j + 1) + "] " + rows[i][0] + "（" + rows[i][3] + "）");
        }
        Console.Write("输入序号下载安装（可逗号分隔，如 1,2；直接回车跳过）：");
        string sel = Console.ReadLine();
        if (sel == null) return;
        sel = sel.Trim();
        if (sel.Length == 0) return;

        var targets = new List<string[]>();
        foreach (string part in sel.Split(new char[] { ',', '，', ' ', '、', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int n;
            if (int.TryParse(part, out n) && n >= 1 && n <= upd.Count)
                targets.Add(rows[upd[n - 1]]);
        }
        foreach (var t in targets)
        {
            Console.WriteLine();
            DownloadAndInstall(t[0], t[4]);
        }
    }

    // 软件源管理（增/删/改），数据存 config\apk_sources.txt
    static void ManageApkSources()
    {
        while (true)
        {
            var list = ReadApkSourcesFile();
            Console.WriteLine("========================================");
            Console.WriteLine("  软件源管理（" + list.Count + " 个）");
            Console.WriteLine("========================================");
            if (list.Count == 0)
                Console.WriteLine("  （软件源为空，可按 A 添加）");
            for (int i = 0; i < list.Count; i++)
                Console.WriteLine("  [" + (i + 1) + "] " + list[i][0] + "（" + list[i][1]
                    + "，包名: " + (list[i][2].Length > 0 ? list[i][2] : "未填") + "）");
            Console.WriteLine("========================================");
            Console.WriteLine("  序号  修改该源（名称/仓库/包名）");
            Console.WriteLine("  A     添加新源");
            Console.WriteLine("  D     删除（可逗号分隔，如 D 1,3）");
            Console.WriteLine("  回车  返回");
            Console.Write(">>> ");
            string input = Console.ReadLine();
            if (input == null) return;
            input = input.Trim();
            if (input.Length == 0) return;

            string lower = input.ToLowerInvariant();
            if (lower == "a" || lower == "add")
            {
                Console.Write("名称: ");
                string n = Console.ReadLine(); if (n == null) return; n = n.Trim();
                Console.Write("GitHub 仓库（owner/repo）: ");
                string r = Console.ReadLine(); if (r == null) return; r = r.Trim();
                Console.Write("包名（可选，用于检查更新）: ");
                string p = Console.ReadLine(); if (p == null) return; p = p.Trim();
                if (n.Length == 0 || r.Length == 0) { Console.WriteLine("名称和仓库不能为空。"); continue; }
                list.Add(new string[] { n, r, p });
                SaveApkSources(list);
                Console.WriteLine("已添加: " + n + "（" + r + "）");
                continue;
            }

            if (lower.StartsWith("d"))
            {
                string nums = input.Length > 1 ? input.Substring(1).Trim() : "";
                var delIdx = new List<int>();
                var names = new List<string>();
                foreach (string part in nums.Split(new char[] { ',', '，', ' ', '、', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int n;
                    if (int.TryParse(part, out n) && n >= 1 && n <= list.Count)
                    { delIdx.Add(n); names.Add(list[n - 1][0]); }
                }
                if (delIdx.Count > 0)
                {
                    delIdx.Sort();
                    delIdx.Reverse();
                    foreach (int di in delIdx) list.RemoveAt(di - 1);
                    SaveApkSources(list);
                    Console.WriteLine("已删除: " + string.Join(", ", names));
                }
                else Console.WriteLine("无效输入：" + input);
                continue;
            }

            int idx;
            if (int.TryParse(input, out idx) && idx >= 1 && idx <= list.Count)
            {
                var e = list[idx - 1];
                Console.WriteLine("当前: " + e[0] + " | " + e[1] + " | 包名: " + (e[2].Length > 0 ? e[2] : "未填"));
                Console.Write("新名称（回车保持不变）: ");
                string n = Console.ReadLine(); if (n == null) return; n = n.Trim();
                Console.Write("新仓库（owner/repo，回车保持不变）: ");
                string r = Console.ReadLine(); if (r == null) return; r = r.Trim();
                Console.Write("新包名（回车保持不变）: ");
                string p = Console.ReadLine(); if (p == null) return; p = p.Trim();
                if (n.Length > 0) e[0] = n;
                if (r.Length > 0) e[1] = r;
                if (p.Length > 0) e[2] = p;
                SaveApkSources(list);
                Console.WriteLine("已修改: " + e[0] + "（" + e[1] + "）");
                continue;
            }
            Console.WriteLine("无效输入：" + input);
        }
    }

    // 下载 APK-方式4：编辑 GitHub Token（config\github_token.txt）
    static void EditGithubToken()
    {
        string cfgPath;
        try
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgDir = Path.Combine(exeDir, "config");
            if (!Directory.Exists(cfgDir)) Directory.CreateDirectory(cfgDir);
            cfgPath = Path.Combine(cfgDir, "github_token.txt");
            if (!File.Exists(cfgPath))
                File.WriteAllText(cfgPath,
                    "# 在此粘贴 GitHub Personal Access Token（可选，可避免 API 403 限流）\r\n"
                    + "# 获取：github.com → Settings → Developer settings → Personal access tokens\r\n", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine("! 无法访问配置文件：" + ex.Message);
            return;
        }

        Console.WriteLine("========================================");
        Console.WriteLine("  编辑 GitHub Token");
        Console.WriteLine("========================================");
        // 显示当前状态（脱敏）
        string cur = "";
        try
        {
            string t = File.ReadAllText(cfgPath).Trim();
            if (t.Length > 0 && !t.StartsWith("#"))
                cur = t.Split('\n')[0].Trim();
        }
        catch { }
        Console.WriteLine("  当前 Token: " + (cur.Length > 0
            ? cur.Substring(0, Math.Min(8, cur.Length)) + "****（已设置）"
            : "（未设置）"));
        Console.WriteLine();
        Console.WriteLine("  粘贴新 Token（格式如 ghp_xxx 或 github_pat_xxx）；直接回车保持不变；输入 0 清除");
        Console.Write(">>> ");
        string input = Console.ReadLine();
        if (input == null) return;
        input = input.Trim();
        if (input.Length == 0) { Console.WriteLine("保持不变。"); return; }
        if (input == "0")
        {
            try
            {
                File.WriteAllText(cfgPath,
                    "# 在此粘贴 GitHub Personal Access Token（可选，可避免 API 403 限流）\r\n", Encoding.UTF8);
                _githubToken = "";
                Console.WriteLine("已清除 Token。");
            }
            catch (Exception ex) { Console.WriteLine("! 清除失败：" + ex.Message); }
            return;
        }
        try
        {
            File.WriteAllText(cfgPath, input + "\r\n", Encoding.UTF8);
            _githubToken = input;   // 立即生效
            Console.WriteLine("已保存 Token（立即生效）。");
        }
        catch (Exception ex)
        {
            Console.WriteLine("! 保存失败：" + ex.Message);
        }
    }

    // 获取指定仓库的最新 APK 资产并下载安装（下载安装/检查更新共用）
    static void DownloadAndInstall(string name, string repo)
    {
        Console.WriteLine("正在查询 " + name + "（" + NormalizeRepo(repo) + "）的最新 Release...");
        var apks = GetGithubApkAssets(repo);
        if (apks.Count == 0)
        {
            Console.WriteLine("! 未找到 APK 资产。若为网络/限流问题，可在 config\\github_token.txt 填入 Token 后重试；");
            Console.WriteLine("  或确认该 Release 确实附带 .apk 文件。");
            return;
        }

        string assetName, assetUrl;
        string[] picked = PickBestApk(apks);
        if (picked != null)
        {
            assetName = picked[0];
            assetUrl = picked[1];
            Console.WriteLine("已按架构自动选择: " + assetName);
        }
        else if (apks.Count == 1)
        {
            assetName = apks[0][0];
            assetUrl = apks[0][1];
            Console.WriteLine("找到: " + assetName);
        }
        else
        {
            Console.WriteLine("未识别到 arm64-v8a / armeabi-v7a，找到 " + apks.Count + " 个 APK，请选择：");
            for (int i = 0; i < apks.Count; i++)
                Console.WriteLine("  [" + (i + 1) + "] " + apks[i][0]);
            Console.Write(">>> ");
            string asel = Console.ReadLine();
            if (asel == null) return;
            asel = asel.Trim();
            int ai;
            if (!int.TryParse(asel, out ai) || ai < 1 || ai > apks.Count)
            {
                Console.WriteLine("已取消。");
                return;
            }
            assetName = apks[ai - 1][0];
            assetUrl = apks[ai - 1][1];
        }

        Console.Write("确认下载并安装 " + assetName + "？（y/N）：");
        string yn = Console.ReadLine();
        if (yn == null) return;
        yn = yn.Trim().ToLowerInvariant();
        if (yn != "y" && yn != "yes") { Console.WriteLine("已取消。"); return; }

        // 下载到临时文件
        string tmp = Path.Combine(Path.GetTempPath(),
            "adbtoolbox_dl_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".apk");
        Console.WriteLine("正在下载...");
        try
        {
            DownloadFile(assetUrl, tmp);
        }
        catch (Exception ex)
        {
            Console.WriteLine("! 下载失败：" + ex.Message);
            return;
        }
        long size = 0;
        try { size = new FileInfo(tmp).Length; } catch { }
        Console.WriteLine("下载完成（" + (size / 1024.0 / 1024.0).ToString("0.0") + " MB），正在安装...");
        string result = RunAdb("install -r -d \"" + tmp + "\"");
        Console.WriteLine(result.Trim());
        if (result.Contains("Success"))
            Console.WriteLine("安装成功！");
        else
            Console.WriteLine("! 安装失败，请检查上面的输出。");
        try { File.Delete(tmp); } catch { }
    }

    // 从多个 APK 中按架构优先级挑选最佳候选：arm64-v8a > armeabi-v7a > 无
    // 都不匹配时返回 null，由调用方列出全部 APK 供手动选择
    static string[] PickBestApk(List<string[]> apks)
    {
        foreach (var a in apks)
            if (a[0].IndexOf("arm64-v8a", StringComparison.OrdinalIgnoreCase) >= 0
                || a[0].IndexOf("arm64", StringComparison.OrdinalIgnoreCase) >= 0)
                return a;
        foreach (var a in apks)
            if (a[0].IndexOf("armeabi-v7a", StringComparison.OrdinalIgnoreCase) >= 0
                || a[0].IndexOf("armv7", StringComparison.OrdinalIgnoreCase) >= 0)
                return a;
        return null;
    }

    // 读取 APK 软件源列表（config\apk_sources.txt，首次运行自动生成默认模板）
    static List<string[]> LoadApkSources()
    {
        try
        {
            string cfgPath = ApkSourcesPath();
            string cfgDir = Path.GetDirectoryName(cfgPath);
            if (!Directory.Exists(cfgDir)) Directory.CreateDirectory(cfgDir);
            if (!File.Exists(cfgPath))
            {
                File.WriteAllText(cfgPath,
                    "# APK 软件源列表（每行：名称|GitHub仓库|包名，# 注释）\r\n"
                    + "# 包名用于「检查更新」，可不填；可在菜单中增删改\r\n"
                    + "LSPosed|LSPosed/LSPosed|org.lsposed.manager\r\n"
                    + "Shizuku|RikkaApps/Shizuku|moe.shizuku.privileged.api\r\n"
                    + "ReVanced Manager|ReVanced/revanced-manager|app.revanced.manager.flutter\r\n", Encoding.UTF8);
            }
            return ReadApkSourcesFile();
        }
        catch (Exception ex)
        {
            Console.WriteLine("! 读取 config\\apk_sources.txt 失败：" + ex.Message);
            return new List<string[]>();
        }
    }

    // 读取 config\apk_sources.txt 全部条目 {名称, 仓库, 包名}
    static List<string[]> ReadApkSourcesFile()
    {
        var list = new List<string[]>();
        try
        {
            string cfgPath = ApkSourcesPath();
            if (File.Exists(cfgPath))
            {
                foreach (string line in File.ReadAllLines(cfgPath))
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#")) continue;
                    string[] parts = t.Split('|');
                    if (parts.Length >= 2)
                    {
                        string n = parts[0].Trim();
                        string r = parts[1].Trim();
                        string p = parts.Length >= 3 ? parts[2].Trim() : "";
                        if (n.Length > 0 && r.Length > 0)
                            list.Add(new string[] { n, r, p });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("! 读取 config\\apk_sources.txt 失败：" + ex.Message);
        }
        return list;
    }

    // 写回 config\apk_sources.txt
    static void SaveApkSources(List<string[]> list)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("# APK 软件源列表（每行：名称|GitHub仓库|包名）");
            sb.AppendLine("# 包名用于「检查更新」，可不填");
            foreach (var e in list)
                sb.AppendLine(e[0] + "|" + e[1] + "|" + e[2]);
            File.WriteAllText(ApkSourcesPath(), sb.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Console.WriteLine("! 写入 config\\apk_sources.txt 失败：" + ex.Message);
        }
    }

    // config\apk_sources.txt 完整路径
    static string ApkSourcesPath()
    {
        string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return Path.Combine(Path.Combine(exeDir, "config"), "apk_sources.txt");
    }

    // 把仓库地址规范化为 owner/repo（兼容用户填完整 URL，如 https://github.com/owner/repo/releases）
    static string NormalizeRepo(string repo)
    {
        string r = (repo ?? "").Trim().TrimEnd('/');
        string key = "github.com/";
        int i = r.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (i >= 0)
            r = r.Substring(i + key.Length);
        else
        {
            int p = r.IndexOf("://", StringComparison.Ordinal);
            if (p >= 0) r = r.Substring(p + 3);
        }
        string[] parts = r.Split('/');
        if (parts.Length >= 2)
            return parts[0] + "/" + parts[1];
        return r;
    }

    // 获取 GitHub 最新 Release 的版本号（tag_name）；API 受限时回退到 Release 页面提取
    static string GetGithubReleaseTag(string repo)
    {
        try
        {
            var req = CreateGitHubRequest(
                "https://api.github.com/repos/" + NormalizeRepo(repo) + "/releases/latest");
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                string json = reader.ReadToEnd();
                var ser = new JavaScriptSerializer();
                var rel = ser.DeserializeObject(json) as Dictionary<string, object>;
                if (rel != null && rel.ContainsKey("tag_name"))
                    return Convert.ToString(rel["tag_name"]);
            }
        }
        catch (Exception ex)
        {
            string body = WebErrorBody(ex);
            Console.WriteLine("! API 获取失败（" + ex.Message
                + (body.Length > 0 ? "；详情：" + body : "") + "），改用 Release 页面提取...");
            return GetGithubReleaseTagHtml(repo);
        }
        return "";
    }

    // 从 Release 页面 HTML 提取版本号（tag），备选方案
    static string GetGithubReleaseTagHtml(string repo)
    {
        try
        {
            var req = CreateGitHubRequest("https://github.com/" + NormalizeRepo(repo) + "/releases/latest");
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                string html = reader.ReadToEnd();
                Match m = Regex.Match(html, "<title>Release ([^<]+) ·", RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value.Trim();
                m = Regex.Match(html, @"releases/download/([^/""]+)/[^""]*\.apk", RegexOptions.IgnoreCase);
                if (m.Success) return m.Groups[1].Value.Trim();
            }
        }
        catch { }
        return "";
    }

    // 读取已安装应用的版本号（dumpsys package 的 versionName）
    static string GetInstalledVersion(string pkg)
    {
        string info = RunAdb("shell dumpsys package " + pkg);
        foreach (string line in info.Split('\n'))
        {
            string t = line.Trim();
            if (t.StartsWith("versionName="))
                return t.Substring("versionName=".Length).Trim();
        }
        return "";
    }

    // 简单版本号比较：a > b 返回 1，a < b 返回 -1，相等返回 0
    static int CompareVersions(string a, string b)
    {
        if (a == b) return 0;
        string pa = (a.Length > 0 && (a[0] == 'v' || a[0] == 'V')) ? a.Substring(1) : a;
        string pb = (b.Length > 0 && (b[0] == 'v' || b[0] == 'V')) ? b.Substring(1) : b;
        string[] sa = Regex.Split(pa, @"[^\d]+");
        string[] sb = Regex.Split(pb, @"[^\d]+");
        int n = Math.Max(sa.Length, sb.Length);
        for (int i = 0; i < n; i++)
        {
            int x = (i < sa.Length && sa[i].Length > 0) ? int.Parse(sa[i]) : 0;
            int y = (i < sb.Length && sb[i].Length > 0) ? int.Parse(sb[i]) : 0;
            if (x > y) return 1;
            if (x < y) return -1;
        }
        return 0;
    }

    // 从 GitHub 获取最新 Release 中的 APK 资产：{文件名, 下载地址}；API 受限时回退到 Release 页面提取
    static List<string[]> GetGithubApkAssets(string repo)
    {
        var list = new List<string[]>();
        try
        {
            var req = CreateGitHubRequest(
                "https://api.github.com/repos/" + NormalizeRepo(repo) + "/releases/latest");
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                string json = reader.ReadToEnd();
                var ser = new JavaScriptSerializer();
                var rel = ser.DeserializeObject(json) as Dictionary<string, object>;
                if (rel != null && rel.ContainsKey("assets"))
                {
                    var assets = rel["assets"] as object[];
                    if (assets != null)
                    {
                        foreach (object a in assets)
                        {
                            var asset = a as Dictionary<string, object>;
                            if (asset == null) continue;
                            string an = Convert.ToString(asset["name"]);
                            string au = Convert.ToString(asset["browser_download_url"]);
                            if (an.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                                list.Add(new string[] { an, au });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            string body = WebErrorBody(ex);
            Console.WriteLine("! API 获取失败（" + ex.Message
                + (body.Length > 0 ? "；详情：" + body : "") + "），改用 Release 页面提取...");
            list = GetGithubApkAssetsHtml(repo);
            if (list.Count == 0)
                Console.WriteLine("! Release 页面也未能提取到 APK 链接。");
        }
        return list;
    }

    // 从 Release 页面提取 .apk 下载链接，备选方案：
    // 先请求 expanded_assets 展开页（纯 <a> 片段），失败则回退主页全文提取（含内嵌 JSON 数据）
    static List<string[]> GetGithubApkAssetsHtml(string repo)
    {
        var list = new List<string[]>();
        string r = NormalizeRepo(repo);
        string tag = GetGithubReleaseTagHtml(repo);
        if (tag.Length == 0) return list;

        // 方式一：expanded_assets 展开页
        try
        {
            var req = CreateGitHubRequest("https://github.com/" + r + "/releases/expanded_assets/" + Uri.EscapeDataString(tag));
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                ExtractApkUrls(reader.ReadToEnd(), list);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("! expanded_assets 提取失败（" + ex.Message + "），改从主页提取...");
        }
        if (list.Count > 0) return list;

        // 方式二：主页全文（href + 内嵌 JSON 里的 browser_download_url）
        try
        {
            var req = CreateGitHubRequest("https://github.com/" + r + "/releases/tag/" + Uri.EscapeDataString(tag));
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                string html = reader.ReadToEnd().Replace("\\u002f", "/").Replace("\\u0026", "&");
                ExtractApkUrls(html, list);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("! 主页提取失败（" + ex.Message + "）。");
        }
        return list;
    }

    // 从 HTML 文本中提取 github.com 的 release 下载链接（href 或 JSON 内嵌均兼容）
    static void ExtractApkUrls(string html, List<string[]> list)
    {
        foreach (Match m in Regex.Matches(html,
            @"https://github\.com/[^""'\s<>\\]+/releases/download/[^""'\s<>\\]+\.apk", RegexOptions.IgnoreCase))
        {
            string url = m.Value;
            string fname = Path.GetFileName(url);
            if (!list.Exists(e => e[1] == url))
                list.Add(new string[] { fname, url });
        }
    }

    // 创建 GitHub 请求：统一 UA/超时，支持 Token 与系统代理（含 HTTP_PROXY / HTTPS_PROXY 环境变量）
    static HttpWebRequest CreateGitHubRequest(string url)
    {
        var req = (HttpWebRequest)WebRequest.Create(url);
        req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) ADBToolbox/1.0";
        req.Timeout = 20000;
        try
        {
            // GitHub Token（config\github_token.txt），可将 API 限额提升到 5000/小时
            string tk = GithubToken;
            if (tk.Length > 0 && url.IndexOf("api.github.com", StringComparison.OrdinalIgnoreCase) >= 0)
                req.Headers["Authorization"] = "token " + tk;

            // 优先用显式的代理环境变量（.NET Framework 默认不读它们），否则用系统代理
            string envProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            if (string.IsNullOrEmpty(envProxy))
                envProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
            if (!string.IsNullOrEmpty(envProxy))
                req.Proxy = new WebProxy(envProxy);
            else
                req.Proxy = WebRequest.GetSystemWebProxy();
        }
        catch { }
        return req;
    }

    // 惰性读取 config\github_token.txt（首次运行自动生成模板）
    static string _githubToken;
    static string GithubToken
    {
        get
        {
            if (_githubToken == null)
            {
                _githubToken = "";
                try
                {
                    string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string cfgDir = Path.Combine(exeDir, "config");
                    string tp = Path.Combine(cfgDir, "github_token.txt");
                    if (!Directory.Exists(cfgDir)) Directory.CreateDirectory(cfgDir);
                    if (!File.Exists(tp))
                        File.WriteAllText(tp,
                            "# 在此粘贴 GitHub Personal Access Token（可选，可避免 API 403 限流）\r\n"
                            + "# 获取：github.com → Settings → Developer settings → Personal access tokens → 新建\r\n"
                            + "# 只需勾选 public_repo 即可；每行可放一个，取第一个\r\n", Encoding.UTF8);
                    string t = File.ReadAllText(tp).Trim();
                    if (t.Length > 0 && !t.StartsWith("#"))
                        _githubToken = t.Split('\n')[0].Trim();
                }
                catch { }
            }
            return _githubToken;
        }
    }

    // 尽量从 WebException 响应里读出错误详情（GitHub 会返回限流/封禁原因）
    static string WebErrorBody(Exception ex)
    {
        var wex = ex as WebException;
        if (wex == null || wex.Response == null) return "";
        try
        {
            using (var r = new StreamReader(wex.Response.GetResponseStream(), Encoding.UTF8))
            {
                string s = r.ReadToEnd().Trim();
                if (s.Length > 200) s = s.Substring(0, 200) + "...";
                return s;
            }
        }
        catch { return ""; }
    }

    // 下载文件到本地
    static void DownloadFile(string url, string dest)
    {
        var req = CreateGitHubRequest(url);
        req.ReadWriteTimeout = 120000;   // 大文件传输放宽读写超时
        using (var resp = (HttpWebResponse)req.GetResponse())
        using (var inS = resp.GetResponseStream())
        using (var outS = File.Create(dest))
            inS.CopyTo(outS);
    }
}

