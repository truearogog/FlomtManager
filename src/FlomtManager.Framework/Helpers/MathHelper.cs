using System.Numerics;

namespace FlomtManager.Framework.Helpers;

public static class MathHelper
{
    public static float Integrate(ReadOnlySpan<float> values, ReadOnlySpan<float> workingTimeSpan) => Integrate(values, workingTimeSpan, 3600f);
    public static uint Integrate(ReadOnlySpan<uint> values, ReadOnlySpan<uint> workingTimeSpan) => Integrate(values, workingTimeSpan, (uint)3600);
    public static ushort Integrate(ReadOnlySpan<ushort> values, ReadOnlySpan<ushort> workingTimeSpan) => Integrate(values, workingTimeSpan, (ushort)3600);

    private static T Integrate<T>(ReadOnlySpan<T> valueSpan, ReadOnlySpan<T> workingTimeSpan, T secondsInHour) where T : INumber<T>
    {
        if (Vector.IsHardwareAccelerated)
        {
            return IntegrateSIMD(valueSpan, workingTimeSpan, secondsInHour);
        }
        return IntegrateScalar(valueSpan, workingTimeSpan, secondsInHour);
    }

    private static T IntegrateScalar<T>(ReadOnlySpan<T> valueSpan, ReadOnlySpan<T> workingTimeSpan, T secondsInHour) where T : INumber<T>
    {
        if (valueSpan.Length != workingTimeSpan.Length)
        {
            throw new InvalidOperationException("Collections must have the same length.");
        }

        T sum = default;
        for (var i = 0; i < valueSpan.Length; ++i)
        {
            sum += valueSpan[i] * workingTimeSpan[i] / secondsInHour;
        }
        return sum;
    }

    private static T IntegrateSIMD<T>(ReadOnlySpan<T> valueSpan, ReadOnlySpan<T> workingTimeSpan, T secondsInHour) where T : INumber<T>
    {
        if (valueSpan.Length != workingTimeSpan.Length)
        {
            throw new InvalidOperationException("Collections must have the same length.");
        }

        T sum = default;
        int vectorSize = Vector<T>.Count;
        int i;

        var divisor = new Vector<T>(secondsInHour);

        for (i = 0; i <= valueSpan.Length - vectorSize; i += vectorSize)
        {
            var valueVector = new Vector<T>(valueSpan.Slice(i));
            var workingTimeVector = new Vector<T>(workingTimeSpan.Slice(i));
            var resultVector = valueVector * workingTimeVector / divisor;
            sum += Vector.Sum(resultVector);
        }

        for (; i < valueSpan.Length; ++i)
        {
            sum += valueSpan[i] * workingTimeSpan[i] / secondsInHour;
        }

        return sum;
    }

    public static (T Min, T Max) GetMinMax<T>(this ReadOnlySpan<T> span) where T : INumber<T>
    {
        if (Vector.IsHardwareAccelerated)
        {
            return GetMinMaxSIMD(span);
        }
        return GetMinMaxScalar(span);
    }

    private static (T Min, T Max) GetMinMaxScalar<T>(this ReadOnlySpan<T> span) where T : INumber<T>
    {
        if (span.Length == 0)
        {
            throw new ArgumentException("Span cannot be empty.", nameof(span));
        }

        T min = span[0];
        T max = span[0];
        for (int i = 1; i < span.Length; ++i)
        {
            T current = span[i];
            min = T.Min(min, current);
            max = T.Max(max, current);
        }
        return (min, max);
    }

    public static (T Min, T Max) GetMinMaxSIMD<T>(this ReadOnlySpan<T> span) where T : INumber<T>
    {
        if (span.Length == 0)
        {
            throw new ArgumentException("Span cannot be empty.", nameof(span));
        }

        int vectorSize = Vector<T>.Count;
        int i = 0;
        var maxes = new Vector<T>(span);
        var mins = new Vector<T>(span);
        for (i = 0; i <= span.Length - vectorSize; i += vectorSize)
        {
            var valueVector = new Vector<T>(span.Slice(i));
            maxes = Vector.Max(maxes, valueVector);
            mins = Vector.Min(mins, valueVector);
        }

        var max = maxes[0];
        var min = mins[0];

        for (var j = 1; j < vectorSize; ++j)
        {
            if (maxes[j] > max)
            {
                max = maxes[j];
            }
            if (mins[j] < min)
            {
                min = mins[j];
            }
        }

        for (; i < span.Length; ++i)
        {
            if (span[i] > max)
            {
                max = span[i];
            }
            if (span[i] < min)
            {
                min = span[i];
            }
        }

        return (min, max);
    }
}