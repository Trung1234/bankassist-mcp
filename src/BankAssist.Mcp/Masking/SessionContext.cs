namespace BankAssist.Mcp.Masking;

/// <summary>
/// Ngữ cảnh phiên được lưu giữ trong scoped service. Chứa session ID và RM ID
/// từ header HTTP `X-Session-Id` và `X-Rm-Id`.
/// </summary>
public sealed class SessionContext
{
    public required string SessionId { get; set; }

    public string? RmId { get; set; }

    public bool IsSessionIdFromHeader { get; set; }
}
