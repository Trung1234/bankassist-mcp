# SPEC — Đặc tả Kỹ thuật

**Đọc file này trước khi viết bất kỳ dòng code nào.**

---

## 1. Ngăn xếp công nghệ

| Thành phần | Lựa chọn | Ghi chú |
|---|---|---|
| Runtime | .NET 10 | macOS arm64 |
| Ngôn ngữ | C# 13, nullable enabled | |
| Web | ASP.NET Core Minimal API | |
| MCP | `ModelContextProtocol.AspNetCore` | Ghim version cụ thể sau khi restore |
| Masking | `TasmanianDevil` + recognizer VN tự viết | Offline, không gọi mạng |
| Nguồn dữ liệu | File JSON trong `data/` | Nạp một lần lúc khởi động |
| Nhật ký | File JSON Lines | Không dùng database cho MVP |
| Test | xUnit + FluentAssertions | |

Không thêm package nào ngoài danh sách trên mà chưa hỏi.

## 2. Kiến trúc

```
┌──────────────── BankAssist.Mcp (một tiến trình) ───────────────┐
│                                                                 │
│  /mcp  ──> McpServer ──> Tools ──> ICrmRepository ──> data/*.json│
│                            │                                     │
│                            v                                     │
│                        PiiMasker ──> IVault (in-memory)          │
│                            │                                     │
│                            v                                     │
│                        LeakGuard ──> chặn nếu còn sót            │
│                            │                                     │
│                            v                                     │
│                        AuditLogger ──> logs/audit-*.jsonl        │
│                                                                  │
│  /vault/resolve ──> IVault   (REST thường, KHÔNG phải MCP tool)  │
│  /health                                                         │
└──────────────────────────────────────────────────────────────────┘
```

Mọi tool đều đi qua đúng chuỗi: **Repository → Masker → LeakGuard → Audit → trả về**. Không tool nào được bỏ qua một mắt xích.

## 3. Mô hình dữ liệu

### 3.1 File JSON

Đặt trong `data/`, nạp lúc khởi động vào bộ nhớ.

`customers.json`
```json
[
  {
    "customerId": "CUS0001",
    "fullName": "Nguyễn Văn An",
    "phone": "0912345678",
    "cccd": "001199012345",
    "accountNo": "19001234567890",
    "email": "an.nguyen@example.com",
    "segment": "Priority",
    "assignedRmId": "RM007",
    "openedDate": "2019-04-12"
  }
]
```

`opportunities.json`
```json
[
  {
    "oppId": "OPP0001",
    "customerId": "CUS0001",
    "productCode": "SAV12M",
    "amount": 500000000,
    "maturityDate": "2026-08-15",
    "stage": "Open",
    "campaignId": "CMP0002"
  }
]
```

`interactions.json`
```json
[
  {
    "interactionId": "INT0001",
    "customerId": "CUS0001",
    "channel": "Call",
    "createdAt": "2026-07-28T09:12:00+07:00",
    "note": "Khách hẹn gọi lại số vợ 0987654321 sau 17h"
  }
]
```

`campaigns.json`
```json
[
  {
    "campaignId": "CMP0002",
    "name": "Ưu đãi tiết kiệm quý 3",
    "startDate": "2026-07-01",
    "endDate": "2026-09-30"
  }
]
```

`products.json`
```json
[
  {
    "productCode": "SAV12M",
    "name": "Tiết kiệm có kỳ hạn 12 tháng",
    "type": "Savings",
    "interestRate": 5.6,
    "description": "Kỳ hạn 12 tháng, lãi cuối kỳ, tối thiểu 10 triệu đồng"
  }
]
```

**Yêu cầu dữ liệu mẫu:** tối thiểu 200 khách, 300 cơ hội, 800 tương tác, 5 chiến dịch, 35 sản phẩm. Sinh bằng script, không dùng tên người có thật. Trong `interactions.json` phải cố ý gài ít nhất 20 ghi chú có chứa số điện thoại, CCCD hoặc email lẫn trong văn bản — đây là dữ liệu để kiểm thử lớp quét toàn văn.

### 3.2 Tầng truy cập dữ liệu

