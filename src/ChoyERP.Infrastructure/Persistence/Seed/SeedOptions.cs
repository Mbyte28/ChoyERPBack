namespace ChoyERP.Infrastructure.Persistence.Seed;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }

    public bool DemoData { get; set; }

    public string AdminEmail { get; set; } = "admin@choy.local";

    public string AdminPassword { get; set; } = string.Empty;
}
