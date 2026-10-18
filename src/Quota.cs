using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;

namespace CodexUsageBar
{
    public class UsageWindow
    {
        public double UsedPercent { get; set; }
        public int Minutes { get; set; }
        public long? ResetsAt { get; set; }
        public double Remaining
        {
            get {
                return Math.Max(0, Math.Min(100, 100 - UsedPercent));
            }
        }
        public string Label
        {
            get {
                if (Minutes == 10080)
                    return "周";
                if (Minutes % 1440 == 0)
                    return (Minutes / 1440) + "天";
                if (Minutes % 60 == 0)
                    return (Minutes / 60) + "h";
                return Minutes + "m";
            }
        }
    }
    public class UsageBucket
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<UsageWindow> Windows { get; set; }
    }
    public class Quota
    {
        public List<UsageBucket> Buckets { get; set; }
        public DateTime RetrievedUtc { get; set; }
        public UsageBucket Main
        {
            get {
                return Buckets.FirstOrDefault(b => b.Id == "codex") ?? Buckets.FirstOrDefault();
            }
        }
        static Dictionary<string, object> AsObject(object v)
        {
            return v as Dictionary<string, object>;
        }
        static object GetValue(Dictionary<string, object> d, string key)
        {
            object v;
            return d != null && d.TryGetValue(key, out v) ? v : null;
        }
        static UsageWindow ParseWindow(object raw)
        {
            var d = AsObject(raw);
            var usedRaw = GetValue(d, "usedPercent");
            var minutesRaw = GetValue(d, "windowDurationMins");
            if (usedRaw == null || minutesRaw == null || usedRaw is bool || minutesRaw is bool)
                return null;
            double used;
            int minutes;
            if (!double.TryParse(Convert.ToString(usedRaw, CultureInfo.InvariantCulture), NumberStyles.Float,
                                 CultureInfo.InvariantCulture, out used) ||
                double.IsNaN(used) || double.IsInfinity(used) ||
                !int.TryParse(Convert.ToString(minutesRaw, CultureInfo.InvariantCulture), out minutes) ||
                minutes <= 0)
                return null;
            long reset;
            long? resetsAt = null;
            if (long.TryParse(Convert.ToString(GetValue(d, "resetsAt")), out reset) && reset > 0 &&
                reset < 253402300800L)
                resetsAt = reset;
            return new UsageWindow { UsedPercent = Math.Max(0, Math.Min(100, used)), Minutes = minutes,
                                     ResetsAt = resetsAt };
        }
        public static Quota Parse(string json)
        {
            var root = AsObject(new JavaScriptSerializer().DeserializeObject(json));
            var result = AsObject(GetValue(root, "result")) ?? root;
            var map = AsObject(GetValue(result, "rateLimitsByLimitId"));
            var buckets = new List<UsageBucket>();
            if (map == null || map.Count == 0)
            {
                map = new Dictionary<string, object>();
                var legacy = AsObject(GetValue(result, "rateLimits"));
                if (legacy != null)
                    map[Convert.ToString(GetValue(legacy, "limitId")) == ""
                            ? "codex"
                            : Convert.ToString(GetValue(legacy, "limitId"))] = legacy;
            }
            foreach (var pair in map)
            {
                var d = AsObject(pair.Value);
                if (d == null)
                    continue;
                var windows = new List<UsageWindow>();
                foreach (var key in new[] { "primary", "secondary" })
                {
                    var window = ParseWindow(GetValue(d, key));
                    if (window != null)
                        windows.Add(window);
                }
                if (windows.Count > 0)
                    buckets.Add(new UsageBucket { Id = pair.Key,
                                                  Name = Convert.ToString(GetValue(d, "limitName")),
                                                  Windows = windows.OrderBy(w => w.Minutes).ToList() });
            }
            return new Quota { Buckets = buckets, RetrievedUtc = DateTime.UtcNow };
        }
    }

}
