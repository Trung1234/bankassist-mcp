using BankAssist.Mcp.Models;

namespace BankAssist.Mcp.Data;

/// <summary>
/// Cửa duy nhất để chạm vào dữ liệu CRM. MVP đọc JSON; giai đoạn sau thay bằng SQL
/// mà không sửa một dòng nào trong <c>Tools/</c> — đó là tiêu chí kiểm tra thiết kế.
/// </summary>
public interface ICrmRepository
{
    Task<Customer?> GetCustomerAsync(string customerId, CancellationToken ct);

    Task<IReadOnlyList<Customer>> SearchCustomersAsync(string? segment, string? rmId, int take, CancellationToken ct);

    Task<IReadOnlyList<Opportunity>> GetOpportunitiesAsync(string? customerId, int? maturingWithinDays, string? stage, CancellationToken ct);

    Task<IReadOnlyList<Interaction>> GetInteractionsAsync(string customerId, int take, CancellationToken ct);

    Task<IReadOnlyList<Campaign>> GetCampaignsAsync(bool activeOnly, CancellationToken ct);

    Task<IReadOnlyList<Product>> SearchProductsAsync(string keyword, string? type, CancellationToken ct);
}

/// <summary>Giới hạn phân trang dùng chung cho mọi cài đặt của <see cref="ICrmRepository"/>.</summary>
public static class QueryLimits
{
    public const int DefaultTake = 20;

    public const int MaxTake = 100;

    public const int DefaultInteractionTake = 10;

    /// <summary>
    /// Ép <paramref name="take"/> về khoảng hợp lệ: số không hợp lệ dùng mặc định,
    /// số quá lớn bị cắt về trần (SPEC/TEST-SPEC R-04, R-05, R-06).
    /// </summary>
    public static int Normalize(int take, int defaultTake = DefaultTake) =>
        take <= 0 ? defaultTake : Math.Min(take, MaxTake);
}
