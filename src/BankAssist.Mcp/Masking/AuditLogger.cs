using System.Text.Json;
using System.Text.Json.Serialization;
using BankAssist.Mcp.Configuration;
using Microsoft.Extensions.Options;

namespace BankAssist.Mcp.Masking;

/// <summary>
/// Ghi audit log một dòng JSON mỗi lần tool được gọi.
/// File là `logs/audit-{yyyyMMdd}.jsonl`, an toàn khi ghi đồng thời nhiều luồng.
/// </summary>
public sealed partial class AuditLogger(
    IOptions<AuditOptions> options,
    ILogger<AuditLogger> logger)
{
    private readonly AuditOptions _options = options.Value;

    private readonly Lock _writeLock = new();

    public void LogToolCall(
        string sessionId,
        string? rmId,
        string toolName,
        object? args,
        int resultCount,
        Dictionary<string, int> maskedCounts,
        long durationMs)
    {
        try
        {
            var entry = new AuditEntry
            {
                Timestamp = DateTimeOffset.Now,
                SessionId = sessionId,
                RmId = rmId,
                Tool = toolName,
                Arguments = args,
                ResultCount = resultCount,
                MaskedCounts = maskedCounts,
                DurationMs = durationMs,
                Level = "Info"
            };

            WriteEntry(entry);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi ghi audit log cho tool {ToolName}.", toolName);
        }
    }

    public void LogSecurityAlert(
        string sessionId,
        string? rmId,
        string toolName,
        string detectedKind)
    {
        try
        {
            var entry = new AuditEntry
            {
                Timestamp = DateTimeOffset.Now,
                SessionId = sessionId,
                RmId = rmId,
                Tool = toolName,
                Arguments = null,
                ResultCount = 0,
                MaskedCounts = new(),
                DurationMs = 0,
                Level = "SecurityAlert",
                SecurityAlert = new { DetectedKind = detectedKind }
            };

            WriteEntry(entry);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi ghi security alert cho tool {ToolName}.", toolName);
        }
    }

    private void WriteEntry(AuditEntry entry)
    {
        Directory.CreateDirectory(_options.Directory);

        var fileName = $"audit-{entry.Timestamp:yyyyMMdd}.jsonl";
        var filePath = Path.Combine(_options.Directory, fileName);

        var json = JsonSerializer.Serialize(entry, AuditEntrySerializerContext.Default.AuditEntry);

        lock (_writeLock)
        {
            File.AppendAllText(filePath, json + Environment.NewLine);
        }
    }

    [JsonSerializable(typeof(AuditEntry))]
    private sealed partial class AuditEntrySerializerContext : JsonSerializerContext;

    private sealed class AuditEntry
    {
        [JsonPropertyName("ts")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = "";

        [JsonPropertyName("rmId")]
        public string? RmId { get; set; }

        [JsonPropertyName("tool")]
        public string Tool { get; set; } = "";

        [JsonPropertyName("args")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Arguments { get; set; }

        [JsonPropertyName("resultCount")]
        public int ResultCount { get; set; }

        [JsonPropertyName("maskedCounts")]
        public Dictionary<string, int> MaskedCounts { get; set; } = new();

        [JsonPropertyName("durationMs")]
        public long DurationMs { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; } = "";

        [JsonPropertyName("securityAlert")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? SecurityAlert { get; set; }
    }
}
