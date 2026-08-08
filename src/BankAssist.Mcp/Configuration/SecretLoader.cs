using System.Security.Cryptography;

namespace BankAssist.Mcp.Configuration;

/// <summary>
/// Nạp bí mật lúc khởi động. Thiếu khoá thì ném ngay để tiến trình chết —
/// SPEC mục 9: thà chết sớm còn hơn chạy với khoá mặc định.
/// </summary>
public static class SecretLoader
{
    /// <summary>AES-256 nên khoá phải đúng 32 byte sau khi giải Base64.</summary>
    private const int AesKeySizeBytes = 32;

    private const int MinApiKeyLength = 16;

    public static SecretOptions Load(IConfiguration configuration)
    {
        var rawAesKey = configuration[SecretOptions.AesKeyVariable];
        var apiKey = configuration[SecretOptions.ApiKeyVariable];

        if (string.IsNullOrWhiteSpace(rawAesKey))
        {
            throw new InvalidOperationException(
                $"Thiếu biến môi trường {SecretOptions.AesKeyVariable}. " +
                $"Đặt một khoá AES-256 dạng Base64 ({AesKeySizeBytes} byte) rồi chạy lại. " +
                "Sinh nhanh: dotnet run --project tools/BankAssist.DataGen -- newkey");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Thiếu biến môi trường {SecretOptions.ApiKeyVariable}. " +
                "Đây là khoá mà host dùng để gọi /vault/resolve.");
        }

        if (apiKey.Length < MinApiKeyLength)
        {
            throw new InvalidOperationException(
                $"{SecretOptions.ApiKeyVariable} quá ngắn, cần tối thiểu {MinApiKeyLength} ký tự.");
        }

        byte[] aesKey;
        try
        {
            aesKey = Convert.FromBase64String(rawAesKey);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"{SecretOptions.AesKeyVariable} không phải chuỗi Base64 hợp lệ.", ex);
        }

        if (aesKey.Length != AesKeySizeBytes)
        {
            throw new InvalidOperationException(
                $"{SecretOptions.AesKeyVariable} phải giải Base64 ra đúng {AesKeySizeBytes} byte, " +
                $"hiện có {aesKey.Length} byte.");
        }

        return new SecretOptions { AesKey = aesKey, ApiKey = apiKey };
    }

    public static string GenerateAesKeyBase64() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(AesKeySizeBytes));
}
