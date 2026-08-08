using BankAssist.Mcp.Models;

namespace BankAssist.Mcp.Data;

/// <summary>
/// Cài đặt MVP: lọc trên danh sách đã nạp sẵn trong bộ nhớ. Không có I/O nào ở đây,
/// nhưng chữ ký vẫn bất đồng bộ để bản SQL sau này thay vào không phải sửa tool.
/// </summary>
public sealed class JsonCrmRepository(CrmDataStore store, TimeProvider timeProvider) : ICrmRepository
{
    public Task<Customer?> GetCustomerAsync(string customerId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(store.FindCustomer(customerId));
    }

    public Task<IReadOnlyList<Customer>> SearchCustomersAsync(string? segment, string? rmId, int take, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var matches = store.Customers
            .Where(c => string.IsNullOrWhiteSpace(segment)
                || string.Equals(c.Segment, segment, StringComparison.OrdinalIgnoreCase))
            .Where(c => string.IsNullOrWhiteSpace(rmId)
                || string.Equals(c.AssignedRmId, rmId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.CustomerId, StringComparer.Ordinal)
            .Take(QueryLimits.Normalize(take))
            .ToList();

        return Task.FromResult<IReadOnlyList<Customer>>(matches);
    }

    public Task<IReadOnlyList<Opportunity>> GetOpportunitiesAsync(
        string? customerId, int? maturingWithinDays, string? stage, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var query = store.Opportunities.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            query = query.Where(o => string.Equals(o.CustomerId, customerId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(o => string.Equals(o.Stage, stage, StringComparison.OrdinalIgnoreCase));
        }

        if (maturingWithinDays is > 0)
        {
            // Khoảng đóng hai đầu: đáo hạn đúng hôm nay vẫn được tính (TEST-SPEC R-08).
            var today = Today();
            var until = today.AddDays(maturingWithinDays.Value);
            query = query.Where(o => o.MaturityDate >= today && o.MaturityDate <= until);
        }

        var matches = query
            .OrderBy(o => o.MaturityDate)
            .ThenBy(o => o.OppId, StringComparer.Ordinal)
            .Take(QueryLimits.MaxTake)
            .ToList();

        return Task.FromResult<IReadOnlyList<Opportunity>>(matches);
    }

    public Task<IReadOnlyList<Interaction>> GetInteractionsAsync(string customerId, int take, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var matches = store.Interactions
            .Where(i => string.Equals(i.CustomerId, customerId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.InteractionId, StringComparer.Ordinal)
            .Take(QueryLimits.Normalize(take, QueryLimits.DefaultInteractionTake))
            .ToList();

        return Task.FromResult<IReadOnlyList<Interaction>>(matches);
    }

    public Task<IReadOnlyList<Campaign>> GetCampaignsAsync(bool activeOnly, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var today = Today();
        var matches = store.Campaigns
            .Where(c => !activeOnly || (c.StartDate <= today && c.EndDate >= today))
            .OrderBy(c => c.StartDate)
            .ThenBy(c => c.CampaignId, StringComparer.Ordinal)
            .ToList();

        return Task.FromResult<IReadOnlyList<Campaign>>(matches);
    }

    public Task<IReadOnlyList<Product>> SearchProductsAsync(string keyword, string? type, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var matches = store.Products
            .Where(p => string.IsNullOrWhiteSpace(type)
                || string.Equals(p.Type, type, StringComparison.OrdinalIgnoreCase))
            .Where(p => VietnameseText.ContainsIgnoringDiacritics(p.Name, keyword)
                || VietnameseText.ContainsIgnoringDiacritics(p.Description, keyword)
                || VietnameseText.ContainsIgnoringDiacritics(p.ProductCode, keyword))
            .OrderBy(p => p.ProductCode, StringComparer.Ordinal)
            .Take(QueryLimits.MaxTake)
            .ToList();

        return Task.FromResult<IReadOnlyList<Product>>(matches);
    }

    /// <summary>
    /// "Hôm nay" theo giờ Việt Nam (UTC+7). Dùng offset cố định thay vì tra tên múi giờ
    /// để kết quả không phụ thuộc vào tzdata của máy chạy.
    /// </summary>
    private DateOnly Today() =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).Date);
}
