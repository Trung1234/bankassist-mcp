using System.ComponentModel;

namespace BankAssist.Mcp.Models;

/// <summary>
/// Hồ sơ khách hàng ĐÃ CHE. Các trường định danh cá nhân ở dạng token <c>[TYPE_n]</c>.
/// Đây là kiểu duy nhất được phép rời khỏi tiến trình qua giao thức MCP.
/// </summary>
public sealed record CustomerDto(
    [property: Description("Mã khách hàng nội bộ, không phải dữ liệu cá nhân")] string CustomerId,
    [property: Description("Họ tên đã che, dạng [PERSON_n]")] string FullName,
    [property: Description("Số điện thoại đã che, dạng [PHONE_n]")] string? Phone,
    [property: Description("Số CCCD đã che, dạng [ID_n]")] string? Cccd,
    [property: Description("Số tài khoản đã che, dạng [ACCOUNT_n]")] string? AccountNo,
    [property: Description("Email đã che, dạng [EMAIL_n]")] string? Email,
    [property: Description("Phân khúc khách hàng, ví dụ Priority")] string Segment,
    [property: Description("Mã RM phụ trách")] string AssignedRmId,
    [property: Description("Ngày mở quan hệ, dạng yyyy-MM-dd")] string OpenedDate);

public sealed record OpportunityDto(
    [property: Description("Mã cơ hội")] string OppId,
    [property: Description("Mã khách hàng")] string CustomerId,
    [property: Description("Họ tên khách đã che, dạng [PERSON_n]")] string CustomerName,
    [property: Description("Mã sản phẩm")] string ProductCode,
    [property: Description("Số tiền, đơn vị VND — không che vì không phải dữ liệu định danh")] decimal Amount,
    [property: Description("Ngày đáo hạn, dạng yyyy-MM-dd")] string MaturityDate,
    [property: Description("Trạng thái cơ hội: Open, Won, Lost")] string Stage,
    [property: Description("Mã chiến dịch gắn kèm, có thể rỗng")] string? CampaignId);

public sealed record InteractionDto(
    [property: Description("Mã tương tác")] string InteractionId,
    [property: Description("Mã khách hàng")] string CustomerId,
    [property: Description("Kênh tiếp xúc: Call, Email, Branch, Chat")] string Channel,
    [property: Description("Thời điểm tạo, dạng ISO 8601")] string CreatedAt,
    [property: Description("Ghi chú của RM, đã quét và che PII toàn văn")] string Note);

public sealed record CampaignDto(
    [property: Description("Mã chiến dịch")] string CampaignId,
    [property: Description("Tên chiến dịch")] string Name,
    [property: Description("Ngày bắt đầu, dạng yyyy-MM-dd")] string StartDate,
    [property: Description("Ngày kết thúc, dạng yyyy-MM-dd")] string EndDate);

public sealed record ProductDto(
    [property: Description("Mã sản phẩm")] string ProductCode,
    [property: Description("Tên sản phẩm")] string Name,
    [property: Description("Nhóm sản phẩm: Savings, Loan, Card, Insurance, Investment")] string Type,
    [property: Description("Lãi suất năm, đơn vị phần trăm")] decimal InterestRate,
    [property: Description("Mô tả điều kiện sản phẩm")] string Description);

/// <summary>
/// Bao ngoài mọi kết quả tool. Khi <paramref name="Items"/> rỗng, <paramref name="Message"/>
/// giải thích vì sao — SPEC mục 6.3 cấm ném exception ra phía client.
/// </summary>
public sealed record ToolResult<T>(
    [property: Description("Danh sách kết quả, có thể rỗng")] IReadOnlyList<T> Items,
    [property: Description("Số bản ghi trả về")] int Count,
    [property: Description("Thông báo cho agent khi kết quả rỗng hoặc bị chặn")] string? Message = null)
{
    public static ToolResult<T> Ok(IReadOnlyList<T> items) => new(items, items.Count);

    public static ToolResult<T> Empty(string message) => new([], 0, message);
}
