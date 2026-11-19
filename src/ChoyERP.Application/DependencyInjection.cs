using ChoyERP.Application.Features.Auth;
using ChoyERP.Application.Features.Clients;
using ChoyERP.Application.Features.Contracting;
using ChoyERP.Application.Features.Installations;
using ChoyERP.Application.Features.Inventory;
using ChoyERP.Application.Features.Roles;
using ChoyERP.Application.Features.Sales;
using ChoyERP.Application.Features.Users;
using ChoyERP.Application.Features.Workers;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ChoyERP.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<AuthService>(ServiceLifetime.Scoped);

        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<RoleService>();
        services.AddScoped<PermissionService>();
        services.AddScoped<WorkerService>();
        services.AddScoped<ClientService>();
        services.AddScoped<ProjectRequestService>();
        services.AddScoped<QuoteService>();
        services.AddScoped<ContractService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<InventoryItemService>();
        services.AddScoped<InventoryMovementService>();
        services.AddScoped<SaleService>();
        services.AddScoped<InstallationService>();

        return services;
    }
}
