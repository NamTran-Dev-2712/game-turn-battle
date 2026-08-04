# 46 — Tutorial/Onboarding

> Mục đích: Hiện thực **hướng dẫn/onboarding** người mới — dẫn dắt qua loop cốt lõi trong ~5 phút đầu (first-login), tăng tỉ lệ giữ chân D1.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 10 Retention & Tích hợp | P5 | S11 | F13 |

# Mục tiêu

Luồng tutorial: thắng 1 trận, có 5–6 hero (pull đầu được đảm bảo), hiểu các nút chính; bước tutorial data-driven (config), tiến độ tutorial lưu server; guard các bước.

# Lý do

Onboarding là Must (F13) — quyết định D1 retention (mvp/02 first-login ~5 phút). Cần sau khi loop + summon đủ để dẫn dắt. Guaranteed first pull tránh khởi đầu xui.

# Phụ thuộc

- **Trước:** 30 (battle), 33 (summon), 29 (formation), 17/20 (boot/auth), 37 (loop).
- **Sau:** 47 (daily login), 51 (telemetry funnel tutorial).

# Phạm vi

- Kịch bản tutorial data-driven (bước, highlight, mục tiêu, phần thưởng đảm bảo).
- Pull đầu đảm bảo (banner/kịch bản riêng, server cấp).
- Tiến độ tutorial lưu server (chống skip/lặp thưởng).
- Client: overlay hướng dẫn, khoá thao tác ngoài luồng.

# Không thuộc phạm vi

- Tutorial nâng cao/nhánh (Post-MVP).
- Số liệu phần thưởng (config).
- Skip cho returning player (UX2 — có thể thêm sau).

# Deliverables

- Luồng tutorial (config-driven) + guaranteed first pull + tiến độ server.
- Integration/gdUnit4 test: hoàn tất onboarding; thưởng đảm bảo cấp một lần; guard bước.
- Client overlay hướng dẫn.
- Cập nhật [`../mvp/02-core-game-loop.md`](../mvp/02-core-game-loop.md) (first-login) / feature doc.

# Công việc cần thực hiện

- [ ] Schema tutorial (config): danh sách bước, mục tiêu, highlight, phần thưởng.
- [ ] Server: tiến độ tutorial trên profile; cấp guaranteed first pull (kịch bản summon riêng, một lần, idempotent).
- [ ] Guard: bước chỉ hoàn tất khi đạt điều kiện; chống skip/lặp thưởng.
- [ ] Client: overlay hướng dẫn (highlight nút, khoá thao tác ngoài luồng), tiến qua bước.
- [ ] Test: chạy hết onboarding → thắng 1 trận + có ≥5–6 hero; thưởng đảm bảo một lần; guard.
- [ ] Cập nhật `../mvp/02-core-game-loop.md`.

# Tiêu chí hoàn thành

- Người mới hoàn tất onboarding: thắng 1 trận, có ≥5–6 hero, hiểu nút chính.
- Guaranteed first pull cấp đúng một lần (idempotent).
- Tiến độ tutorial server-authoritative; chống skip/lặp thưởng.
- Test onboarding xanh.

# Cách kiểm tra

- gdUnit4 + integration: chạy luồng onboarding tới hết; kiểm điều kiện đạt.
- Local: tài khoản mới → theo tutorial → hoàn tất loop cơ bản.
- Thử lặp lại first pull → không double.

# Rủi ro

- **Tutorial cứng nhắc/không data-driven** → bước ở config; dễ chỉnh.
- **Lặp thưởng guaranteed** → idempotency + tiến độ server.
- **Khoá thao tác gây kẹt** → có lối thoát/guard hợp lý; test.

# Ghi chú

Onboarding ~5 phút (mvp/02). Số liệu/nội dung là config. Skip returning (UX2) có thể thêm sau. Bám [`../mvp/02-core-game-loop.md`](../mvp/02-core-game-loop.md).

# Technical Debt Review

- **Maintainability:** bước tutorial là data.
- **Scalability:** thêm bước/nhánh qua config.
- **Testing:** funnel onboarding có test.
- **Security:** tiến độ + thưởng server-authoritative.
- **Nợ:** tutorial nâng cao/skip (Post-MVP).

# Phase Review

Đóng khi onboarding dẫn người mới qua loop + guaranteed pull idempotent + tiến độ server, test xanh.

---

## Liên kết
- [`../mvp/02-core-game-loop.md`](../mvp/02-core-game-loop.md) · [`../mvp/04-feature-analysis.md`](../mvp/04-feature-analysis.md) · [`../godot/ui-architecture.md`](../godot/ui-architecture.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-004-data-driven-design.md`](../adr/ADR-004-data-driven-design.md)
- Roadmap: [`README.md`](README.md) → kế: [`47-daily-login.md`](47-daily-login.md)
