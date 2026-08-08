using System.Text.Json;
using System.Text.RegularExpressions;
using BankAssist.Mcp.Masking;
using BankAssist.Mcp.Models;

namespace BankAssist.Mcp.Tools;

/// <summary>
/// Ba mắt xích cuối của mọi tool: LeakGuard → Audit → trả về (SPEC mục 2).
/// Gom vào một chỗ để không tool nào lỡ tay bỏ qua một bước.
/// </summary>
public static partial class ToolPipeline
{
    /// <summary>Thông báo trả cho client khi LeakGuard chặn — SPEC mục 5, không được lộ chi tiết.</summary>
    public const string BlockedMessage = "Không thể xử lý yêu cầu này. Sự việc đã được ghi nhận.";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <param name="auditArgs">Tham số đã che, dùng để ghi audit (SPEC mục 8).</param>
    public static ToolResult<T> Complete<T>(
        string toolName,
        IReadOnlyList<T> maskedItems,
        object? auditArgs,
        string? emptyMessage,
        long durationMs,
        ILeakGuard guard,
        IAuditLogger audit,
        ISessionContext session)
    {
        var json = JsonSerializer.Serialize(maskedItems, SerializerOptions);

        var verdict = guard.Check(json);
        if (!verdict.IsClean)
        {
            audit.LogSecurityAlert(session.SessionId, session.RmId, toolName, verdict.DetectedKind ?? "UNKNOWN");
            return ToolResult<T>.Empty(BlockedMessage);
        }

        audit.LogToolCall(
            session.SessionId,
            session.RmId,
            toolName,
            auditArgs,
            maskedItems.Count,
            CountTokensByType(json),
            durationMs);

        return maskedItems.Count == 0 && emptyMessage is not null
            ? ToolResult<T>.Empty(emptyMessage)
            : ToolResult<T>.Ok(maskedItems);
    }

    /// <summary>Đếm token phân biệt theo loại. Cùng một token lặp lại chỉ tính một lần.</summary>
    private static Dictionary<string, int> CountTokensByType(string json)
    {
        var distinctByType = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var match in TokenPattern().EnumerateMatches(json))
        {
            var token = json.Substring(match.Index, match.Length);
            var entityType = token[1..token.LastIndexOf('_')];

            if (!distinctByType.TryGetValue(entityType, out var seen))
            {
                seen = new HashSet<string>(StringComparer.Ordinal);
                distinctByType[entityType] = seen;
            }

            seen.Add(token);
        }

        return distinctByType.ToDictionary(pair => pair.Key, pair => pair.Value.Count, StringComparer.Ordinal);
    }

    [GeneratedRegex(@"\[[A-Z]+_\d+\]")]
    private static partial Regex TokenPattern();
}
