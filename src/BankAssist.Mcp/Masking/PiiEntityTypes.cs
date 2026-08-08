namespace BankAssist.Mcp.Masking;

/// <summary>
/// Tiền tố token theo SPEC mục 4.1. Token có dạng <c>[TYPE_n]</c>.
/// Đây là chuỗi đi vào ngữ cảnh của LLM nên coi như một phần giao diện công khai:
/// đổi tên ở đây là đổi hợp đồng với host.
/// </summary>
public static class PiiEntityTypes
{
    public const string Person = "PERSON";

    public const string Phone = "PHONE";

    public const string Id = "ID";

    public const string Account = "ACCOUNT";

    public const string Email = "EMAIL";

    public const string Card = "CARD";

    public const string Ip = "IP";

    public static string Token(string entityType, int ordinal) => $"[{entityType}_{ordinal}]";
}
