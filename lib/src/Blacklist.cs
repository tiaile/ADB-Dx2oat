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
    // ============ 黑名单（config\blacklist.txt） ============
    static HashSet<string> Blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // 加载黑名单；文件不存在则生成模板
    static void LoadBlacklist()
    {
        Blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgDir = Path.Combine(exeDir, "config");
            Directory.CreateDirectory(cfgDir);
            string path = Path.Combine(cfgDir, "blacklist.txt");
            if (!File.Exists(path))
            {
                File.WriteAllText(path,
                    "# 黑名单（每行一个包名，# 开头为注释）\n" +
                    "# 黑名单内的应用不会出现在“未处理应用”列表，也不会被一键编译。\n",
                    new UTF8Encoding(true));
            }
            foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
            {
                string t = line.Trim();
                if (t.Length == 0 || t.StartsWith("#")) continue;
                Blacklist.Add(t);
            }
        }
        catch { }
    }

    // 加入黑名单（保留注释，按包名 A-Z 排序重写文件）
    static bool AddToBlacklist(string pkg)
    {
        try
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgDir = Path.Combine(exeDir, "config");
            Directory.CreateDirectory(cfgDir);
            string path = Path.Combine(cfgDir, "blacklist.txt");
            var comments = new List<string>();
            var pkgs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(path))
            {
                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#")) { comments.Add(line); continue; }
                    pkgs.Add(t);
                }
            }
            if (!pkgs.Add(pkg)) return false;   // 已在黑名单

            var sb = new StringBuilder();
            foreach (string c in comments) sb.AppendLine(c);
            if (comments.Count > 0) sb.AppendLine();
            var keys = new List<string>(pkgs);
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string k in keys) sb.AppendLine(k);
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));

            Blacklist.Add(pkg);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("! 写入 config\\blacklist.txt 失败：" + ex.Message);
            return false;
        }
    }

    // 移出黑名单（保留注释，按包名 A-Z 排序重写文件）
    static bool RemoveFromBlacklist(string pkg)
    {
        try
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgDir = Path.Combine(exeDir, "config");
            Directory.CreateDirectory(cfgDir);
            string path = Path.Combine(cfgDir, "blacklist.txt");
            var comments = new List<string>();
            var pkgs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(path))
            {
                foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#")) { comments.Add(line); continue; }
                    pkgs.Add(t);
                }
            }
            if (!pkgs.Remove(pkg)) return false;   // 不在黑名单

            var sb = new StringBuilder();
            foreach (string c in comments) sb.AppendLine(c);
            if (comments.Count > 0) sb.AppendLine();
            var keys = new List<string>(pkgs);
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string k in keys) sb.AppendLine(k);
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));

            Blacklist.Remove(pkg);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("! 写入 config\\blacklist.txt 失败：" + ex.Message);
            return false;
        }
    }

    // 功能4：黑名单管理（查看/移出）
    static void ManageBlacklist()
    {
        while (true)
        {
            var list = new List<string>(Blacklist);
            list.Sort(StringComparer.OrdinalIgnoreCase);

            Console.WriteLine("========================================");
            Console.WriteLine("  黑名单管理（" + list.Count + " 个）");
            Console.WriteLine("========================================");
            if (list.Count == 0)
            {
                Console.WriteLine("  （黑名单为空）");
                Console.WriteLine("  提示：可在 dex2oat 检查的未处理列表中输入序号添加黑名单。");
                Console.WriteLine("========================================");
                return;
            }
            for (int i = 0; i < list.Count; i++)
            {
                string pkg = list[i];
                string name = AppName(pkg);
                Console.WriteLine("  [" + (i + 1) + "] " + (name == pkg ? pkg : name + " (" + pkg + ")"));
            }
            Console.WriteLine("========================================");
            Console.WriteLine("  序号  移出黑名单（可逗号分隔，如 1,3,5）");
            Console.WriteLine("  回车  返回菜单");
            Console.Write(">>> ");
            string input = Console.ReadLine();
            if (input == null) return;
            input = input.Trim();
            if (input.Length == 0) return;

            bool any = false;
            var removed = new List<string>();
            foreach (string part in input.Split(new char[] { ',', '，', ' ', '、', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int n;
                if (int.TryParse(part, out n) && n >= 1 && n <= list.Count)
                {
                    string pkg = list[n - 1];
                    if (RemoveFromBlacklist(pkg)) { removed.Add(pkg); any = true; }
                }
            }
            if (any)
            {
                Console.WriteLine("已移出黑名单：" + string.Join(", ", removed));
                // 继续循环，列表刷新后重新显示
            }
            else
            {
                Console.WriteLine("无效输入：" + input);
            }
        }
    }
}
