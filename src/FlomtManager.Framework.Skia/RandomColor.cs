using SkiaSharp;

namespace FlomtManager.Framework.Skia
{
    public static class RandomColor
    {
        private const int SAME_HUE_RANGE = 5;

        public static SKColor Next(IEnumerable<float>? hues = null)
        {
            var h = Random.Shared.Next(0, 360);
            if (hues != null)
            {
                if (hues.Any(x => Math.Abs(h - x) < SAME_HUE_RANGE))
                {
                    h = Random.Shared.Next(0, 360);
                }
            }
            var s = Random.Shared.Next(42, 70);
            var l = Random.Shared.Next(40, 60);
            return SKColor.FromHsl(h, s, l);
        }
    }
}
