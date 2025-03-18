using System.Text;

namespace FlomtManager.Framework.Extensions;

public static class StringBuilderExtensions
{
    public static StringBuilder RemoveLast(this StringBuilder sb, int count)
    {
        if (count < 0)
        {
            return sb;
        }

        count = Math.Min(count, sb.Length);
        return sb.Remove(sb.Length - count, count);
    }
}
