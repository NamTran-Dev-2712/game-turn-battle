# 49 — Remote config nâng cao + feature flags

> Mục đích: Nâng Configuration Service lên **remote config có versioning/rollback** + **feature flags** — bật/tắt tính năng và đổi cấu hình không cần build lại (nền LiveOps, ADR-005/006).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 11 LiveOps Foundation | P6 | S12 | F35 (nền) |

# Mục tiêu

Mở rộng Config Service (21): publish/rollback bundle có version, feature flag runtime (bật/tắt feature theo cờ), lịch time-based (start/end) cho nội dung — tất cả không rebuild client. Đặt schema event/banner/shop-rotation (chừa chỗ, chưa bật đầy đủ).

# Lý do

ADR-006: nền LiveOps trên Config Service, để hooks sẵn không refactor lớn. Feature flags cho phép tắt tính năng lỗi nhanh trong vận hành. Sau khi MVP retention-ready (P5).

# Phụ thuộc

- **Trước:** 21 (Config Service), 22 (client bundle), 12 (Redis).
- **Sau:** 50 (mail broadcast dùng scheduling), 51 (telemetry), Post-MVP CMS.

# Phạm vi

- Feature flag: định nghĩa cờ (config), đọc runtime server + client (gate feature); bật/tắt không build.
- Publish/rollback bundle có version (mở rộng 21): chuyển "current" nguyên tử, quay lại version cũ.
- Schema time-based (start/end + version) cho event/banner/shop-rotation — **định nghĩa, chưa bật đầy đủ** (ADR-006).
- Server-time cho lịch (chống clock-cheat).

# Không thuộc phạm vi

- Admin CMS/UI vận hành (Post-MVP).
- A/B testing đầy đủ (Post-MVP — chỉ đặt nền).
- Bật nội dung limited thật (Post-MVP).

# Deliverables

- Feature flag runtime (server + client gate).
- Rollback bundle version (thao tác + test).
- Schema time-based (event/banner/shop) chừa chỗ.
- Integration test: bật/tắt feature qua flag; publish→rollback; lịch theo server-time.
- Cập nhật [`../liveops/remote-config.md`](../liveops/remote-config.md) + [`../liveops/feature-flags-and-ab-testing.md`](../liveops/feature-flags-and-ab-testing.md).

# Công việc cần thực hiện

- [ ] Định nghĩa feature flag trong config; provider đọc flag runtime (server); gate feature theo flag.
- [ ] Client: đọc flag từ bundle → ẩn/hiện feature (không build lại).
- [ ] Publish/rollback: con trỏ "current version" chuyển nguyên tử; rollback về version cũ (immutable giữ lại).
- [ ] Schema time-based (start/end + version) cho event/banner/shop-rotation — định nghĩa + validate (07), chưa bật đủ.
- [ ] Lịch dùng server-time (`IClock`); chống clock-cheat.
- [ ] Integration test: bật/tắt feature; publish→rollback; time-based đúng server-time.
- [ ] Cập nhật `../liveops/remote-config.md` + `../liveops/feature-flags-and-ab-testing.md` + `../liveops/content-scheduling.md`.

# Tiêu chí hoàn thành

- Bật/tắt một feature qua flag **không** rebuild client (chứng minh).
- Publish version mới + rollback về version cũ hoạt động (config versioning/rollback).
- Schema time-based định nghĩa + validate; lịch theo server-time.
- Data-driven, server-authoritative.

# Cách kiểm tra

- `dotnet test` + gdUnit4: flag on/off ẩn/hiện feature; publish→rollback; time-based server-time.
- Local: tắt 1 feature qua flag → client ẩn (không build).
- Rollback bundle → phục vụ version cũ.

# Rủi ro

- **Rollback không nhất quán** → immutable versions + con trỏ current nguyên tử.
- **Flag rải rác khó quản** → tập trung định nghĩa flag; tài liệu hoá.
- **Lịch theo client time** → server-time bắt buộc.

# Ghi chú

Đặt **nền** LiveOps (hooks), chưa bật nội dung limited thật (Post-MVP). Live bundle swap không cần deploy có thể bật ở đây/Post-MVP tuỳ LO2. Bám ADR-005/006 + [`../liveops/`](../liveops/README.md).

# Technical Debt Review

- **Maintainability:** flag + versioning tập trung.
- **Scalability:** rollback + time-based cho vận hành dài hạn.
- **Testing:** flag/rollback/lịch có test.
- **Security:** server-time + server-authoritative flags.
- **Nợ:** CMS/A-B/nội dung limited (Post-MVP).

# Phase Review

Đóng khi feature flags + publish/rollback + schema time-based (server-time) hoạt động không rebuild client, test xanh.

---

## Liên kết
- [`../liveops/remote-config.md`](../liveops/remote-config.md) · [`../liveops/feature-flags-and-ab-testing.md`](../liveops/feature-flags-and-ab-testing.md) · [`../liveops/content-scheduling.md`](../liveops/content-scheduling.md)
- ADR: [`../adr/ADR-005-configuration-strategy.md`](../adr/ADR-005-configuration-strategy.md) · [`../adr/ADR-006-liveops.md`](../adr/ADR-006-liveops.md)
- Roadmap: [`README.md`](README.md) → kế: [`50-mail-broadcast-scheduling.md`](50-mail-broadcast-scheduling.md)
