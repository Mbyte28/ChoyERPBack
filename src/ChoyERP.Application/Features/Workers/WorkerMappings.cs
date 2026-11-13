using ChoyERP.Application.Features.Workers.Dtos;
using ChoyERP.Domain.Entities.Workers;

namespace ChoyERP.Application.Features.Workers;

internal static class WorkerMappings
{
    public static WorkerDto ToDto(this Worker worker) =>
        new(
            worker.Id,
            worker.FirstName,
            worker.LastName,
            worker.FullName,
            worker.DocumentType,
            worker.DocumentNumber,
            worker.Position,
            worker.Department,
            worker.Phone,
            worker.Email,
            worker.HireDate,
            worker.IsActive,
            worker.UserAccount is not null,
            worker.CreatedAt,
            worker.UpdatedAt);
}
