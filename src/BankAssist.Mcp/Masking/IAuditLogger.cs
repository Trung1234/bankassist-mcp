namespace BankAssist.Mcp.Masking;

/// <summary>Ghi nhật ký audit một dòng JSON mỗi lời gọi tool (SPEC mục 8).</summary>
public interface IAuditLogger
{
    void LogToolCall(
        string sessionId,
        string? rmId,
        string toolName,
        object? args,
        int resultCount,
        Dictionary<string, int> maskedCounts,
        long durationMs);

    void LogSecurityAlert(string sessionId, string? rmId, string toolName, string detectedKind);
}
