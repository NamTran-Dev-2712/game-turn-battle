# 48 — Integration hardening client-server

> Mục đích: **Củng cố tích hợp** client-server end-to-end: xử lý mất mạng/timeout/retry/idempotency toàn diện, đảm bảo tiêu chí MVP `mvp/01 §2` đạt trước khi vào LiveOps/Polish.

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 10 Retention & Tích hợp | P5 | S11 | tích hợp |

# Mục tiêu

Rà soát & làm chắc mọi luồng client-server: mất mạng giữa trận/giao dịch (UX3), retry an toàn (idempotency), đồng bộ state sau offline, thông báo lỗi thân thiện; xác nhận tiêu chí "sẵn sàng retention" của MVP.

# Lý do

Sau khi đủ feature (nhóm 5–10), cần một phase **hardening tích hợp** để các edge case (mạng, đồng bộ, double-action) không làm hỏng trải nghiệm/kinh tế. Đây là cổng chất lượng trước P6/P7.

# Phụ thuộc

- **Trước:** toàn bộ 30–47 (feature loop + retention).
- **Sau:** 49–51 (LiveOps), 52–55 (Polish/Release).

# Phạm vi

- Rà toàn bộ command nhạy cảm có **idempotency key** (battle/gacha/claim/purchase).
- Xử lý mất mạng giữa trận/giao dịch (UX3): queue/thông báo, không tự quyết kết quả.
- Đồng bộ state client sau reconnect (refresh từ server, StateCache).
- Thông báo lỗi thân thiện + retry; loại bỏ trạng thái treo.
- Kiểm chéo tiêu chí `mvp/01 §2` (MVP retention-ready).

# Không thuộc phạm vi

- Perf mobile (phase 52).
- Security audit sâu (phase 53).
- Smoke suite tổng (phase 54).

# Deliverables

- Bảng rà idempotency (mọi command nhạy cảm) + vá thiếu.
- Xử lý mất mạng/reconnect nhất quán (client) + test.
- Danh sách lỗi UX + thông báo thân thiện.
- Checklist `mvp/01 §2` đạt.
- Cập nhật [`../mvp/01-mvp-definition.md`](../mvp/01-mvp-definition.md) (đánh dấu đạt) / audit.

# Công việc cần thực hiện

- [ ] Lập bảng mọi command ghi nhạy cảm; xác nhận có idempotency key + test; vá chỗ thiếu.
- [ ] Client: xử lý mất mạng giữa trận/giao dịch (UX3) — queue intent/thông báo, không bịa kết quả; reconnect → refresh state.
- [ ] Đồng bộ StateCache sau offline (ưu tiên server, nhãn cache cũ).
- [ ] Chuẩn hoá thông báo lỗi (mạng/authz/nghiệp vụ) thân thiện + retry; loại trạng thái treo UI.
- [ ] Kịch bản integration end-to-end: rớt mạng giữa battle/gacha/claim → không double, không mất tiền, không treo.
- [ ] Đối chiếu `mvp/01 §2` (retention-ready) → đánh dấu đạt/còn thiếu.
- [ ] Cập nhật `../mvp/01-mvp-definition.md` / `../audit/bootstrap-audit.md`.

# Tiêu chí hoàn thành

- Mọi command nhạy cảm có idempotency (bảng rà đầy đủ, test phủ).
- Mất mạng giữa trận/giao dịch → không double-grant, không mất tiền, không treo; reconnect đồng bộ đúng.
- Thông báo lỗi thân thiện + retry ở mọi luồng chính.
- Tiêu chí `mvp/01 §2` đạt (hoặc ghi rõ phần còn lại).

# Cách kiểm tra

- Integration test mô phỏng rớt mạng/timeout/retry cho battle/gacha/claim/purchase.
- Local: bật/tắt mạng giữa hành động → kiểm không double/không treo.
- Đối chiếu checklist `mvp/01 §2`.

# Rủi ro

- **Edge case bỏ sót** → bảng rà + kịch bản mạng có hệ thống.
- **Reconnect gây state lệch** → luôn refresh từ server; server là chân lý.
- **Retry double-effect** → idempotency toàn diện.

# Ghi chú

Đây là cổng chất lượng tích hợp trước LiveOps/Polish (P5 acceptance). Bám ADR-007/008 (idempotency, không client-authority, xử lý mất mạng). Đối chiếu mvp/09 (rủi ro TE1).

# Technical Debt Review

- **Maintainability:** xử lý lỗi/đồng bộ nhất quán, ít đặc thù rải rác.
- **Scalability:** idempotency + refresh chuẩn cho tải thật.
- **Testing:** kịch bản mạng là hợp đồng độ bền.
- **Security:** khoá double-grant/cheat qua idempotency + server-authority.
- **Nợ:** perf/security/smoke (52–54).

# Phase Review

Đóng khi idempotency toàn diện + xử lý mất mạng/reconnect + thông báo lỗi + `mvp/01 §2` đạt, test xanh. **Kết thúc P5 — retention-ready.**

---

## Liên kết
- [`../mvp/01-mvp-definition.md`](../mvp/01-mvp-definition.md) · [`../mvp/09-risk-analysis.md`](../mvp/09-risk-analysis.md) · [`../backend/api-and-versioning.md`](../backend/api-and-versioning.md)
- ADR: [`../adr/ADR-007-save-strategy.md`](../adr/ADR-007-save-strategy.md) · [`../adr/ADR-008-networking.md`](../adr/ADR-008-networking.md)
- Roadmap: [`README.md`](README.md) → kế: [`49-remote-config-flags.md`](49-remote-config-flags.md)
