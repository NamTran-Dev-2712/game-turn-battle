# 54 — Regression/Smoke suite + golden coverage

> Mục đích: Dựng **bộ smoke/regression** phủ các luồng cốt lõi + mở rộng golden vector coverage, làm cổng chất lượng cuối trước release.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 12 Polish & Release | P7 | S13 | polish |

# Mục tiêu

Suite smoke chạy nhanh phủ loop cốt lõi (login→summon→team→battle→reward→upgrade→AFK claim) + các hệ (shop/quest/mail/leaderboard); golden vector combat mở rộng phủ nội dung; tất cả gắn CI làm cổng release.

# Lý do

Trước release cần "lưới an toàn" phát hiện hồi quy nhanh. Smoke + golden là cổng cuối, đảm bảo thay đổi về sau (LiveOps/content) không phá loop/combat.

# Phụ thuộc

- **Trước:** toàn bộ feature (30–51); 26 (golden); 48 (hardening).
- **Sau:** 55 (release dùng smoke làm cổng).

# Phạm vi

- Smoke suite (integration server + gdUnit4 client) phủ loop cốt lõi + các hệ chính.
- Golden vector combat mở rộng phủ nhiều hero/skill/counter.
- Gắn smoke + golden vào CI làm **cổng release** (release.yml phụ thuộc).
- Báo cáo coverage các luồng.

# Không thuộc phạm vi

- Unit test mới cho feature (đã có ở từng phase).
- Load/stress test quy mô lớn (Post-MVP).
- Tính năng mới.

# Deliverables

- Smoke suite (server + client) chạy trong CI.
- Golden vector mở rộng (phủ nội dung chính).
- CI: cổng smoke+golden trước release.
- Báo cáo coverage luồng cốt lõi.
- Cập nhật [`../testing/README.md`](../testing/README.md) + backend/godot testing.

# Công việc cần thực hiện

- [ ] Liệt kê luồng cốt lõi cần smoke (loop + shop/quest/mail/leaderboard/tutorial).
- [ ] Viết smoke integration server (end-to-end qua API, DB/Redis Testcontainers).
- [ ] Viết smoke client gdUnit4 (boot→loop chính, mock/stub server nơi cần).
- [ ] Mở rộng golden vector (26) phủ nhiều hero/skill/counter; gate hai phía.
- [ ] Gắn smoke + golden vào CI; `release.yml` phụ thuộc các gate này.
- [ ] Báo cáo coverage luồng; xác định khoảng trống.
- [ ] Cập nhật `../testing/README.md`, `../testing/backend-testing.md`, `../testing/godot-testing.md`.

# Tiêu chí hoàn thành

- Smoke suite phủ loop cốt lõi + các hệ chính, chạy xanh trong CI.
- Golden vector mở rộng phủ nội dung; gate hai phía xanh.
- Release bị chặn nếu smoke/golden đỏ (cổng hoạt động — thử negative).
- Báo cáo coverage luồng có, khoảng trống ghi rõ.

# Cách kiểm tra

- CI chạy smoke + golden trên PR/nhánh release → xanh.
- Negative: phá 1 luồng → smoke đỏ → release bị chặn → revert.
- Local: chạy smoke server (`dotnet test` suite smoke) + client (gdUnit4).

# Rủi ro

- **Smoke chậm/flaky** → giữ nhanh, deterministic; tách khỏi unit; ổn định hoá.
- **Phủ thiếu luồng** → checklist luồng cốt lõi; bổ sung.
- **Golden phình to** → chọn vector đại diện; cập nhật baseline có chủ đích (26).

# Ghi chú

Smoke + golden là cổng release (P7). Load/stress lớn là Post-MVP. Bám [`../testing/`](../testing/README.md) + ADR-011 (golden).

# Technical Debt Review

- **Maintainability:** lưới an toàn giảm sợ thay đổi.
- **Scalability:** smoke nhanh cho CI thường xuyên.
- **Testing:** đây là cổng chất lượng cuối.
- **Security:** smoke phủ luồng nhạy cảm (không double/không cheat).
- **Nợ:** load/stress (Post-MVP).

# Phase Review

Đóng khi smoke + golden mở rộng chạy trong CI làm cổng release (kèm negative test), coverage luồng cốt lõi đạt.

---

## Liên kết
- [`../testing/README.md`](../testing/README.md) · [`../testing/backend-testing.md`](../testing/backend-testing.md) · [`../testing/godot-testing.md`](../testing/godot-testing.md)
- ADR: [`../adr/ADR-011-combat-authority-and-determinism.md`](../adr/ADR-011-combat-authority-and-determinism.md)
- Roadmap: [`README.md`](README.md) → kế: [`55-release-soft-launch.md`](55-release-soft-launch.md)
