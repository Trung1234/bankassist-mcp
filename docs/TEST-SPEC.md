# TEST-SPEC — Đặc tả Kiểm thử

Viết test trước, chạy cho fail, rồi mới viết code.

Đặt `[Trait("Category", "...")]` để lọc: `Repository`, `Pii`, `Vault`, `Guard`, `Tool`, `Audit`, `Integration`.

---

## 1. ICrmRepository

| Mã | Tình huống | Kỳ vọng |
|---|---|---|
| R-01 | Lấy khách theo mã tồn tại | Trả đúng bản ghi |
| R-02 | Lấy khách theo mã không tồn tại | Trả `null`, không ném exception |
| R-03 | Mã khách chữ thường `cus0001` | Vẫn tìm thấy — so sánh không phân biệt hoa thường |
| R-04 | Tìm khách không truyền tiêu chí | Trả tối đa 20 bản ghi |
| R-05 | `take = 500` | Bị cắt về 100 |
| R-06 | `take = 0` hoặc âm | Dùng mặc định 20 |
| R-07 | Cơ hội đáo hạn trong 7 ngày | Chỉ trả cơ hội trong khoảng, sắp xếp tăng dần |
| R-08 | Cơ hội đáo hạn hôm nay | Được tính là nằm trong khoảng |
| R-09 | Tương tác của khách | Mới nhất trước, mặc định 10 bản |
| R-10 | Chiến dịch `activeOnly = true` | Chỉ trả chiến dịch còn hiệu lực theo ngày hiện tại |
| R-11 | Tìm sản phẩm bằng từ khoá không dấu `tiet kiem` | Vẫn khớp `Tiết kiệm` |
| R-12 | File JSON hỏng cú pháp | Server báo lỗi rõ ràng lúc khởi động, không chạy tiếp |

## 2. Masking — trường có cấu trúc

| Mã | Đầu vào | Kỳ vọng |
|---|---|---|
| M-01 | `fullName = "Nguyễn Văn An"` | `[PERSON_1]` |
| M-02 | Cùng tên xuất hiện lần hai trong phiên | Vẫn `[PERSON_1]` |
| M-03 | Khách thứ hai tên khác | `[PERSON_2]` |
| M-04 | Cùng tên nhưng ở phiên khác | Đánh số lại từ `[PERSON_1]` |
| M-05 | `phone = "0912345678"` | `[PHONE_1]` |
| M-06 | `cccd = "001199012345"` | `[ID_1]` |
| M-07 | `accountNo = "19001234567890"` | `[ACCOUNT_1]` |
| M-08 | `segment = "Priority"` | Giữ nguyên |
| M-09 | `customerId = "CUS0001"` | Giữ nguyên |
| M-10 | `amount = 500000000` | Giữ nguyên |
| M-11 | Trường `null` hoặc rỗng | Không sinh token, không ném exception |
| M-12 | Tên có ký tự đặc biệt `Trần Thị Ánh-Nguyệt` | Che trọn vẹn cả cụm |

## 3. Masking — quét toàn văn

Các ca này dùng trường `Interaction.note`.

| Mã | Ghi chú đầu vào | Kỳ vọng |
|---|---|---|
| F-01 | `"Khách hẹn gọi lại số vợ 0987654321 sau 17h"` | Số bị thay bằng `[PHONE_n]`, phần còn lại giữ nguyên |
| F-02 | `"Gọi số +84912345678 không nghe máy"` | Nhận diện được dạng `+84` |
| F-03 | `"CCCD khách là 001199012345, đã đối chiếu"` | Che số CCCD |
| F-04 | `"Email cá nhân an.nguyen@gmail.com"` | Che email |
| F-05 | `"Chuyển khoản 500000000 đồng"` | **Không che** — số tiền không phải PII |
| F-06 | `"Hẹn ngày 15082026 tại chi nhánh"` | Không che — chuỗi 8 số không khớp mẫu nào |
| F-07 | `"Số thẻ 4012888888881881"` | Che — hợp lệ theo Luhn |
| F-08 | `"Mã tham chiếu 1234567890123456"` | Không che nếu không qua Luhn |
| F-09 | `"Liên hệ 0912345678 hoặc 0987654321"` | Hai token khác nhau |
| F-10 | `"khach hen goi lai so 0912345678"` (không dấu) | Vẫn che được số |
| F-11 | Ghi chú rỗng | Trả rỗng, không lỗi |
| F-12 | Ghi chú dài 5.000 ký tự | Xử lý dưới 100 ms |

## 4. Vault

| Mã | Tình huống | Kỳ vọng |
|---|---|---|
| V-01 | Che rồi hoàn nguyên | Trả lại đúng chuỗi gốc |
| V-02 | Hoàn nguyên token không tồn tại | Bỏ qua token đó, không ném exception |
| V-03 | Hoàn nguyên token của phiên khác | Không trả về — cô lập theo phiên |
| V-04 | Sau TTL hết hạn | Token không còn hoàn nguyên được |
| V-05 | Vượt 5.000 mục một phiên | Ghi cảnh báo, từ chối thêm mục mới |
| V-06 | `Drop(sessionId)` | Toàn bộ ánh xạ phiên đó biến mất |
| V-07 | Giá trị lưu trong cache | Ở dạng đã mã hoá, không phải chuỗi rõ |
| V-08 | 100 phiên song song, mỗi phiên 50 mục | Không lẫn dữ liệu giữa các phiên |

