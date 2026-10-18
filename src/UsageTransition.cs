using System;

namespace CodexUsageBar
{
    sealed class UsageTransition
    {
        double from, target;
        long started;
        bool initialized;
        public const int DurationMs = AppSettings.QuotaAnimationMs;
        public double Value(long now)
        {
            if (!initialized)
                return 0;
            double t = Math.Max(0, Math.Min(1, (now - started) / (double)DurationMs));
            return from + (target - from) * (1 - Math.Pow(1 - t, 3));
        }
        public bool Active(long now)
        {
            return initialized && from != target && now - started < DurationMs;
        }
        public void SetTarget(double used, long now, bool animate)
        {
            used = Math.Max(0, Math.Min(100, used));
            if (initialized && used == target)
                return;
            from = initialized && animate ? Value(now) : used;
            target = used;
            started = now;
            initialized = true;
        }
    }

}
