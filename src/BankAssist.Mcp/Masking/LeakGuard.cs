using System.Text.RegularExpressions;
using BankAssist.Mcp.Masking.FreeText;
using TasmanianDevil;
using TasmanianDevil.Analyzer;

namespace BankAssist.Mcp.Masking;

/// <summary>
/// Quét lần hai trên chuỗi JSON đã serialize. Nếu tìm thấy PII chưa che,
/// dừng lại và trả về lỗi. Đây là lưới an toàn cuối (SPEC mục 5).
/// </summary>
public sealed partial class LeakGuard(ILogger<LeakGuard> logger)
{
    private readonly EntityRecognizer[] _recognizers =
    [
        new VnPhoneRecognizer(),
        new VnCccdRecognizer(),
        new VnCmndRecognizer(),
    ];

    public GuardResult Check(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return new GuardResult(true, null);
        }

        foreach (var recognizer in _recognizers)
        {
            try
            {
                var results = recognizer.Analyze(jsonContent, []);
                if (results.Count > 0)
                {
                    return new GuardResult(false, MapRecognizerToKind(recognizer.GetType().Name));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi quét LeakGuard bằng {RecognizerType}.", recognizer.GetType().Name);
            }
        }

        return new GuardResult(true, null);
    }

    private string? MapRecognizerToKind(string recognizerTypeName) =>
        recognizerTypeName switch
        {
            nameof(VnPhoneRecognizer) => PiiEntityTypes.Phone,
            nameof(VnCccdRecognizer) => PiiEntityTypes.Id,
            nameof(VnCmndRecognizer) => PiiEntityTypes.Id,
            _ => "UNKNOWN",
        };
}

/// <summary>Kết quả kiểm tra của LeakGuard.</summary>
public sealed record GuardResult(
    bool IsClean,
    string? DetectedKind);
