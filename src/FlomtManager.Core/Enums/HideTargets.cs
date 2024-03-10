namespace FlomtManager.Core.Enums
{
    [Flags]
    public enum HideTargets
    {
        Chart = 1,
        All = 1 << 8 - 1,
    }
}
