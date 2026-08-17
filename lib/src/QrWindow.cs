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
    // ================= 二维码生成（QRCoder 成熟库） =================
    // 生成二维码矩阵（已含 4 模块静区）
    static List<System.Collections.BitArray> QrEncodeLib(string payload)
    {
        using (var gen = new QRCodeGenerator())
        {
            var data = gen.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            return data.ModuleMatrix;
        }
    }

    // 弹窗显示二维码（高清位图，手机直接扫屏幕）；返回窗口，配对结束后关闭
    static Form ShowQrWindow(string payload)
    {
        var mm = QrEncodeLib(payload);
        int n = mm.Count;
        int scale = 10;
        var bmp = new Bitmap(n * scale, n * scale);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            using (var dark = new SolidBrush(Color.Black))
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                        if (mm[y][x]) g.FillRectangle(dark, x * scale, y * scale, scale, scale);
        }
        var form = new Form
        {
            Text = "ADB 无线调试 - 二维码配对",
            StartPosition = FormStartPosition.CenterScreen,
            ClientSize = new Size(bmp.Width + 24, bmp.Height + 96),
            BackColor = Color.White,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };
        form.Controls.Add(new Label
        {
            Text = "手机：开发者选项 → 无线调试 → 使用二维码配对设备，扫描下方二维码",
            Dock = DockStyle.Top,
            Height = 72,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Microsoft YaHei", 11f)
        });
        form.Controls.Add(new PictureBox
        {
            Image = bmp,
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.White
        });
        return form;
    }

    // 控制台回退显示（弹窗不可用时）：仅空格+背景色，无特殊字符
    static void RenderQrConsole(List<System.Collections.BitArray> mm)
    {
        int n = mm.Count;
        ConsoleColor fg = Console.ForegroundColor;
        ConsoleColor bg = Console.BackgroundColor;
        try
        {
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    Console.BackgroundColor = mm[y][x] ? ConsoleColor.Black : ConsoleColor.White;
                    Console.Write("  ");
                }
                Console.WriteLine();
            }
        }
        finally
        {
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }
    }

    // 配对结束后关闭二维码窗口（可安全重复调用）
    static void CloseQr(Form f)
    {
        if (f == null) return;
        try { f.Invoke((Action)(() => f.Close())); } catch { }
    }
}
