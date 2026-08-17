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
    // 功能2：列出用户安装应用；输入序号可改/加名称，0=切换"只看未命名"
    static void ListApps()
    {
        ListAppsInner(false, "-3", "用户安装应用");
    }

    // 功能3：列出系统应用；交互同功能2
    static void ListSystemApps()
    {
        ListAppsInner(false, "-s", "系统应用");
    }

    static void ListAppsInner(bool onlyUnnamed, string pkgArgs, string title)
    {
        Console.WriteLine("正在获取应用列表...");
        string listOut = RunAdb("shell pm list packages " + pkgArgs);
        var pkgs = new List<string>();
        foreach (string line in listOut.Split('\n'))
        {
            string t = line.Trim();
            if (t.StartsWith("package:"))
                pkgs.Add(t.Substring("package:".Length).Trim());
        }
        pkgs.Sort(StringComparer.OrdinalIgnoreCase);

        // 构建显示项：{包名, 名称}；onlyUnnamed 时过滤掉已命名的
        var items = new List<string[]>();
        foreach (string pkg in pkgs)
        {
            string name = AppName(pkg);
            if (onlyUnnamed && name != pkg) continue;
            items.Add(new string[] { pkg, name });
        }

        Console.WriteLine(onlyUnnamed
            ? "未命名" + title + "（共 " + items.Count + " 个，可直接输入序号补名）："
            : title + "（共 " + items.Count + " 个）：");
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
        Console.WriteLine("输入序号可修改/添加该应用名称；[0] "
            + (onlyUnnamed ? "返回全部列表" : "只看未命名应用") + "；直接回车返回菜单");
        Console.Write(">>> ");
        string sel = Console.ReadLine();
        if (sel == null) return;
        sel = sel.Trim();
        if (sel.Length == 0) return;
        if (sel == "0")
        {
            ListAppsInner(!onlyUnnamed, pkgArgs, title);   // 切换未命名筛选
            return;
        }
        int idx;
        if (!int.TryParse(sel, out idx) || idx < 1 || idx > items.Count)
        {
            Console.WriteLine("无效序号：" + sel);
            return;
        }
        string spkg = items[idx - 1][0];
        string sname = items[idx - 1][1];
        Console.WriteLine("当前：包名=" + spkg + "，名称=" + (sname == spkg ? "(未命名)" : sname));
        Console.Write("输入新名称（直接回车取消）：");
        string newName = Console.ReadLine();
        if (newName == null) return;
        newName = newName.Trim();
        if (newName.Length == 0)
        {
            Console.WriteLine("已取消。");
            return;
        }
        if (SaveAppName(spkg, newName))
        {
            Console.WriteLine("已保存：" + spkg + " = " + newName);
            ListAppsInner(onlyUnnamed, pkgArgs, title);   // 刷新列表（改名的会从未命名区消失/更新）
        }
    }
}
