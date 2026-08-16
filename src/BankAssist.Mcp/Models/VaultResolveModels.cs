using System.Text.Json.Serialization;

namespace BankAssist.Mcp.Models;

public sealed record VaultResolveRequest(
    [property: JsonPropertyName("sessionId")] string? SessionId,
    [property: JsonPropertyName("tokens")] IReadOnlyList<string>? Tokens,
    [property: JsonPropertyName("purpose")] string? Purpose = null,
    [property: JsonPropertyName("tenantId")] string? TenantId = null,
    [property: JsonPropertyName("actorId")] string? ActorId = null,
    [property: JsonPropertyName("approvalId")] string? ApprovalId = null);

public sealed record VaultResolveResponse(
    [property: JsonPropertyName("resolved")] IReadOnlyDictionary<string, string> Resolved);
