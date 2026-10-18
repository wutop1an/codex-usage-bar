using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CodexUsageBar
{
    sealed class UsageBar : Control
    {
        public IntPtr Host { get; private set; }
        public float ScaleFactor = 1;
        public Quota Quota;
        public bool Busy, Stale;
        public HostOverlayMonitor OverlayMonitor;
        public long PaintCount { get; private set; }
        readonly List<UsageTransition> transitions = new List<UsageTransition>();
        readonly List<int> durations = new List<int>();
        readonly Stopwatch animationClock = Stopwatch.StartNew();
        readonly RevealTransition reveal = new RevealTransition();
        readonly System.Windows.Forms.Timer frames = new System.Windows.Forms.Timer();
        readonly ToolTip tooltip = new ToolTip { InitialDelay = 450, ReshowDelay = 100 };
        string tip = "";
        public UsageBar(IntPtr host)
        {
            Host = host;
            Name = "CodexUsageBar";
            AccessibleName = "Codex 使用额度与重置时间";
            TabStop = false;
            BackColor = Palette.Background;
            Visible = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable,
                     true);
            SetStyle(ControlStyles.Selectable, false);
            frames.Interval = AppSettings.FrameIntervalMs;
            frames.Tick += delegate
            {
                long now = animationClock.ElapsedMilliseconds;
                Native.SetLayeredWindowAttributes(Handle, 0, reveal.Alpha(now), Native.AlphaAttribute);
                bool quotaMoving = transitions.Any(t => t.Active(now));
                if (Visible)
                    Invalidate();
                if (!quotaMoving && !reveal.Active(now))
                    frames.Stop();
            };
        }
        public void SetDisplayVisible(bool show)
        {
            if (show == Visible)
                return;
            if (!show)
            {
                Visible = false;
                frames.Stop();
                return;
            }
            reveal.Start(animationClock.ElapsedMilliseconds, Native.AnimationsEnabled);
            Native.SetLayeredWindowAttributes(Handle, 0, reveal.Alpha(animationClock.ElapsedMilliseconds),
                                              Native.AlphaAttribute);
            Visible = true;
            Invalidate();
            frames.Start();
        }
        public void SetState(Quota quota, bool busy, bool stale)
        {
            bool changed = Quota != quota || Busy != busy || Stale != stale;
            Quota = quota;
            Busy = busy;
            Stale = stale;
            if (quota == null || quota.Main == null)
            {
                transitions.Clear();
                durations.Clear();
            }
            else
            {
                var windows = quota.Main.Windows;
                if (transitions.Count != windows.Count ||
                    !durations.SequenceEqual(windows.Select(w => w.Minutes)))
                {
                    transitions.Clear();
                    durations.Clear();
                    foreach (var w in windows)
                    {
                        transitions.Add(new UsageTransition());
                        durations.Add(w.Minutes);
                    }
                }
                for (int i = 0; i < windows.Count; i++)
                    transitions[i].SetTarget(windows[i].UsedPercent, animationClock.ElapsedMilliseconds,
                                             Native.AnimationsEnabled);
                if (transitions.Any(t => t.Active(animationClock.ElapsedMilliseconds)))
                    frames.Start();
                string nextTip = string.Join(
                    "\n",
                    windows.Select(w => w.Label + "额度：已用 " + w.UsedPercent.ToString("0.#") + "%，剩余 " +
                                        w.Remaining.ToString("0.#") + "%；" + Presentation.ResetText(w)));
                if (stale)
                    nextTip = "上次读取的数据（本次刷新失败或已过期）\n" + nextTip;
                nextTip += "\n点击刷新；右键退出。";
                if (nextTip != tip)
                {
                    tooltip.SetToolTip(this, nextTip);
                    tip = nextTip;
                }
            }
            if (changed)
                Invalidate();
        }
        protected override CreateParams CreateParams
        {
            get {
                var cp = base.CreateParams;
                cp.Parent = Host;
                cp.Style = unchecked((int)(((uint)cp.Style & ~0x80000000u) | 0x40000000u | 0x04000000u));
                cp.ExStyle = (cp.ExStyle & ~0x00040008) | 0x08080000; // layered child, no taskbar/topmost
                return cp;
            }
        }
        public void Mount()
        {
            // Match the host's per-monitor DPI context before creating the native child.
            IntPtr prior = IntPtr.Zero;
            try
            {
                prior = Native.SetThreadDpiAwarenessContext(Native.GetWindowDpiAwarenessContext(Host));
            }
            catch
            {
            }
            try
            {
                CreateControl();
            }
            finally
            {
                if (prior != IntPtr.Zero)
                    Native.SetThreadDpiAwarenessContext(prior);
            }
            if (Native.GetParent(Handle) != Host ||
                (Native.GetWindowLongPtr(Handle, Native.WindowStyleIndex).ToInt64() & Native.ChildStyle) == 0)
                throw new InvalidOperationException("Native child attachment failed");
            if (!Native.SetLayeredWindowAttributes(Handle, 0, 255, Native.AlphaAttribute))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x21)
            {
                m.Result = new IntPtr(3);
                return;
            }
            base.WndProc(ref m);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Presentation.Draw(e.Graphics, ScaleFactor, Quota, Busy, Stale,
                              transitions.Select(t => t.Value(animationClock.ElapsedMilliseconds)).ToArray());
            PaintCount++;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (OverlayMonitor != null)
                    OverlayMonitor.Dispose();
                frames.Stop();
                frames.Dispose();
                tooltip.Dispose();
            }
            base.Dispose(disposing);
        }
    }

}
