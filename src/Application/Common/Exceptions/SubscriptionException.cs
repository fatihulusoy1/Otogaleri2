namespace AutoGallerySaaS.Application.Common.Exceptions;

/// <summary>
/// Abonelik/deneme suresi dolan veya plan limitine ulasan tenant islemleri icin firlatilir.
/// HTTP 402 (Payment Required) ile eslenir; on yuz bu kodu "plan yukseltme" akisina yonlendirebilir.
/// </summary>
public class SubscriptionException : Exception
{
    public SubscriptionException(string message)
        : base(message)
    {
    }
}
