using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace CodexUsageBar
{
    static class Program
    {
        internal static readonly string DataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexUsageBar");
        internal static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
        const string MutexName = @"Local\CodexUsageBar.2";
        internal const string StopName = @"Local\CodexUsageBar.Stop.2";
        [STAThread]
        static int Main(string[] args)
        {
            Directory.CreateDirectory(DataDir);
            if (args.Contains("--stop"))
            {
                try
                {
                    using (var stop = EventWaitHandle.OpenExisting(StopName)) stop.Set();
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                }
                try
                {
                    using (var mutex = Mutex.OpenExisting(MutexName))
                    {
                        bool acquired = false;
                        try
                        {
                            acquired = mutex.WaitOne(5000);
                        }
                        catch (AbandonedMutexException)
                        {
                            acquired = true;
                        }
                        if (!acquired)
                            return 1;
                        mutex.ReleaseMutex();
                    }
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                }
                return 0;
            }
            try
            {
                Native.SetProcessDpiAwarenessContext(new IntPtr(-4));
            }
            catch
            {
                Native.SetProcessDPIAware();
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#if TEST_BUILD
            if (args.Contains("--self-test"))
                return Tests.Run();
            if (args.Contains("--render-samples"))
                return Tests.RenderSamples();
#endif
            bool created;
            using (var mutex = new Mutex(true, MutexName, out created))
            {
                if (!created)
                    return 0;
                using (var stop = new EventWaitHandle(false, EventResetMode.ManualReset, StopName)) using (
                    var app = new BarApplication(stop)) Application.Run(app);
                mutex.ReleaseMutex();
            }
            return 0;
        }
    }
}
