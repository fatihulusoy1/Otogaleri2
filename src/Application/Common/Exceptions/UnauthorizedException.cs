namespace AutoGallerySaaS.Application.Common.Exceptions;

/// <summary>
/// Kimlik dogrulama / yetki hatalarinda firlatilir. HTTP 401 Unauthorized ile eslenir.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message)
        : base(message)
    {
    }
}
