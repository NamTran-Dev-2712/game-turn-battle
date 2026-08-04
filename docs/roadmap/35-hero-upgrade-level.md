# 35 — Hero upgrade (Level/EXP)

> Mục đích: Hiện thực **nâng cấp hero bằng Level/EXP** (trục nâng cấp Must đầu tiên) — tiêu tài nguyên để tăng sức mạnh, giúp vượt "wall" campaign.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 8 Đóng vòng Core Loop | P3 | S9 | F06 |

# Mục tiêu

Hero có level (EXP), nâng cấp tiêu tài nguyên (gold/EXP item) qua command atomic; chỉ số hero tính theo level + config (data-driven); Power Rating cập nhật; server-authoritative.

# Lý do

Nâng cấp Level là trục Must (F06) — mắt xích "thưởng → nâng cấp → đẩy xa" của loop. Cần sau campaign (34, nguồn tài nguyên) và trước AFK (37) để có sink cho AFK reward.

# Phụ thuộc

- **Trước:** 27 (hero), 31 (currency spend), 34 (nguồn tài nguyên), 21 (config đường cong level).
- **Sau:** 37 (AFK cấp tài nguyên nâng cấp), 39 (ascension trục kế).

# Phạm vi

- Level/EXP trên OwnedHero; đường cong cost/stat theo config (EC4) — số liệu ở config.
- Command `LevelUpHero` (tiêu gold/EXP item atomic) → tăng level → chỉ số tính lại.
- Chỉ số hero = base(config) × hàm(level) — deterministic, integer.
- Cập nhật Power Rating (dùng gate độ khó campaign).

# Không thuộc phạm vi

- Star/Ascension (phase 39).
- Equipment (phase 38).
- Skill level (Could/Post-MVP).

# Deliverables

- Level/EXP + command nâng cấp atomic.
- Chỉ số theo level (data-driven) phản ánh vào combat sim.
- Integration test: nâng cấp tiêu tài nguyên atomic; chỉ số tăng đúng công thức; thiếu tài nguyên chặn.
- Cập nhật [`../gameplay/hero-system.md`](../gameplay/hero-system.md) + [`../mvp/05-player-progression.md`](../mvp/05-player-progression.md).

# Công việc cần thực hiện

- [ ] Schema: đường cong level (cost gold/EXP, stat theo level) trong config — không nhúng số vào code.
- [ ] Domain: level/EXP trên OwnedHero; hàm tính stat theo level (integer, deterministic).
- [ ] Application: `LevelUpHeroCommand` (spend tài nguyên atomic 31) → tăng level → cập nhật stat/Power.
- [ ] Đảm bảo sim (24/25) dùng chỉ số theo level (snapshot khi battle).
- [ ] Client: UI nâng cấp hero (hiển thị cost, kết quả).
- [ ] Integration test: nâng cấp atomic; stat tăng đúng công thức config; thiếu tài nguyên chặn; đổi config đường cong→đổi.
- [ ] Cập nhật `../gameplay/hero-system.md`.

# Tiêu chí hoàn thành

- Nâng cấp tiêu tài nguyên atomic; thiếu → chặn.
- Chỉ số hero tăng theo công thức config (đổi config → đổi, không sửa code).
- Level ảnh hưởng combat (team snapshot dùng stat mới).
- Server-authoritative; client chỉ hiển thị/gửi intent.

# Cách kiểm tra

- `dotnet test`: level-up atomic, stat đúng, thiếu tài nguyên chặn, data-driven.
- Local: nâng cấp hero → chỉ số tăng → trận mạnh hơn.
- Đổi đường cong config → hành vi đổi (test).

# Rủi ro

- **Client tự tăng level/stat** → server-authoritative + spend atomic.
- **Công thức stat dùng float** → integer/fixed-point (nhất quán combat).
- **Gold sink lệch (lạm phát/thiếu)** → số ở config, tune (EC4).

# Ghi chú

Level là 1 trong 3 trục nâng cấp MVP (A06: Level + Star + Gear). Số liệu đường cong là tuning (config). Bám [`../gameplay/hero-system.md`](../gameplay/hero-system.md) + [`../mvp/05-player-progression.md`](../mvp/05-player-progression.md).

# Technical Debt Review

- **Maintainability:** đường cong là data; tune không sửa code.
- **Scalability:** thêm trục (star/gear) tái dùng pattern.
- **Testing:** atomic + công thức có test.
- **Security:** nâng cấp server-authoritative.
- **Nợ:** star/gear (39/38); balance số (tuning).

# Phase Review

Đóng khi level-up atomic + stat data-driven ảnh hưởng combat + server-authoritative, test xanh.

---

## Liên kết
- [`../gameplay/hero-system.md`](../gameplay/hero-system.md) · [`../mvp/05-player-progression.md`](../mvp/05-player-progression.md) · [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md)
- ADR: [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`36-energy-system.md`](36-energy-system.md)
