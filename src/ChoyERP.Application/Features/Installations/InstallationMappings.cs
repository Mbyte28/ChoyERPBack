using ChoyERP.Application.Common.Extensions;
using ChoyERP.Application.Features.Installations.Dtos;
using ChoyERP.Domain.Entities.Installations;

namespace ChoyERP.Application.Features.Installations;

internal static class InstallationMappings
{
    public static InstallationDto ToDto(this Installation installation) =>
        new(
            installation.Id,
            installation.Code,
            installation.ClientId,
            installation.Client.Name,
            installation.SaleId,
            installation.Sale?.Code,
            installation.ProjectId,
            installation.Project?.Code,
            installation.ClientAddressId,
            installation.ClientAddress?.ToSingleLine(),
            installation.Location,
            installation.Name,
            installation.Description,
            installation.Status,
            installation.ScheduledDate,
            installation.StartedDate,
            installation.CompletedDate,
            installation.TechnicianWorkerId,
            installation.Technician?.FullName,
            installation.CancellationReason,
            installation.Notes,
            installation.CreatedAt,
            installation.UpdatedAt);
}
