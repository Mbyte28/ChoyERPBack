using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChoyERP.Infrastructure.Persistence.Configurations;

public sealed class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.ToTable("workers");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(w => w.LastName).HasMaxLength(100).IsRequired();
        builder.Property(w => w.DocumentType).HasMaxLength(20);
        builder.Property(w => w.DocumentNumber).HasMaxLength(30).IsRequired();
        builder.Property(w => w.Position).HasMaxLength(100);
        builder.Property(w => w.Department).HasMaxLength(100);
        builder.Property(w => w.Phone).HasMaxLength(30);
        builder.Property(w => w.Email).HasMaxLength(320);
        builder.Property(w => w.Notes).HasMaxLength(2000);

        builder.HasIndex(w => w.DocumentNumber).IsUnique();
        builder.HasIndex(w => w.IsActive);

        builder.Ignore(w => w.FullName);
    }
}

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.LegalName).HasMaxLength(200);
        builder.Property(c => c.TaxId).HasMaxLength(30);
        builder.Property(c => c.ContactName).HasMaxLength(150);
        builder.Property(c => c.Email).HasMaxLength(320);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.TaxId).IsUnique();
        builder.HasIndex(c => c.IsActive);

        builder.HasMany(c => c.Addresses)
            .WithOne(a => a.Client)
            .HasForeignKey(a => a.ClientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ClientAddressConfiguration : IEntityTypeConfiguration<ClientAddress>
{
    public void Configure(EntityTypeBuilder<ClientAddress> builder)
    {
        builder.ToTable("client_addresses");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Label).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Line1).HasMaxLength(250).IsRequired();
        builder.Property(a => a.Line2).HasMaxLength(250);
        builder.Property(a => a.City).HasMaxLength(100);
        builder.Property(a => a.State).HasMaxLength(100);
        builder.Property(a => a.Country).HasMaxLength(100);
        builder.Property(a => a.PostalCode).HasMaxLength(20);

        builder.HasIndex(a => a.ClientId);
    }
}
