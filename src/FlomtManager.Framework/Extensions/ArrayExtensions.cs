using System.Numerics;

namespace FlomtManager.Framework.Extensions
{
    public static class ArrayExtensions
    {
        public static int BinarySearchClosestValueIndex<T>(this T[] sortedArray, T target) where T : INumber<T>
        {
            int index = Array.BinarySearch(sortedArray, target);
            if (index >= 0)
            {
                return index;
            }

            int nextLargerIndex = ~index;
            if (nextLargerIndex < sortedArray.Length)
            {
                if (nextLargerIndex == 0)
                {
                    return 0;
                }

                var previousElement = sortedArray[nextLargerIndex - 1];
                var nextElement = sortedArray[nextLargerIndex];

                if (T.Abs(previousElement - target) < T.Abs(nextElement - target))
                {
                    return nextLargerIndex - 1;
                }
                else
                {
                    return nextLargerIndex;
                }
            }
            else
            {
                return sortedArray.Length - 1;
            }
        }
    }
}
