using System.Text.RegularExpressions;
using BankAssist.Mcp.Data;
using TasmanianDevil.Analyzer;

namespace BankAssist.Mcp.Masking.FreeText;

/// <summary>
/// CMND cũ 9 số. SPEC mục 4.1 xếp mẫu này "dễ nhầm, đặt ngưỡng điểm thấp": chín chữ số
/// cũng chính là hình dạng của một số tiền (500000000 = 500 triệu), và TEST-SPEC F-05
/// đòi số tiền phải giữ nguyên. Vì vậy điểm nền nằm DƯỚI ngưỡng, chỉ được nâng lên khi
/// gần đó có từ khoá nói rõ đang nhắc tới giấy tờ.
/// </summary>
/// <remarks>Xem BACKLOG mục "Cần quyết định" D-4 — có giữ mẫu này lâu dài hay không vẫn đang chờ.</remarks>
public sealed partial class VnCmndRecognizer() : EntityRecognizer([PiiEntityTypes.Id], name: nameof(VnCmndRecognizer))
{
    /// <summary>Dưới ngưỡng của analyzer: một mình con số không đủ để bị che.</summary>
    private const double BareScore = 0.25;

    private const double ScoreWithContext = 0.75;

    /// <summary>Số ký tự nhìn sang hai bên để tìm từ khoá.</summary>
    private const int ContextWindow = 40;

    /// <summary>Từ khoá đã bỏ dấu, vì RM hay gõ không dấu.</summary>
    private static readonly string[] ContextKeywords =
    [
        "cmnd", "cmt", "chung minh nhan dan", "chung minh thu", "giay to tuy than",
    ];

    [GeneratedRegex(@"(?<!\d)\d{9}(?!\d)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex Cmnd();

    public override IReadOnlyList<RecognizerResult> Analyze(string text, IReadOnlyList<string>? entities)
    {
        var results = new List<RecognizerResult>();

        foreach (var match in Cmnd().EnumerateMatches(text))
        {
            var score = HasNearbyKeyword(text, match.Index, match.Length) ? ScoreWithContext : BareScore;

            results.Add(new RecognizerResult(
                PiiEntityTypes.Id,
                match.Index,
                match.Index + match.Length,
                score));
        }

        return results;
    }

    private static bool HasNearbyKeyword(string text, int start, int length)
    {
        var from = Math.Max(0, start - ContextWindow);
        var to = Math.Min(text.Length, start + length + ContextWindow);
        var window = VietnameseText.RemoveDiacritics(text[from..to]);

        return ContextKeywords.Any(keyword => window.Contains(keyword, StringComparison.Ordinal));
    }
}
