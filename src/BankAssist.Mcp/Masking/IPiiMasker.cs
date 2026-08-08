namespace BankAssist.Mcp.Masking;

/// <summary>
/// Che giá trị định danh cá nhân, kết nối <see cref="IVault"/>.
/// Hai nguồn phát hiện PII: theo trường (entity-driven) và quét toàn văn (free text).
/// </summary>
public interface IPiiMasker
{
    /// <summary>Che một giá trị nếu nó là trường PII có cấu trúc.</summary>
    string MaskField(string sessionId, string? value, string fieldName);

    /// <summary>Che toàn bộ văn bản bằng cách quét và thay thế các mẫu PII.</summary>
    string MaskFreeText(string sessionId, string? text);

    /// <summary>Che một đối tượng DTO, tự động quét trường trong danh sách ScanFreeTextFields.</summary>
    T MaskDto<T>(string sessionId, T dto) where T : class;
}
