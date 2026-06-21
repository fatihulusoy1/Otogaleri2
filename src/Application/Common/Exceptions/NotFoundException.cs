namespace AutoGallerySaaS.Application.Common.Exceptions;

/// <summary>
/// Istenen kaynak bulunamadiginda firlatilir. HTTP 404 Not Found ile eslenir.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}
