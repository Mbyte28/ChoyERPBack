using ChoyERP.Domain.Common;
using ChoyERP.Domain.Entities.Users;

namespace ChoyERP.Domain.Entities.Workers;

/// <summary>
/// Personal de CHOY. No todas las personas deben tener una cuenta de acceso.
/// </summary>
public class Worker : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? DocumentType { get; set; }

    public string DocumentNumber { get; set; } = string.Empty;

    public string? Position { get; set; }

    public string? Department { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public DateOnly? HireDate { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public User? UserAccount { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
