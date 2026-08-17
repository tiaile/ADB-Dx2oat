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
    // ================= 二维码生成（版本1-4，纠错级别 M，字节模式） =================
    static bool GfInited = false;
    static byte[] GfLog = new byte[256];
    static byte[] GfAntiLog = new byte[256];

    static void InitGf()
    {
        if (GfInited) return;
        GfInited = true;
        int x = 1;
        for (int i = 0; i < 255; i++)
        {
            GfAntiLog[i] = (byte)x;
            GfLog[x] = (byte)i;
            x <<= 1;
            if ((x & 0x100) != 0) x ^= 0x11D;
        }
    }

    static byte GfMul(byte a, byte b)
    {
        if (a == 0 || b == 0) return 0;
        int l = GfLog[a] + GfLog[b];
        if (l >= 255) l -= 255;
        return GfAntiLog[l];
    }

    static byte GfPow(int i)
    {
        return GfAntiLog[i % 255];
    }

    static void AddBits(List<bool> bits, int value, int count)
    {
        for (int i = count - 1; i >= 0; i--)
            bits.Add(((value >> i) & 1) != 0);
    }

    static byte BitsToByte(List<bool> bits, int offset)
    {
        int v = 0;
        for (int i = 0; i < 8; i++)
            if (bits[offset + i]) v |= 1 << (7 - i);
        return (byte)v;
    }

    // Reed-Solomon 生成多项式（degree 次）
    static byte[] RsGenPoly(int degree)
    {
        byte[] g = { 1 };
        for (int i = 0; i < degree; i++)
        {
            byte[] ng = new byte[g.Length + 1];
            for (int j = 0; j < g.Length; j++)
            {
                ng[j] ^= GfMul(g[j], GfPow(i));
                ng[j + 1] ^= g[j];
            }
            g = ng;
        }
        return g;
    }

    // Reed-Solomon 纠错码计算
    static byte[] RsEncode(byte[] data, int eccLen)
    {
        byte[] g = RsGenPoly(eccLen);
        byte[] res = new byte[data.Length + eccLen];
        Array.Copy(data, res, data.Length);
        for (int i = 0; i < data.Length; i++)
        {
            byte coef = res[i];
            if (coef == 0) continue;
            for (int j = 0; j < g.Length; j++)
                res[i + j] ^= GfMul(g[j], coef);
        }
        byte[] ecc = new byte[eccLen];
        Array.Copy(res, data.Length, ecc, 0, eccLen);
        return ecc;
    }

    // 生成二维码矩阵（true=深色模块）
    static bool[,] QrEncode(string text)
    {
        InitGf();
        byte[] data = Encoding.UTF8.GetBytes(text);
        int[] caps = { 14, 26, 42, 62 };      // 版本1-4（M级）字节容量
        int[] dataCw = { 16, 28, 44, 64 };    // 版本1-4（M级）数据码字数
        int[] eccCw = { 10, 16, 26, 36 };     // 版本1-4（M级）纠错码字数
        int ver = 1;
        for (int v = 1; v <= 4; v++)
            if (data.Length <= caps[v - 1]) { ver = v; break; }
        if (data.Length > caps[3])
            throw new Exception("内容超过 62 字节");

        // 数据位：模式指示符 0100 + 8位字符数 + 数据
        var bits = new List<bool>();
        AddBits(bits, 0x4, 4);
        AddBits(bits, data.Length, 8);
        foreach (byte b in data) AddBits(bits, b, 8);
        while (bits.Count % 8 != 0) bits.Add(false);
        int totalData = dataCw[ver - 1];
        int totalEcc = eccCw[ver - 1];
        byte[] pad = { 0xEC, 0x11 };
        int pi = 0;
        while (bits.Count / 8 < totalData)
        {
            AddBits(bits, pad[pi], 8);
            pi = 1 - pi;
        }
        byte[] cw = new byte[totalData];
        for (int i = 0; i < totalData; i++)
            cw[i] = BitsToByte(bits, i * 8);
        byte[] ecc = RsEncode(cw, totalEcc);

        int size = 17 + 4 * ver;
        var m = new bool[size, size];
        var res = new bool[size, size];   // 数据不可占用区域

        // 定位图形 + 分隔符（三个 8x8 区域）
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                res[i, j] = true;
                res[size - 8 + i, j] = true;
                res[i, size - 8 + j] = true;
            }
        PlaceFinder(m, 0, 0);
        PlaceFinder(m, size - 7, 0);
        PlaceFinder(m, 0, size - 7);

        // 时序图形
        for (int i = 8; i < size - 8; i++)
        {
            bool v = (i % 2 == 0);
            m[6, i] = v;
            m[i, 6] = v;
            res[6, i] = true;
            res[i, 6] = true;
        }

        // 对齐图形（版本2-4 各 1 个）
        int[][] align = { null, null, new int[] { 6, 18 }, new int[] { 6, 22 }, new int[] { 6, 26 } };
        if (ver >= 2)
        {
            int[] centers = align[ver];
            for (int a = 0; a < centers.Length; a++)
                for (int b = 0; b < centers.Length; b++)
                {
                    if ((a == 0 && b == 0) || (a == 0 && b == centers.Length - 1) || (a == centers.Length - 1 && b == 0))
                        continue;   // 与三个定位图形重叠的位置
                    for (int dr = -2; dr <= 2; dr++)
                        for (int dc = -2; dc <= 2; dc++)
                            res[centers[a] + dr, centers[b] + dc] = true;
                    PlaceAlignment(m, centers[a], centers[b]);
                }
        }

        // 深色模块 + 格式信息区域（整行8 / 整列8）
        m[size - 8, 8] = true;
        for (int i = 0; i < size; i++)
        {
            res[8, i] = true;
            res[i, 8] = true;
        }

        // 全部数据位（数据码字 + 纠错码字）
        var all = new List<bool>();
        foreach (byte c in cw) AddBits(all, c, 8);
        foreach (byte e in ecc) AddBits(all, e, 8);

        // 依次尝试 8 种掩码，选惩罚分最低的
        int bestPenalty = int.MaxValue;
        bool[,] best = null;
        for (int mask = 0; mask < 8; mask++)
        {
            bool[,] mm = (bool[,])m.Clone();
            int bi = 0;
            int col = size - 1;
            bool up = true;
            while (col > 0)
            {
                if (col == 6) col--;   // 跳过时序列
                int row = up ? size - 1 : 0;
                while (row >= 0 && row < size)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        int c = col - k;
                        if (c >= 0 && !res[row, c])
                        {
                            bool bit = bi < all.Count ? all[bi] : false;
                            if (bi < all.Count) bi++;
                            mm[row, c] = bit != MaskBit(mask, row, c);
                        }
                    }
                    row += up ? -1 : 1;
                }
                up = !up;
                col -= 2;
            }

            // 格式信息（M 级指示符 00）
            int fmt = mask;
            int rem = fmt << 10;
            for (int i = 14; i >= 10; i--)
                if (((rem >> i) & 1) != 0)
                    rem ^= 0x537 << (i - 10);
            int final = ((fmt << 10) | rem) ^ 0x5412;
            PlaceFormat(mm, size, final);

            int p = QrPenalty(mm, size);
            if (p < bestPenalty) { bestPenalty = p; best = mm; }
        }
        return best;
    }

    static void PlaceFinder(bool[,] m, int r0, int c0)
    {
        for (int r = 0; r < 7; r++)
            for (int c = 0; c < 7; c++)
            {
                bool dark = (r == 0 || r == 6 || c == 0 || c == 6) || (r >= 2 && r <= 4 && c >= 2 && c <= 4);
                m[r0 + r, c0 + c] = dark;
            }
    }

    static void PlaceAlignment(bool[,] m, int r, int c)
    {
        for (int dr = -2; dr <= 2; dr++)
            for (int dc = -2; dc <= 2; dc++)
            {
                bool dark = (Math.Abs(dr) == 2 || Math.Abs(dc) == 2) || (dr == 0 && dc == 0);
                m[r + dr, c + dc] = dark;
            }
    }

    static void PlaceFormat(bool[,] m, int size, int fmt)
    {
        for (int i = 0; i < 15; i++)
        {
            bool bit = ((fmt >> i) & 1) != 0;
            // 副本1：左上角定位图形周围
            if (i < 6) m[8, i] = bit;
            else if (i == 6) m[8, 7] = bit;
            else if (i == 7) m[8, 8] = bit;
            else if (i == 8) m[7, 8] = bit;
            else m[14 - i, 8] = bit;
            // 副本2：左下（列8）放 bits 0-7，右上（行8）放 bits 8-14（对齐 qrcodegen 参考实现）
            if (i < 8) m[size - 1 - i, 8] = bit;
            else m[8, size - 15 + i] = bit;
        }
    }

    static bool MaskBit(int mask, int r, int c)
    {
        switch (mask)
        {
            case 0: return (r + c) % 2 == 0;
            case 1: return r % 2 == 0;
            case 2: return c % 3 == 0;
            case 3: return (r + c) % 3 == 0;
            case 4: return (r / 2 + c / 3) % 2 == 0;
            case 5: return (r * c) % 2 + (r * c) % 3 == 0;
            case 6: return ((r * c) % 2 + (r * c) % 3) % 2 == 0;
            default: return ((r + c) % 2 + (r * c) % 3) % 2 == 0;
        }
    }

    // 掩码惩罚分（4 条规则）
    static int QrPenalty(bool[,] m, int size)
    {
        int penalty = 0;
        // 规则1：连续 ≥5 个同色
        for (int r = 0; r < size; r++)
        {
            int run = 1;
            for (int c = 1; c < size; c++)
            {
                if (m[r, c] == m[r, c - 1]) { run++; continue; }
                if (run >= 5) penalty += 3 + (run - 5);
                run = 1;
            }
            if (run >= 5) penalty += 3 + (run - 5);
        }
        for (int c = 0; c < size; c++)
        {
            int run = 1;
            for (int r = 1; r < size; r++)
            {
                if (m[r, c] == m[r - 1, c]) { run++; continue; }
                if (run >= 5) penalty += 3 + (run - 5);
                run = 1;
            }
            if (run >= 5) penalty += 3 + (run - 5);
        }
        // 规则2：2x2 同色块
        for (int r = 0; r < size - 1; r++)
            for (int c = 0; c < size - 1; c++)
            {
                bool v = m[r, c];
                if (v == m[r, c + 1] && v == m[r + 1, c] && v == m[r + 1, c + 1])
                    penalty += 3;
            }
        // 规则3：1:1:3:1:1 找位模式
        for (int r = 0; r < size; r++)
            for (int c = 0; c + 6 < size; c++)
            {
                bool pat = m[r, c] && !m[r, c + 1] && m[r, c + 2] && m[r, c + 3] && m[r, c + 4] && !m[r, c + 5] && m[r, c + 6];
                if (!pat) continue;
                if (c - 4 >= 0 && !m[r, c - 1] && !m[r, c - 2] && !m[r, c - 3] && !m[r, c - 4]) penalty += 40;
                if (c + 10 < size && !m[r, c + 7] && !m[r, c + 8] && !m[r, c + 9] && !m[r, c + 10]) penalty += 40;
            }
        for (int c = 0; c < size; c++)
            for (int r = 0; r + 6 < size; r++)
            {
                bool pat = m[r, c] && !m[r + 1, c] && m[r + 2, c] && m[r + 3, c] && m[r + 4, c] && !m[r + 5, c] && m[r + 6, c];
                if (!pat) continue;
                if (r - 4 >= 0 && !m[r - 1, c] && !m[r - 2, c] && !m[r - 3, c] && !m[r - 4, c]) penalty += 40;
                if (r + 10 < size && !m[r + 7, c] && !m[r + 8, c] && !m[r + 9, c] && !m[r + 10, c]) penalty += 40;
            }
        // 规则4：深浅比例
        int dark = 0;
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
                if (m[r, c]) dark++;
        int percent = dark * 100 / (size * size);
        int prev5 = (percent / 5) * 5;
        int a = Math.Abs(prev5 - 50) / 5;
        int b = Math.Abs(prev5 + 5 - 50) / 5;
        penalty += Math.Min(a, b) * 10;
        return penalty;
    }

    // 在控制台渲染二维码（白底黑块，四周留 4 模块静区）
    // 只用空格 + 背景色绘制，不依赖 █▀▄ 等特殊字符，避免控制台编码（如 GBK）不支持时显示成"?“
    static void RenderQr(bool[,] m)
    {
        int size = m.GetLength(0);
        int pad = 4;
        int total = size + pad * 2;
        ConsoleColor fg = Console.ForegroundColor;
        ConsoleColor bg = Console.BackgroundColor;
        try
        {
            for (int y = 0; y < total; y++)
            {
                for (int x = 0; x < total; x++)
                {
                    bool dark = y - pad >= 0 && x - pad >= 0 && y - pad < size && x - pad < size
                                && m[y - pad, x - pad];
                    Console.BackgroundColor = dark ? ConsoleColor.Black : ConsoleColor.White;
                    // 每个模块画 2 个空格（终端字符高约为宽的 2 倍），保证二维码整体为正方形
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
}
