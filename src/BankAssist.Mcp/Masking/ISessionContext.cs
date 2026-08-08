namespace BankAssist.Mcp.Masking;

/// <summary>
/// Danh tính phiên của một lời gọi tool. Token trong vault được đánh theo
/// <see cref="SessionId"/>, nên hai request khác phiên không bao giờ thấy token của nhau.
/// </summary>
public interface ISessionContext
{
    string SessionId { get; }

    string? RmId { get; }
}
