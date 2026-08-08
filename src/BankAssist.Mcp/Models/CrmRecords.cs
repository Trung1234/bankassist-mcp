namespace BankAssist.Mcp.Models;

/// <summary>
/// Bản ghi khách hàng như nằm trong <c>data/customers.json</c>. Đây là dữ liệu THÔ,
/// chưa che. Không bao giờ đưa thẳng kiểu này ra ngoài qua MCP.
/// </summary>
public sealed record Customer(
    string CustomerId,
    string FullName,
    string? Phone,
    string? Cccd,
    string? AccountNo,
    string? Email,
    string Segment,
    string AssignedRmId,
    DateOnly OpenedDate);

public sealed record Opportunity(
    string OppId,
    string CustomerId,
    string ProductCode,
    decimal Amount,
    DateOnly MaturityDate,
    string Stage,
    string? CampaignId);

public sealed record Interaction(
    string InteractionId,
    string CustomerId,
    string Channel,
    DateTimeOffset CreatedAt,
    string Note);

public sealed record Campaign(
    string CampaignId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record Product(
    string ProductCode,
    string Name,
    string Type,
    decimal InterestRate,
    string Description);
