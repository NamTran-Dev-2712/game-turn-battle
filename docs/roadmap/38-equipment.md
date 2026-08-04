# 38 — Equipment (basic gear)

> Mục đích: Hiện thực **trang bị cơ bản** (gear cộng chỉ số) — trục nâng cấp thứ hai (Should), lắp/tháo lên hero, ảnh hưởng combat.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 9 Kinh tế & QoL | P4 | S10 | F15 |

# Mục tiêu

Item trang bị (config-driven, cộng stat) trong inventory; lắp/tháo lên hero qua command server; chỉ số hero cộng gear khi vào combat; server-authoritative.

# Lý do

Equipment là trục nâng cấp Should (A06: Level+Star+Gear) thêm chiều sâu sau khi loop nền chắc (post-P3). Data-driven để thêm gear không sửa code.

# Phụ thuộc

- **Trước:** 32 (inventory item), 27/35 (hero/stat), 31 (nếu craft tốn tiền), 06 (schema item).
- **Sau:** 39 (ascension), tuning kinh tế.

# Phạm vi

- Item gear định nghĩa config (slot, stat cộng); lưu trong inventory.
- Command lắp/tháo gear lên OwnedHero (server, validate slot/sở hữu).
- Chỉ số hero = base + level + **gear** khi snapshot battle.
- Client: UI trang bị (lắp/tháo, xem stat).

# Không thuộc phạm vi

- Set/artifact/forge/reforge sâu (Future — F33).
- Số liệu stat gear (config).
- Ascension (phase 39).

# Deliverables

- Gear item + lắp/tháo command + tác động combat.
- Integration test: lắp/tháo hợp lệ; stat cộng đúng; validate slot/sở hữu; ảnh hưởng sim.
- Client UI trang bị.
- Cập nhật [`../gameplay/inventory-and-equipment.md`](../gameplay/inventory-and-equipment.md).

# Công việc cần thực hiện

- [ ] Schema gear (mở rộng 06): slot, stat cộng, rarity — số ở config.
- [ ] Domain: gear là item trong inventory; quan hệ hero↔gear đã lắp.
- [ ] Application: `EquipGearCommand`/`UnequipGearCommand` (validate slot đúng, hero+gear thuộc sở hữu, một gear một slot).
- [ ] Snapshot battle cộng stat gear (vào sim 24/25).
- [ ] Client feature `equipment/`: lắp/tháo, hiển thị stat trước/sau.
- [ ] Integration test: lắp/tháo, stat cộng đúng, validate lỗi, tác động sim.
- [ ] Cập nhật `../gameplay/inventory-and-equipment.md`.

# Tiêu chí hoàn thành

- Lắp/tháo gear server-authoritative; validate slot/sở hữu.
- Stat gear cộng vào hero, ảnh hưởng combat (test đối chứng).
- Data-driven (đổi stat gear config → đổi, không sửa code).
- Client hiển thị đúng; không client-authority.

# Cách kiểm tra

- `dotnet test`: equip/unequip, stat, validate, tác động sim.
- Local: lắp gear → hero mạnh hơn trong trận.
- gdUnit4: UI trang bị.

# Rủi ro

- **Client tự cộng stat** → server-authoritative, snapshot server.
- **Lắp gear không sở hữu/sai slot** → validate server.
- **Stat gear dùng float** → integer (nhất quán combat).

# Ghi chú

MVP giữ gear **cơ bản** (tránh bùng nổ balance — mvp/05). Set/forge là Future. Bám [`../gameplay/inventory-and-equipment.md`](../gameplay/inventory-and-equipment.md) + ADR-004.

# Technical Debt Review

- **Maintainability:** gear là data; thêm không sửa code.
- **Scalability:** nền cho gear sâu hơn (Post-MVP) không phá.
- **Testing:** lắp/tháo + tác động sim có test.
- **Security:** gear server-authoritative.
- **Nợ:** set/forge (Future); balance (tuning).

# Phase Review

Đóng khi gear lắp/tháo server-authoritative + cộng stat + ảnh hưởng combat + data-driven, test xanh.

---

## Liên kết
- [`../gameplay/inventory-and-equipment.md`](../gameplay/inventory-and-equipment.md) · [`../gameplay/hero-system.md`](../gameplay/hero-system.md)
- ADR: [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`39-ascension-star.md`](39-ascension-star.md)
