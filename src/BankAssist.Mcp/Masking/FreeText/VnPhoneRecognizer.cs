using System.Text.RegularExpressions;
using TasmanianDevil.Analyzer;

namespace BankAssist.Mcp.Masking.FreeText;

/// <summary>
/// Số di động Việt Nam trong văn bản tự do.
/// SPEC mục 4.1 cho mẫu liền <c>(?:\+84|0)(?:3|5|7|8|9)\d{8}</c>; TEST-SPEC mục 11 còn
/// đòi nhận dạng có dấu cách "091 234 5678", nên mẫu dưới đây cho phép dấu ngăn cách
/// giữa các chữ số. Hai đầu chặn bằng lookaround thay vì <c>\b</c> để một chuỗi số dài
/// không bị cắt ra thành số điện thoại giả.
/// </summary>
public sealed partial class VnPhoneRecognizer() : EntityRecognizer([PiiEntityTypes.Phone], name: nameof(VnPhoneRecognizer))
{
    private const double Score = 0.9;

    [GeneratedRegex(
        @"(?<!\d)(?:\+84|0)[\s.\-]?[35789](?:[\s.\-]?\d){8}(?!\d)",
        RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 200)]
    private static partial Regex VnMobile();

    public override IReadOnlyList<RecognizerResult> Analyze(string text, IReadOnlyList<string>? entities)
    {
        var results = new List<RecognizerResult>();

        foreach (var match in VnMobile().EnumerateMatches(text))
        {
            results.Add(new RecognizerResult(
                PiiEntityTypes.Phone,
                match.Index,
                match.Index + match.Length,
                Score));
        }

        return results;
    }
}
