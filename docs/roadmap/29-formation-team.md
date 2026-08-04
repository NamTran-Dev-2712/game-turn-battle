# 29 — Formation & Team-of-6

> Mục đích: Hiện thực **đội hình 6 hero + vị trí (formation)** — quyết định chiến thuật duy nhất trong combat auto; lưu server-authoritative, ảnh hưởng target/aggro của sim.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 6 Gameplay Vertical Slice | P2 | S7 | F03 |

# Mục tiêu

Người chơi chọn 6 hero vào lưới formation (vị trí ảnh hưởng aggro/target theo spec combat 23); đội hình lưu ở server (gắn profile); client dựng UI kéo-thả/chọn ô; sim đọc formation làm input.

# Lý do

Formation là "lựa chọn tactical" duy nhất (A02/A05/A12) trong combat full-auto — mắt xích giữa collection và combat. Cần trước Battle flow (30).

# Phụ thuộc

- **Trước:** 27 (hero), 24/25 (sim đọc vị trí), 19 (profile lưu team).
- **Sau:** 30 (battle dùng team+formation), 43 (sweep dùng team).

# Phạm vi

- Server: model Team (6 slot) + Formation (lưới vị trí, ~2×3 theo A12 — cấu hình được), lưu profile, validate (đúng 6, không trùng hero — GP6).
- Client: UI chọn hero + đặt vị trí; hiển thị formation; lưu qua server command.
- Snapshot team để đưa vào sim (server tạo snapshot khi battle).

# Không thuộc phạm vi

- Battle thực (phase 30).
- Nhiều đội/preset (Post-MVP nếu có).
- Số liệu bonus vị trí (config/tuning).

# Deliverables

- Server: Team/Formation model + command lưu + validate.
- Client: UI formation (chọn hero, đặt vị trí, lưu).
- Test: lưu/đọc team; validate 6 hero; vị trí ảnh hưởng sim (input khác → kết quả khác).
- Cập nhật [`../gameplay/hero-system.md`](../gameplay/hero-system.md) (formation) / combat-framework.

# Công việc cần thực hiện

- [ ] Server Domain: `Team` (6 slot, ref OwnedHero) + `Formation` (lưới vị trí cấu hình từ config).
- [ ] Application: `SaveTeamCommand` (validate đúng 6, hero thuộc sở hữu, không trùng) + `GetMyTeamQuery`.
- [ ] Tạo team snapshot (chỉ số tại thời điểm) để feed sim (phase 30).
- [ ] Client feature `formation/`: UI chọn hero + đặt vị trí lưới; lưu qua command; hiển thị.
- [ ] Contract DTO team/formation + codegen.
- [ ] Test: lưu/đọc team; validate lỗi (thiếu hero/trùng/không sở hữu); đổi vị trí → sim input đổi.
- [ ] Cập nhật `../gameplay/hero-system.md`.

# Tiêu chí hoàn thành

- Lưu đội 6 hero + vị trí; validate chặn sai (≠6, trùng, không sở hữu).
- Team lưu server-authoritative; client chỉ gửi intent.
- Vị trí formation ảnh hưởng sim (target/aggro) — test chứng minh input khác→kết quả khác.
- Test server + client xanh.

# Cách kiểm tra

- `dotnet test`: save/get team, validate, snapshot; đổi formation → sim khác.
- gdUnit4: UI đặt hero/vị trí, lưu, hiển thị lại đúng.
- Thử lưu team sai (5 hero) → bị chặn.

# Rủi ro

- **Client tự ý sửa team không qua server** → mọi lưu qua command + validate server.
- **Vị trí không tác động sim** → spec 23 định nghĩa aggro theo vị trí; test đối chứng.
- **Trùng hero/không sở hữu** → validate server bắt buộc.

# Ghi chú

Grid formation kích thước theo config (A12 ~2×3, chưa khoá cứng). Một-hero-một-slot (GP6) theo assumption; validate theo config. Bám [`../gameplay/hero-system.md`](../gameplay/hero-system.md).

# Technical Debt Review

- **Maintainability:** team/formation tách rõ; grid cấu hình.
- **Scalability:** hỗ trợ preset/nhiều đội sau (Post-MVP).
- **Testing:** validate + tác động sim có test.
- **Security:** team server-authoritative, validate sở hữu.
- **Nợ:** bonus vị trí (config); preset (Post-MVP).

# Phase Review

Đóng khi team 6+formation lưu server-authoritative, validate, tác động sim, UI hoạt động, test xanh.

---

## Liên kết
- [`../gameplay/hero-system.md`](../gameplay/hero-system.md) · [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) · [`../godot/ui-architecture.md`](../godot/ui-architecture.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`30-battle-flow-e2e.md`](30-battle-flow-e2e.md)
