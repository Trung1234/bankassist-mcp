using System.Text.Json;
using BankAssist.Mcp.Models;

namespace BankAssist.Mcp.Data;

/// <summary>
/// Toàn bộ dữ liệu CRM nạp một lần lúc khởi động. Bất biến sau khi dựng xong,
/// nên dùng chung cho mọi request mà không cần khoá.
/// </summary>
public sealed class CrmDataStore
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly Dictionary<string, Customer> _customersById;

    public CrmDataStore(
        IReadOnlyList<Customer> customers,
        IReadOnlyList<Opportunity> opportunities,
        IReadOnlyList<Interaction> interactions,
        IReadOnlyList<Campaign> campaigns,
        IReadOnlyList<Product> products)
    {
        Customers = customers;
        Opportunities = opportunities;
        Interactions = interactions;
        Campaigns = campaigns;
        Products = products;

        _customersById = customers.ToDictionary(c => c.CustomerId, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<Customer> Customers { get; }

    public IReadOnlyList<Opportunity> Opportunities { get; }

    public IReadOnlyList<Interaction> Interactions { get; }

    public IReadOnlyList<Campaign> Campaigns { get; }

    public IReadOnlyList<Product> Products { get; }

    /// <summary>Tra khách theo mã, không phân biệt hoa thường (TEST-SPEC R-03).</summary>
    public Customer? FindCustomer(string? customerId) =>
        !string.IsNullOrWhiteSpace(customerId) && _customersById.TryGetValue(customerId, out var customer)
            ? customer
            : null;

    /// <summary>
    /// Nạp năm file JSON từ <paramref name="directory"/>. File thiếu hoặc sai cú pháp
    /// làm hỏng khởi động ngay, kèm tên file — TEST-SPEC R-12.
    /// </summary>
    public static CrmDataStore LoadFrom(string directory)
    {
        return new CrmDataStore(
            ReadArray<Customer>(directory, "customers.json"),
            ReadArray<Opportunity>(directory, "opportunities.json"),
            ReadArray<Interaction>(directory, "interactions.json"),
            ReadArray<Campaign>(directory, "campaigns.json"),
            ReadArray<Product>(directory, "products.json"));
    }

    private static IReadOnlyList<T> ReadArray<T>(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Không tìm thấy file dữ liệu '{path}'. Sinh dữ liệu mẫu bằng: " +
                "dotnet run --project tools/BankAssist.DataGen");
        }

        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<List<T>>(stream, JsonOptions)
                ?? throw new InvalidOperationException($"File '{path}' rỗng hoặc chứa null.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"File '{path}' sai cú pháp JSON tại dòng {ex.LineNumber}: {ex.Message}", ex);
        }
    }
}
