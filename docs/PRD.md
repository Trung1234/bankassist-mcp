# PRD — Tài liệu Yêu cầu Sản phẩm

**Sản phẩm:** BankAssist CRM MCP Server
**Phiên bản:** 1.0 · MVP

---

## 1. Người dùng của sản phẩm này

Lưu ý quan trọng: người dùng trực tiếp của MCP server **không phải RM**, mà là **ứng dụng AI agent**. RM là người dùng cuối, ở phía sau host.

| Vai | Là ai | Cần gì từ server |
|---|---|---|
| Host application | Ứng dụng chat của ngân hàng | Tool ổn định, kết quả đã che, cửa hoàn nguyên |
| AI agent | Vòng lặp LLM trong host | Mô tả tool rõ ràng để chọn đúng tool |
| Kỹ sư vận hành | Đội CNTT | Health check, nhật ký, cấu hình đơn giản |
| Kiểm toán viên | Bộ phận tuân thủ | Nhật ký đầy đủ, chứng minh không rò rỉ |

## 2. Danh mục tool

| Tên tool | Mô tả | Tham số | Trả về |
|---|---|---|---|
| `get_customer` | Hồ sơ một khách hàng | `customerId` | 1 CustomerDto đã che |
| `search_customers` | Tìm khách theo tiêu chí | `segment?`, `rmId?`, `take?` | Danh sách CustomerDto đã che |
| `get_opportunities` | Cơ hội bán hàng | `customerId?`, `maturingWithinDays?`, `stage?` | Danh sách OpportunityDto |
| `get_interactions` | Lịch sử tương tác | `customerId`, `take?` | Danh sách InteractionDto |
| `get_campaigns` | Chiến dịch đang chạy | `activeOnly?` | Danh sách CampaignDto |
| `search_products` | Tra cứu sản phẩm | `keyword`, `type?` | Danh sách ProductDto |

`search_products` là tool duy nhất không chứa dữ liệu cá nhân — dữ liệu sản phẩm không phải PII.

## 3. User story và tiêu chí chấp nhận

### US-01 — Tra cứu hồ sơ khách hàng

> Là một AI agent, tôi cần lấy hồ sơ một khách hàng theo mã, để trả lời câu hỏi của RM về khách đó.

**Tiêu chí chấp nhận**

- Cho mã khách tồn tại, tool trả về đủ trường: mã, tên, điện thoại, CCCD, số tài khoản, phân khúc, mã RM phụ trách.
- Trường `fullName` trả về dạng `[PERSON_n]`, không phải tên thật.
- Trường `phone`, `cccd`, `accountNo` cũng ở dạng token.
- Trường `segment`, `customerId`, `assignedRmId` giữ nguyên vì không phải dữ liệu cá nhân.
- Cho mã khách không tồn tại, tool trả về kết quả rỗng kèm thông báo rõ ràng, **không ném exception**.

### US-02 — Tìm khách theo tiêu chí

> Là một AI agent, tôi cần lọc danh sách khách theo phân khúc hoặc theo RM phụ trách.

**Tiêu chí chấp nhận**

- Không truyền tiêu chí nào thì trả về tối đa 20 bản ghi.
- `take` tối đa 100; vượt quá thì tự cắt về 100.
- Mỗi khách trong danh sách nhận token riêng, không trùng nhau.
- Cùng một khách xuất hiện ở hai tool khác nhau trong cùng phiên thì nhận **cùng một token**.

### US-03 — Tra cứu cơ hội đáo hạn

> Là một AI agent, tôi cần biết khách nào có sổ tiết kiệm sắp đáo hạn.

**Tiêu chí chấp nhận**

- `maturingWithinDays = 7` trả về cơ hội có ngày đáo hạn từ hôm nay tới 7 ngày sau.
- Kết quả sắp xếp theo ngày đáo hạn tăng dần.
- Số tiền `amount` **giữ nguyên**, không che — số tiền không phải dữ liệu định danh cá nhân và agent cần nó để soạn nội dung.
- Tên khách trong kết quả ở dạng token.

