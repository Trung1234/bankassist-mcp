namespace BankAssist.Mcp.Masking;

/// <summary>
/// Dựng <see cref="SessionContext"/> cho mỗi request từ header HTTP (SPEC mục 4.4).
/// </summary>
public static class SessionContextFactory
{
    public const string SessionIdHeader = "X-Session-Id";

    public const string RmIdHeader = "X-Rm-Id";

    public static SessionContext FromHttpContext(HttpContext? httpContext, ILogger logger)
    {
        var headerSessionId = httpContext?.Request.Headers[SessionIdHeader].ToString();

        if (!string.IsNullOrWhiteSpace(headerSessionId))
        {
            return new SessionContext
            {
                SessionId = headerSessionId,
                RmId = NullIfBlank(httpContext?.Request.Headers[RmIdHeader].ToString()),
                IsSessionIdFromHeader = true,
            };
        }

        // Vẫn phục vụ được, nhưng host không giữ id này nên token sẽ không hoàn nguyên được.
        var generated = $"anon-{Guid.NewGuid():N}";
        logger.LogWarning(
            "Request thiếu header {Header}, đã tự sinh phiên {SessionId}. Token của phiên này không hoàn nguyên được.",
            SessionIdHeader, generated);

        return new SessionContext
        {
            SessionId = generated,
            RmId = NullIfBlank(httpContext?.Request.Headers[RmIdHeader].ToString()),
            IsSessionIdFromHeader = false,
        };
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
