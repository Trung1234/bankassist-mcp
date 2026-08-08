namespace BankAssist.Mcp.Masking;

/// <summary>Lưới an toàn cuối, chạy trên chuỗi JSON đã serialize (SPEC mục 5).</summary>
public interface ILeakGuard
{
    GuardResult Check(string jsonContent);
}
