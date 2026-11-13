namespace ChoyERP.Domain.Common;

/// <summary>
/// Base para entidades persistentes con identidad y marcas de auditoría.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
