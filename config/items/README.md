# `config/items/` — Catalog vật phẩm

Định nghĩa vật phẩm (item catalog) data-driven (ADR-004) — nền cho inventory (Phase 32). **Owner:** Game design. Số lượng người chơi sở hữu do **server-authoritative** (không nằm ở config). Mảnh hero (fragment) tham chiếu `hero_id`, KHÔNG phải catalog item. Schema: `../../shared/config-schema/item.schema.json`.
