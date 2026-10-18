using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace CodexUsageBar
{
    static class Tests
    {
        static int count;
        static void Check(bool ok, string message)
        {
            if (!ok)
                throw new Exception(message);
            count++;
        }
        static double InkCenter(Bitmap image, int left, int right)
        {
            int top = image.Height, bottom = -1;
            for (int y = 0; y < image.Height; y++)
                for (int x = left; x < right; x++)
                {
                    var pixel = image.GetPixel(x, y);
                    if (Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) > 45)
                    {
                        top = Math.Min(top, y);
                        bottom = Math.Max(bottom, y);
                    }
                }
            Check(bottom >= top, "visible ink in tested region");
            return (top + bottom) / 2.0;
        }
        static Quota Sample(double percent)
        {
            return new Quota { RetrievedUtc = DateTime.UtcNow,
                               Buckets = new List<UsageBucket> { new UsageBucket {
                                   Id = "codex",
                                   Windows = new List<UsageWindow> { new UsageWindow {
                                       UsedPercent = percent, Minutes = 10080, ResetsAt = 1792904478
                                   } }
                               } } };
        }
        public static int Run()
        {
            try
            {
                var q = Quota.Parse(
                    "{\"result\":{\"rateLimitsByLimitId\":{\"codex\":{\"primary\":{\"usedPercent\":" +
                    "2,\"windowDurationMins\":10080,\"resetsAt\":1792904478},\"secondary\":null}}}}");
                Check(q.Main.Windows.Count == 1 && q.Main.Windows[0].Remaining == 98 &&
                          q.Main.Windows[0].Label == "周",
                      "weekly primary only");
                Check(Quota.Parse("{\"rateLimits\":null}").Main == null, "missing is not full");
                var reveal = new RevealTransition();
                reveal.Start(100, true);
                Check(reveal.Alpha(100) == 0 && reveal.Active(100), "reveal starts transparent");
                Check(reveal.Alpha(190) > 0 && reveal.Alpha(190) < 255, "reveal has an intermediate alpha");
                Check(reveal.Alpha(280) == 255 && !reveal.Active(280), "reveal finishes in 180 ms");
                reveal.Start(300, false);
                Check(reveal.Alpha(300) == 255 && !reveal.Active(300), "reduced motion skips reveal");
                var now = DateTime.UtcNow;
                Check(HostOverlayMonitor.CanDisplay("clear", now, now), "show on current clear app surface");
                Check(!HostOverlayMonitor.CanDisplay("dialog", now, now),
                      "hide for app image preview or dialog");
                Check(!HostOverlayMonitor.CanDisplay("checking", now, now) &&
                          !HostOverlayMonitor.CanDisplay("unavailable", now, now),
                      "unknown overlay state never draws above app");
                Check(!HostOverlayMonitor.CanDisplay("clear", now.AddSeconds(-4), now),
                      "expired clear state cannot leave the bar above an overlay");
                Check(Quota.Parse("{\"rateLimits\":{\"primary\":{\"windowDurationMins\":300}}}").Main == null,
                      "missing percentage");
                var dual = Quota.Parse(
                    "{\"rateLimits\":{\"primary\":{\"usedPercent\":55.5,\"windowDurationMins\":300}" +
                    ",\"secondary\":{\"usedPercent\":110,\"windowDurationMins\":10080}}}");
                Check(dual.Main.Windows.Count == 2 && dual.Main.Windows[0].Remaining == 44.5 &&
                          dual.Main.Windows[1].Remaining == 0,
                      "dual and clamp");
                Check(Palette.Curve(0) == 0 && Palette.Curve(1) == 1 && Palette.Curve(.5) == .5,
                      "curve anchors");
                double previous = -1;
                for (int i = 0; i <= 100; i++)
                {
                    var t = Palette.Curve(i / 100.0);
                    Check(t >= previous, "monotonic");
                    previous = t;
                }
                Check(Palette.Curve(.1) - Palette.Curve(0) > 15 * (Palette.Curve(.5) - Palette.Curve(.4)),
                      "fast ends slow middle");
                Check(Math.Abs((1 - Palette.Curve(.9)) - Palette.Curve(.1)) < 1e-9, "symmetric ends");
                Check(Palette.At(0).R > Palette.At(0).B && Palette.At(1).B > Palette.At(1).R,
                      "red left and blue right");
                Check(Presentation.ResetText(q.Main.Windows[0]).Length == 11 &&
                          !Presentation.ResetText(q.Main.Windows[0]).Contains("重置"),
                      "date and time visible without redundant label");
                Check(Presentation.ResetText(new UsageWindow()).Contains("暂不可用"),
                      "missing reset not invented");
                Check(Presentation.FillEndX(0) > Presentation.FillEndX(50) &&
                          Presentation.FillEndX(50) > Presentation.FillEndX(100),
                      "consumption moves right to left");
                Check(Presentation.RemainingFraction(0) == 1 && Presentation.RemainingFraction(100) == 0,
                      "remaining length endpoints");
                var motion = new UsageTransition();
                motion.SetTarget(5, 0, true);
                Check(motion.Value(0) == 5 && !motion.Active(0), "first fetch has no artificial animation");
                motion.SetTarget(80, 100, true);
                double mid = motion.Value(300);
                Check(mid > 5 && mid < 80 && Presentation.FillEndX(mid) < Presentation.FillEndX(5),
                      "animation drains left");
                motion.SetTarget(80, 300, true);
                Check(motion.Value(520) == 80 && !motion.Active(520),
                      "identical polling does not restart motion");
                motion.SetTarget(0, 600, true);
                Check(motion.Value(1020) == 0, "reset restores the full track");
                motion.SetTarget(50, 1100, false);
                Check(motion.Value(1100) == 50 && !motion.Active(1100), "reduced motion is immediate");
                using (var bitmap = new Bitmap(Presentation.CellWidth, Presentation.Height)) using (
                    var g = Graphics.FromImage(bitmap))
                {
                    Presentation.Draw(g, 1, Sample(50), false, false);
                    var remaining = bitmap.GetPixel(120, 16);
                    var consumed = bitmap.GetPixel(180, 16);
                    Check(remaining.R > remaining.G + 20, "remaining left section stays red");
                    Check(consumed.R == consumed.G && consumed.G == consumed.B,
                          "consumed section loses its color");
                    Presentation.Draw(g, 1, Sample(100), false, false);
                    var empty = bitmap.GetPixel(150, 16);
                    Check(empty.R == empty.G && empty.G == empty.B, "empty quota has no colored trail");
                    bool grayText = true;
                    for (int x = 0; x < 88; x++)
                        for (int y = 0; y < Presentation.Height; y++)
                        {
                            var pixel = bitmap.GetPixel(x, y);
                            if (pixel.R != pixel.G || pixel.G != pixel.B)
                                grayText = false;
                        }
                    Check(grayText, "menu text has grayscale edges without LCD fringes");
                    using (var font = Presentation.MenuFont(1))
                    {
                        Check(g.MeasureString("周限额 100%", font, 1000, StringFormat.GenericTypographic)
                                      .Width <= Presentation.LabelWidth,
                              "largest weekly value fits");
                        Check(g.MeasureString(Presentation.ResetText(Sample(0).Main.Windows[0]), font, 1000,
                                              StringFormat.GenericTypographic)
                                      .Width <= Presentation.ResetWidth,
                              "date and time fit");
                    }
                }
                foreach (float scale in new[] { 1f, 1.25f, 1.5f, 2f })
                {
                    using (var bitmap = new Bitmap(
                               (int)(Presentation.CellWidth * scale),
                               (int)(Presentation.Height * scale))) using (var g = Graphics.FromImage(bitmap))
                    {
                        Presentation.Draw(g, scale, Sample(0), false, false);
                        double labelCenter = InkCenter(bitmap, 0, (int)(Presentation.LabelWidth * scale));
                        double iconCenter = InkCenter(bitmap, (int)(232 * scale), (int)(248 * scale));
                        double dateCenter =
                            InkCenter(bitmap, (int)(Presentation.ResetX * scale), bitmap.Width);
                        Check(Math.Abs(labelCenter - iconCenter) <= 1 &&
                                  Math.Abs(dateCenter - iconCenter) <= 1,
                              "glyph and icon alignment at DPI scale " + scale);
                        var left = bitmap.GetPixel((int)(115 * scale), (int)(16 * scale));
                        var right = bitmap.GetPixel((int)(215 * scale), (int)(16 * scale));
                        Check(left.R > left.B && right.B > right.R,
                              "rendered gradient red left blue right at DPI scale " + scale);
                    }
                }
                using (var form = new Form())
                {
                    form.Size = new Size(1400, 500);
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(-10000, -10000);
                    using (var child = new UsageBar(form.Handle))
                    {
                        child.Mount();
                        Check(Native.GetParent(child.Handle) == form.Handle, "native parent matches");
                        Check((Native.GetWindowLongPtr(child.Handle, -16).ToInt64() & 0x40000000L) != 0,
                              "WS_CHILD flag");
                        Check((Native.GetWindowLongPtr(child.Handle, -20).ToInt64() & 8) == 0,
                              "no TOPMOST flag");
                    }
                    Check(Native.IsWindow(form.Handle), "disposing child preserves host");
                }
                File.WriteAllText(Path.Combine(Program.DataDir, "self-test.txt"),
                                  "PASS: " + count +
                                      " quota, depletion, motion, layout and native embedding assertions",
                                  Encoding.UTF8);
                return 0;
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(Program.DataDir, "self-test.txt"), "FAIL: " + ex,
                                  Encoding.UTF8);
                return 1;
            }
        }
        public static int RenderSamples()
        {
            // Offscreen rendering of our own control only, not a desktop screenshot.
            using (var bitmap = new Bitmap(595, 490)) using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Palette.Background);
                var values = new[] { 0.0, 5.0, 50.0, 90.0, 100.0 };
                using (var font = Presentation.MenuFont(1.5f))
                {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    using (var brush = new SolidBrush(Palette.Secondary))
                        g.DrawString("样式预览（示例额度）", font, brush, 24, 16);
                    for (int i = 0; i < values.Length; i++)
                    {
                        int x = 24, y = 65 + i * 65;
                        using (var sample = new Bitmap((int)(Presentation.CellWidth * 1.5f), 48)) using (
                            var sg = Graphics.FromImage(sample))
                        {
                            Presentation.Draw(sg, 1.5f, Sample(values[i]), false, false);
                            g.DrawImageUnscaled(sample, x, y);
                        }
                    }
                    using (var sample = new Bitmap((int)(Presentation.CellWidth * 1.5f), 48)) using (
                        var sg = Graphics.FromImage(sample))
                    {
                        Presentation.Draw(sg, 1.5f, null, false, false);
                        g.DrawImageUnscaled(sample, 24, 390);
                    }
                }
                bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appearance-preview.png"));
            }
            return 0;
        }
    }

}
