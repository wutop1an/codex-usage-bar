using System;
using System.Drawing;

namespace CodexUsageBar
{
    static class Palette
    {
        public static readonly Color Background = Color.FromArgb(32, 32, 32);
        public static readonly Color Secondary = Color.FromArgb(143, 143, 143);
        public static readonly Color Track = Color.FromArgb(65, 65, 65);
        public static double Curve(double fraction)
        {
            double u = Math.Max(0, Math.Min(1, fraction)) - .5;
            return .5 + 4 * u * u * u;
        }
        public static Color At(double fraction)
        {
            double t = Curve(fraction), v;
            Color a, b;
            if (t < .5)
            {
                a = Color.FromArgb(231, 91, 99);
                b = Color.FromArgb(158, 119, 213);
                v = t * 2;
            }
            else
            {
                a = Color.FromArgb(158, 119, 213);
                b = Color.FromArgb(82, 127, 240);
                v = t * 2 - 1;
            }
            return Color.FromArgb((int)Math.Round(a.R + (b.R - a.R) * v),
                                  (int)Math.Round(a.G + (b.G - a.G) * v),
                                  (int)Math.Round(a.B + (b.B - a.B) * v));
        }
    }

    // A real quota change animates once. Polling the same value never restarts it.

}
