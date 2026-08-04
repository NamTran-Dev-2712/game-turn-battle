# 42 — Mail system

> Mục đích: Hiện thực **hệ thư (mail)** — kênh trao thưởng/đền bù vận hành (ships trong MVP), claim đính kèm atomic idempotent, server-authoritative (ADR-006).

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 9 Kinh tế & QoL | P4 | S10 | F17 |

# Mục tiêu

Mail gắn người chơi: tiêu đề/nội dung + đính kèm (currency/item), trạng thái đọc/đã claim, hết hạn; claim đính kèm atomic idempotent; nền gửi hàng loạt (broadcast) để phase 50 mở rộng.

# Lý do

Mail là kênh vận hành **bắt buộc từ ngày đầu** (ADR-006, F17) — đền bù, phát thưởng sự kiện, hỗ trợ CSKH. Phải có trước soft-launch. Nền broadcast để LiveOps (50).

# Phụ thuộc

- **Trước:** 31–32 (đính kèm currency/item), 36 (server-time hết hạn), 19 (profile).
- **Sau:** 50 (mail broadcast hàng loạt + scheduling), 47 (daily login có thể qua mail).

# Phạm vi

- Mail model (gửi tới người chơi): nội dung, đính kèm, đọc/claim, hết hạn (server-time).
- Command `ClaimMailCommand` atomic idempotent (cấp đính kèm) + đánh dấu claimed.
- Query hộp thư; xoá/hết hạn theo server-time.
- Nền gửi mail (cá nhân) — broadcast hàng loạt ở phase 50.

# Không thuộc phạm vi

- Broadcast hàng loạt + scheduling (phase 50).
- Admin CMS gửi mail (Post-MVP).
- Push notification (Post-MVP — F25).

# Deliverables

- Mail model + claim atomic idempotent + hết hạn server-time.
- Integration test: claim đính kèm atomic; idempotent; hết hạn không claim được; query hộp thư.
- Client UI hộp thư (đọc, claim, claim-all).
- Cập nhật [`../liveops/mail-system.md`](../liveops/mail-system.md).

# Công việc cần thực hiện

- [ ] Domain: `Mail` (recipient, nội dung, đính kèm, đọc/claimed, expireAt server-time).
- [ ] Application: `ClaimMailCommand` (atomic cấp đính kèm 31/32, idempotent) + `ClaimAllCommand`; `GetMailboxQuery`.
- [ ] Hết hạn theo `IClock` server; mail hết hạn không claim được (lazy check).
- [ ] Nền gửi mail cá nhân (interface `IMailSender`) — broadcast để phase 50.
- [ ] Client feature `mail/`: hộp thư, đọc, claim, claim-all.
- [ ] Integration test: claim atomic/idempotent, hết hạn chặn, claim-all, query.
- [ ] Cập nhật `../liveops/mail-system.md`.

# Tiêu chí hoàn thành

- Claim đính kèm atomic; idempotent (retry không double).
- Mail hết hạn (server-time) không claim được.
- Query hộp thư + claim-all hoạt động.
- Server-authoritative; client hiển thị/gửi intent.

# Cách kiểm tra

- `dotnet test`: claim atomic/idempotent, hết hạn, claim-all, query.
- Local: gửi mail có đính kèm → claim → thưởng vào ví/kho; retry không double.
- Mock clock qua expireAt → mail hết hạn.

# Rủi ro

- **Double claim đính kèm** → idempotency.
- **Claim mail hết hạn** → kiểm server-time.
- **Broadcast tương lai gây tải** → nền interface, tối ưu batch ở phase 50.

# Ghi chú

Mail ships MVP (khác nhiều feature khác). Broadcast hàng loạt + scheduling là phase 50 (LiveOps). Bám [`../liveops/mail-system.md`](../liveops/mail-system.md) + ADR-006.

# Technical Debt Review

- **Maintainability:** mail tách rõ; nền broadcast.
- **Scalability:** batch/broadcast tối ưu ở 50.
- **Testing:** claim/hết hạn/idempotent có test.
- **Security:** đính kèm server-authoritative; idempotent.
- **Nợ:** broadcast/scheduling (50); admin CMS/push (Post-MVP).

# Phase Review

Đóng khi mail claim atomic idempotent + hết hạn server-time + hộp thư, test xanh.

---

## Liên kết
- [`../liveops/mail-system.md`](../liveops/mail-system.md) · [`../mvp/07-liveops-planning.md`](../mvp/07-liveops-planning.md)
- ADR: [`../adr/ADR-006-liveops.md`](../adr/ADR-006-liveops.md) · [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md)
- Roadmap: [`README.md`](README.md) → kế: [`43-sweep-speed.md`](43-sweep-speed.md)
