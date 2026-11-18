using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Common.Extensions;

public static class SequentialCodeExtensions
{
    /// <summary>
    /// Genera el siguiente código secuencial con el prefijo indicado, por ejemplo <c>SOL-00001</c>.
    /// El índice único de la base de datos protege la integridad ante concurrencia.
    /// </summary>
    public static async Task<string> NextSequentialCodeAsync(
        this IQueryable<string> codes,
        string prefix,
        CancellationToken cancellationToken)
    {
        var lastCode = await codes
            .Where(code => code.StartsWith(prefix))
            .OrderByDescending(code => code)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNumber = 1;

        if (lastCode is not null && int.TryParse(lastCode[prefix.Length..], out var parsed))
        {
            nextNumber = parsed + 1;
        }

        return $"{prefix}{nextNumber:D5}";
    }
}
