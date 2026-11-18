using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Security;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Inventory;
using ChoyERP.Domain.Entities.Users;
using ChoyERP.Domain.Entities.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChoyERP.Infrastructure.Persistence.Seed;

/// <summary>
/// Siembra permisos, roles del sistema, el administrador inicial y datos de demostración opcionales.
/// Es idempotente: puede ejecutarse en cada arranque.
/// </summary>
public sealed class DatabaseSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IOptions<SeedOptions> options,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedAdministratorAsync(cancellationToken);

        if (options.Value.DemoData)
        {
            await SeedDemoDataAsync(cancellationToken);
        }
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existing = await db.Permissions.ToDictionaryAsync(p => p.Code, cancellationToken);

        foreach (var definition in Permissions.All)
        {
            if (existing.TryGetValue(definition.Code, out var permission))
            {
                permission.Name = definition.Name;
                permission.Module = definition.Module;
            }
            else
            {
                db.Permissions.Add(new Permission
                {
                    Code = definition.Code,
                    Name = definition.Name,
                    Module = definition.Module,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var permissionsByCode = await db.Permissions.ToDictionaryAsync(p => p.Code, cancellationToken);
        var roles = await db.Roles
            .Include(r => r.RolePermissions)
            .ToDictionaryAsync(r => r.NormalizedName, cancellationToken);

        foreach (var definition in RoleCatalog.All)
        {
            var normalizedName = definition.Name.ToLowerInvariant();

            if (!roles.TryGetValue(normalizedName, out var role))
            {
                role = new Role
                {
                    Name = definition.Name,
                    NormalizedName = normalizedName,
                    Description = definition.Description,
                    IsSystemRole = definition.IsSystemRole,
                };

                db.Roles.Add(role);
            }

            foreach (var permissionCode in definition.PermissionCodes)
            {
                var permission = permissionsByCode[permissionCode];

                if (role.RolePermissions.All(rp => rp.PermissionId != permission.Id))
                {
                    role.RolePermissions.Add(new RolePermission { Permission = permission });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdministratorAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.AdminPassword))
        {
            logger.LogWarning(
                "Seed:AdminPassword no está configurado; no se creará el usuario administrador inicial.");
            return;
        }

        var email = options.Value.AdminEmail.Trim();
        var normalizedEmail = EmailNormalizer.Normalize(email);
        var adminRole = await db.Roles
            .SingleAsync(r => r.NormalizedName == RoleNames.Administrator.ToLowerInvariant(), cancellationToken);

        var admin = await db.Users
            .Include(u => u.UserRoles)
            .SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (admin is null)
        {
            admin = new User
            {
                Email = email,
                NormalizedEmail = normalizedEmail,
                PasswordHash = passwordHasher.Hash(options.Value.AdminPassword),
                IsActive = true,
            };

            admin.UserRoles.Add(new UserRole { Role = adminRole });
            db.Users.Add(admin);

            logger.LogWarning(
                "Se creó el usuario administrador inicial {Email}. Cambie la contraseña en el primer acceso.",
                email);
        }
        else if (admin.UserRoles.All(ur => ur.RoleId != adminRole.Id))
        {
            admin.UserRoles.Add(new UserRole { Role = adminRole });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDemoDataAsync(CancellationToken cancellationToken)
    {
        if (!await db.Workers.AnyAsync(cancellationToken))
        {
            db.Workers.AddRange(
                new Worker
                {
                    FirstName = "Luis",
                    LastName = "Ramírez",
                    DocumentType = "DNI",
                    DocumentNumber = "12345678",
                    Position = "Técnico de refrigeración",
                    Department = "Operaciones",
                    Phone = "999111222",
                    Email = "luis.ramirez@choy.local",
                    HireDate = new DateOnly(2021, 3, 15),
                },
                new Worker
                {
                    FirstName = "María",
                    LastName = "Torres",
                    DocumentType = "DNI",
                    DocumentNumber = "23456789",
                    Position = "Ingeniera de proyectos",
                    Department = "Ingeniería",
                    Phone = "999333444",
                    Email = "maria.torres@choy.local",
                    HireDate = new DateOnly(2019, 8, 1),
                },
                new Worker
                {
                    FirstName = "Carlos",
                    LastName = "Mendoza",
                    DocumentType = "DNI",
                    DocumentNumber = "34567890",
                    Position = "Almacenero",
                    Department = "Almacén",
                    Phone = "999555666",
                    Email = "carlos.mendoza@choy.local",
                    HireDate = new DateOnly(2022, 1, 10),
                });
        }

        if (!await db.Clients.AnyAsync(cancellationToken))
        {
            var andina = new Client
            {
                Code = "CLI-00001",
                Name = "Corporación Andina",
                LegalName = "Corporación Andina S.A.C.",
                TaxId = "20123456789",
                ContactName = "Jorge Salazar",
                Email = "compras@andina.local",
                Phone = "014567890",
            };

            andina.Addresses.Add(new ClientAddress
            {
                Label = "Planta principal",
                Line1 = "Av. Industrial 1250",
                City = "Lima",
                State = "Lima",
                Country = "Perú",
                IsPrimary = true,
            });

            var frigorifico = new Client
            {
                Code = "CLI-00002",
                Name = "Frigorífico del Sur",
                LegalName = "Frigorífico del Sur S.A.",
                TaxId = "20987654321",
                ContactName = "Ana Quispe",
                Email = "operaciones@frigosur.local",
                Phone = "054321654",
            };

            frigorifico.Addresses.Add(new ClientAddress
            {
                Label = "Cámara de congelamiento",
                Line1 = "Carretera Panamericana Km 12",
                City = "Arequipa",
                State = "Arequipa",
                Country = "Perú",
                IsPrimary = true,
            });

            db.Clients.AddRange(andina, frigorifico);
        }

        await SeedInventoryDemoDataAsync(cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedInventoryDemoDataAsync(CancellationToken cancellationToken)
    {
        if (await db.InventoryItems.AnyAsync(cancellationToken))
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var items = new[]
        {
            new InventoryItem
            {
                Code = "ART-00001",
                Name = "Gas refrigerante R-404A",
                Description = "Cilindro de gas refrigerante para sistemas de baja temperatura.",
                Category = InventoryCategory.Supply,
                Unit = InventoryUnit.Kilogram,
                Brand = "Freon",
                Location = "Estante A-1",
                QuantityOnHand = 35m,
                MinimumStock = 20m,
                AverageUnitCost = 28.5m,
            },
            new InventoryItem
            {
                Code = "ART-00002",
                Name = "Compresor hermético 1 HP",
                Description = "Compresor para cámara frigorífica pequeña.",
                Category = InventoryCategory.Equipment,
                Unit = InventoryUnit.Unit,
                Brand = "Embraco",
                Model = "NEK6210Z",
                Location = "Estante B-2",
                QuantityOnHand = 1m,
                MinimumStock = 2m,
                AverageUnitCost = 850m,
            },
            new InventoryItem
            {
                Code = "ART-00003",
                Name = "Filtro secador 3/8\"",
                Description = "Filtro secador para línea de líquido.",
                Category = InventoryCategory.Spare,
                Unit = InventoryUnit.Unit,
                Location = "Cajón C-3",
                QuantityOnHand = 24m,
                MinimumStock = 10m,
                AverageUnitCost = 12m,
            },
            new InventoryItem
            {
                Code = "ART-00004",
                Name = "Tubería de cobre 1/2\"",
                Category = InventoryCategory.Component,
                Unit = InventoryUnit.Meter,
                Location = "Rack D-1",
                QuantityOnHand = 120m,
                MinimumStock = 50m,
                AverageUnitCost = 8.4m,
            },
        };

        db.InventoryItems.AddRange(items);

        var movements = items.Select((item, index) => new InventoryMovement
        {
            Code = $"MOV-{index + 1:D5}",
            Item = item,
            Type = InventoryMovementType.Entry,
            MovementDate = today,
            Quantity = item.QuantityOnHand,
            QuantityDelta = item.QuantityOnHand,
            StockBefore = 0,
            StockAfter = item.QuantityOnHand,
            UnitCost = item.AverageUnitCost,
            TotalCost = Math.Round(
                item.QuantityOnHand * item.AverageUnitCost,
                2,
                MidpointRounding.AwayFromZero),
            Reason = "Stock inicial de demostración",
        });

        db.InventoryMovements.AddRange(movements);
    }
}
