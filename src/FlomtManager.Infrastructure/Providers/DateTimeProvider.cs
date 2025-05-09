﻿using FlomtManager.Domain.Abstractions.Providers;

namespace FlomtManager.Infrastructure.Providers;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}
