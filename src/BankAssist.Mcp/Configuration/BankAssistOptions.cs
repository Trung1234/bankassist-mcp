namespace BankAssist.Mcp.Configuration;

public sealed class VaultOptions
{
    public const string SectionName = "Vault";

    public int TtlMinutes { get; set; } = 30;

    public int MaxEntriesPerSession { get; set; } = 5000;
}

public sealed class MaskingOptions
{
    public const string SectionName = "Masking";

    /// <summary>
    /// Tên trường văn bản tự do cần quét toàn văn. So khớp không phân biệt hoa thường.
    /// </summary>
    public IList<string> ScanFreeTextFields { get; set; } = ["note", "description"];
}

public sealed class DataOptions
{
    public const string SectionName = "Data";

    public string Directory { get; set; } = "data";
}

public sealed class AuditOptions
{
    public const string SectionName = "Audit";

    public string Directory { get; set; } = "logs";
}

/// <summary>
/// Bí mật đọc từ biến môi trường, không bao giờ từ file có commit.
/// </summary>
public sealed class SecretOptions
{
    public const string AesKeyVariable = "BANKASSIST_AES_KEY";
    public const string ApiKeyVariable = "BANKASSIST_API_KEY";

    public required byte[] AesKey { get; init; }

    public required string ApiKey { get; init; }
}
