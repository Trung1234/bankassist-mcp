namespace BankAssist.Mcp.Tests;

using BankAssist.Mcp.Configuration;
using BankAssist.Mcp.Data;
using BankAssist.Mcp.Masking;
using BankAssist.Mcp.Models;
using BankAssist.Mcp.Tools;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// TEST-SPEC mục 6 — tool khách hàng. Tên trong dữ liệu dựng sẵn là tên bịa,
/// dùng làm mồi: nếu chuỗi đó xuất hiện trong kết quả tool thì masking đã hỏng.
/// </summary>
[Trait("Category", "Pii")]
public sealed class CustomerToolsTests : IDisposable
{
    private const string RealName = "Trần Thị Bích Ngọc";

    private const string RealPhone = "0912345678";

    private const string RealCccd = "001199012345";

    private const string RealAccount = "19001234567890";

    private const string RealEmail = "bich.ngoc@example.com";

    private readonly string _auditDirectory =
        Path.Combine(Path.GetTempPath(), $"bankassist-audit-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_auditDirectory))
        {
            Directory.Delete(_auditDirectory, recursive: true);
        }
    }

    [Fact(DisplayName = "T-04: get_customer với mã hợp lệ - kết quả không chứa chuỗi tên thật")]
    public async Task GetCustomer_WithValidId_LeaksNoRealValue()
    {
        var tools = CreateTools();

        var result = await tools.GetCustomerAsync("CUS0001", CancellationToken.None);

        result.Count.Should().Be(1);

        var dto = result.Items[0];
        dto.CustomerId.Should().Be("CUS0001", "mã khách là định danh nội bộ, không che");

        dto.FullName.Should().NotContain(RealName).And.StartWith("[PERSON_");
        dto.Phone.Should().NotContain(RealPhone).And.StartWith("[PHONE_");
        dto.Cccd.Should().NotContain(RealCccd).And.StartWith("[ID_");
        dto.AccountNo.Should().NotContain(RealAccount).And.StartWith("[ACCOUNT_");
        dto.Email.Should().NotContain(RealEmail).And.StartWith("[EMAIL_");
    }

    [Fact(DisplayName = "T-05: get_customer với mã không tồn tại - rỗng, có thông báo, không exception")]
    public async Task GetCustomer_WithUnknownId_ReturnsEmptyWithMessage()
    {
        var tools = CreateTools();

        var result = await tools.GetCustomerAsync("CUS9999", CancellationToken.None);

        result.Count.Should().Be(0);
        result.Items.Should().BeEmpty();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact(DisplayName = "US-02: search_customers trả danh sách đã che, cùng phiên cùng token")]
    public async Task SearchCustomers_SameSession_ReusesTokenForSameCustomer()
    {
        var tools = CreateTools();

        var single = await tools.GetCustomerAsync("CUS0001", CancellationToken.None);
        var listed = await tools.SearchCustomersAsync("Priority", null, 20, CancellationToken.None);

        var fromList = listed.Items.Single(c => c.CustomerId == "CUS0001");
        fromList.FullName.Should().Be(single.Items[0].FullName, "SPEC mục 4.2: cùng giá trị trong cùng phiên là cùng token");
    }

    [Fact(DisplayName = "A-01: mỗi lời gọi tool sinh đúng một dòng audit, không chứa PII thô")]
    public async Task GetCustomer_WritesExactlyOneCleanAuditLine()
    {
        var tools = CreateTools();

        await tools.GetCustomerAsync("CUS0001", CancellationToken.None);

        var lines = Directory.GetFiles(_auditDirectory, "audit-*.jsonl").SelectMany(File.ReadAllLines).ToList();

        lines.Should().HaveCount(1);
        lines[0].Should().Contain("get_customer").And.Contain("CUS0001");
        lines[0].Should().NotContain(RealName).And.NotContain(RealPhone).And.NotContain(RealCccd);
    }

    private CustomerTools CreateTools()
    {
        var secrets = new SecretOptions
        {
            AesKey = Convert.FromBase64String(SecretLoader.GenerateAesKeyBase64()),
            ApiKey = "test-api-key-0123456789",
        };

        var protector = new AesValueProtector(secrets);
        var vaultOptions = Options.Create(new VaultOptions { TtlMinutes = 30, MaxEntriesPerSession = 5000 });
        var vault = new MemoryVault(
            new MemoryCache(new MemoryCacheOptions()),
            protector,
            vaultOptions,
            NullLogger<MemoryVault>.Instance);

        var maskingOptions = Options.Create(new MaskingOptions { ScanFreeTextFields = ["note", "description"] });
        var masker = new PiiMasker(vault, maskingOptions, NullLogger<PiiMasker>.Instance);

        var guard = new LeakGuard(NullLogger<LeakGuard>.Instance);
        var audit = new AuditLogger(
            Options.Create(new AuditOptions { Directory = _auditDirectory }),
            NullLogger<AuditLogger>.Instance);

        var session = new SessionContext { SessionId = "test-session", RmId = "RM007" };

        return new CustomerTools(
            new JsonCrmRepository(CreateStore(), TimeProvider.System),
            masker,
            guard,
            audit,
            session,
            NullLogger<CustomerTools>.Instance);
    }

    private static CrmDataStore CreateStore()
    {
        var customers = new List<Customer>
        {
            new("CUS0001", RealName, RealPhone, RealCccd, RealAccount, RealEmail, "Priority", "RM007", new DateOnly(2019, 4, 12)),
            new("CUS0002", "Lê Hoàng Vũ", "0987654321", "001199054321", "19001234567891", "vu.le@example.com", "Priority", "RM007", new DateOnly(2020, 1, 5)),
        };

        return new CrmDataStore(customers, [], [], [], []);
    }
}
