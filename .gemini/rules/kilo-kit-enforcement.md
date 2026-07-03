# Rule: Kilo-Kit Hard-Gate & Iron Law Enforcement

**Description:** Trình tự bắt buộc khi bắt đầu một Phase, Spec hoặc Task mới.

**Triggers:** 
- Khi người dùng yêu cầu bắt đầu một phase mới.
- Khi một spec trước đó vừa hoàn thành và chuẩn bị chuyển sang spec tiếp theo.
- Khi bắt đầu một phiên làm việc mới với một task cụ thể.

**Strict Constraints (Tuyệt đối tuân thủ):**
1. Không bao giờ được phép code, tạo file, hay viết test theo quán tính mà chưa nạp context.
2. Bắt buộc phải gọi MCP tool `kilo_route_intent` với message chứa tiến độ hiện tại để nhận chỉ dẫn bước tiếp theo.
3. Bắt buộc phải gọi MCP tool `kilo_get_skill` để nạp nội dung của skill (ví dụ: engineering/tdd) được trả về từ route intent.
4. Chỉ sau khi cả hai tool trên chạy thành công và context được nạp vào bộ nhớ, Agent mới được phép phân tích spec và tiến hành bước đầu tiên (thường là viết Unit Test).
