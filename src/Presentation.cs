using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace CodexUsageBar
{
    static class Presentation
    {
        public const int CellWidth = 354, Gap = 24, Height = 32, Top = 2, Right = 170;
        public const float TrackX = 110, TrackWidth = 110, TrackHeight = 10;
        public const float LabelWidth = 102, ResetX = 256, ResetWidth = 98;
        public const string FontFamilyName = "Segoe UI";
        public const float FontSize = 14, LineHeight = 20;
        static readonly Dictionary<string, Bitmap> textCache = new Dictionary<string, Bitmap>();
        static readonly Image resetIcon = LoadResetIcon();
        static Image LoadResetIcon()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                       "CodexUsageBar.Reset.png")) using (Image source =
                                                              Image.FromStream(
                                                                  stream)) return new Bitmap(source);
        }
        public static Font MenuFont(float scale)
        {
            // Live ChatGPT controls: system sans stack, 14 CSS px / 20px, weight 400.
            // On Windows this is Segoe UI with system CJK fallback. DPI applies once here.
            return new Font(FontFamilyName, FontSize * scale, FontStyle.Regular, GraphicsUnit.Pixel);
        }
        public static string ResetText(UsageWindow w)
        {
            if (!w.ResetsAt.HasValue)
                return "时间暂不可用";
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(w.ResetsAt.Value)
                .ToLocalTime()
                .ToString("MM/dd HH:mm", CultureInfo.InvariantCulture);
        }
        public static int Width(Quota q)
        {
            return (q != null && q.Main != null ? q.Main.Windows.Count : 1) * (CellWidth + Gap) - Gap;
        }
        public static double RemainingFraction(double used)
        {
            return 1 - Math.Max(0, Math.Min(100, used)) / 100;
        }
        public static float FillEndX(double used)
        {
            return TrackX + TrackWidth * (float)RemainingFraction(used);
        }
        public static void Draw(Graphics g, float scale, Quota quota, bool busy, bool stale,
                                double[] displayedUsed = null)
        {
            g.Clear(Palette.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // Electron's supplied menu reference uses grayscale glyph edges, not LCD color fringes.
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using (var font = MenuFont(scale))
            {
                if (quota == null || quota.Main == null)
                {
                    DrawText(g, busy ? "读取额度…" : "额度暂不可用", font, R(0, 0, LabelWidth, Height, scale),
                             Palette.Secondary, true);
                    DrawTrack(g, scale, 0, null, stale);
                    DrawText(g, "时间暂不可用", font, R(ResetX, 0, ResetWidth, Height, scale),
                             Palette.Secondary);
                    return;
                }
                int index = 0;
                foreach (var w in quota.Main.Windows)
                {
                    float x = index * (CellWidth + Gap);
                    double used = displayedUsed != null && index < displayedUsed.Length ? displayedUsed[index]
                                                                                        : w.UsedPercent;
                    string label = w.Label + "限额 " +
                                   w.Remaining.ToString("0.#", CultureInfo.InvariantCulture) + "%" +
                                   (stale ? "*" : "");
                    DrawText(g, label, font, R(x, 0, LabelWidth, Height, scale), Palette.Secondary, true);
                    DrawTrack(g, scale, x, used, stale);
                    g.DrawImage(resetIcon,
                                new RectangleF((x + 232) * scale, 8 * scale, 16 * scale, 16 * scale));
                    DrawText(g, ResetText(w), font, R(x + ResetX, 0, ResetWidth, Height, scale),
                             Palette.Secondary);
                    index++;
                }
            }
        }
        static void DrawText(Graphics g, string text, Font font, Rectangle rect, Color color,
                             bool alignRight = false)
        {
            string key = font.Name + "|" + font.Size.ToString("R", CultureInfo.InvariantCulture) + "|" +
                         rect.Width + "|" + color.ToArgb() + "|" + text;
            Bitmap stamp;
            if (!textCache.TryGetValue(key, out stamp))
            {
                // GDI+ centers font leading, which leaves the visible date below the icon.
                // Cache the actual glyph ink instead; do not rescale or blur the glyph bitmap.
                if (textCache.Count >= 128)
                {
                    foreach (var entry in textCache.Values)
                        entry.Dispose();
                    textCache.Clear();
                }
                const int padding = 8;
                int height = Math.Max(64, font.Height + padding * 4);
                using (var source = new Bitmap(rect.Width + padding * 2, height)) using (
                    var sg = Graphics.FromImage(
                        source)) using (var format = (StringFormat)StringFormat.GenericTypographic
                                                         .Clone()) using (var brush = new SolidBrush(color))
                {
                    sg.Clear(Palette.Background);
                    sg.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    format.FormatFlags = StringFormatFlags.NoWrap;
                    format.Trimming = StringTrimming.EllipsisCharacter;
                    sg.DrawString(text, font, brush,
                                  new RectangleF(padding, padding, rect.Width, height - padding * 2), format);
                    int top = height, bottom = -1, right = padding;
                    for (int y = 0; y < height; y++)
                        for (int x = padding; x < padding + rect.Width; x++)
                        {
                            if (source.GetPixel(x, y).R > Palette.Background.R + 2)
                            {
                                top = Math.Min(top, y);
                                bottom = Math.Max(bottom, y);
                                right = Math.Max(right, x);
                            }
                        }
                    if (bottom < top)
                    {
                        top = 0;
                        bottom = 0;
                    }
                    stamp = source.Clone(new Rectangle(padding, top, right - padding + 1, bottom - top + 1),
                                         source.PixelFormat);
                }
                textCache.Add(key, stamp);
            }
            int lineHeight = (int)Math.Round(LineHeight * font.Size / FontSize);
            float lineTop = rect.Top + (rect.Height - lineHeight) / 2f;
            int textTop = (int)Math.Round(lineTop + lineHeight / 2f - (stamp.Height - 1) / 2f,
                                          MidpointRounding.AwayFromZero);
            // Keep the visible label 8 DIP from the bar regardless of digit count.
            g.DrawImageUnscaled(stamp, alignRight ? rect.Right - stamp.Width : rect.Left, textTop);
        }
        static void DrawTrack(Graphics g, float scale, float offset, double? used, bool stale)
        {
            var rect = new RectangleF((offset + TrackX) * scale, (Height - TrackHeight) * scale / 2,
                                      TrackWidth * scale, TrackHeight * scale);
            using (var path = Capsule(rect))
            {
                using (var brush = new SolidBrush(Palette.Track)) g.FillPath(brush, path);
                if (used.HasValue && RemainingFraction(used.Value) > 0)
                {
                    var filled = new RectangleF(
                        rect.Left, rect.Top, rect.Width * (float)RemainingFraction(used.Value), rect.Height);
                    using (var fillPath = Capsule(filled)) using (
                        var gradient = new LinearGradientBrush(rect, Palette.At(0), Palette.At(1), 0f))
                    {
                        var blend = new ColorBlend(101);
                        for (int i = 0; i <= 100; i++)
                        {
                            blend.Positions[i] = i / 100f;
                            blend.Colors[i] = stale ? Color.FromArgb(104, 104, 104) : Palette.At(i / 100.0);
                        }
                        gradient.InterpolationColors = blend;
                        g.FillPath(gradient, fillPath);
                    }
                }
            }
        }
        static GraphicsPath Capsule(RectangleF r)
        {
            var p = new GraphicsPath();
            float d = Math.Min(r.Width, r.Height);
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
        static Rectangle R(float x, float y, float w, float h, float scale)
        {
            return new Rectangle((int)Math.Round(x * scale), (int)Math.Round(y * scale),
                                 (int)Math.Round(w * scale), (int)Math.Round(h * scale));
        }
    }

}