```csharp
public interface ICrmRepository
{
    Task<Customer?> GetCustomerAsync(string customerId, CancellationToken ct);
    Task<IReadOnlyList<Customer>> SearchCustomersAsync(string? segment, string? rmId, int take, CancellationToken ct);
    Task<IReadOnlyList<Opportunity>> GetOpportunitiesAsync(string? customerId, int? maturingWithinDays, string? stage, CancellationToken ct);
    Task<IReadOnlyList<Interaction>> GetInteractionsAsync(string customerId, int take, CancellationToken ct);
    Task<IReadOnlyList<Campaign>> GetCampaignsAsync(bool activeOnly, CancellationToken ct);
    Task<IReadOnlyList<Product>> SearchProductsAsync(string keyword, string? type, CancellationToken ct);
}
```

MVP cài đặt `JsonCrmRepository`. Giai đoạn sau thêm `SqlCrmRepository` mà **không sửa một dòng nào trong `Tools/`** — đây là tiêu chí kiểm tra thiết kế có đúng hay không.

## 4. Lớp masking

### 4.1 Hai nguồn phát hiện

**Nguồn 1 — theo trường (entity-driven).** Với dữ liệu đến từ record CRM, ta biết chính xác trường nào là PII. Không cần đoán.

| Thực thể | Trường nguồn | Tiền tố token |
|---|---|---|
| Họ tên | `fullName` | `PERSON` |
| Điện thoại | `phone` | `PHONE` |
| CCCD | `cccd` | `ID` |
| Số tài khoản | `accountNo` | `ACCOUNT` |
| Email | `email` | `EMAIL` |

**Nguồn 2 — quét toàn văn.** Áp dụng cho trường văn bản tự do, hiện chỉ có `Interaction.note`. Dùng TasmanianDevil cho email, thẻ, IP; cộng thêm recognizer VN tự viết:

| Mẫu | Biểu thức | Ghi chú |
|---|---|---|
| Di động VN | `(?:\+84\|0)(?:3\|5\|7\|8\|9)\d{8}\b` | |
| CCCD | `\b\d{12}\b` | Quét sau số thẻ |
| CMND cũ | `\b\d{9}\b` | Dễ nhầm, đặt ngưỡng điểm thấp |

**Không viết regex chung cho số tài khoản.** Nó sẽ nuốt cả số tiền. Số tài khoản chỉ lấy từ nguồn 1.

Thứ tự quét: thẻ → email → điện thoại → CCCD → CMND. Mẫu dài trước, tránh bị mẫu ngắn cắt mất.

### 4.2 Token

Định dạng `[TYPE_n]`, `n` đếm từ 1 trong phạm vi một phiên.

Quy tắc bất biến: **cùng một giá trị thật trong cùng một phiên luôn nhận cùng một token.** Nếu `get_customer` trả `[PERSON_1]` cho khách CUS0001 thì `get_opportunities` cũng phải trả `[PERSON_1]` cho khách đó. Không giữ được điều này, agent sẽ tưởng là hai người khác nhau.

### 4.3 Vault

```csharp
public interface IVault
{
    string Tokenize(string sessionId, string realValue, string entityType);
    IReadOnlyDictionary<string, string> Resolve(string sessionId, IEnumerable<string> tokens);
    void Drop(string sessionId);
}
```

- Cài đặt MVP: `IMemoryCache`, khoá theo `sessionId`.
- TTL trượt 30 phút.
- Giới hạn 5.000 mục mỗi phiên; vượt thì ghi cảnh báo và từ chối thêm.
- Giá trị lưu trong bộ nhớ **được mã hoá AES**, khoá đọc từ cấu hình. Lý do: dump bộ nhớ tiến trình không được lộ danh sách khách hàng.

### 4.4 Xác định phiên

Host truyền `X-Session-Id` và `X-Rm-Id` trong header của mỗi request MCP. Thiếu `X-Session-Id` thì server tự sinh một id theo kết nối, nhưng ghi cảnh báo — vì host không truyền thì không hoàn nguyên được.

## 5. LeakGuard

Chạy trên **chuỗi JSON đã serialize** của kết quả, ngay trước khi trả về.

```csharp
public sealed record GuardResult(bool IsClean, string? DetectedKind);
```

Nếu `IsClean == false`:
1. Không trả dữ liệu.
2. Trả nội dung lỗi: `"Không thể xử lý yêu cầu này. Sự việc đã được ghi nhận."`
3. Ghi audit mức `SecurityAlert` kèm tên tool và loại phát hiện — **không kèm giá trị**.
4. Không thử lại.

