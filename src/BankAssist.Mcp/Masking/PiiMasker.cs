using System.Reflection;
using System.Text.RegularExpressions;
using BankAssist.Mcp.Configuration;
using BankAssist.Mcp.Masking.FreeText;
using Microsoft.Extensions.Options;
using TasmanianDevil;
using TasmanianDevil.Analyzer;

namespace BankAssist.Mcp.Masking;

/// <summary>
/// Cài đặt masking: che theo trường và quét toàn văn.
/// </summary>
public sealed partial class PiiMasker(
    IVault vault,
    IOptions<MaskingOptions> options,
    ILogger<PiiMasker> logger) : IPiiMasker
{
    private readonly MaskingOptions _options = options.Value;

    private readonly EntityRecognizer[] _recognizers =
    [
        new VnPhoneRecognizer(),
        new VnCccdRecognizer(),
        new VnCmndRecognizer(),
    ];

    public string MaskField(string sessionId, string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value ?? "";
        }

        // Trường có cấu trúc: phát hiện bằng tên trường, không cần quét.
        var entityType = GetEntityTypeForField(fieldName);
        if (entityType is null)
        {
            return value;
        }

        return vault.Tokenize(sessionId, value, entityType);
    }

    public string MaskFreeText(string sessionId, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text ?? "";
        }

        var result = text;

        foreach (var recognizer in _recognizers)
        {
            try
            {
                var results = recognizer.Analyze(result, []);

                foreach (var match in results.OrderByDescending(r => r.Start))
                {
                    var matchedText = result[match.Start..match.End];
                    var entityType = MapRecognizerToEntityType(recognizer.GetType().Name);

                    if (entityType is not null)
                    {
                        var token = vault.Tokenize(sessionId, matchedText, entityType);
                        result = result.Remove(match.Start, match.End - match.Start).Insert(match.Start, token);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi quét PII bằng {RecognizerType}.", recognizer.GetType().Name);
            }
        }

        return result;
    }

    public T MaskDto<T>(string sessionId, T dto) where T : class
    {
        if (dto is null)
        {
            return dto!;
        }

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var needsCopy = false;
        var copy = (T?)Activator.CreateInstance(typeof(T));

        if (copy is null)
        {
            return dto;
        }

        foreach (var prop in properties)
        {
            if (!prop.CanRead || !prop.CanWrite)
            {
                continue;
            }

            var value = prop.GetValue(dto);
            var newValue = value;

            // Che theo trường nếu là trường PII có cấu trúc
            if (value is string strValue && GetEntityTypeForField(prop.Name) is not null)
            {
                newValue = MaskField(sessionId, strValue, prop.Name);
                needsCopy = true;
            }
            // Quét toàn văn nếu trường nằm trong danh sách quét
            else if (value is string textValue && ShouldScanFreeText(prop.Name))
            {
                newValue = MaskFreeText(sessionId, textValue);
                needsCopy = true;
            }

            if (needsCopy)
            {
                prop.SetValue(copy, newValue);
            }
            else
            {
                prop.SetValue(copy, value);
            }
        }

        return needsCopy ? copy : dto;
    }

    private bool ShouldScanFreeText(string fieldName) =>
        _options.ScanFreeTextFields.Any(f => f.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

    private string? GetEntityTypeForField(string fieldName) =>
        fieldName.ToLowerInvariant() switch
        {
            "fullname" => PiiEntityTypes.Person,
            "phone" => PiiEntityTypes.Phone,
            "cccd" => PiiEntityTypes.Id,
            "accountno" => PiiEntityTypes.Account,
            "email" => PiiEntityTypes.Email,
            _ => null,
        };

    private string? MapRecognizerToEntityType(string recognizerTypeName) =>
        recognizerTypeName switch
        {
            nameof(VnPhoneRecognizer) => PiiEntityTypes.Phone,
            nameof(VnCccdRecognizer) => PiiEntityTypes.Id,
            nameof(VnCmndRecognizer) => PiiEntityTypes.Id,
            _ => null,
        };
}
