using ChoyERP.Application.Common.Interfaces;

namespace ChoyERP.Infrastructure.Identity;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
