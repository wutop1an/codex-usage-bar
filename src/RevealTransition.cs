using System;

namespace CodexUsageBar
{
    internal sealed class RevealTransition
    {
        private long started;
        private bool animate;

        public void Start(long now, bool enabled)
        {
            started = now;
            animate = enabled;
        }

        public bool Active(long now)
        {
            return animate && now - started < AppSettings.FadeInMs;
        }

        public byte Alpha(long now)
        {
            double fraction =
                animate ? Math.Max(0, Math.Min(1, (now - started) / (double)AppSettings.FadeInMs)) : 1;
            return (byte)Math.Round(255 * (1 - Math.Pow(1 - fraction, 3)));
        }
    }
}
