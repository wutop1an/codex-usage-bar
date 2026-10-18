using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Automation;

namespace CodexUsageBar
{
    // Read-only UIA metadata inspection. Never traverses the full conversation tree.
    // A native child cannot participate in Chromium's DOM z-index, so hide it for dialogs.
    internal sealed class HostOverlayMonitor : IDisposable
    {
        private sealed class Snapshot
        {
            internal string State;
            internal DateTime CheckedUtc;
            internal long QueryMilliseconds;
        }

        private readonly IntPtr host;
        private readonly AutoResetEvent wake = new AutoResetEvent(false);
        private readonly object gate = new object();
        private volatile bool disposed;
        private volatile Snapshot snapshot =
            new Snapshot { State = "checking", CheckedUtc = DateTime.MinValue };
        private int revision;
        private DateTime openedUtc = DateTime.MinValue;
        private volatile string lastError = "";
        public string LastError
        {
            get {
                return lastError;
            }
        }

        public HostOverlayMonitor(IntPtr hostWindow)
        {
            host = hostWindow;
            var worker = new Thread(Watch) { IsBackground = true, Name = "Codex bar visibility" };
            worker.SetApartmentState(ApartmentState.MTA);
            worker.Start();
        }

        public string State
        {
            get {
                return snapshot.State;
            }
        }
        public long QueryMilliseconds
        {
            get {
                return snapshot.QueryMilliseconds;
            }
        }
        public bool AllowsDisplay
        {
            get {
                var current = snapshot;
                return CanDisplay(current.State, current.CheckedUtc, DateTime.UtcNow);
            }
        }

        internal static bool CanDisplay(string state, DateTime checkedUtc, DateTime now)
        {
            return state == "clear" && now >= checkedUtc &&
                   now - checkedUtc < TimeSpan.FromSeconds(AppSettings.OverlayFreshnessSeconds);
        }

        private void Signal()
        {
            try
            {
                wake.Set();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void OnOpened(object sender, AutomationEventArgs args)
        {
            lock (gate)
            {
                revision++;
                openedUtc = DateTime.UtcNow;
                snapshot = new Snapshot { State = "dialog", CheckedUtc = openedUtc };
            }
            Signal();
        }

        private void Watch()
        {
            AutomationElement root = null;
            bool openedAttached = false, closedAttached = false, focusAttached = false;
            AutomationEventHandler opened = OnOpened;
            AutomationEventHandler closed = delegate
            {
                Signal();
            };
            AutomationFocusChangedEventHandler focused = delegate
            {
                Signal();
            };

            try
            {
                while (!disposed && Native.IsWindow(host))
                {
                    try
                    {
                        if (root == null)
                            root = AutomationElement.FromHandle(host);
                        if (root == null)
                            throw new InvalidOperationException("No accessibility root");
                        if (!openedAttached)
                        {
                            TrySubscribe(delegate {
                                Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent, root,
                                                                     TreeScope.Descendants, opened);
                            });
                            openedAttached = true;
                        }
                        if (!closedAttached)
                        {
                            TrySubscribe(delegate {
                                Automation.AddAutomationEventHandler(WindowPattern.WindowClosedEvent, root,
                                                                     TreeScope.Descendants, closed);
                            });
                            closedAttached = true;
                        }
                        if (!focusAttached)
                        {
                            TrySubscribe(
                                delegate { Automation.AddAutomationFocusChangedEventHandler(focused); });
                            focusAttached = true;
                        }

                        int before;
                        lock (gate) before = revision;
                        var stopwatch = Stopwatch.StartNew();
                        string state = InspectFocusedAncestors();
                        lastError = "";
                        stopwatch.Stop();
                        lock (gate)
                        {
                            // An older query must never undo a newly delivered dialog-open event.
                            bool settling = state == "clear" &&
                                            DateTime.UtcNow - openedUtc < TimeSpan.FromMilliseconds(250);
                            if (before == revision && !settling)
                                snapshot = new Snapshot { State = state, CheckedUtc = DateTime.UtcNow,
                                                          QueryMilliseconds = stopwatch.ElapsedMilliseconds };
                        }
                    }
                    catch (Exception exception)
                    {
                        lastError = exception.GetType().Name;
                        lock (gate) snapshot =
                            new Snapshot { State = "unavailable", CheckedUtc = DateTime.UtcNow };
                    }
                    if (!disposed)
                        wake.WaitOne(AppSettings.OverlayPollMs);
                }
            }
            finally
            {
                if (openedAttached)
                    TryRemove(delegate {
                        Automation.RemoveAutomationEventHandler(WindowPattern.WindowOpenedEvent, root,
                                                                opened);
                    });
                if (closedAttached)
                    TryRemove(delegate {
                        Automation.RemoveAutomationEventHandler(WindowPattern.WindowClosedEvent, root,
                                                                closed);
                    });
                if (focusAttached)
                    TryRemove(delegate { Automation.RemoveAutomationFocusChangedEventHandler(focused); });
                wake.Dispose();
            }
        }

        private string InspectFocusedAncestors()
        {
            // Background windows keep their last known state; focus outside the host is not proof of a
            // dialog.
            if (Native.GetForegroundWindow() != host)
                return snapshot.State;
            AutomationElement element = AutomationElement.FocusedElement;
            bool dialog = false;
            for (int depth = 0; element != null && depth < AppSettings.FocusAncestorLimit; depth++)
            {
                var info = element.Current;
                if (new IntPtr(info.NativeWindowHandle) == host)
                    return dialog ? "dialog" : "clear";
                if (!info.IsOffscreen)
                {
                    string role = info.LocalizedControlType;
                    if (info.ControlType == ControlType.Window || role == "dialog" || role == "对话框")
                        dialog = true;
                    // Older Chromium versions expose image dialogs as a named Pane.
                    if (info.ControlType == ControlType.Pane)
                    {
                        string name = info.Name;
                        if (name == "Image preview" || name == "图片预览" || name == "图像预览")
                            dialog = true;
                    }
                }
                element = TreeWalker.ControlViewWalker.GetParent(element);
            }
            return "checking";
        }

        private static void TryRemove(Action remove)
        {
            try
            {
                remove();
            }
            catch
            {
            }
        }

        private static void TrySubscribe(Action subscribe)
        {
            // Unsupported provider events must not disable the polling fallback.
            try
            {
                subscribe();
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            disposed = true;
            Signal(); // Never block the drawing thread waiting for a UIA provider.
        }
    }
}
