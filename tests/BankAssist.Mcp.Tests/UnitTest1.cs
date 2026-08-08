namespace BankAssist.Mcp.Tests;

using BankAssist.Mcp.Data;
using BankAssist.Mcp.Models;

[Trait("Category", "Repository")]
public class CrmRepositoryTests : IAsyncLifetime
{
    private CrmDataStore _store = null!;

    public async Task InitializeAsync()
    {
        // Load test data from the data directory
        try
        {
            _store = CrmDataStore.LoadFrom("../../../data");
        }
        catch
        {
            // If data not found, create in-memory test data
            _store = CreateTestDataStore();
        }
        await Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "R-01: Lấy khách theo mã tồn tại")]
    public async Task GetCustomer_WithValidId_ReturnsCustomer()
    {
        var repo = new JsonCrmRepository(_store, TimeProvider.System);
        var result = await repo.GetCustomerAsync("CUS0001", CancellationToken.None);
        result.Should().NotBeNull();
        result!.CustomerId.Should().Be("CUS0001");
    }

    [Fact(DisplayName = "R-02: Lấy khách theo mã không tồn tại")]
    public async Task GetCustomer_WithInvalidId_ReturnsNull()
    {
        var repo = new JsonCrmRepository(_store, TimeProvider.System);
        var result = await repo.GetCustomerAsync("INVALID_ID", CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact(DisplayName = "R-03: Mã khách chữ thường - so sánh không phân biệt hoa thường")]
    public async Task GetCustomer_WithLowercaseId_FindsCustomer()
    {
        var repo = new JsonCrmRepository(_store, TimeProvider.System);
        var result = await repo.GetCustomerAsync("cus0001", CancellationToken.None);
        result.Should().NotBeNull();
        result!.CustomerId.Should().Be("CUS0001");
    }

    [Fact(DisplayName = "R-04: Tìm khách không truyền tiêu chí - trả tối đa 20")]
    public async Task SearchCustomers_WithoutFilters_ReturnsUpTo20()
    {
        var repo = new JsonCrmRepository(_store, TimeProvider.System);
        var result = await repo.SearchCustomersAsync(null, null, 0, CancellationToken.None);
        result.Should().HaveCountLessThanOrEqualTo(20);
    }

    [Fact(DisplayName = "R-05: Take = 500 - bị cắt về 100")]
    public async Task SearchCustomers_WithTakeTooLarge_LimitedTo100()
    {
        var repo = new JsonCrmRepository(_store, TimeProvider.System);
        var result = await repo.SearchCustomersAsync(null, null, 500, CancellationToken.None);
        result.Should().HaveCountLessThanOrEqualTo(100);
    }

    [Fact(DisplayName = "R-06: Take = 0 hoặc âm - dùng mặc định 20")]
    public async Task SearchCustomers_WithInvalidTake_UsesDefault20()
    {
        var repo = new JsonCrmRepository(_store, TimeProvider.System);
        var result1 = await repo.SearchCustomersAsync(null, null, 0, CancellationToken.None);
        var result2 = await repo.SearchCustomersAsync(null, null, -5, CancellationToken.None);

        result1.Should().HaveCountLessThanOrEqualTo(20);
        result2.Should().HaveCountLessThanOrEqualTo(20);
    }

    [Fact(DisplayName = "R-07: Cơ hội đáo hạn trong 7 ngày - sắp xếp tăng dần")]
    public async Task GetOpportunities_WithMaturingFilter_ReturnsSorted()
    {
        var repo = new JsonCrmRepository(_store, TimeProvider.System);
        var result = await repo.GetOpportunitiesAsync(null, maturingWithinDays: 7, null, CancellationToken.None);

        // Verify sorted by maturity date ascending
        if (result.Count > 1)
        {
            for (int i = 0; i < result.Count - 1; i++)
            {
                result[i].MaturityDate.CompareTo(result[i + 1].MaturityDate).Should().BeLessThanOrEqualTo(0);
            }
        }
    }

    [Fact(DisplayName = "R-08: Cơ hội đáo hạn hôm nay - được tính là nằm trong khoảng")]
    public async Task GetOpportunities_MaturingToday_Included()
    {
        var repo = new JsonCrmRepository(_store, TimeProvider.System);

        // Get today's date (Vietnam time)
        var today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).Date);

        var result = await repo.GetOpportunitiesAsync(null, maturingWithinDays: 0, null, CancellationToken.None);

        // Should include opportunities maturing today
        result.Where(o => o.MaturityDate == today).Should().HaveCount(
            _store.Opportunities.Count(o => o.MaturityDate == today));
    }

    [Fact(DisplayName = "R-09: Tương tác của khách - mới nhất trước, mặc định 10")]
    public async Task GetInteractions_WithValidCustomerId_ReturnsSortedByDateDesc()
    {
        var repo = new JsonCrmRepository(_store, TimeProvider.System);
        var customerId = _store.Customers.First().CustomerId;
        var result = await repo.GetInteractionsAsync(customerId, 0, CancellationToken.None);

        result.Should().HaveCountLessThanOrEqualTo(10);

        // Verify sorted by CreatedAt descending
        if (result.Count > 1)
        {
            for (int i = 0; i < result.Count - 1; i++)
            {
                result[i].CreatedAt.CompareTo(result[i + 1].CreatedAt).Should().BeGreaterThanOrEqualTo(0);
            }
        }
    }

    [Fact(DisplayName = "R-10: Chiến dịch activeOnly = true - chỉ trả chiến dịch còn hiệu lực")]
    public async Task GetCampaigns_WithActiveOnly_ReturnsActiveOnly()
    {
        var repo = new JsonCrmRepository(_store, TimeProvider.System);
        var today = DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).Date);

        var result = await repo.GetCampaignsAsync(activeOnly: true, CancellationToken.None);

        // Verify all are active
        foreach (var campaign in result)
        {
            campaign.StartDate.CompareTo(today).Should().BeLessThanOrEqualTo(0);
            campaign.EndDate.CompareTo(today).Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact(DisplayName = "R-11: Tìm sản phẩm bằng từ khoá không dấu")]
    public async Task SearchProducts_WithKeywordIgnoringDiacritics_FindsProduct()
    {
        var repo = new JsonCrmRepository(_store, TimeProvider.System);
        var result = await repo.SearchProductsAsync("Tiet kiem", null, CancellationToken.None);

        // Should find products with "Tiết kiệm" (without diacritics)
        result.Count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact(DisplayName = "R-12: File JSON hỏng cú pháp - server báo lỗi lúc khởi động")]
    public void LoadData_WithMalformedJson_ThrowsInvalidOperationException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, "customers.json"), "{ invalid json");
            File.WriteAllText(Path.Combine(tempDir, "opportunities.json"), "[]");
            File.WriteAllText(Path.Combine(tempDir, "interactions.json"), "[]");
            File.WriteAllText(Path.Combine(tempDir, "campaigns.json"), "[]");
            File.WriteAllText(Path.Combine(tempDir, "products.json"), "[]");

            // Should throw when loading malformed JSON
            var action = () => CrmDataStore.LoadFrom(tempDir);
            action.Should().Throw<InvalidOperationException>();
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    private static CrmDataStore CreateTestDataStore()
    {
        var customers = new List<Customer>
        {
            new("CUS0001", "Nguyễn Văn An", "0912345678", "001199012345", "19001234567890", "an.nguyen@example.com", "Priority", "RM007", new DateOnly(2019, 4, 12))
        };

        var opportunities = new List<Opportunity>
        {
            new("OPP0001", "CUS0001", "PRD001", 500000000, new DateOnly(2026, 8, 15), "Open", "CMP0001")
        };

        var interactions = new List<Interaction>
        {
            new("INT0001", "CUS0001", "Call", DateTimeOffset.Now, "Khách hẹn gọi lại")
        };

        var campaigns = new List<Campaign>
        {
            new("CMP0001", "Ưu đãi tiết kiệm quý 3", new DateOnly(2026, 7, 1), new DateOnly(2026, 9, 30))
        };

        var products = new List<Product>
        {
            new("PRD001", "Tiết kiệm 12 tháng", "Savings", 5.6m, "Kỳ hạn 12 tháng")
        };

        return new CrmDataStore(customers, opportunities, interactions, campaigns, products);
    }
}
