# 39 — Ascension / Star-up

> Mục đích: Hiện thực **nâng sao/ascension** (tối giản) — trục nâng cấp thứ ba, tiêu **fragment** (từ dupes gacha) để tăng bậc sao, mở sink cho fragment.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 9 Kinh tế & QoL | P4 | S10 | F14 |

# Mục tiêu

Hero có bậc sao; nâng sao tiêu fragment (+ tài nguyên) theo config; tăng chỉ số/mở tiềm năng; server-authoritative atomic. Đóng vòng "dupes→fragment→ascension".

# Lý do

Ascension là trục Should (A06) + sink chính cho fragment (từ gacha 33), giải bottleneck fragment (mvp/05). Thêm chiều sâu sau equipment.

# Phụ thuộc

- **Trước:** 33 (fragment nguồn), 32 (inventory), 31 (tài nguyên), 27/35 (hero/stat).
- **Sau:** tuning kinh tế; leaderboard power (45).

# Phạm vi

- Bậc sao trên OwnedHero; cost (fragment + tài nguyên) + hiệu ứng stat theo config.
- Command `AscendHeroCommand` atomic (tiêu fragment/tài nguyên) → tăng sao → stat.
- Ràng buộc: đủ fragment/điều kiện mới nâng.
- Client: UI nâng sao.

# Không thuộc phạm vi

- Awakening/enhancement (Future).
- Số liệu cost/stat (config — EC4).
- Skill unlock theo sao nâng cao (Post-MVP nếu có).

# Deliverables

- Ascension + command atomic + tác động stat.
- Integration test: nâng sao tiêu fragment atomic; thiếu→chặn; stat tăng; data-driven.
- Client UI nâng sao.
- Cập nhật [`../gameplay/hero-system.md`](../gameplay/hero-system.md) + [`../mvp/05-player-progression.md`](../mvp/05-player-progression.md).

# Công việc cần thực hiện

- [ ] Schema ascension (mở rộng 06): cost fragment+tài nguyên theo bậc, hiệu ứng stat.
- [ ] Domain: bậc sao trên OwnedHero; hàm stat theo sao (integer).
- [ ] Application: `AscendHeroCommand` (spend fragment/tài nguyên atomic 31/32) → tăng sao → stat/Power.
- [ ] Snapshot battle dùng stat theo sao.
- [ ] Client feature (trong hero/): UI nâng sao (cost, kết quả).
- [ ] Integration test: atomic, thiếu fragment chặn, stat đúng, đổi config→đổi.
- [ ] Cập nhật `../gameplay/hero-system.md`.

# Tiêu chí hoàn thành

- Nâng sao tiêu fragment/tài nguyên atomic; thiếu → chặn.
- Stat theo sao ảnh hưởng combat; data-driven.
- Server-authoritative; client hiển thị/gửi intent.
- Dupes gacha → fragment → ascension khép mạch (test đối chứng).

# Cách kiểm tra

- `dotnet test`: ascend atomic, thiếu chặn, stat, data-driven.
- Local: dupe hero → fragment → nâng sao → hero mạnh hơn.
- Đổi cost config → đổi (test).

# Rủi ro

- **Client tự nâng sao** → server-authoritative + spend atomic.
- **Fragment bottleneck** → cân số ở config (EC), theo mvp/05 escape valve.
- **Stat sao dùng float** → integer.

# Ghi chú

MVP giữ ascension **tối giản** (mvp/05: 3 trục để tránh overload). Số liệu là tuning. Bám [`../gameplay/hero-system.md`](../gameplay/hero-system.md) + ADR-004.

# Technical Debt Review

- **Maintainability:** cost/stat là data.
- **Scalability:** nền awakening (Future) không phá.
- **Testing:** atomic + stat + mạch fragment.
- **Security:** server-authoritative.
- **Nợ:** awakening (Future); balance (tuning).

# Phase Review

Đóng khi ascension tiêu fragment atomic + stat data-driven + mạch dupes→fragment→sao, test xanh.

---

## Liên kết
- [`../gameplay/hero-system.md`](../gameplay/hero-system.md) · [`../mvp/05-player-progression.md`](../mvp/05-player-progression.md) · [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md)
- ADR: [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`40-shop-static.md`](40-shop-static.md)
