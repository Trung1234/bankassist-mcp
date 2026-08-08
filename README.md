# BankAssist CRM MCP Server

MCP server viết bằng .NET, cung cấp tool tra cứu CRM ngân hàng cho AI agent, có lớp che dữ liệu cá nhân (PII masking) đặt ngay trong server.

**Giai đoạn MVP:** dữ liệu đọc từ file JSON, chưa có database.

---

## Vì sao masking nằm trong MCP server

Kết quả trả về từ tool CRM chính là dữ liệu khách hàng thật, và nó được chèn thẳng vào ngữ cảnh hội thoại của LLM. Nếu để host tự che, mỗi host mới lại phải cài lại lớp bảo vệ — và chỉ cần một host quên là dữ liệu rời hệ thống.

Đặt masking trong server nghĩa là: **không có đường nào lấy được dữ liệu thật qua giao thức MCP.** Mọi tool đều trả bản đã che.

Việc hoàn nguyên đi qua một cửa riêng: `POST /vault/resolve` — endpoint REST thường, không đăng ký làm MCP tool, nên LLM không nhìn thấy và không gọi được. Chỉ host gọi được bằng API key.

```
Agent ──MCP──> Tool ──> JSON ──> Masking ──> Bản đã che ──> LLM
Host  ──REST─> /vault/resolve ──> Bản gốc          (LLM không thấy đường này)
```

---

## Yêu cầu môi trường

- macOS (Apple Silicon), .NET 10 SDK arm64
- Không cần database, không cần Docker cho MVP

```bash
dotnet --version     # kỳ vọng 10.x
```

## Chạy

```bash
git clone <repo> && cd bankassist-mcp
dotnet restore
dotnet run --project src/BankAssist.Mcp
```

Server lắng nghe `http://localhost:5280`.

| Đường dẫn | Mục đích |
|---|---|
| `/mcp` | Endpoint MCP (Streamable HTTP) |
| `/vault/resolve` | Hoàn nguyên token — cần header `X-Api-Key` |
| `/health` | Kiểm tra sống |

## Kiểm tra bằng MCP Inspector

```bash
npx @modelcontextprotocol/inspector
# Transport: Streamable HTTP
# URL: http://localhost:5280/mcp
```

Bấm **List Tools** phải thấy 6 tool. Gọi thử `get_customer` với `customerId = "CUS0001"` — kết quả trả về phải chứa `[PERSON_1]`, không được có tên thật.

## Chạy test

```bash
dotnet test
```

---

## Cấu trúc thư mục

```
src/BankAssist.Mcp/
  Program.cs
  Tools/            # các class [McpServerToolType]
  Masking/          # PiiMasker, VnRecognizers, Vault
  Data/             # ICrmRepository + JsonCrmRepository
  Audit/            # AuditLogger
  Models/           # DTO
data/               # JSON nguồn cho MVP
tests/BankAssist.Mcp.Tests/
docs/               # BRD, PRD, SPEC, TEST-SPEC, BACKLOG
```

## Tài liệu

| File | Nội dung |
|---|---|
| [docs/BRD.md](docs/BRD.md) | Bối cảnh nghiệp vụ, vấn đề, phạm vi |
| [docs/PRD.md](docs/PRD.md) | Yêu cầu sản phẩm, user story, tiêu chí chấp nhận |
| [docs/SPEC.md](docs/SPEC.md) | Đặc tả kỹ thuật — đọc file này trước khi code |
| [docs/TEST-SPEC.md](docs/TEST-SPEC.md) | Ca kiểm thử, đặc biệt các ca PII tiếng Việt |
| [docs/BACKLOG.md](docs/BACKLOG.md) | Danh sách công việc theo thứ tự thực hiện |
| [CLAUDE.md](CLAUDE.md) | Quy ước cho Claude Code |

---

## Ba điều tuyệt đối không làm

1. **Không đưa dữ liệu khách hàng thật vào `data/`.** Toàn bộ file JSON là dữ liệu giả sinh bằng script.
2. **Không log giá trị chưa che.** Kể cả khi debug. Log bản đã che, kèm hash của bản gốc nếu cần đối chiếu.
3. **Không biến `/vault/resolve` thành MCP tool.** Nếu LLM gọi được endpoint này thì toàn bộ thiết kế mất tác dụng.

## Giấy phép

Nội bộ — không phát hành công khai.
