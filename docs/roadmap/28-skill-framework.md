# 28 — Skill framework (effect-data + handler registry)

> Mục đích: Hiện thực **khung skill data-driven**: skill = effect-data + handler registry (ADR-004), tích hợp vào combat sim hai phía — mở rộng skill bằng data, không `switch/if`.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 6 Gameplay Vertical Slice | P2 | S7 | F03 |

# Mục tiêu

Khung skill: định nghĩa skill trong config (effect-data: loại effect, target, hệ số, điều kiện energy/cooldown), registry handler cho từng loại effect (damage/heal/buff/debuff…) ở **cả** sim server (24) và client (25), khớp golden vector.

# Lý do

ADR-004 cấm `switch/if` để mở rộng gameplay; skill phải là data + registry. Skill là cơ chế cốt lõi của combat auto; cần trước Battle (30) và để combat có chiều sâu.

# Phụ thuộc

- **Trước:** 24 (sim server), 25 (sim client), 27 (hero refs skill), 06 (skill schema).
- **Sau:** 30 (battle dùng skill), 44 (faction advantage), nội dung skill mở rộng.

# Phạm vi

- Schema skill (mở rộng phase 06): effect-data (type, target, magnitude từ config, energy/cooldown, điều kiện).
- Registry handler effect ở server & client (cùng quy tắc, khớp golden).
- Vài loại effect nền (damage, heal, buff/debuff, ultimate theo energy).
- Cập nhật golden vector (phase 26) phủ skill.

# Không thuộc phạm vi

- Toàn bộ nội dung skill của mọi hero (content, thêm dần qua config).
- Skill preview animation nâng cao (Could — phase UI sau).
- Số liệu balance skill (config/tuning).

# Deliverables

- Schema skill hoàn chỉnh + registry effect hai phía.
- Vài effect nền hoạt động trong sim, khớp golden.
- Golden vector mở rộng phủ skill (đồng bộ phase 26).
- Cập nhật [`../gameplay/skill-framework.md`](../gameplay/skill-framework.md).

# Công việc cần thực hiện

- [ ] Hoàn thiện `skill.schema.json` (effect-data: type/target/magnitude/energy/cooldown/điều kiện).
- [ ] Server: registry effect handler (`IEffectHandler` theo type) — thêm không `switch` mở rộng.
- [ ] Client: registry effect handler GDScript tương ứng (khớp quy tắc server).
- [ ] Hiện thực effect nền: damage, heal, buff/debuff, ultimate (energy-triggered).
- [ ] Tích hợp registry vào sim (24/25): sim gọi handler theo effect-data.
- [ ] Mở rộng golden vector (26) phủ các skill → gate xanh hai phía.
- [ ] Test: skill từ config chạy đúng; thêm skill mới bằng config không sửa lõi.
- [ ] Cập nhật `../gameplay/skill-framework.md`.

# Tiêu chí hoàn thành

- Skill định nghĩa hoàn toàn bằng config (effect-data); không `switch/if` để mở rộng loại.
- Effect chạy đúng trong sim server & client, **khớp golden vector** (phase 26 mở rộng).
- Thêm 1 skill mới qua config (không sửa code lõi) → hoạt động.
- Test hai phía xanh; golden gate xanh.

# Cách kiểm tra

- `dotnet test` + gdUnit4: skill effect đúng; golden vector phủ skill khớp.
- Thêm skill config mới trong test → chạy không sửa lõi.
- Grep: không `switch/if` mở rộng loại skill (dùng registry).

# Rủi ro

- **Registry lệch giữa hai phía** → golden vector phủ skill bắt drift; cùng spec (23).
- **Effect phức tạp phá determinism** → mọi effect tuân fixed-point + thứ tự xác định.
- **Cám dỗ hardcode skill đặc biệt** → bắt buộc qua effect-data; review ADR-004.

# Ghi chú

Skill mới = thêm effect-data (config) + (nếu loại effect mới) thêm handler đăng ký registry. Bám [`../gameplay/skill-framework.md`](../gameplay/skill-framework.md) + ADR-004/011.

# Technical Debt Review

- **Maintainability:** skill là data + registry; mở rộng an toàn.
- **Scalability:** thêm hero/skill không phình lõi.
- **Testing:** golden phủ skill; test effect.
- **Security:** determinism giữ server-authority.
- **Nợ:** nội dung skill đầy đủ (content); animation preview (UI sau).

# Phase Review

Đóng khi skill data-driven + registry hai phía chạy khớp golden, thêm skill bằng config không sửa lõi, test/gate xanh.

---

## Liên kết
- [`../gameplay/skill-framework.md`](../gameplay/skill-framework.md) · [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md)
- ADR: [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md) · [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`29-formation-team.md`](29-formation-team.md)
