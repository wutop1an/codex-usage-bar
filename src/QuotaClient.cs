using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CodexUsageBar
{
    static class QuotaClient
    {
        static Process child;
        static readonly object Gate = new object();
        public static void Stop()
        {
            lock (Gate)
            {
                try
                {
                    if (child != null && !child.HasExited)
                        child.Kill();
                }
                catch
                {
                }
            }
        }
        static string FindCodex()
        {
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                    @"OpenAI\Codex\bin");
            if (Directory.Exists(root))
            {
                var files = Directory.GetDirectories(root)
                                .Select(p => Path.Combine(p, "codex.exe"))
                                .Where(File.Exists)
                                .OrderByDescending(File.GetLastWriteTimeUtc);
                var exe = files.FirstOrDefault();
                if (exe != null)
                    return exe;
            }
            throw new InvalidOperationException("Codex CLI not found");
        }
        public static async Task<Quota> Fetch()
        {
            using (var p = new Process())
            {
                p.StartInfo = new ProcessStartInfo(
                    FindCodex(), "app-server --stdio") { UseShellExecute = false,
                                                         CreateNoWindow = true,
                                                         RedirectStandardInput = true,
                                                         RedirectStandardOutput = true,
                                                         RedirectStandardError = true,
                                                         StandardOutputEncoding = new UTF8Encoding(false),
                                                         StandardErrorEncoding = new UTF8Encoding(false),
                                                         WorkingDirectory = Program.DataDir };
                // Drain stderr; never persist server logs, tokens, or account metadata.
                p.ErrorDataReceived += delegate {};
                p.Start();
                lock (Gate) child = p;
                p.BeginErrorReadLine();
                try
                {
                    var conversation = Exchange(p);
                    if (await Task.WhenAny(conversation, Task.Delay(AppSettings.RequestTimeoutMs))
                            .ConfigureAwait(false) != conversation)
                    {
                        try
                        {
                            p.Kill();
                        }
                        catch
                        {
                        }
                        try
                        {
                            await conversation.ConfigureAwait(false);
                        }
                        catch
                        {
                        }
                        throw new TimeoutException();
                    }
                    return await conversation.ConfigureAwait(false);
                }
                finally
                {
                    lock (Gate)
                    {
                        if (child == p)
                            child = null;
                    }
                    try
                    {
                        if (!p.HasExited)
                            p.Kill();
                    }
                    catch
                    {
                    }
                }
            }
        }
        static async Task<string> Reply(Process p, int id)
        {
            while (true)
            {
                var line = await p.StandardOutput.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                    throw new IOException("App server closed");
                Dictionary<string, object> data;
                try
                {
                    data = new JavaScriptSerializer().DeserializeObject(line) as Dictionary<string, object>;
                }
                catch
                {
                    continue;
                }
                object responseId;
                if (data == null || !data.TryGetValue("id", out responseId) ||
                    Convert.ToString(responseId) != id.ToString())
                    continue;
                if (data.ContainsKey("error"))
                    throw new InvalidOperationException("Account usage API unavailable");
                return line;
            }
        }
        static async Task<Quota> Exchange(Process p)
        {
            var request = new Dictionary<string, object> {
                { "id", 0 },
                { "method", "initialize" },
                { "params", new { clientInfo = new { name = "codex_usage_bar", title = "Codex Usage Bar",
                                                     version = AppSettings.Version } } }
            };
            p.StandardInput.WriteLine(new JavaScriptSerializer().Serialize(request));
            p.StandardInput.Flush();
            await Reply(p, 0).ConfigureAwait(false);
            p.StandardInput.WriteLine("{\"method\":\"initialized\",\"params\":{}}");
            p.StandardInput.WriteLine("{\"id\":1,\"method\":\"account/rateLimits/read\"}");
            p.StandardInput.Flush();
            return Quota.Parse(await Reply(p, 1).ConfigureAwait(false));
        }
    }
}
