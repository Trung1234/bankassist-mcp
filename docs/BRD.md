# BRD — Tài liệu Yêu cầu Nghiệp vụ

**Dự án:** BankAssist CRM MCP Server
**Phiên bản:** 1.0 · MVP
**Trạng thái:** Đã duyệt để triển khai

---

## 1. Bối cảnh

Nhân viên quan hệ khách hàng (RM) của ngân hàng hiện phải mở nhiều màn hình CRM khác nhau để tra cứu thông tin trước mỗi cuộc gọi hoặc mỗi email chăm sóc. Ước tính mỗi ca làm việc mất 2–3 giờ cho thao tác tra cứu và soạn thảo lặp lại.

Ngân hàng muốn thử nghiệm một trợ lý AI trò chuyện bằng tiếng Việt. Để trợ lý đó truy cập được dữ liệu CRM một cách có kiểm soát, cần một lớp trung gian chuẩn hoá — đó là phạm vi của dự án này.

## 2. Vấn đề nghiệp vụ

| Mã | Vấn đề | Ảnh hưởng |
|---|---|---|
| P-1 | Thông tin khách nằm rải rác ở 4 module CRM | RM mất thời gian ghép thủ công |
| P-2 | Mỗi ứng dụng AI lại tự viết tích hợp CRM riêng | Trùng lặp công sức, khó kiểm soát |
| P-3 | Dữ liệu khách hàng có nguy cơ bị gửi nguyên vẹn lên mô hình ngôn ngữ đặt ngoài lãnh thổ | Rủi ro pháp lý |
| P-4 | Không có nhật ký thống nhất về việc ai đã truy cập dữ liệu khách nào | Không giải trình được khi kiểm toán |

## 3. Mục tiêu

| Mã | Mục tiêu | Cách đo |
|---|---|---|
| G-1 | Một điểm truy cập chuẩn cho mọi AI agent tới dữ liệu CRM | 6 tool hoạt động qua giao thức MCP |
| G-2 | Dữ liệu cá nhân không rời hệ thống dưới dạng thô | Không tool nào trả giá trị chưa che |
| G-3 | Mọi lần truy cập đều được ghi lại | 100% lời gọi tool có bản ghi audit |
| G-4 | Thay nguồn dữ liệu không phải sửa tool | Đổi JSON sang SQL chỉ sửa một class |

## 4. Ràng buộc pháp lý

Thông tin khách hàng của tổ chức tín dụng được Nghị định 13/2023/NĐ-CP xếp vào nhóm **dữ liệu cá nhân nhạy cảm**. Kéo theo:

- Phải có biện pháp bảo vệ tương xứng khi xử lý.
- Nếu dữ liệu được gửi tới dịch vụ đặt ở nước ngoài thì phát sinh nghĩa vụ về chuyển dữ liệu xuyên biên giới.
- Phải lập và lưu giữ hồ sơ đánh giá tác động xử lý dữ liệu cá nhân.

**Hệ quả thiết kế:** che dữ liệu trước khi nó rời hệ thống là cách kỹ thuật để tránh phát sinh nghĩa vụ chuyển dữ liệu xuyên biên giới. Đây không phải tính năng phụ, mà là lý do tồn tại của lớp masking.

Ngoài ra Thông tư 50/2024/TT-NHNN yêu cầu bảo đảm bí mật thông tin khách hàng và không lưu thông tin khách hàng ở vùng mạng tiếp giáp Internet.

## 5. Phạm vi

### Trong phạm vi MVP

- MCP server cung cấp 6 tool đọc dữ liệu CRM
- Che dữ liệu cá nhân trong mọi kết quả tool
- Cửa hoàn nguyên riêng cho ứng dụng host
- Nhật ký truy cập dạng file
- Dữ liệu giả đọc từ file JSON

### Ngoài phạm vi MVP

| Hạng mục | Lý do hoãn |
|---|---|
| Kết nối database thật | Chưa có môi trường sandbox CRM |
| Tool ghi dữ liệu (tạo, sửa) | Rủi ro cao, cần quy trình duyệt riêng |
| Xác thực OAuth 2.1 | MVP chạy trong mạng nội bộ, dùng API key |
| Giao diện quản trị | Chưa cần cho giai đoạn thử nghiệm |
| Nhận diện tên người bằng mô hình NER | Đã có tên từ record, chưa cần |
| Đa ngôn ngữ | Chỉ phục vụ RM người Việt |

## 6. Các bên liên quan

| Vai trò | Quan tâm chính |
|---|---|
| RM | Tra cứu nhanh, thông tin chính xác |
| Trưởng phòng CNTT | Dễ bảo trì, dễ thay nguồn dữ liệu |
| Bộ phận Tuân thủ | Chứng minh được dữ liệu không rò rỉ |
| Đội thực tập | Tài liệu đủ rõ để tự triển khai |

## 7. Giả định

- CRM sandbox sẽ cung cấp API tương thích với cấu trúc JSON trong `data/`
- Ứng dụng host chạy trong cùng mạng nội bộ với MCP server
- Mỗi phiên hội thoại tương ứng một RM cụ thể, danh tính do host cung cấp

## 8. Rủi ro

| Rủi ro | Mức | Giảm thiểu |
|---|---|---|
| Masking bỏ sót một dạng dữ liệu | Cao | Lớp guard quét lần hai, chặn nếu còn sót |
| Vault phình to trong bộ nhớ | Trung bình | TTL 30 phút, giới hạn số mục mỗi phiên |
| Nhóm thực tập chưa quen MCP | Trung bình | Backlog chia nhỏ, task đầu không đụng MCP |
| Cấu trúc JSON lệch với CRM thật | Trung bình | Cô lập trong `ICrmRepository` |

## 9. Tiêu chí nghiệm thu giai đoạn MVP

1. MCP Inspector kết nối được và liệt kê đủ 6 tool.
2. Gọi bất kỳ tool nào cũng không trả về tên, số điện thoại, CCCD, số tài khoản ở dạng thô.
3. Gọi `/vault/resolve` với token hợp lệ trả lại đúng giá trị gốc.
4. Gọi `/vault/resolve` không có API key bị từ chối.
5. Mỗi lần gọi tool sinh đúng một dòng trong nhật ký audit.
6. Toàn bộ ca kiểm thử trong TEST-SPEC đạt.