### US-04 — Xem lịch sử tương tác

> Là một AI agent, tôi cần đọc các lần tiếp xúc gần nhất với một khách hàng.

**Tiêu chí chấp nhận**

- Mặc định trả 10 bản ghi gần nhất, sắp xếp mới nhất trước.
- Trường `note` là văn bản tự do do RM nhập, **phải quét PII toàn văn** chứ không chỉ thay theo trường.
- Nếu ghi chú có số điện thoại người thân, số này cũng phải bị che.

### US-05 — Hoàn nguyên token

> Là ứng dụng host, tôi cần đổi token về giá trị thật để hiển thị cho RM và soạn email.

**Tiêu chí chấp nhận**

- `POST /vault/resolve` nhận `sessionId` và danh sách token, trả về ánh xạ.
- Thiếu hoặc sai header `X-Api-Key` thì trả 401.
- Token không tồn tại trong phiên thì bỏ qua, không lộ thông tin phiên khác.
- Endpoint này **không xuất hiện** trong kết quả `tools/list`.

### US-06 — Ghi nhật ký truy cập

> Là kiểm toán viên, tôi cần biết ai đã truy cập dữ liệu khách nào và lúc nào.

**Tiêu chí chấp nhận**

- Mỗi lần gọi tool sinh một dòng JSON trong `logs/audit-{yyyyMMdd}.jsonl`.
- Dòng ghi gồm: thời điểm, mã phiên, mã RM, tên tool, tham số đã che, số bản ghi trả về, số thực thể đã che theo loại, thời gian xử lý.
- **Không ghi giá trị chưa che**, kể cả trong trường tham số.
- File chỉ ghi thêm, không sửa, không xoá.

### US-07 — Chặn khi phát hiện sót

> Là bộ phận tuân thủ, tôi cần chắc chắn không có dữ liệu thô lọt ra ngoài dù masking có lỗi.

**Tiêu chí chấp nhận**

- Trước khi trả kết quả, server quét lần hai bản đã che.
- Nếu còn khớp mẫu điện thoại, CCCD, thẻ hoặc email thì **không trả dữ liệu**, trả thông báo lỗi trung tính.
- Sự kiện này ghi vào nhật ký với mức `SecurityAlert`.
- Server **không tự thử lại** và không trả bản chưa che trong bất kỳ trường hợp nào.

## 4. Yêu cầu phi chức năng

| Mã | Yêu cầu | Ngưỡng |
|---|---|---|
| NFR-1 | Thời gian phản hồi mỗi tool | p95 < 300 ms với 1.000 bản ghi JSON |
| NFR-2 | Chi phí masking | < 50 ms cho một bản ghi khách hàng |
| NFR-3 | Bộ nhớ Vault | < 5 MB cho 100 phiên đồng thời |
| NFR-4 | Vòng đời Vault | TTL 30 phút kể từ lần truy cập cuối |
| NFR-5 | Khởi động | Server sẵn sàng trong < 3 giây |
| NFR-6 | Nền tảng | Chạy được trên macOS arm64 và Linux x64 |

## 5. Ngoài phạm vi phiên bản này

Ghi lại để nhóm không tự ý mở rộng:

- Tool ghi hoặc sửa dữ liệu
- Phân trang bằng cursor
- Streaming kết quả
- Nhiều ngôn ngữ
- Nhận diện tên người bằng mô hình học máy
- Xác thực OAuth cho MCP endpoint

## 6. Câu hỏi còn mở

| # | Câu hỏi | Cần ai quyết |
|---|---|---|
| Q-1 | Số tiền có được coi là dữ liệu nhạy cảm khi đi kèm token khách hàng không? | Bộ phận Tuân thủ |
| Q-2 | Vault nên nằm trong bộ nhớ hay Redis khi lên nhiều instance? | Kiến trúc |
| Q-3 | Có cần che địa chỉ khách hàng không? | Bộ phận Tuân thủ |

Chưa có câu trả lời thì làm theo phương án an toàn hơn và ghi chú lại trong code.
