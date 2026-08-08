using System.ComponentModel;
using System.Diagnostics;
using BankAssist.Mcp.Data;
using BankAssist.Mcp.Masking;
using BankAssist.Mcp.Models;
using ModelContextProtocol.Server;

namespace BankAssist.Mcp.Tools;

/// <summary>
/// Tool tra cứu khách hàng. Chuỗi bắt buộc: repo → mask → guard → audit → trả về.
/// </summary>
[McpServerToolType]
public sealed class CustomerTools(
    ICrmRepository repository,
    IPiiMasker masker,
    ILeakGuard guard,
    IAuditLogger audit,
    ISessionContext session,
    ILogger<CustomerTools> logger)
{
    [McpServerTool(Name = "get_customer")]
    [Description("Lấy hồ sơ khách hàng theo mã khách hàng. Trả về dữ liệu đã che thông tin cá nhân.")]
    public async Task<ToolResult<CustomerDto>> GetCustomerAsync(
        [Description("Mã khách hàng, ví dụ CUS0001")] string customerId,
        CancellationToken ct)
    {
        const string ToolName = "get_customer";
        var stopwatch = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(customerId))
        {
            return ToolResult<CustomerDto>.Empty("Thiếu mã khách hàng. Truyền mã dạng CUS0001.");
        }

        try
        {
            var customer = await repository.GetCustomerAsync(customerId, ct);

            var masked = customer is null
                ? (IReadOnlyList<CustomerDto>)[]
                : [Mask(customer)];

            return ToolPipeline.Complete(
                ToolName,
                masked,
                new { customerId },
                $"Không tìm thấy khách hàng có mã '{customerId}'.",
                stopwatch.ElapsedMilliseconds,
                guard,
                audit,
                session);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Chi tiết ở lại phía server: stack trace có thể chứa dữ liệu (SPEC mục 6.3).
            logger.LogError(ex, "Lỗi khi chạy tool {ToolName}.", ToolName);
            return ToolResult<CustomerDto>.Empty("Không thể xử lý yêu cầu này. Vui lòng thử lại.");
        }
    }

    [McpServerTool(Name = "search_customers")]
    [Description("Tìm khách hàng theo phân khúc hoặc theo mã RM phụ trách. Trả về dữ liệu đã che thông tin cá nhân.")]
    public async Task<ToolResult<CustomerDto>> SearchCustomersAsync(
        // Ba tham số đều phải có giá trị mặc định, nếu không JSON Schema sinh ra sẽ liệt chúng
        // vào `required` và agent buộc phải truyền đủ — trái với mô tả "bỏ trống để không lọc".
        [Description("Phân khúc khách hàng, ví dụ Priority. Bỏ trống để không lọc theo phân khúc")] string? segment = null,
        [Description("Mã RM phụ trách, ví dụ RM007. Bỏ trống để không lọc theo RM")] string? rmId = null,
        [Description("Số bản ghi tối đa, mặc định 20, tối đa 100")] int take = 20,
        CancellationToken ct = default)
    {
        const string ToolName = "search_customers";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var customers = await repository.SearchCustomersAsync(segment, rmId, take, ct);
            var masked = customers.Select(Mask).ToList();

            return ToolPipeline.Complete<CustomerDto>(
                ToolName,
                masked,
                new { segment, rmId, take },
                "Không có khách hàng nào khớp tiêu chí. Thử bỏ bớt bộ lọc.",
                stopwatch.ElapsedMilliseconds,
                guard,
                audit,
                session);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Lỗi khi chạy tool {ToolName}.", ToolName);
            return ToolResult<CustomerDto>.Empty("Không thể xử lý yêu cầu này. Vui lòng thử lại.");
        }
    }

    private CustomerDto Mask(Customer customer) => new(
        customer.CustomerId,
        masker.MaskField(session.SessionId, customer.FullName, nameof(customer.FullName)),
        masker.MaskField(session.SessionId, customer.Phone, nameof(customer.Phone)),
        masker.MaskField(session.SessionId, customer.Cccd, nameof(customer.Cccd)),
        masker.MaskField(session.SessionId, customer.AccountNo, nameof(customer.AccountNo)),
        masker.MaskField(session.SessionId, customer.Email, nameof(customer.Email)),
        customer.Segment,
        customer.AssignedRmId,
        customer.OpenedDate.ToString("yyyy-MM-dd"));
}