## 5. LeakGuard

| Mã | Đầu vào cho guard | Kỳ vọng |
|---|---|---|
| G-01 | JSON đã che hoàn toàn | `IsClean = true` |
| G-02 | JSON còn số điện thoại | `IsClean = false`, kind = `PHONE` |
| G-03 | JSON còn CCCD | `IsClean = false`, kind = `ID` |
| G-04 | JSON còn email | `IsClean = false`, kind = `EMAIL` |
| G-05 | Guard bắt lỗi | Tool trả thông báo trung tính, **không có dữ liệu** |
| G-06 | Guard bắt lỗi | Audit ghi mức `SecurityAlert` |
| G-07 | Guard bắt lỗi | Audit **không chứa** giá trị bị phát hiện |
| G-08 | Guard bắt lỗi | Không có lần thử lại nào |

**G-09 — ca quan trọng nhất:** cố tình gọi tool với masker bị thay bằng phiên bản không làm gì (`NoOpMasker`). Guard phải chặn. Nếu ca này fail, lưới an toàn không tồn tại.

## 6. Tool MCP

| Mã | Tình huống | Kỳ vọng |
|---|---|---|
| T-01 | `tools/list` | Trả đúng 6 tool, tên đúng `snake_case` |
| T-02 | Mỗi tool | Có mô tả tiếng Việt không rỗng |
| T-03 | Mỗi tham số | Có `[Description]` |
| T-04 | `get_customer` với mã hợp lệ | Kết quả không chứa chuỗi tên thật |
| T-05 | `get_customer` với mã không tồn tại | Kết quả rỗng, thông báo rõ, không exception |
| T-06 | `get_opportunities` + `get_customer` cùng phiên | Cùng khách nhận cùng token |
| T-07 | `search_products` | Không sinh token nào — dữ liệu sản phẩm không phải PII |
| T-08 | Thiếu header `X-Session-Id` | Vẫn chạy, nhưng ghi cảnh báo |
| T-09 | Tool ném exception nội bộ | Client nhận thông báo trung tính, không có stack trace |

## 7. Cửa hoàn nguyên

| Mã | Tình huống | Kỳ vọng |
|---|---|---|
| E-01 | `POST /vault/resolve` đúng API key | 200, trả ánh xạ |
| E-02 | Thiếu `X-Api-Key` | 401 |
| E-03 | Sai `X-Api-Key` | 401 |
| E-04 | `tools/list` | **Không** chứa bất kỳ tool nào liên quan tới vault hay resolve |
| E-05 | Body rỗng | 400 kèm hướng dẫn |

E-04 là ca bảo vệ thiết kế. Nếu ai đó vô tình thêm `[McpServerTool]` lên phương thức resolve, ca này phải đỏ.

## 8. Audit

| Mã | Tình huống | Kỳ vọng |
|---|---|---|
| A-01 | Gọi một tool | Sinh đúng một dòng JSON |
| A-02 | Dòng audit | Có đủ trường theo SPEC mục 8 |
| A-03 | Tham số chứa số điện thoại | Trong audit đã bị che |
| A-04 | Nội dung file audit | Không chứa bất kỳ giá trị PII thô nào |
| A-05 | Gọi 100 tool | 100 dòng, không mất, không trùng |
| A-06 | Ghi đồng thời từ nhiều luồng | File không bị hỏng |

## 9. Kiểm thử tích hợp

| Mã | Kịch bản |
|---|---|
| I-01 | Khởi động server, MCP Inspector kết nối, liệt kê 6 tool |
| I-02 | Gọi `get_customer` qua Inspector, xác nhận bằng mắt không có tên thật |
| I-03 | Lấy token từ I-02, gọi `/vault/resolve`, nhận lại tên thật |
| I-04 | Chuỗi ba lượt: `get_customer` → `get_opportunities` → `get_interactions` cùng phiên, kiểm tra token nhất quán |
| I-05 | Không đặt biến môi trường khoá, server phải từ chối khởi động |

## 10. Ngưỡng hiệu năng

| Mã | Phép đo | Ngưỡng |
|---|---|---|
| P-01 | `get_customer` với 1.000 bản ghi trong bộ nhớ | p95 < 300 ms |
| P-02 | Masking một khách hàng | < 50 ms |
| P-03 | Quét toàn văn ghi chú 5.000 ký tự | < 100 ms |
| P-04 | Bộ nhớ với 100 phiên × 50 mục | < 5 MB |
| P-05 | Thời gian khởi động | < 3 giây |

## 11. Bộ dữ liệu kiểm thử tiếng Việt

Đặt trong `tests/TestData/vietnamese-pii-cases.json`, mỗi mục gồm `input`, `expectedMasked`, `note`. Tối thiểu 20 ca, bắt buộc có:

- Tên ba từ, bốn từ, có dấu gạch nối
- Số điện thoại các đầu số 03, 05, 07, 08, 09
- Dạng `+84` và dạng có dấu cách `091 234 5678`
- CCCD 12 số và CMND 9 số
- Số tiền lớn không được che
- Ngày tháng dạng số không được che
- Văn bản không dấu
- Ghi chú có nhiều loại PII cùng lúc

Bộ này là nguồn sự thật khi tranh luận về việc masking đúng hay sai. Thêm ca mới mỗi khi phát hiện lỗi thực tế.
