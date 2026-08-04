# 51 — Telemetry/Analytics events

> Mục đích: Ghi nhận **telemetry sự kiện cốt lõi** (funnel tutorial, source/sink kinh tế, retention D1/D7) — đo được để tune, giải rủi ro LO2 "thiếu analytics".

| Nhóm | P-map | S-map | Feature |
|---|---|---|---|
| 11 LiveOps Foundation | P6 | S12 | F36 |

# Mục tiêu

Khung telemetry: định nghĩa event cốt lõi (login, tutorial step, battle, summon, currency source/sink, AFK claim, level/star up), ghi nhận server-side (tin cậy) + client (hành vi UI), xuất ra sink phân tích (chọn công cụ theo AL2).

# Lý do

Không đo được thì không tune được (LO2, mvp/09 top risk). Telemetry cần trước soft-launch để đánh giá funnel/economy. Đặt sau LiveOps foundation.

# Phụ thuộc

- **Trước:** 30/33/37/46 (nguồn sự kiện), 10 (LoggingBehavior nền), 49 (flag bật/tắt telemetry).
- **Sau:** 52–55 (đo perf/funnel khi hardening/release).

# Phạm vi

- Danh mục event cốt lõi (schema event) — source/sink kinh tế, funnel tutorial, retention.
- Ghi event **server-side** cho dữ liệu tin cậy (economy/battle); client cho hành vi UI.
- Sink telemetry: interface trừu tượng (chọn Firebase/GameAnalytics/custom theo AL2) — không khoá cứng.
- Tôn trọng consent/privacy (AL4) — không thu thập vượt mức.

# Không thuộc phạm vi

- Dashboard/BI đầy đủ (Post-MVP/ngoài code).
- A/B testing (Post-MVP).
- Quyết định công cụ cuối (AL2 — có thể để open-question, dùng abstraction).

# Deliverables

- Danh mục event + schema; ghi nhận server + client.
- Interface sink telemetry (đổi backend dễ).
- Test: event cốt lõi phát đúng khi hành động; không rò dữ liệu nhạy cảm/PII.
- Cập nhật [`../mvp/07-liveops-planning.md`](../mvp/07-liveops-planning.md) + open-question AL1/AL2/AL4.

# Công việc cần thực hiện

- [ ] Định nghĩa danh mục event cốt lõi (login, tutorial_step, battle_end, summon, currency_earn/spend[source/sink], afk_claim, hero_levelup/ascend) + schema.
- [ ] Ghi event server-side cho dữ liệu tin cậy (economy/battle/gacha); dùng nền LoggingBehavior/domain event.
- [ ] Ghi event client cho hành vi UI (mở màn, nhấn nút chính, funnel tutorial).
- [ ] Interface `ITelemetrySink` (server) + tương ứng client; adapter chọn theo AL2 (mặc định: log/custom, đổi được).
- [ ] Cờ bật/tắt telemetry (feature flag 49) + tôn trọng consent (AL4).
- [ ] Test: hành động → event phát đúng; không PII/nhạy cảm rò; tắt cờ → không ghi.
- [ ] Cập nhật `../mvp/07-liveops-planning.md`; cập nhật AL1/AL2/AL4 trong `../mvp/10`.

# Tiêu chí hoàn thành

- Event cốt lõi (funnel/source-sink/retention) phát đúng khi hành động.
- Dữ liệu kinh tế/battle ghi **server-side** (tin cậy, không dựa client).
- Sink qua abstraction (đổi công cụ không sửa call site).
- Không thu thập PII/nhạy cảm vượt mức; tôn trọng consent + cờ bật/tắt.

# Cách kiểm tra

- `dotnet test` + gdUnit4: hành động → event đúng; tắt cờ→không ghi; không PII.
- Local: chơi loop → kiểm event nguồn/sink xuất hiện ở sink (log/dev).
- Rà: không log token/PII.

# Rủi ro

- **Chưa chốt công cụ (AL2)** → dùng abstraction + mặc định log/custom; đổi sau không sửa call site.
- **Rò PII/nhạy cảm** → whitelist trường; review; consent (AL4).
- **Client tự báo economy (không tin cậy)** → economy đo server-side.

# Ghi chú

Đo càng sớm càng tốt sau MVP (mvp/09 LO2). Công cụ cuối (AL2) là open-question; abstraction giữ linh hoạt. Bám mvp/07 + [`../liveops/`](../liveops/README.md).

# Technical Debt Review

- **Maintainability:** sink qua abstraction; danh mục event tập trung.
- **Scalability:** event async, không chặn gameplay.
- **Testing:** event phát đúng + không PII có test.
- **Security/Privacy:** consent + whitelist trường; server-side cho tin cậy.
- **Nợ:** dashboard/A-B/công cụ cuối (Post-MVP).

# Phase Review

Đóng khi telemetry event cốt lõi (server+client) qua abstraction + consent/cờ + không PII, test xanh. **Kết thúc P6 — vận hành được.**

---

## Liên kết
- [`../mvp/07-liveops-planning.md`](../mvp/07-liveops-planning.md) · [`../mvp/09-risk-analysis.md`](../mvp/09-risk-analysis.md) · [`../mvp/10-open-questions.md`](../mvp/10-open-questions.md)
- ADR: [`../adr/ADR-006-liveops.md`](../adr/ADR-006-liveops.md) · [`../adr/ADR-003-backend-architecture.md`](../adr/ADR-003-backend-architecture.md)
- Roadmap: [`README.md`](README.md) → kế: [`52-performance-pass.md`](52-performance-pass.md)
