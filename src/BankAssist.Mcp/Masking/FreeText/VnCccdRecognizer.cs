using System.Text.RegularExpressions;
using TasmanianDevil.Analyzer;

namespace BankAssist.Mcp.Masking.FreeText;

/// <summary>
/// Căn cước công dân 12 số (SPEC mục 4.1). Quét sau số thẻ để mẫu dài giành phần trước.
/// </summary>
public sealed partial class VnCccdRecognizer() : EntityRecognizer([PiiEntityTypes.Id], name: nameof(VnCccdRecognizer))
{
    private const double Score = 0.8;

    [GeneratedRegex(@"(?<!\d)\d{12}(?!\d)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex Cccd();

    public override IReadOnlyList<RecognizerResult> Analyze(string text, IReadOnlyList<string>? entities)
    {
        var results = new List<RecognizerResult>();

        foreach (var match in Cccd().EnumerateMatches(text))
        {
            results.Add(new RecognizerResult(
                PiiEntityTypes.Id,
                match.Index,
                match.Index + match.Length,
                Score));
        }

        return results;
    }
}
