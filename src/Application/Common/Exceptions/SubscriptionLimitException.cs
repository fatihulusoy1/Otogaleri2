namespace AutoGallerySaaS.Application.Common.Exceptions;

/// <summary>
/// Abonelik paketi limiti (kullanıcı/araç) aşıldığında fırlatılır.
/// API katmanında özel bir kod ile döndürülür; istemci kullanıcıyı abonelik ekranına yönlendirir.
/// </summary>
public class SubscriptionLimitException : Exception
{
    public SubscriptionLimitException(string message)
        : base(message)
    {
    }
}
