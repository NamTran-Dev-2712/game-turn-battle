# 44 — Faction/Element advantage

> Mục đích: Hiện thực **khắc chế theo faction/element** (counter) — thêm chiều chiến thuật cho combat (Could), hoàn toàn data-driven trong sim.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 9 Kinh tế & QoL | P4 | S11 | F21 |

# Mục tiêu

Bảng khắc chế (faction hoặc element — trục theo GP3/A11) định nghĩa config; sim (24/25) áp bonus damage/hiệu ứng khi có counter; khớp golden vector; UI gợi ý counter.

# Lý do

Counter tạo chiều sâu "đội hình theo địch" (F21, Could). Đưa vào sim qua config (ADR-004) — không hardcode. Làm sau khi combat/skill ổn định.

# Phụ thuộc

- **Trước:** 24/25 (sim), 28 (skill/effect), 27 (hero có faction/element), 26 (golden).
- **Sau:** cập nhật golden (26); tuning.

# Phạm vi

- Bảng counter config (trục faction **hoặc** element theo quyết định GP3/A11; nếu chưa chốt → ghi open-question, làm cấu hình linh hoạt).
- Sim áp bonus khi attacker counter defender (magnitude từ config, integer).
- Cập nhật golden vector phủ counter.
- UI: chỉ báo lợi/bất lợi trong formation/battle.

# Không thuộc phạm vi

- Nhiều tầng counter phức tạp (Post-MVP).
- Số liệu % bonus (config).
- Quyết định trục counter (nếu chưa chốt → open-question, không tự quyết).

# Deliverables

- Bảng counter config + áp dụng trong sim hai phía + golden cập nhật.
- Integration/gdUnit4 test: counter tăng damage đúng config; golden phủ counter khớp hai phía.
- UI chỉ báo counter.
- Cập nhật [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) + [`../mvp/10-open-questions.md`](../mvp/10-open-questions.md) (GP3 nếu cần).

# Công việc cần thực hiện

- [ ] Xác nhận trục counter (faction/element) từ GP3/A11; nếu chưa chốt → ghi `../mvp/10`, thiết kế cấu hình linh hoạt (đổi trục qua config).
- [ ] Schema counter (mở rộng 06): cặp counter + magnitude.
- [ ] Sim server (24): áp bonus khi counter (integer, deterministic).
- [ ] Sim client (25): áp dụng khớp server.
- [ ] Cập nhật golden vector (26) phủ counter → gate xanh hai phía.
- [ ] Client: chỉ báo lợi/bất lợi ở formation/battle.
- [ ] Test: counter tăng damage đúng config; golden khớp; đổi bảng config → đổi.
- [ ] Cập nhật `../gameplay/combat-framework.md`.

# Tiêu chí hoàn thành

- Counter áp dụng trong sim theo config; khớp golden vector hai phía.
- Đổi bảng counter config → hành vi đổi (data-driven).
- UI chỉ báo counter đúng.
- Deterministic (integer), không phá golden hiện có.

# Cách kiểm tra

- `dotnet test` + gdUnit4: counter damage đúng; golden phủ counter khớp.
- Đổi bảng counter config → test phản ánh.
- Local: đội counter thắng dễ hơn (đối chứng).

# Rủi ro

- **Trục counter chưa chốt (GP3)** → cấu hình linh hoạt + open-question; không hardcode trục.
- **Phá golden hiện có** → cập nhật baseline có chủ đích (quy trình phase 26).
- **Bonus dùng float** → integer.

# Ghi chú

Could-have — có thể cắt nếu trễ (MoSCoW). Trục counter (faction/element) theo GP3/A11; nếu chưa quyết, thiết kế đổi được qua config. Bám ADR-004/011.

# Technical Debt Review

- **Maintainability:** counter là bảng data; đổi trục/số qua config.
- **Scalability:** nền cho hệ counter sâu hơn (Post-MVP).
- **Testing:** golden phủ counter.
- **Security:** determinism giữ server-authority.
- **Nợ:** counter đa tầng (Post-MVP); tune số.

# Phase Review

Đóng khi counter data-driven áp dụng trong sim + khớp golden hai phía + UI chỉ báo, test xanh. **Kết thúc nhóm 9 (P4).**

---

## Liên kết
- [`../gameplay/combat-framework.md`](../gameplay/combat-framework.md) · [`../mvp/03-core-gameplay.md`](../mvp/03-core-gameplay.md) · [`../mvp/10-open-questions.md`](../mvp/10-open-questions.md)
- ADR: [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md) · [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`45-ranking-leaderboard.md`](45-ranking-leaderboard.md)
