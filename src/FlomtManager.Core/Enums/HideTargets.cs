﻿namespace FlomtManager.Core.Enums
{
    [Flags]
    public enum HideTargets
    {
        Chart = 1 << 0,
        Table = 1 << 1,
        All = 0xff,
    }
}
