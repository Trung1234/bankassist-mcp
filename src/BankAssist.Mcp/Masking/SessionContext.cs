namespace BankAssist.Mcp.Masking;

/// <summary>
/// Ngữ cảnh phiên lấy từ header HTTP <c>X-Session-Id</c> và <c>X-Rm-Id</c> (SPEC mục 4.4).
/// </summary>
public sealed class SessionContext : ISessionContext
{
    public required string SessionId { get; init; }

    public string? RmId { get; init; }

    /// <summary>
    /// False khi host không truyền <c>X-Session-Id</c> và server phải tự sinh. Khi đó
    /// host không giữ được id để gọi <c>/vault/resolve</c>, nên token thành không hoàn nguyên được.
    /// </summary>
    public bool IsSessionIdFromHeader { get; init; }
}
