namespace ChoyERP.Application.Common.Exceptions;

/// <summary>
/// Error de negocio o de aplicación con un código HTTP asociado.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string resource, object key)
        : base($"{resource} con identificador '{key}' no fue encontrado.", 404)
    {
    }

    public NotFoundException(string message)
        : base(message, 404)
    {
    }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, 409)
    {
    }
}

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message)
        : base(message, 401)
    {
    }
}

public sealed class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message)
        : base(message, 403)
    {
    }
}

/// <summary>
/// Errores de validación de entrada agrupados por campo.
/// </summary>
public sealed class ValidationAppException : AppException
{
    public ValidationAppException(IDictionary<string, string[]> errors)
        : base("La solicitud contiene datos inválidos.", 422)
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}
