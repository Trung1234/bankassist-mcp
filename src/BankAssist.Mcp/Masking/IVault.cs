namespace BankAssist.Mcp.Masking;

/// <summary>
/// Kho ánh xạ token ↔ giá trị thật, sống trong bộ nhớ theo phiên.
/// KHÔNG được đăng ký bất kỳ phương thức nào ở đây làm MCP tool.
/// </summary>
public interface IVault
{
    /// <summary>
    /// Trả token đại diện cho <paramref name="realValue"/>. Cùng giá trị trong cùng phiên
    /// luôn nhận cùng token — bất biến quan trọng nhất của lớp masking (SPEC mục 4.2).
    /// </summary>
    string Tokenize(string sessionId, string realValue, string entityType);

    /// <summary>
    /// Đổi token về giá trị thật. Token lạ hoặc thuộc phiên khác thì bị bỏ qua,
    /// không ném exception và không lộ thông tin phiên khác.
    /// </summary>
    IReadOnlyDictionary<string, string> Resolve(string sessionId, IEnumerable<string> tokens);

    void Drop(string sessionId);
}
