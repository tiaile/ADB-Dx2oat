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

    // 把 包名=名称 写回 exe 旁的 config\appnames.txt（新增或覆盖，保持 A-Z 排序）
    static bool SaveAppName(string pkg, string name)
    {
        try
        {
            string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string cfgDir = Path.Combine(exeDir, "config");
            Directory.CreateDirectory(cfgDir);
            string cfgPath = Path.Combine(cfgDir, "appnames.txt");
            var comments = new List<string>();
            var entries = new Dictionary<string, string>();
            if (File.Exists(cfgPath))
            {
                foreach (string line in File.ReadAllLines(cfgPath, Encoding.UTF8))
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#")) { comments.Add(line); continue; }
                    int eq = t.IndexOf('=');
                    if (eq <= 0) { comments.Add(line); continue; }
                    entries[t.Substring(0, eq).Trim()] = t.Substring(eq + 1).Trim();
                }
            }
            entries[pkg] = name;   // 新增或覆盖

            // 重写：原有注释 + 按包名 A-Z 排序的条目
            var sb = new StringBuilder();
            foreach (string c in comments) sb.AppendLine(c);
            if (comments.Count > 0) sb.AppendLine();
            var keys = new List<string>(entries.Keys);
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string k in keys) sb.AppendLine(k + "=" + entries[k]);
            File.WriteAllText(cfgPath, sb.ToString(), new UTF8Encoding(true));

            AppNames[pkg] = name;   // 同步内存表
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("! 写入 config\\appnames.txt 失败：" + ex.Message);
            return false;
        }
    }
}
