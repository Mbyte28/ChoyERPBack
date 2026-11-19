using ChoyERP.Domain.Entities.Clients;

namespace ChoyERP.Application.Common.Extensions;

public static class ClientAddressExtensions
{
    /// <summary>Compone la dirección en una sola línea legible.</summary>
    public static string ToSingleLine(this ClientAddress address)
    {
        var parts = new[]
        {
            address.Line1,
            address.Line2,
            address.City,
            address.State,
            address.Country,
        };

        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
