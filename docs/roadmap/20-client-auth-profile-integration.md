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

- [x] Trong boot (phase 17): nếu chưa có token → gọi `/auth/guest`, lưu token; nếu có → dùng lại. *(→ `AuthProfileFlow.run()` gọi từ `boot_controller.gd` giữa health và config; test `test_happy_path_login_token_profile_into_state_cache` + `test_existing_valid_token_skips_guest_login` xanh.)*
- [x] Token store an toàn (dùng cơ chế lưu của nền tảng; không log; không commit). *(→ `token_store.gd`: persist mã hoá `FileAccess.open_encrypted_with_pass` vào `user://auth/token.dat` (git-ignored); grep guard: không log token; test round-trip/clear/expired/missing xanh.)*
- [x] NetworkClient (phase 15) đọc token từ store; xử lý 401 → emit `unauthorized` → boot re-login. *(→ `NetworkClient` gắn `Bearer` từ `TokenStore` (đã có); `AuthProfileFlow` re-login có giới hạn `MAX_RELOGIN=1`; test `test_unauthorized_triggers_bounded_relogin_then_succeeds` + `test_relogin_is_bounded_no_infinite_loop_when_401_persists` xanh.)*
- [x] Gọi `GET /profile` → nạp StateCache → hub hiển thị (currency/tên placeholder). *(→ `parse_profile` → `StateCache.apply_snapshot`; `MainHubPresenter` hiển thị tên·level + currency placeholder; test presenter xanh.)*
- [x] Xử lý mất mạng: hiển thị cache cũ có nhãn, không tự tạo dữ liệu. *(→ offline-fallback ở boot + `AuthProfileFlow._degraded`; nhãn `[offline]` ở hub; test `test_offline_with_cache_returns_offline_view_without_fabricating` + `test_health_failure_with_cache_enters_hub_offline` xanh.)*
- [x] Test gdUnit4 với server mock: happy-path; token hết hạn→re-login; mất mạng→cache. *(→ `FakeHttpTransport`; `auth_profile_flow_test.gd` 6/6 + `token_store_test.gd` 4/4 + `main_hub_presenter_test.gd` 3/3 + boot 9/9; toàn suite **65/65 pass, 0 orphan**.)*
- [x] Cập nhật [`../godot/state-and-signals.md`](../godot/state-and-signals.md). *(→ §4.1 Auth+Profile + §3.1 note tái dùng `unauthorized`/`state_refreshed`; kèm `ui-architecture.md` §4.1.)*

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

**Kết luận: ĐỦ ĐIỀU KIỆN ĐÓNG (local PASS 2026-08-23).** 7/7 `# Công việc cần thực hiện` `[x]` có bằng chứng
run; 4/4 `# Tiêu chí hoàn thành` thoả:
- Lần đầu → guest login → hub với profile: `test_happy_path_login_token_profile_into_state_cache`.
- Mở lại → dùng token lưu (không login lại): `test_existing_valid_token_skips_guest_login` + persist mã hoá đĩa.
- 401/hết hạn → re-login có giới hạn (chống vòng lặp); mất mạng → cache có nhãn: 4 test nhánh xanh.
- Không dữ liệu chân lý ở client: StateCache chỉ `apply_snapshot`; grep authority sạch.

**Bằng chứng:** Godot 4.7.1-stable local `--headless --import` exit 0 (0 error/0 warning, class mới đăng ký);
gdUnit4 headless **65/65 pass, 0 error/0 failure/0 orphan** (mới: token_store 4 + auth_profile_flow 6 +
main_hub_presenter 3 + boot 5 mới/9 tổng; regression 14/15/16/17 xanh); grep guard: `HTTPRequest` chỉ
`core/net/`, không log token/passphrase, `EventBus.EVENTS` giữ 5 (KHÔNG thêm event); không drift
`client/src/data/generated`. Doc-sync: `state-and-signals.md` §4.1/§3.1 + `ui-architecture.md` §4.1 +
CLAUDE.md §4.6 + `.instructions/client.md` + agent godot-client + doc-sync matrix + `.memory/0018` + auto-memory.

**Kiến trúc:** vòng đời auth TẬP TRUNG ở boot + `AuthProfileFlow` (RefCounted, không God autoload); `TokenStore`
persist mã hoá (tái dùng, không tạo storage 2); tái dùng `NetworkClient`/`StateCache`/`ProfileDto`/`unauthorized`/
`state_refreshed` — KHÔNG tạo client/DTO/bus/event trùng. ADR-007/008 audit: phase 20 **thực thi** quyết định sẵn
có (JWT guest, client read-cache, offline hiển thị cache), **không** quyết định kiến trúc mới ⇒ không sửa ADR.

**Nợ có chủ đích (ngoài phạm vi):** refresh-token endpoint thật (refresh token đã lưu nhưng chưa đổi), link
account provider (Post-MVP), currency/wallet (phase 31), config bundle e2e (phase 22), secure keychain OS thật
(cần native plugin). **CI-pending:** `ci-client.yml` trên GitHub Actions (đóng khi Actions xanh — §4.5).

---

## Liên kết
- [`../godot/state-and-signals.md`](../godot/state-and-signals.md) · [`../godot/ui-architecture.md`](../godot/ui-architecture.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md)
- Roadmap: [`README.md`](README.md) → kế: [`21-configuration-service.md`](21-configuration-service.md)
