# 20 — Client integration: auth + profile

> Mục đích: Nối client vào luồng **guest login → nhận JWT → tải profile** trong boot, lưu token an toàn, hiển thị profile từ StateCache.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 4 Auth, Save & Config Service | P1 | S4 | F11 |

# Mục tiêu

Client boot: gọi `/auth/guest` (lần đầu) → lưu JWT an toàn → NetworkClient gắn token → tải `GET /profile` → StateCache giữ để hiển thị. Đăng nhập lại tự động bằng token lưu; xử lý 401 → re-login.

# Lý do

Đóng vòng auth/save phía client: chứng minh end-to-end danh tính + profile server-authoritative hiển thị đúng trên client (chỉ cache đọc — ADR-007).

# Phụ thuộc

- **Trước:** 18 (auth server), 19 (profile server), 15 (NetworkClient), 16 (StateCache), 17 (boot).
- **Sau:** 22 (config e2e), mọi feature cần danh tính + profile.

# Phạm vi

- Luồng guest login trong boot (lần đầu) + lưu token an toàn (không plaintext lộ liễu).
- Auto re-login bằng token lưu; xử lý token hết hạn/401 → re-login guest hoặc refresh.
- Tải profile → StateCache → hiển thị ở hub.
- Không lưu chân lý ở client; profile là read-cache.

# Không thuộc phạm vi

- Link account provider (Post-MVP).
- Logic nghiệp vụ state (feature phase sau).
- Config bundle (phase 22).

# Deliverables

- Luồng auth+profile tích hợp trong boot.
- Token store an toàn + auto re-login + xử lý 401.
- Hub hiển thị dữ liệu profile từ StateCache.
- Test gdUnit4 (mock server): login→token→profile→hiển thị; 401→re-login.

# Công việc cần thực hiện

- [ ] Trong boot (phase 17): nếu chưa có token → gọi `/auth/guest`, lưu token; nếu có → dùng lại.
- [ ] Token store an toàn (dùng cơ chế lưu của nền tảng; không log; không commit).
- [ ] NetworkClient (phase 15) đọc token từ store; xử lý 401 → emit `unauthorized` → boot re-login.
- [ ] Gọi `GET /profile` → nạp StateCache → hub hiển thị (currency/tên placeholder).
- [ ] Xử lý mất mạng: hiển thị cache cũ có nhãn, không tự tạo dữ liệu.
- [ ] Test gdUnit4 với server mock: happy-path; token hết hạn→re-login; mất mạng→cache.
- [ ] Cập nhật [`../godot/state-and-signals.md`](../godot/state-and-signals.md).

# Tiêu chí hoàn thành

- Lần đầu mở app → guest login tự động → vào hub với profile.
- Mở lại app → dùng token lưu, không login lại (trừ khi hết hạn).
- 401 → re-login trơn tru; mất mạng → hiển thị cache có nhãn.
- Không dữ liệu chân lý tính ở client.

# Cách kiểm tra

- Chạy server local + client: mở app lần 1 (login) & lần 2 (dùng token) → hub có profile.
- Ép token hết hạn → client re-login.
- gdUnit4 mock: các nhánh trên.

# Rủi ro

- **Token lưu không an toàn** → dùng secure storage nền tảng; không plaintext trong file thường.
- **Vòng lặp re-login khi 401 liên tục** → giới hạn thử + báo lỗi.
- **Hiển thị cache nhầm là chân lý** → nhãn offline + ưu tiên server.

# Ghi chú

Client chỉ hiển thị; mọi thay đổi state qua server. Link account là Post-MVP. Bám ADR-007/008.

# Technical Debt Review

- **Maintainability:** luồng auth tập trung ở boot + NetworkClient.
- **Scalability:** token stateless; profile cache giảm tải.
- **Testing:** mock server cho các nhánh auth.
- **Security:** token an toàn, không client-authority — trọng tâm.
- **Nợ:** refresh nâng cao, link account (Post-MVP).

# Phase Review

Đóng khi client login guest + lưu token + tải/hiển thị profile + xử lý 401/offline, test gdUnit4 xanh.

---

## Liên kết
- [`../godot/state-and-signals.md`](../godot/state-and-signals.md) · [`../godot/ui-architecture.md`](../godot/ui-architecture.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md)
- Roadmap: [`README.md`](README.md) → kế: [`21-configuration-service.md`](21-configuration-service.md)
