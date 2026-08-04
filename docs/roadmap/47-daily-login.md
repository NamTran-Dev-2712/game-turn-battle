# 47 — Daily login (minimal)

> Mục đích: Hiện thực **điểm danh hằng ngày tối giản** — thưởng đăng nhập theo ngày (server-time), tạo thói quen quay lại.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 10 Retention & Tích hợp | P5 | S11 | login |

# Mục tiêu

Người chơi nhận thưởng khi đăng nhập mỗi ngày (chuỗi hoặc bảng ngày, config-driven); xác định "ngày" theo **server-time**; claim atomic idempotent; client hiển thị.

# Lý do

Daily login (Should, mvp/07) là công cụ retention rẻ, hiệu quả. Server-time chống cheat. Tối giản cho MVP (calendar tháng là Post-MVP).

# Phụ thuộc

- **Trước:** 31–32 (thưởng), 36 (server-time), 42 (mail nếu phát qua mail), 19 (profile).
- **Sau:** 51 (telemetry login), Post-MVP login calendar.

# Phạm vi

- Bảng thưởng login (config): theo ngày/chuỗi.
- Xác định "ngày mới" theo server-time; claim một lần/ngày.
- Claim atomic idempotent.
- Client: popup/điểm danh.

# Không thuộc phạm vi

- Login calendar tháng + sự kiện (Post-MVP).
- Chuỗi streak phức tạp/bù ngày lỡ (Post-MVP).
- Số liệu thưởng (config).

# Deliverables

- Daily login + claim atomic idempotent + server-time.
- Integration test: claim một lần/ngày; ngày mới (mock clock) cho claim tiếp; idempotent.
- Client popup điểm danh.
- Cập nhật [`../mvp/07-liveops-planning.md`](../mvp/07-liveops-planning.md).

# Công việc cần thực hiện

- [ ] Schema login reward (config): theo ngày/chuỗi.
- [ ] Server: theo dõi ngày claim gần nhất (server-time); xác định đủ điều kiện claim ngày mới.
- [ ] `ClaimDailyLoginCommand` atomic idempotent (một lần/ngày); cấp thưởng (31/32) hoặc qua mail (42).
- [ ] Client: popup điểm danh khi vào game (nếu chưa claim).
- [ ] Integration test: claim/ngày; mock clock sang ngày mới→claim tiếp; đúp trong ngày→chặn; idempotent.
- [ ] Cập nhật `../mvp/07-liveops-planning.md`.

# Tiêu chí hoàn thành

- Claim một lần/ngày theo **server-time**; ngày mới cho claim tiếp.
- Claim đúp trong ngày bị chặn; idempotent.
- Data-driven (đổi bảng thưởng config → đổi).
- Server-authoritative; client hiển thị.

# Cách kiểm tra

- `dotnet test`: claim/ngày, mock clock sang ngày, đúp chặn, idempotent.
- Local: đăng nhập → điểm danh → thưởng; đúp không được.
- Mock clock +1 ngày → claim tiếp.

# Rủi ro

- **Đổi giờ client để claim nhiều** → server-time bắt buộc.
- **Double claim** → idempotency.
- **Định nghĩa "ngày" (timezone)** → chốt mốc reset server (config), tài liệu hoá.

# Ghi chú

Tối giản MVP; calendar/streak nâng cao Post-MVP. Mốc reset theo server timezone (config). Bám mvp/07 + ADR-006/007.

# Technical Debt Review

- **Maintainability:** bảng thưởng là data.
- **Scalability:** nền calendar (Post-MVP).
- **Testing:** claim/ngày/idempotent có test.
- **Security:** server-time + idempotent.
- **Nợ:** calendar/streak (Post-MVP).

# Phase Review

Đóng khi daily login claim một-lần-mỗi-ngày (server-time) + idempotent + data-driven, test xanh.

---

## Liên kết
- [`../mvp/07-liveops-planning.md`](../mvp/07-liveops-planning.md) · [`../liveops/mail-system.md`](../liveops/mail-system.md)
- ADR: [`../adr/ADR-006-liveops.md`](../adr/ADR-006-liveops.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`48-integration-hardening.md`](48-integration-hardening.md)
