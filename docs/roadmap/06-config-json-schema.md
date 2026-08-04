# 06 — JSON Schema cho config (data-driven foundation)

> Mục đích: Định nghĩa **JSON Schema** cho mọi loại config gameplay (hero/skill/stage/gacha/shop/reward/economy/quest) trong `shared/config-schema/`, làm khung dữ liệu data-driven — code phụ thuộc **schema**, không phụ thuộc giá trị.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 1 Hợp đồng & Config | P1 | S1 | nền data-driven |

# Mục tiêu

Mở rộng `config-bundle.schema.json` (hiện là placeholder) thành bộ schema đầy đủ per-type theo ADR-004/005, có `schema_version`, ràng buộc kiểu (integer cho giá trị combat), và tham chiếu ID chéo (hero↔skill, stage↔reward…).

# Lý do

Data-driven là nền của **mọi** gameplay & LiveOps. Schema phải có trước khi tác giả config điền số, và trước khi Configuration Service (phase 21) nạp/validate. Định nghĩa schema (không phải giá trị) tránh refactor lớn khi thêm nội dung.

# Phụ thuộc

- **Trước:** 05 (enum dùng chung để tham chiếu trong schema).
- **Sau:** 07 (validator dùng schema), 21 (Config Service validate), 27/28/33/34/38… (feature đọc config theo schema).

# Phạm vi

- Schema per-type: `hero`, `skill`, `stage`, `gacha` (rate/pity structure, **không giá trị**), `shop`, `reward`, `economy` (đường cong dạng cấu trúc), `quest`.
- Chuẩn: JSON Schema draft 2020-12, `snake_case` key, **integer** cho giá trị combat (không float — ADR-011), mọi file có `schema_version`.
- ID quy ước prefix theo type (`hero_*`, `stage_*`) khớp `.tres` (ADR-004).
- `_versions/` cho schema migration; envelope bundle giữ tương thích `config@vN`.

# Không thuộc phạm vi

- **Giá trị balance thực** (số rate, chỉ số hero) — thuộc nội dung/tuning, không thuộc roadmap; điền dần ở phase feature, tham chiếu [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md).
- Công cụ validate (phase 07).
- Config Service runtime (phase 21).

# Deliverables

- Bộ file `shared/config-schema/*.schema.json` cho 8 loại config.
- Vài **config mẫu tối thiểu** (fixture) để test schema (không phải balance thật).
- Tài liệu ánh xạ schema ↔ tài liệu gameplay tương ứng.
- Ghi chú quy tắc migration schema trong `_versions/`.

# Công việc cần thực hiện

- [ ] Viết schema `hero.schema.json` (id, faction/class/element/role enum, base stats integer, skill refs) — bám [`../gameplay/hero-system.md`](../gameplay/hero-system.md).
- [ ] `skill.schema.json`: effect-data + type (theo registry, ADR-004) — bám [`../gameplay/skill-framework.md`](../gameplay/skill-framework.md).
- [ ] `stage.schema.json`, `reward.schema.json`, `gacha.schema.json` (cấu trúc rate/pity, không số) — bám [`../gameplay/progression-and-economy.md`](../gameplay/progression-and-economy.md).
- [ ] `shop.schema.json`, `economy.schema.json` (cấu trúc source/sink & đường cong), `quest.schema.json` — bám [`../gameplay/quest-system.md`](../gameplay/quest-system.md).
- [ ] Ràng buộc: `required`, kiểu integer cho giá trị combat, `additionalProperties:false` nơi cần chặt, `schema_version`.
- [ ] Định nghĩa quy ước ID + prefix; ghi cách tham chiếu chéo (hero→skill id).
- [ ] Tạo fixture mẫu tối thiểu cho mỗi type để test.
- [ ] Cập nhật `../gameplay/configuration-and-data.md` bảng ánh xạ schema.

# Tiêu chí hoàn thành

- 8 schema tồn tại, hợp lệ JSON Schema 2020-12 (self-validate).
- Fixture mẫu pass schema; một fixture cố ý sai **fail** đúng quy tắc.
- Không giá trị balance nào bị "khoá cứng" trong schema (chỉ ràng buộc kiểu/khoảng).
- Key `snake_case`, giá trị combat kiểu integer.

# Cách kiểm tra

- Chạy validator JSON Schema (ajv/python-jsonschema) trên fixture hợp lệ → pass.
- Fixture sai kiểu (float ở giá trị combat / thiếu `schema_version`) → fail đúng.
- Rà tay: mọi schema có `schema_version`, prefix ID nhất quán.

# Rủi ro

- **Schema quá chặt chặn tuning về sau** → chỉ ràng buộc kiểu/khoảng cấu trúc, không cố định giá trị.
- **Tham chiếu ID chéo khó kiểm bằng schema đơn** → referential-integrity để phase 07 (validator).
- **Float lọt vào** → lint kiểu integer cho trường combat.

# Ghi chú

Schema là hợp đồng dữ liệu (ADR-004/005). Đổi schema breaking ⇒ tăng `schema_version` + migration trong `_versions/` + doc-sync (`../liveops/remote-config.md`, `../gameplay/configuration-and-data.md`).

# Technical Debt Review

- **Maintainability:** schema tách khỏi giá trị ⇒ thêm nội dung không đụng code.
- **Scalability:** versioning + `_versions/` cho tiến hoá dài hạn.
- **Testing:** fixture pass/fail là hợp đồng kiểm.
- **Security:** validate chặn dữ liệu dị dạng vào runtime.
- **Nợ:** referential integrity (phase 07); giá trị thật (phase feature).

# Phase Review

Đóng khi 8 schema hợp lệ, fixture pass/fail đúng, không hardcode balance, quy ước ID/version rõ.

---

## Liên kết
- [`../gameplay/configuration-and-data.md`](../gameplay/configuration-and-data.md) · [`../gameplay/hero-system.md`](../gameplay/hero-system.md) · [`../gameplay/skill-framework.md`](../gameplay/skill-framework.md) · [`../conventions/data-and-docs-conventions.md`](../conventions/data-and-docs-conventions.md)
- ADR: [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md) · [`../adr/ADR-005-configuration-strategy.md`](../adr/ADR-005-configuration-strategy.md) · [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`07-config-validator-tool.md`](07-config-validator-tool.md)
