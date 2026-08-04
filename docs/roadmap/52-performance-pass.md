# 52 — Performance pass (mobile + asset loading)

> Mục đích: **Tối ưu hiệu năng mobile** — đạt ngưỡng FPS/bộ nhớ/thời gian tải trên thiết bị tầm trung; hiện thực đầy đủ chiến lược asset (ADR-009).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 12 Polish & Release | P7 | S13 | polish |

# Mục tiêu

Đo & tối ưu: FPS trận đấu, thời gian boot/scene, bộ nhớ, tải asset; áp dụng async on-demand load + object pooling (combat/VFX) + atlas/nén texture + giải phóng khi thoát scene (ADR-009); đạt ngưỡng perf đặt ra.

# Lý do

Idle RPG chạy trên mobile tầm trung (mvp/01 §5). Perf kém → churn. Hardening perf trước soft-launch. ADR-009 các chiến lược đã quyết, phase này hiện thực đầy đủ + đo.

# Phụ thuộc

- **Trước:** toàn bộ feature (30–51); ADR-009.
- **Sau:** 54 (regression giữ ngưỡng), 55 (release).

# Phạm vi

- Định ngưỡng perf (FPS, boot, memory) cho thiết bị tầm trung.
- Async on-demand load asset nặng (hero art/VFX), tải nền không chặn.
- Object pooling combat/VFX; giải phóng khi thoát scene.
- Atlas + nén texture mobile; tách data nhẹ (load sớm) / art nặng (lazy).
- Profiling + tối ưu điểm nóng (sim, UI, GC).

# Không thuộc phạm vi

- Security (phase 53).
- Smoke suite (phase 54).
- Tính năng gameplay mới.

# Deliverables

- Ngưỡng perf tài liệu hoá + báo cáo đo (before/after).
- Asset loading async + pooling + release hoàn chỉnh.
- Tối ưu điểm nóng (có số liệu).
- Cập nhật [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md) + [`../godot/tooling-and-testing.md`](../godot/tooling-and-testing.md).

# Công việc cần thực hiện

- [ ] Đặt ngưỡng perf (FPS trận ≥ mục tiêu, boot < X s, memory < Y MB) cho thiết bị tầm trung.
- [ ] Profiling baseline (Godot profiler + thiết bị thật): FPS, boot, memory, GC.
- [ ] Async on-demand load asset nặng; splash/tải nền không chặn.
- [ ] Object pooling combat/VFX; giải phóng asset khi thoát scene (chống rò).
- [ ] Atlas + nén texture mobile; map asset path từ config (ADR-009).
- [ ] Tối ưu điểm nóng (sim client, UI update, allocation/GC).
- [ ] Đo lại (after) → đạt ngưỡng; ghi báo cáo.
- [ ] Cập nhật `../godot/resources-and-assets.md`.

# Tiêu chí hoàn thành

- Đạt ngưỡng perf trên thiết bị tầm trung (FPS/boot/memory) — có số liệu before/after.
- Không rò tài nguyên khi chuyển scene lặp (memory ổn định).
- Asset nặng tải async không chặn UI; pooling giảm giật.
- Path asset từ config (không hardcode rải rác).

# Cách kiểm tra

- Profiling trên thiết bị thật (Android tầm trung): FPS trận, boot, memory.
- Test rò rỉ: vào/ra scene N lần → memory không tăng dần.
- So báo cáo before/after đạt ngưỡng.

# Rủi ro

- **Thiết bị thật khác nhau** → chọn thiết bị tham chiếu tầm trung; ghi cấu hình.
- **Pooling gây bug trạng thái** → reset object khi lấy từ pool; test.
- **Async load gây pop-in** → placeholder + ưu tiên tải phần thấy trước.

# Ghi chú

Ngưỡng perf cụ thể chốt ở đây (D9, mvp/15). ADR-009 chiến lược; phase này thực thi + đo. Bám [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md).

# Technical Debt Review

- **Maintainability:** asset qua config + pooling chuẩn.
- **Scalability:** async/pooling chịu nội dung lớn.
- **Testing:** profiling + rò rỉ có quy trình.
- **Security:** không áp dụng trực tiếp.
- **Nợ:** tối ưu sâu theo thiết bị (liên tục).

# Phase Review

Đóng khi đạt ngưỡng perf mobile (before/after) + asset async/pooling/release + không rò rỉ, báo cáo đầy đủ.

---

## Liên kết
- [`../godot/resources-and-assets.md`](../godot/resources-and-assets.md) · [`../godot/tooling-and-testing.md`](../godot/tooling-and-testing.md) · [`../mvp/08-technical-impact.md`](../mvp/08-technical-impact.md)
- ADR: [`../adr/ADR-009-asset-loading.md`](../adr/ADR-009-asset-loading.md)
- Roadmap: [`README.md`](README.md) → kế: [`53-security-pass.md`](53-security-pass.md)