Đây là lưới an toàn cuối. Nó phải bắt được cả trường hợp lập trình viên quên gọi masker.

## 6. Tool MCP

### 6.1 Khai báo

```csharp
[McpServerToolType]
public sealed class CustomerTools(
    ICrmRepository repo,
    IPiiMasker masker,
    ILeakGuard guard,
    IAuditLogger audit,
    ISessionContext session)
{
    [McpServerTool(Name = "get_customer")]
    [Description("Lấy hồ sơ khách hàng theo mã khách hàng. Trả về dữ liệu đã che thông tin cá nhân.")]
    public async Task<CustomerDto?> GetCustomerAsync(
        [Description("Mã khách hàng, ví dụ CUS0001")] string customerId,
        CancellationToken ct)
    {
        // 1. repo  2. mask  3. guard  4. audit  5. return
    }
}
```

Mô tả tool viết bằng tiếng Việt, **cụ thể và ngắn**. Agent chọn tool dựa trên chuỗi này, nên nó là một phần của thiết kế chứ không phải chú thích.

### 6.2 Đăng ký

```csharp
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

app.MapMcp("/mcp");
```

### 6.3 Xử lý lỗi

- Không tìm thấy dữ liệu: trả `null` hoặc danh sách rỗng kèm mô tả, **không ném exception**.
- Tham số sai: trả thông báo hướng dẫn giá trị hợp lệ.
- Lỗi hệ thống: log đầy đủ phía server, trả thông báo trung tính cho client.

Không bao giờ để stack trace lọt vào kết quả tool — nó có thể chứa dữ liệu.

## 7. Cửa hoàn nguyên

```
POST /vault/resolve
Header: X-Api-Key: <khoá>
Body:   { "sessionId": "...", "tokens": ["[PERSON_1]", "[PHONE_1]"] }
200:    { "resolved": { "[PERSON_1]": "Nguyễn Văn An", "[PHONE_1]": "0912345678" } }
401:    thiếu hoặc sai khoá
```

**Endpoint này không được đăng ký làm MCP tool.** Ghi rõ lý do trong comment ngay tại chỗ map endpoint, để người sau không "tiện tay" chuyển nó thành tool:

```csharp
// KHÔNG chuyển thành MCP tool. Nếu LLM gọi được endpoint này,
// toàn bộ mục đích của lớp masking mất tác dụng.
app.MapPost("/vault/resolve", ...).RequireApiKey();
```

## 8. Nhật ký audit

Một dòng JSON mỗi lời gọi, ghi vào `logs/audit-{yyyyMMdd}.jsonl`:

```json
{
  "ts": "2026-08-08T10:14:22+07:00",
  "sessionId": "s-8f2a",
  "rmId": "RM007",
  "tool": "get_customer",
  "args": { "customerId": "CUS0001" },
  "resultCount": 1,
  "maskedCounts": { "PERSON": 1, "PHONE": 1, "ID": 1, "ACCOUNT": 1 },
  "durationMs": 24,
  "level": "Info"
}
```

`args` phải đi qua masker trước khi ghi. Mã khách hàng giữ nguyên vì nó là mã định danh nội bộ, không phải dữ liệu cá nhân — nhưng nếu tham số là tên hoặc số điện thoại thì phải che.

## 9. Cấu hình

`appsettings.json` (có commit) chứa giá trị mặc định không nhạy cảm. `appsettings.Development.json` (trong `.gitignore`) chứa khoá.

```json
{
  "Vault": { "TtlMinutes": 30, "MaxEntriesPerSession": 5000 },
  "Masking": { "ScanFreeTextFields": ["note", "description"] },
  "Data": { "Directory": "data" },
  "Audit": { "Directory": "logs" }
}
```

Khoá AES và API key đọc từ biến môi trường `BANKASSIST_AES_KEY`, `BANKASSIST_API_KEY`. Không có thì server **từ chối khởi động** — thà chết sớm còn hơn chạy với khoá mặc định.

## 10. Thứ tự triển khai

Bám theo `docs/BACKLOG.md`. Nguyên tắc: bốn task đầu **không đụng tới MCP**, để nhóm quen với domain trước khi học giao thức mới.
