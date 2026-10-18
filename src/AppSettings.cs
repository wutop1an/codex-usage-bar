namespace CodexUsageBar
{
    // Compile-time defaults. There is intentionally no user configuration UI yet.
    internal static class AppSettings
    {
        public const string Version = "2.2.0";
        public const int LayoutIntervalMs = 100;
        public const int HostScanSeconds = 3;
        public const int RefreshSeconds = 120;
        public const int RefreshDebounceSeconds = 10;
        public const int StaleSeconds = 300;
        public const int StatusIntervalSeconds = 5;
        public const int RequestTimeoutMs = 25000;
        public const int OverlayPollMs = 350;
        public const int OverlayFreshnessSeconds = 3;
        public const int FocusAncestorLimit = 40;
        public const int FadeInMs = 180;
        public const int FrameIntervalMs = 16;
        public const int QuotaAnimationMs = 420;
    }
}
