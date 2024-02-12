namespace FlomtManager.Framework.Extensions
{
    public static class FloatExtensions
    {
        public static float TrimDecimalPlaces(this float value, int numberOfDigits)
        {
            if (numberOfDigits <= 0)
            {
                return value;
            }
            var divisor = float.Pow(10, numberOfDigits);

            // ensure that we trim only decimal places
            return float.Max(float.Truncate(value), MathF.Floor(value * divisor) / divisor);
        }
    }
}
