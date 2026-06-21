using System.Collections.Concurrent;
using AutoGallerySaaS.Application.Common.Exceptions;
using AutoGallerySaaS.Application.Common.Interfaces;

namespace AutoGallerySaaS.Infrastructure.Services;

/// <summary>
/// E-posta bazlı basit brute-force koruması. Belirli sürede çok fazla başarısız giriş olursa
/// hesabı geçici olarak kilitler. Tek instance için bellek içi (singleton) yeterlidir;
/// çok instance'lı dağıtımda ortak bir store (Redis vb.) ile değiştirilebilir.
/// </summary>
public class LoginThrottle : ILoginThrottle
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, Attempt> _attempts = new();

    private sealed class Attempt
    {
        public int Count;
        public DateTime WindowStartUtc;
    }

    public void EnsureNotLocked(string email)
    {
        var key = Normalize(email);
        if (_attempts.TryGetValue(key, out var attempt) &&
            attempt.Count >= MaxAttempts &&
            DateTime.UtcNow - attempt.WindowStartUtc < LockoutWindow)
        {
            var remaining = (int)Math.Ceiling((LockoutWindow - (DateTime.UtcNow - attempt.WindowStartUtc)).TotalMinutes);
            throw new BusinessRuleException($"Çok fazla başarısız giriş denemesi. Lütfen {Math.Max(1, remaining)} dakika sonra tekrar deneyin.");
        }
    }

    public void RegisterFailure(string email)
    {
        var key = Normalize(email);
        _attempts.AddOrUpdate(
            key,
            _ => new Attempt { Count = 1, WindowStartUtc = DateTime.UtcNow },
            (_, existing) =>
            {
                if (DateTime.UtcNow - existing.WindowStartUtc >= LockoutWindow)
                {
                    existing.Count = 1;
                    existing.WindowStartUtc = DateTime.UtcNow;
                }
                else
                {
                    existing.Count++;
                }

                return existing;
            });
    }

    public void Reset(string email)
    {
        _attempts.TryRemove(Normalize(email), out _);
    }

    private static string Normalize(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}
