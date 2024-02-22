namespace FlomtManager.Framework.Extensions
{
    public static class EnumExtensions
    {
        public static TAttribute? GetAttribute<TAttribute>(this Enum @enum)
            where TAttribute : Attribute
        {
            var type = @enum.GetType();
            var memInfo = type.GetMember(@enum.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(TAttribute), false);
            return attributes.Length > 0 ? ((TAttribute)attributes[0]) : null;
        }
    }
}
