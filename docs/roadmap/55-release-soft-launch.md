# 55 — Release build & soft-launch prep

> Mục đích: Hoàn tất **quy trình phát hành** (build Android trước, publish config bundle, docker image, monitoring) và chuẩn bị **soft launch** — chốt MVP hoàn chỉnh cho playtest thật.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 12 Polish & Release | P7 | S13 | release |

# Mục tiêu

Hoàn thiện `release.yml`: build+test (cổng smoke/golden 54), docker image push registry, **Godot Android export + signing**, publish config bundle versioned; monitoring/health cơ bản; checklist soft-launch (funnel/source-sink đo được) đạt.

# Lý do

Đích cuối P7/M6: bản MVP hoàn chỉnh chạy trên thiết bị thật để kiểm giả thuyết retention (mvp/11 DoD). Cần quy trình release lặp lại được + đo được.

# Phụ thuộc

- **Trước:** 52 (perf), 53 (security), 54 (smoke/golden), 51 (telemetry), 03 (release skeleton).
- **Sau:** (vận hành soft-launch — ngoài roadmap build).

# Phạm vi

- `release.yml` hoàn chỉnh: on tag `v*` → build+test (cổng 54) → docker build + **push registry** → Godot **Android export + signing** → publish config bundle versioned → tạo release (draft→publish).
- Monitoring/health cơ bản (health endpoint + log/metrics tối thiểu).
- Checklist soft-launch: funnel tutorial + source/sink đo được (telemetry 51); MVP DoD (mvp/11 §5) đạt.
- Chạy thử trên thiết bị thật.

# Không thuộc phạm vi

- k8s/scale production đầy đủ (Post-MVP).
- Store submission/compliance chi tiết (DP4 — phối hợp riêng).
- Tính năng gameplay mới.

# Deliverables

- `release.yml` end-to-end (image push + Android export/signing + config publish + release).
- Monitoring/health cơ bản.
- Checklist soft-launch đạt (DoD MVP mvp/11 §5).
- Bản build chạy trên thiết bị thật; báo cáo playtest funnel/source-sink.
- Cập nhật [`../deployment/release-operations.md`](../deployment/release-operations.md) + [`../deployment/ci-cd-pipeline.md`](../deployment/ci-cd-pipeline.md).

# Công việc cần thực hiện

- [ ] Hoàn thiện `release.yml`: phụ thuộc cổng smoke+golden (54); build+test.
- [ ] Docker build image + push registry (secret registry qua CI).
- [ ] Godot Android export headless + **signing** (keystore qua secret; không commit).
- [ ] Publish config bundle versioned (Config Service 21/49) khi release.
- [ ] Monitoring/health cơ bản: health endpoint + log/metrics tối thiểu (server).
- [ ] Checklist soft-launch: đối chiếu DoD MVP (mvp/11 §5) + telemetry funnel/source-sink đo được (51).
- [ ] Chạy build trên thiết bị thật → đo funnel tutorial + source/sink; ghi báo cáo.
- [ ] Cập nhật `../deployment/release-operations.md` + `../deployment/ci-cd-pipeline.md`; đánh dấu **MVP hoàn chỉnh** ở roadmap/audit.

# Tiêu chí hoàn thành

- Tag `v*` → pipeline build+test (cổng 54) → image push + Android build signed + config publish → release.
- Bản build chạy trên thiết bị thật; loop cốt lõi hoạt động.
- Monitoring/health cơ bản có; đo được funnel + source/sink (telemetry).
- DoD MVP (mvp/11 §5) đạt; checklist soft-launch xanh.

# Cách kiểm tra

- Đẩy tag release thử → pipeline hoàn tất các bước (image/apk/config/release).
- Cài build lên thiết bị thật → chơi loop → đo funnel/source-sink.
- Đối chiếu DoD MVP (mvp/11 §5) + `mvp/01 §2`.

# Rủi ro

- **Signing/keystore lộ** → keystore qua secret, không commit; xoay nếu lộ.
- **Android export lỗi trên runner** → Godot headless export template pin version; test sớm.
- **Thiếu monitoring khi live** → health + log/metrics tối thiểu bắt buộc trước launch.

# Ghi chú

Đây là **đích MVP hoàn chỉnh (P7/M6)** cho soft launch. Store submission/compliance (DP1–DP4) phối hợp ngoài roadmap build. Bám [`../deployment/release-operations.md`](../deployment/release-operations.md) + mvp/11 (DoD).

# Technical Debt Review

- **Maintainability:** pipeline release lặp lại được, ít thao tác tay.
- **Scalability:** image/registry nền cho scale sau; monitoring tối thiểu.
- **Testing:** cổng smoke/golden + thiết bị thật.
- **Security:** signing/secret qua CI; không lộ.
- **Nợ:** k8s/scale, store compliance, monitoring nâng cao (Post-MVP).

# Phase Review

Đóng khi pipeline release end-to-end + build chạy thiết bị thật + monitoring cơ bản + DoD MVP đạt. **🎉 MVP hoàn chỉnh cho soft launch — hoàn tất roadmap.**

---

## Liên kết
- [`../deployment/release-operations.md`](../deployment/release-operations.md) · [`../deployment/ci-cd-pipeline.md`](../deployment/ci-cd-pipeline.md) · [`../mvp/11-development-roadmap.md`](../mvp/11-development-roadmap.md)
- ADR: [`../adr/ADR-010-dependency-management.md`](../adr/ADR-010-dependency-management.md) · [`../adr/ADR-001-engine-choice.md`](../adr/ADR-001-engine-choice.md)
- Roadmap: [`README.md`](README.md) · Trước: [`54-regression-smoke.md`](54-regression-smoke.md)
