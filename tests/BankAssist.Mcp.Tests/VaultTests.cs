namespace BankAssist.Mcp.Tests;

using BankAssist.Mcp.Configuration;
using BankAssist.Mcp.Masking;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// TEST-SPEC mục 4 — vault. Đây là nơi duy nhất trong bộ test được phép nhìn thấy
/// giá trị thật đứng sau một token.
/// </summary>
[Trait("Category", "Pii")]
public sealed class VaultTests
{
    private const string RealName = "Trần Thị Bích Ngọc";

    [Fact(DisplayName = "V-01: che rồi hoàn nguyên - trả lại đúng chuỗi gốc")]
    public void Tokenize_ThenResolve_ReturnsOriginalValue()
    {
        var vault = CreateVault();

        var token = vault.Tokenize("s-01", RealName, PiiEntityTypes.Person);
        token.Should().Be("[PERSON_1]");

        var resolved = vault.Resolve("s-01", [token]);

        resolved[token].Should().Be(RealName);
    }

    [Fact(DisplayName = "V-02: hoàn nguyên token không tồn tại - bỏ qua, không ném exception")]
    public void Resolve_WithUnknownToken_SkipsIt()
    {
        var vault = CreateVault();
        vault.Tokenize("s-01", RealName, PiiEntityTypes.Person);

        var resolved = vault.Resolve("s-01", ["[PERSON_99]"]);

        resolved.Should().BeEmpty();
    }

    [Fact(DisplayName = "V-03: hoàn nguyên token của phiên khác - không trả về")]
    public void Resolve_WithTokenFromAnotherSession_ReturnsNothing()
    {
        var vault = CreateVault();
        var token = vault.Tokenize("s-01", RealName, PiiEntityTypes.Person);

        var resolved = vault.Resolve("s-02", [token]);

        resolved.Should().BeEmpty("token phải cô lập theo phiên");
    }

    [Fact(DisplayName = "V-06: Drop(sessionId) - toàn bộ ánh xạ phiên đó biến mất")]
    public void Drop_RemovesEverySessionMapping()
    {
        var vault = CreateVault();
        var token = vault.Tokenize("s-01", RealName, PiiEntityTypes.Person);

        vault.Drop("s-01");

        vault.Resolve("s-01", [token]).Should().BeEmpty();
    }

    [Fact(DisplayName = "V-07: giá trị trong cache ở dạng đã mã hoá, không phải chuỗi rõ")]
    public void ProtectedValues_AreNeverStoredInPlainText()
    {
        var vault = CreateVault();
        vault.Tokenize("s-01", RealName, PiiEntityTypes.Person);

        var stored = vault.SnapshotProtectedValues("s-01");

        stored.Should().ContainSingle();
        System.Text.Encoding.UTF8.GetString(stored[0]).Should().NotContain(RealName);
    }

    private static MemoryVault CreateVault()
    {
        var secrets = new SecretOptions
        {
            AesKey = Convert.FromBase64String(SecretLoader.GenerateAesKeyBase64()),
            ApiKey = "test-api-key-0123456789",
        };

        return new MemoryVault(
            new MemoryCache(new MemoryCacheOptions()),
            new AesValueProtector(secrets),
            Options.Create(new VaultOptions { TtlMinutes = 30, MaxEntriesPerSession = 5000 }),
            NullLogger<MemoryVault>.Instance);
    }
}
