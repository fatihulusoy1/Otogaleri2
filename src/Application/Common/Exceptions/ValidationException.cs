namespace AutoGallerySaaS.Application.Common.Exceptions;

/// <summary>
/// Gecersiz istemci girdisi icin firlatilir. HTTP 400 Bad Request ile eslenir.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message)
        : base(message)
    {
    }
}
