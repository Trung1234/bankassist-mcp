# BACKLOG — Danh sách công việc

Làm theo thứ tự. Không nhảy cóc. Đánh dấu `[x]` khi xong và đạt Definition of Done trong `CLAUDE.md`.

Ký hiệu ước lượng: **S** khoảng nửa ngày · **M** một ngày · **L** hai ngày trở lên.

---

## Đợt 0 — Nền móng (chưa đụng MCP)

Mục đích: quen domain và cấu trúc trước khi học giao thức mới.

### [ ] T-01 · Khởi tạo solution — S
Tạo `BankAssist.Mcp` (ASP.NET Core Minimal API) và `BankAssist.Mcp.Tests` (xUnit). Thêm `/health` trả `200 OK`. Thiết lập `.gitignore` loại trừ `appsettings.Development.json` và `logs/`.

**Xong khi:** `dotnet run` chạy được, `curl localhost:5280/health` trả OK, `dotnet test` xanh với một test giả.

### [ ] T-02 · Model và dữ liệu mẫu — M
Định nghĩa record `Customer`, `Opportunity`, `Interaction`, `Campaign`, `Product` theo SPEC mục 3.1. Viết script sinh dữ liệu giả đủ số lượng yêu cầu.

**Xong khi:** năm file JSON tồn tại, đủ số bản ghi, có ít nhất 20 ghi chú chứa PII lẫn trong văn bản. Không có tên người thật.

### [ ] T-03 · JsonCrmRepository — M
Cài `ICrmRepository` đọc JSON, nạp một lần lúc khởi động vào bộ nhớ.

**Test:** R-01 đến R-12.

### [ ] T-04 · Nạp cấu hình và kiểm tra khoá — S
Đọc `appsettings.json` + biến môi trường. Nếu thiếu `BANKASSIST_AES_KEY` hoặc `BANKASSIST_API_KEY` thì server **từ chối khởi động** kèm thông báo rõ.

**Test:** I-05.

---

## Đợt 1 — Lớp bảo vệ (vẫn chưa đụng MCP)

Làm masking trước khi làm tool, vì tool sẽ phụ thuộc vào nó.

### [ ] T-05 · Vault — M
Cài `IVault` trên `IMemoryCache`, TTL trượt 30 phút, giới hạn 5.000 mục, giá trị mã hoá AES trong bộ nhớ.

**Test:** V-01 đến V-08.

### [ ] T-06 · Masking theo trường — M
`PiiMasker` che các trường có cấu trúc theo bảng SPEC mục 4.1. Bảo đảm bất biến: cùng giá trị trong cùng phiên cho cùng token.

**Test:** M-01 đến M-12.

### [ ] T-07 · Recognizer Việt Nam và quét toàn văn — L
Tích hợp TasmanianDevil. Thêm recognizer cho di động VN, CCCD, CMND. Áp dụng cho các trường liệt kê ở `Masking:ScanFreeTextFields`.

**Test:** F-01 đến F-12. Tạo `tests/TestData/vietnamese-pii-cases.json` theo TEST-SPEC mục 11.

**Chú ý:** ca F-05 và F-06 quan trọng ngang các ca che được — che nhầm số tiền và ngày tháng làm hỏng nội dung agent sinh ra.

### [ ] T-08 · LeakGuard — M
Quét lần hai trên chuỗi JSON đã serialize. Chặn và ghi `SecurityAlert` nếu còn sót.

**Test:** G-01 đến G-09. Ưu tiên viết G-09 trước — nó chứng minh lưới an toàn thật sự hoạt động.

### [ ] T-09 · AuditLogger — M
Ghi JSON Lines vào `logs/audit-{yyyyMMdd}.jsonl`. Che tham số trước khi ghi. An toàn khi ghi đồng thời nhiều luồng.

**Test:** A-01 đến A-06.

---

## Đợt 2 — MCP

### [x] T-10 · Dựng MCP server — M
Thêm `ModelContextProtocol.AspNetCore`, cấu hình `AddMcpServer().WithHttpTransport().WithToolsFromAssembly()`, map `/mcp`. Làm một tool `ping` tạm để kiểm tra kết nối.

**Xong khi:** MCP Inspector kết nối được và thấy tool `ping`.

### [x] T-11 · SessionContext — S
Đọc `X-Session-Id` và `X-Rm-Id` từ header, đưa vào scoped service. Thiếu session id thì tự sinh kèm cảnh báo.

**Test:** T-08.

### [x] T-12 · Tool khách hàng — M
`get_customer` và `search_customers`, đi đủ chuỗi repo → mask → guard → audit.

**Test:** T-04, T-05, US-01, US-02.

### [ ] T-13 · Tool cơ hội và tương tác — M
`get_opportunities`, `get_interactions`. Ghi chú tương tác phải qua quét toàn văn.

**Test:** T-06, US-03, US-04.

### [ ] T-14 · Tool chiến dịch và sản phẩm — S
`get_campaigns`, `search_products`. Tool sản phẩm không sinh token nào.

**Test:** T-07.

### [ ] T-15 · Gỡ tool ping — S
Xoá tool tạm. Xác nhận `tools/list` trả đúng 6 tool.

**Test:** T-01, T-02, T-03.

---

## Đợt 3 — Cửa hoàn nguyên và hoàn thiện

### [ ] T-16 · Endpoint /vault/resolve — M
REST thường, yêu cầu `X-Api-Key`. Kèm comment giải thích vì sao **không** được biến thành MCP tool.

**Test:** E-01 đến E-05. Ca E-04 phải nằm trong bộ test chạy ở CI.

### [ ] T-17 · Kiểm thử tích hợp — M
Viết I-01 đến I-05.

### [ ] T-18 · Đo hiệu năng — S
Đo P-01 đến P-05. Ghi kết quả vào `docs/PERF.md`.

### [ ] T-19 · Hoàn thiện tài liệu — S
Cập nhật README với ảnh chụp màn hình MCP Inspector. Ghi lại các quyết định đã thay đổi so với SPEC ban đầu.

---

## Cần quyết định

Ghi vào đây khi gặp điểm đặc tả chưa rõ. Không tự quyết một mình.

| # | Vấn đề | Ai quyết | Trạng thái |
|---|---|---|---|
| D-1 | Số tiền có phải che khi đi kèm token khách hàng không? | Tuân thủ | Chờ |
| D-2 | Vault chuyển sang Redis khi chạy nhiều instance? | Kiến trúc | Chờ |
| D-3 | Địa chỉ khách hàng có cần che? | Tuân thủ | Chờ |
| D-4 | CMND 9 số dễ nhầm mã nội bộ — giữ hay bỏ? | Kỹ thuật | Chờ |

---

## Hoãn lại — không làm ở MVP

Ghi ra để khỏi bị cám dỗ:

- Tool ghi hoặc sửa dữ liệu
- OAuth 2.1 cho MCP endpoint
- Phân trang cursor
- Nhận diện tên bằng mô hình ONNX
- Giao diện quản trị
- Đẩy audit sang SIEM
- Nhiều ngôn ngữ
