using System.Numerics;

namespace FlomtManager.Framework.Extensions;

public static class INumberExtensions
{
    public static T Sum<T>(this Span<T> values) where T : INumber<T>
    {
        T sum = default;
        int vectorSize = Vector<T>.Count;
        int i;

        // Process elements in chunks using SIMD
        for (i = 0; i <= values.Length - vectorSize; i += vectorSize)
        {
            var vector = new Vector<T>(values.Slice(i));
            sum += Vector.Sum(vector);
        }

        // Handle remaining elements
        for (; i < values.Length; i++)
        {
            sum += values[i];
        }

        return sum;
    }
}
