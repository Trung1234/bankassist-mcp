using System.ComponentModel;
using ModelContextProtocol.Server;

namespace BankAssist.Mcp.Tools;

/// <summary>
/// Tool tạm của T-10, chỉ để xác nhận host kết nối được. T-15 sẽ xoá file này
/// vì `tools/list` phải trả đúng 6 tool nghiệp vụ (TEST-SPEC T-01).
/// </summary>
[McpServerToolType]
public sealed class PingTool
{
    [McpServerTool(Name = "ping")]
    [Description("Kiểm tra kết nối tới máy chủ BankAssist. Trả về chuỗi pong.")]
    public static string Ping() => "pong";
}
