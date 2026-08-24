# 0018 — Client auth + profile integration standardized (Phase 20)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-08-24, Godot 4.7.1-stable). Đóng vòng auth/save phía client.
- **Bối cảnh:** Phase 15–19 dựng các mảnh rời (NetworkClient gắn token, StateCache read-cache, boot, auth server
  `POST /api/v1/auth/guest`, profile server `GET /api/v1/profile`) nhưng CHƯA nối end-to-end. Phase 20 nối:
  guest login → JWT → lưu token an toàn → NetworkClient gắn token → tải profile → StateCache → hub hiển thị;
  đăng nhập lại bằng token lưu; 401/hết hạn → re-login; mất mạng → cache có nhãn.

## Quyết định (user-approved)

- **Token store an toàn = file `user://` MÃ HOÁ** (`FileAccess.open_encrypted_with_pass` → `user://auth/token.dat`;
  khoá = salt app + `OS.get_unique_id()`, ràng thiết bị). **KHÔNG plaintext, KHÔNG log token/passphrase, KHÔNG commit**
  (`user://` git-ignored). KHÔNG phải keychain OS (Godot thuần không có — cần native plugin, ngoài phạm vi). Mở rộng
  `TokenStore` sẵn có (tái dùng — Phase 15 header đã nói "thay/nâng cấp store này ở phase 20"): thêm refresh token + hạn
  (`_expires_at_unix`) + `save_tokens`/`load`/`clear`/`get_refresh_token`/`is_expired`; giữ `has_token`/`get_access_token`
  (NetworkClient chỉ phụ thuộc 2 hàm này). `NetworkClient._ready()` gọi `token_store.load()`.
- **Điều phối auth = `AuthProfileFlow`** (`src/ui/boot/auth_profile_flow.gd`, **RefCounted, KHÔNG autoload** — không God
  manager) do BootController gọi. **User chọn** helper riêng (thay vì nhét inline vào BootController) ⇒ BootController
  mỏng + nhánh auth test được độc lập. Vòng đời auth TẬP TRUNG ở boot + AuthProfileFlow; **NetworkClient chỉ gắn token +
  phát `unauthorized`; UI/view KHÔNG chứa auth logic**.
- **Offline = cache-fallback** (**user chọn**, sửa cổng health cứng của Phase 17): health/auth thất bại NHƯNG có profile
  cache cũ (StateCache boot `source="cache"`) ⇒ vào hub **chế độ offline** (nhãn `[offline]`), KHÔNG bịa dữ liệu; chỉ hiện
  màn lỗi khi KHÔNG có cache. Đây là thay đổi hành vi boot Phase 17 ⇒ đã doc-sync (`ui-architecture.md` §4.1).
- **Boot flow mới** (`boot_controller.gd`, thêm `State.AUTHENTICATING`): health → `AuthProfileFlow.run()` → config
  (best-effort) → hub. `run()` trả `{ok, offline, code}`: dùng token còn hạn / guest login → `GET /profile` →
  `StateCache.apply_snapshot`. **401/hết hạn → re-login CÓ GIỚI HẠN** (`MAX_RELOGIN=1`, đọc `NetResult.kind==UNAUTHORIZED`
  tại chỗ ⇒ điều khiển tất định) — **CHỐNG vòng lặp vô hạn**; `unauthorized` vẫn phát toàn cục.
- **Hiển thị hub:** `parse_profile`/`parse_auth_guest_response` (mới ở `response_parser.gd`) → model generated
  `ProfileDto`/`AuthGuestResponse` sẵn có (**KHÔNG đổi contract ⇒ không drift generated**). `MainHubPresenter` đọc
  `StateCache.get_profile()` hiển thị **tên · level** + nhãn offline; **currency = PLACEHOLDER** (`ProfileDto` chưa mang
  currency — feature phase 31). Presenter tự refresh khi có `state_refreshed`; huỷ đăng ký qua `dispose()` gọi ở
  `view.unbind()`. Deps presenter inject được (state_cache/config_provider) cho test.
- **KHÔNG thêm event EventBus** — danh mục giữ ĐÓNG (5). Tái dùng `unauthorized` (401→re-login) + `state_refreshed`
  (profile về → hub refresh).
- **ADR audit:** Phase 20 **thực thi** quyết định sẵn có (ADR-008 JWT guest + client `core/net`; ADR-007 client
  read-cache, ownership từ token `sub`) — **không** quyết định kiến trúc mới ⇒ KHÔNG sửa ADR.

## Verify

- `--headless --import` exit 0 (0 error/0 warning; đăng ký `TokenStore`/`AuthProfileFlow`/…).
- gdUnit4 **toàn suite 65/65 pass, 0 error/0 failure/0 orphan**. Mới: `token_store_test` 4 (round-trip mã hoá/clear/
  is_expired/missing), `auth_profile_flow_test` 6 (happy / existing-token-skip-login / 401→bounded-relogin /
  401-persist-bounded-no-loop / expired→relogin / offline→cache-no-fabricate), `main_hub_presenter_test` 3, boot +5
  (auth-after-health / auth-fail-no-cache→error / auth-fail-cache→hub-offline / health-fail-cache→hub-offline).
  Regression 14/15/16/17 xanh. Mock qua `FakeHttpTransport` (thêm capture `requests` để kiểm auth header/endpoint).
- **Grep guard xanh:** `HTTPRequest` chỉ `core/net/` (còn lại chỉ comment); không log token/Authorization/passphrase;
  `EventBus.EVENTS` giữ 5; StateCache không mutator chân lý. Không drift `client/src/data/generated`.
- **CI-pending:** `ci-client.yml` (import + gdUnit4 headless xvfb) trên GitHub Actions.

## Ràng buộc cho agent sau

- **Tái dùng `AuthProfileFlow`/`TokenStore`/`NetworkClient`/`StateCache`/`ProfileDto`** — **KHÔNG** tạo AuthManager/
  ProfileManager/token store/HTTP client/profile DTO thứ hai, **không** để auth logic trong view, **không** gọi endpoint
  auth từ view, **không** bypass StateCache, **không** thêm refresh-token architecture ngoài phạm vi, **không** thêm event
  EventBus cho mỗi thao tác.
- **Bảo mật:** token persist MÃ HOÁ (không plaintext), **không log** token/passphrase, không hardcode/commit; test dùng
  giá trị `fake-*`.
- **Authority (ADR-007/011):** client chỉ hiển thị; profile/currency/state từ server qua `StateCache.apply_snapshot`;
  offline hiển thị cache **có nhãn** — không giả là dữ liệu tươi. **401 re-login có giới hạn** (không vòng lặp); lỗi
  không phục hồi ⇒ báo lỗi, không bịa profile.
- **Nợ có chủ đích:** refresh-token endpoint thật (refresh token lưu nhưng chưa đổi), link account (Post-MVP),
  currency/wallet (phase 31), config bundle e2e (phase 22), keychain OS thật (cần native plugin).
- Đồng bộ: `client/src/core/net/token_store.gd` + `client/src/ui/boot/auth_profile_flow.gd` + `boot_controller.gd` +
  `main_hub_{presenter,view}.gd` + `response_parser.gd` + `client/tests/*` + `docs/godot/state-and-signals.md` §4.1/§3.1 +
  `docs/godot/ui-architecture.md` §4.1 + CLAUDE.md §4.6 + `.instructions/client.md` + `.claude/agents/godot-client.md` +
  doc-sync matrix row.

> Quyết định kiến trúc gốc: ADR-007 (save strategy), ADR-008 (networking), ADR-002 (Godot architecture / EventBus).
