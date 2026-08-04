# 50 — Mail broadcast + scheduling (server-time)

> Mục đích: Mở rộng hệ mail (42) thành **gửi hàng loạt (broadcast)** + **lên lịch theo server-time** — công cụ vận hành phát thưởng/đền bù diện rộng.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 11 LiveOps Foundation | P6 | S12 | F35 (nền) |

# Mục tiêu

Gửi mail tới nhiều/tất cả người chơi (broadcast) hiệu quả (batch), có thể lên lịch (gửi/hết hạn theo server-time); nền cho vận hành (đền bù sự cố, thưởng sự kiện) — chưa cần CMS UI.

# Lý do

Mail cá nhân (42) đủ cho MVP, nhưng vận hành cần **phát hàng loạt** (đền bù toàn server). Đặt nền broadcast + scheduling để đội vận hành phát thưởng mà không sửa code mỗi lần.

# Phụ thuộc

- **Trước:** 42 (mail cá nhân), 49 (scheduling/time-based, feature flag), 12 (Redis/queue nếu cần).
- **Sau:** Post-MVP admin CMS, telemetry (51).

# Phạm vi

- Broadcast: gửi mail tới nhóm/tất cả (theo tiêu chí: tất cả, theo điều kiện đơn giản).
- Batch hiệu quả (không tạo N mail đồng bộ chặn); idempotent (không gửi trùng khi retry).
- Lên lịch gửi/hết hạn theo server-time (dùng 49).
- Nguồn kích hoạt: config/lệnh vận hành (chưa cần UI CMS).

# Không thuộc phạm vi

- Admin CMS/UI (Post-MVP).
- Nhắm mục tiêu phức tạp (segment nâng cao — Post-MVP).
- Push notification (Post-MVP).

# Deliverables

- Broadcast mail + batch + idempotent + scheduling server-time.
- Integration test: broadcast tới N người (batch), không trùng, lịch đúng server-time, claim vẫn atomic (42).
- Cập nhật [`../liveops/mail-system.md`](../liveops/mail-system.md) + [`../liveops/content-scheduling.md`](../liveops/content-scheduling.md).

# Công việc cần thực hiện

- [ ] Cơ chế broadcast: tạo mail cho tập người chơi theo tiêu chí (all/điều kiện đơn giản) dạng batch (job nền, không chặn request).
- [ ] Idempotency broadcast: một chiến dịch mail gửi một lần cho mỗi người (không trùng khi retry job).
- [ ] Lên lịch: gửi/hết hạn theo server-time (dùng scheduling 49); job xử lý đến hạn.
- [ ] Nguồn cấu hình chiến dịch (config/lệnh) — chưa cần UI.
- [ ] Đảm bảo claim đính kèm vẫn atomic idempotent (42).
- [ ] Integration test: broadcast N người không trùng; lịch server-time; claim atomic; retry job không double.
- [ ] Cập nhật `../liveops/mail-system.md` + `../liveops/content-scheduling.md`.

# Tiêu chí hoàn thành

- Broadcast tới tập người chơi bằng batch (không chặn), idempotent (không trùng).
- Lên lịch gửi/hết hạn theo **server-time** hoạt động.
- Claim đính kèm vẫn atomic idempotent (không hồi quy 42).
- Server-authoritative.

# Cách kiểm tra

- `dotnet test`: broadcast N người/không trùng; scheduling server-time; retry job không double; claim atomic.
- Local: phát broadcast → nhiều tài khoản nhận → claim đúng.
- Mock clock tới lịch → mail gửi/hết hạn.

# Rủi ro

- **Broadcast tải nặng** → job nền + batch + phân trang; không đồng bộ trong request.
- **Gửi trùng khi retry job** → idempotency theo chiến dịch + người.
- **Lịch theo client time** → server-time.

# Ghi chú

Nền vận hành, chưa cần CMS (Post-MVP). Kích hoạt qua config/lệnh. Bám [`../liveops/mail-system.md`](../liveops/mail-system.md) + ADR-006.

# Technical Debt Review

- **Maintainability:** broadcast tách job; tái dùng mail 42.
- **Scalability:** batch/job chịu tải diện rộng.
- **Testing:** broadcast/không-trùng/lịch có test.
- **Security:** server-authoritative + idempotent.
- **Nợ:** CMS/segment/push (Post-MVP).

# Phase Review

Đóng khi broadcast batch idempotent + scheduling server-time + claim vẫn atomic, test xanh.

---

## Liên kết
- [`../liveops/mail-system.md`](../liveops/mail-system.md) · [`../liveops/content-scheduling.md`](../liveops/content-scheduling.md) · [`../liveops/content-update-and-admin-workflow.md`](../liveops/content-update-and-admin-workflow.md)
- ADR: [`../adr/ADR-006-liveops.md`](../adr/ADR-006-liveops.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`51-telemetry-analytics.md`](51-telemetry-analytics.md)
