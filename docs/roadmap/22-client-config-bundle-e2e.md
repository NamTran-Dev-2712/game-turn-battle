# 22 — Client config bundle end-to-end

> Mục đích: Hoàn tất luồng **config data-driven end-to-end**: client (ConfigProvider) nhận bundle versioned từ Configuration Service, cache theo version, dùng để dựng dữ liệu hiển thị — chứng minh đổi config không rebuild client.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 4 Auth, Save & Config Service | P1 | S5 | nền data-driven |

# Mục tiêu

Client boot: hỏi version hiện hành → nếu khác cache → tải bundle từ `GET /config/bundle` → ConfigProvider (phase 16) cache & phục vụ query cho feature; hiển thị một dữ liệu từ config (ví dụ danh sách hero mẫu) để xác nhận vòng.

# Lý do

Đóng mắt xích cuối Core Framework từ phía client: xác nhận contract config + Configuration Service + ConfigProvider client hoạt động cùng nhau, sẵn sàng cho combat/hero (nhóm 5–6) đọc config thật.

# Phụ thuộc

- **Trước:** 21 (Config Service), 16 (ConfigProvider client), 20 (auth để gọi API).
- **Sau:** 27 (hero từ config), 23–25 (combat đọc hero/skill config), mọi feature.

# Phạm vi

- Client so version (current vs cache) → tải bundle khi cần → cache đĩa theo `config@vN`.
- Query config qua ConfigProvider để dựng dữ liệu hiển thị mẫu (hero list placeholder).
- Xử lý bundle lỗi/thiếu → fallback cache cũ + báo lỗi.
- Chứng minh: đổi giá trị config phía server → client thấy đổi mà không build lại.

# Không thuộc phạm vi

- Feature nghiệp vụ hoàn chỉnh (hero system thật phase 27).
- Combat (nhóm 5).
- Signed/secure bundle nâng cao (LiveOps/Post-MVP).

# Deliverables

- Luồng config e2e chạy; client hiển thị dữ liệu từ bundle.
- Cache đĩa theo version + fallback.
- Test gdUnit4 (mock server): nhận bundle→query→hiển thị; version bump→reload; lỗi→fallback.
- Ghi chú "đổi config không rebuild" trong [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md).

# Công việc cần thực hiện

- [ ] Boot: gọi `GET /config/current` → so với cache; khác → `GET /config/bundle?version=`.
- [ ] ConfigProvider (phase 16) lưu bundle đĩa theo version; load khi boot.
- [ ] Dựng màn mẫu đọc từ config (danh sách hero placeholder từ `hero.schema`) để xác nhận query.
- [ ] Fallback: bundle tải lỗi → dùng cache cũ + báo; không có cache → màn lỗi + retry.
- [ ] Kịch bản chứng minh: đổi giá trị config server → publish → client reload version mới không build lại.
- [ ] Test gdUnit4 mock: nhận→query→hiển thị; version bump; lỗi→fallback.
- [ ] Cập nhật `../gameplay/configuration-and-data.md` (ghi rõ luồng e2e + chứng minh).

# Tiêu chí hoàn thành

- Client nhận bundle version X, hiển thị dữ liệu từ config.
- Server đổi config → version X+1 → client hiển thị đổi **không rebuild** (chứng minh, có ảnh/log).
- Bundle lỗi → fallback cache cũ hoặc màn lỗi + retry.
- Test gdUnit4 xanh.

# Cách kiểm tra

- Chạy server local: đổi giá trị config → publish → mở lại client → thấy đổi.
- gdUnit4 mock: nhận/version-bump/lỗi.
- Rà: dữ liệu hiển thị lấy từ ConfigProvider, không hardcode trong scene.

# Rủi ro

- **Tải bundle lớn chậm** → tải nền (ADR-009), phần nhẹ trước; progress splash.
- **Version lệch client-server** → so version bắt buộc trước khi dùng; immutable per version.
- **Fallback che lỗi thật** → luôn log + báo khi dùng cache cũ.

# Ghi chú

Đây là chứng minh "data-driven & LiveOps-ready" từ đầu-đến-cuối. Sau phase này, mọi feature đọc số liệu từ config bundle, không hardcode. Bám ADR-004/005/009.

# Technical Debt Review

- **Maintainability:** feature đọc config thống nhất; đổi số không đụng client build.
- **Scalability:** cache version + tải nền cho nội dung lớn.
- **Testing:** e2e mock cover luồng chính.
- **Security:** bundle validate ở server; client chỉ đọc.
- **Nợ:** signed bundle & live swap (LiveOps/Post-MVP).

# Phase Review

Đóng khi luồng config e2e chạy, chứng minh đổi config không rebuild client, fallback hoạt động, test xanh. **Hoàn tất P1 — nền data-driven end-to-end sẵn sàng cho gameplay.**

---

## Liên kết
- [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md) · [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md)
- ADR: [`../adr/ADR-005-configuration-strategy.md`](../adr/ADR-005-configuration-strategy.md) · [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md) · [`../adr/ADR-009-asset-loading.md`](../adr/ADR-009-asset-loading.md)
- Roadmap: [`README.md`](README.md) → kế: [`23-combat-spec-fixedpoint.md`](23-combat-spec-fixedpoint.md)
