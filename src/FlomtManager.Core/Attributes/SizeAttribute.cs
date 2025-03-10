﻿namespace FlomtManager.Core.Attributes;

[AttributeUsage(AttributeTargets.All)]
public sealed class SizeAttribute(byte size) : Attribute
{
    public byte Size { get; } = size;
}
