# 27 — Hero system (data-driven)

> Mục đích: Hiện thực **hệ Hero** từ config (faction/class/element/role/stats/skill refs) ở cả server (chân lý) và client (hiển thị) — nền cho team, combat, collection.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 6 Gameplay Vertical Slice | P2 | S7 | F01 |

# Mục tiêu

Định nghĩa Hero data-driven: server đọc định nghĩa hero từ config bundle (qua provider) và quản lý hero sở hữu của người chơi (gắn profile); client hiển thị hero từ ConfigProvider + StateCache. Không hardcode chỉ số.

# Lý do

Hero là "trái tim collection" (F01). Cần trước Formation/Battle (phase 29–30) và Summon (phase 33). Data-driven từ đầu (ADR-004) để thêm hero không đụng code.

# Phụ thuộc

- **Trước:** 22 (config e2e), 19 (profile), 06 (hero schema).
- **Sau:** 28 (skill), 29 (formation), 33 (summon nhận hero), 35 (upgrade).

# Phạm vi

- Server: model Hero definition (đọc config) + Hero sở hữu (instance gắn profile: id, level nền, sao nền — mở rộng sau).
- Client: hiển thị hero (art lazy theo ADR-009), thông tin từ config.
- Query hero của người chơi (server-authoritative).
- Tuân schema hero (phase 06) + glossary faction/class/element/role.

# Không thuộc phạm vi

- Nâng cấp level/sao (phase 35/39).
- Skill logic (phase 28).
- Summon (phase 33) — ở đây chỉ có hero (seed tạm cho test).

# Deliverables

- Server: Hero definition (config) + Hero owned (profile) + query.
- Client: màn hero list/detail đọc config.
- Test: server đọc hero từ config; đổi config hero → dữ liệu đổi; client hiển thị.
- Cập nhật [`../gameplay/hero-system.md`](../gameplay/hero-system.md).

# Công việc cần thực hiện

- [ ] Server Domain: `HeroDefinition` (đọc config: faction/class/element/role/base stats/skill refs) + `OwnedHero` (gắn profile, cấp/sao nền).
- [ ] Application: query `GetMyHeroes`, `GetHeroDefinition` qua `IConfigProvider`.
- [ ] Seed tạm vài hero owned cho test (tới phase 33 summon cấp thật).
- [ ] Client feature `hero/`: list + detail đọc từ ConfigProvider; art lazy (ADR-009).
- [ ] Contract DTO hero (mở rộng phase 05) + codegen client (phase 08).
- [ ] Test: server đọc đúng hero từ config; đổi config → đổi; client render list/detail.
- [ ] Cập nhật `../gameplay/hero-system.md`.

# Tiêu chí hoàn thành

- Server trả danh sách hero owned + definition từ config (không hardcode chỉ số).
- Đổi giá trị hero trong config → dữ liệu đổi không sửa code.
- Client hiển thị hero list/detail; art tải lazy.
- Test server + client xanh.

# Cách kiểm tra

- `dotnet test`: query hero, đổi config→đổi kết quả.
- gdUnit4: render hero list/detail từ config mock.
- Đổi chỉ số hero trong config → client thấy đổi (qua bundle).

# Rủi ro

- **Hardcode chỉ số lọt vào code** → review guard; chỉ số chỉ ở config (ADR-004).
- **Art nặng chặn UI** → lazy load + pooling (ADR-009).
- **Ref skill/hero thiếu** → validator (phase 07) bắt referential.

# Ghi chú

Số lượng hero MVP (A21/GP4) chưa chốt — không cản phase này (data-driven, thêm hero = thêm config). Bám [`../gameplay/hero-system.md`](../gameplay/hero-system.md) + ADR-004.

# Technical Debt Review

- **Maintainability:** hero là data; thêm không đụng code.
- **Scalability:** hỗ trợ nhiều hero; art lazy.
- **Testing:** data-driven test đổi config.
- **Security:** hero owned server-authoritative.
- **Nợ:** upgrade/skill (28/35); art thật (content).

# Phase Review

Đóng khi hero definition+owned server-authoritative đọc config, client hiển thị, đổi config→đổi, test xanh.

---

## Liên kết
- [`../gameplay/hero-system.md`](../gameplay/hero-system.md) · [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md) · [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md)
- ADR: [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md) · [`../adr/ADR-009-asset-loading.md`](../adr/ADR-009-asset-loading.md)
- Roadmap: [`README.md`](README.md) → kế: [`28-skill-framework.md`](28-skill-framework.md)
