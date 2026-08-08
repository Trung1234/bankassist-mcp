using BankAssist.Mcp.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BankAssist.Mcp.Masking;

/// <summary>
/// Cài đặt MVP của <see cref="IVault"/> trên <see cref="IMemoryCache"/>.
/// Một mục cache cho mỗi phiên, TTL trượt theo cấu hình.
/// </summary>
public sealed class MemoryVault(
    IMemoryCache cache,
    AesValueProtector protector,
    IOptions<VaultOptions> options,
    ILogger<MemoryVault> logger) : IVault
{
    /// <summary>Trả về khi phiên đã chạm trần số mục — vẫn là chuỗi an toàn, không lộ gì.</summary>
    public const string OverflowToken = "[REDACTED]";

    private readonly VaultOptions _options = options.Value;

    private readonly Lock _creationGate = new();

    public string Tokenize(string sessionId, string realValue, string entityType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var session = GetOrCreateSession(sessionId);
        var fingerprint = protector.Fingerprint(entityType, realValue);

        lock (session.Gate)
        {
            if (session.TokenByFingerprint.TryGetValue(fingerprint, out var existing))
            {
                return existing;
            }

            if (session.TokenByFingerprint.Count >= _options.MaxEntriesPerSession)
            {
                // Không ghi giá trị, chỉ ghi phiên và loại — CLAUDE.md cấm log PII.
                logger.LogWarning(
                    "Phiên {SessionId} đã vượt trần {Max} mục vault, từ chối thêm token loại {EntityType}.",
                    sessionId, _options.MaxEntriesPerSession, entityType);
                return OverflowToken;
            }

            var ordinal = session.NextOrdinal(entityType);
            var token = PiiEntityTypes.Token(entityType, ordinal);

            session.TokenByFingerprint[fingerprint] = token;
            session.ProtectedValueByToken[token] = protector.Protect(realValue);

            return token;
        }
    }

    public IReadOnlyDictionary<string, string> Resolve(string sessionId, IEnumerable<string> tokens)
    {
        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(sessionId) || !cache.TryGetValue<SessionVault>(sessionId, out var session) || session is null)
        {
            return resolved;
        }

        lock (session.Gate)
        {
            foreach (var token in tokens)
            {
                if (token is not null && session.ProtectedValueByToken.TryGetValue(token, out var protectedValue))
                {
                    resolved[token] = protector.Unprotect(protectedValue);
                }
            }
        }

        return resolved;
    }

    public void Drop(string sessionId) => cache.Remove(sessionId);

    /// <summary>Chỉ dùng trong test: chứng minh giá trị nằm trong cache ở dạng đã mã hoá (TEST-SPEC V-07).</summary>
    internal IReadOnlyList<byte[]> SnapshotProtectedValues(string sessionId)
    {
        if (!cache.TryGetValue<SessionVault>(sessionId, out var session) || session is null)
        {
            return [];
        }

        lock (session.Gate)
        {
            return [.. session.ProtectedValueByToken.Values];
        }
    }

    private SessionVault GetOrCreateSession(string sessionId)
    {
        // Đường nhanh: phiên đã có thì chỉ cần chạm vào để TTL trượt tiếp.
        if (cache.TryGetValue<SessionVault>(sessionId, out var existing) && existing is not null)
        {
            cache.Set(sessionId, existing, SlidingEntryOptions());
            return existing;
        }

        // Đường chậm: khoá toàn cục khi tạo mới. IMemoryCache.Set không nguyên tử, nếu để hai
        // luồng cùng tạo thì một luồng sẽ ghi token vào đối tượng đã bị đè — token đó mất tích
        // lúc hoàn nguyên. Tạo phiên là việc hiếm nên một khoá chung là đủ.
        lock (_creationGate)
        {
            if (cache.TryGetValue<SessionVault>(sessionId, out var raced) && raced is not null)
            {
                cache.Set(sessionId, raced, SlidingEntryOptions());
                return raced;
            }

            var created = new SessionVault();
            cache.Set(sessionId, created, SlidingEntryOptions());
            return created;
        }
    }

    private MemoryCacheEntryOptions SlidingEntryOptions() => new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(_options.TtlMinutes),
    };

    private sealed class SessionVault
    {
        public Lock Gate { get; } = new();

        public Dictionary<string, string> TokenByFingerprint { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, byte[]> ProtectedValueByToken { get; } = new(StringComparer.Ordinal);

        private Dictionary<string, int> OrdinalByEntityType { get; } = new(StringComparer.Ordinal);

        public int NextOrdinal(string entityType)
        {
            var next = OrdinalByEntityType.GetValueOrDefault(entityType) + 1;
            OrdinalByEntityType[entityType] = next;
            return next;
        }
    }
}
