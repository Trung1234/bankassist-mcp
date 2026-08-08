using System.Globalization;
using System.Text;

namespace BankAssist.Mcp.Data;

/// <summary>
/// Tiện ích văn bản tiếng Việt. RM thường gõ không dấu khi tìm kiếm nhanh,
/// nên mọi so khớp từ khoá đều đi qua <see cref="RemoveDiacritics"/> ở cả hai vế.
/// </summary>
public static class VietnameseText
{
    /// <summary>
    /// Bỏ dấu và hạ chữ thường: "Tiết kiệm" và "tiet kiem" cho ra cùng một chuỗi.
    /// </summary>
    public static string RemoveDiacritics(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            // đ/Đ không phân rã được bằng Unicode nên phải xử lý tay.
            switch (ch)
            {
                case 'đ':
                case 'Đ':
                    builder.Append('d');
                    continue;
            }

            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>So khớp "chứa", bỏ qua dấu và hoa thường.</summary>
    public static bool ContainsIgnoringDiacritics(string? haystack, string? needle)
    {
        var normalizedNeedle = RemoveDiacritics(needle);
        return normalizedNeedle.Length == 0
            || RemoveDiacritics(haystack).Contains(normalizedNeedle, StringComparison.Ordinal);
    }
}
