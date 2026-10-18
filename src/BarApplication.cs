using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodexUsageBar
{
    sealed class BarApplication : ApplicationContext
    {
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        readonly EventWaitHandle stop;
        readonly Dictionary<IntPtr, UsageBar> bars = new Dictionary<IntPtr, UsageBar>();
        readonly ContextMenuStrip menu = new ContextMenuStrip();
        readonly NotifyIcon tray = new NotifyIcon();
        Quota quota;
        bool busy, failed, closing;
        string error = "";
        DateTime nextScan = DateTime.MinValue, nextFetch = DateTime.MinValue, nextStatus = DateTime.MinValue,
                 lastFetch = DateTime.MinValue;
        public BarApplication(EventWaitHandle stopEvent)
        {
            stop = stopEvent;
            menu.Items.Add("刷新用量", null, async delegate { await Fetch(); });
            menu.Items.Add("退出用量条", null, delegate { ExitThread(); });
            tray.Icon = SystemIcons.Information;
            tray.Text = "Codex 内嵌用量条";
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
            timer.Interval = AppSettings.LayoutIntervalMs;
            timer.Tick += async delegate
            {
                if (closing)
                    return;
                if (stop.WaitOne(0))
                {
                    ExitThread();
                    return;
                }
                if (DateTime.UtcNow >= nextScan)
                    Scan();
                LayoutBars();
                if (bars.Count > 0 && DateTime.UtcNow >= nextFetch && !busy)
                    await Fetch();
                WriteStatus();
            };
            timer.Start();
        }
        bool IsStale
        {
            get {
                return failed || (quota != null && DateTime.UtcNow - quota.RetrievedUtc >
                                                       TimeSpan.FromSeconds(AppSettings.StaleSeconds));
            }
        }
        void Scan()
        {
            nextScan = DateTime.UtcNow.AddSeconds(AppSettings.HostScanSeconds);
            var ids = new HashSet<uint>();
            foreach (var p in Process.GetProcessesByName("ChatGPT"))
                using (p)
                {
                    try
                    {
                        if (p.MainModule.FileName.IndexOf(@"\OpenAI.Codex_",
                                                          StringComparison.OrdinalIgnoreCase) >= 0)
                            ids.Add((uint)p.Id);
                    }
                    catch
                    {
                    }
                }
            var hosts = new HashSet<IntPtr>();
            Native.EnumWindows(delegate(IntPtr h, IntPtr l) {
                uint pid;
                Native.GetWindowThreadProcessId(h, out pid);
                if (ids.Contains(pid) && Native.IsWindowVisible(h) &&
                    Native.GetWindow(h, Native.OwnerWindow) == IntPtr.Zero)
                {
                    Native.Rect r;
                    Native.GetWindowRect(h, out r);
                    if (Native.IsIconic(h) || (r.Right - r.Left > 500 && r.Bottom - r.Top > 300))
                        hosts.Add(h);
                }
                return true;
            }, IntPtr.Zero);
            foreach (var key in bars.Keys.ToList())
            {
                if (!hosts.Contains(key) || bars[key].IsDisposed || !Native.IsWindow(bars[key].Handle))
                {
                    bars[key].Dispose();
                    bars.Remove(key);
                }
            }
            foreach (var host in hosts)
            {
                if (bars.ContainsKey(host))
                    continue;
                UsageBar bar = null;
                try
                {
                    bar = new UsageBar(host);
                    bar.ContextMenuStrip = menu;
                    bar.MouseClick += async delegate(object sender, MouseEventArgs e)
                    {
                        if (e.Button == MouseButtons.Left)
                            await Fetch();
                    };
                    bar.Mount();
                    bar.OverlayMonitor = new HostOverlayMonitor(host);
                    bars.Add(host, bar);
                    error = "";
                }
                catch (Exception ex)
                {
                    if (bar != null)
                        bar.Dispose();
                    error = "embed: " + ex.GetType().Name;
                }
            }
        }
        void LayoutBars()
        {
            foreach (var pair in bars)
            {
                IntPtr host = pair.Key;
                UsageBar b = pair.Value;
                if (!Native.IsWindow(host) || b.IsDisposed)
                    continue;
                if (Native.IsIconic(host))
                    continue; // Native parent clipping handles minimize/occlusion.
                Native.Rect r;
                if (!Native.GetClientRect(host, out r))
                    continue;
                uint dpi = Native.GetDpiForWindow(host);
                float s = Math.Max(1, dpi) / 96f;
                int w = (int)Math.Round(Presentation.Width(quota) * s),
                    h = (int)Math.Round(Presentation.Height * s);
                int x = r.Right - (int)Math.Round(Presentation.Right * s) - w;
                int y = (int)Math.Round(Presentation.Top * s);
                bool show = x > 360 * s && r.Bottom > 200 * s && b.OverlayMonitor != null &&
                            b.OverlayMonitor.AllowsDisplay;
                b.SetState(quota, busy, IsStale);
                bool resize = b.Width != w || b.Height != h || b.ScaleFactor != s;
                b.ScaleFactor = s;
                if (b.Location != new Point(x, y) || resize)
                    b.Bounds = new Rectangle(x, y, w, h);
                bool newlyVisible = show && !b.Visible;
                b.SetDisplayVisible(show);
                // HWND_TOP here affects sibling controls only; this is never a topmost desktop window.
                if (newlyVisible)
                    Native.SetWindowPos(b.Handle, IntPtr.Zero, 0, 0, 0, 0,
                                        Native.NoActivatePosition | Native.NoResizePosition |
                                            Native.NoMovePosition);
                if (resize || b.PaintCount == 0)
                {
                    b.Invalidate();
                    b.Update();
                }
            }
        }
        async Task Fetch()
        {
            if (busy || closing ||
                (DateTime.UtcNow - lastFetch).TotalSeconds < AppSettings.RefreshDebounceSeconds)
                return;
            lastFetch = DateTime.UtcNow;
            nextFetch = DateTime.UtcNow.AddSeconds(AppSettings.RefreshSeconds);
            busy = true;
            Redraw();
            try
            {
                quota = await QuotaClient.Fetch();
                failed = quota.Main == null;
                error = failed ? "no quota windows" : "";
            }
            catch (Exception ex)
            {
                failed = true;
                error = "usage: " + ex.GetType().Name;
            }
            finally
            {
                busy = false;
                if (!closing)
                {
                    LayoutBars();
                    Redraw();
                    nextStatus = DateTime.MinValue;
                    WriteStatus();
                }
            }
        }
        void Redraw()
        {
            foreach (var b in bars.Values)
            {
                b.SetState(quota, busy, IsStale);
                b.Invalidate();
            }
        }
        void WriteStatus()
        {
            if (closing || DateTime.UtcNow < nextStatus)
                return;
            nextStatus = DateTime.UtcNow.AddSeconds(AppSettings.StatusIntervalSeconds);
            try
            {
                var windows =
                    bars.Values.Where(b => !b.IsDisposed)
                        .Select(b => new {
                            host = b.Host.ToInt64(), handle = b.Handle.ToInt64(),
                            parent = Native.GetParent(b.Handle).ToInt64(),
                            isChild = (Native.GetWindowLongPtr(b.Handle, Native.WindowStyleIndex).ToInt64() &
                                       Native.ChildStyle) != 0,
                            isTopmost =
                                (Native.GetWindowLongPtr(b.Handle, Native.ExtendedStyleIndex).ToInt64() &
                                 Native.TopmostStyle) != 0,
                            visible = Native.IsWindowVisible(b.Handle),
                            dpi = Native.GetDpiForWindow(b.Handle), paintCount = b.PaintCount,
                            overlayState = b.OverlayMonitor == null ? "not-started" : b.OverlayMonitor.State,
                            overlayQueryMs =
                                b.OverlayMonitor == null ? 0 : b.OverlayMonitor.QueryMilliseconds,
                            overlayError = b.OverlayMonitor == null ? "" : b.OverlayMonitor.LastError,
                            childStyle = Native.GetWindowLongPtr(b.Handle, Native.WindowStyleIndex).ToInt64(),
                            hostExStyle =
                                Native.GetWindowLongPtr(b.Host, Native.ExtendedStyleIndex).ToInt64(),
                            x = b.Left, y = b.Top, width = b.Width, height = b.Height
                        })
                        .ToArray();
                var state = new {
                    version = AppSettings.Version,
                    mode = "native-child",
                    appearance = "red-left-blue-right",
                    font = Presentation.FontFamilyName,
                    fontSize = Presentation.FontSize,
                    lineHeight = Presentation.LineHeight,
                    textAlignment = "visible-ink-center",
                    labelColor = "#8F8F8F",
                    labelBarGap = Presentation.TrackX - Presentation.LabelWidth,
                    pid = Process.GetCurrentProcess().Id,
                    windows,
                    busy,
                    stale = IsStale,
                    error,
                    updatedUtc = DateTime.UtcNow,
                    windowsQuota =
                        quota == null || quota.Main == null
                            ? null
                            : quota.Main.Windows
                                  .Select(w => new { used = w.UsedPercent, remaining = w.Remaining,
                                                     reset = Presentation.ResetText(w), minutes = w.Minutes })
                                  .ToArray()
                };
                File.WriteAllText(Path.Combine(Program.DataDir, "status.json"), Program.Json.Serialize(state),
                                  Encoding.UTF8);
                if (quota != null && quota.Main != null)
                {
                    string t =
                        string.Join(" / ", quota.Main.Windows.Select(
                                               w => w.Label + "已用 " + w.UsedPercent.ToString("0.#") + "%"));
                    tray.Text = "Codex · " + (t.Length > 50 ? t.Substring(0, 50) : t);
                }
            }
            catch
            {
            }
        }
        protected override void ExitThreadCore()
        {
            closing = true;
            timer.Stop();
            QuotaClient.Stop();
            foreach (var b in bars.Values)
                b.Dispose();
            bars.Clear();
            tray.Visible = false;
            base.ExitThreadCore();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                menu.Dispose();
                tray.Dispose();
            }
            base.Dispose(disposing);
        }
    }

}
