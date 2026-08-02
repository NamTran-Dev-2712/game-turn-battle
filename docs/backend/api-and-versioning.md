# API Design & Versioning

> Hợp đồng API client↔server: thiết kế, versioning, error contract, và versioning DB/schema. Nền: ADR-008. Áp dụng tư duy `api-design-best-practice`.

---

## 1. Nguyên tắc API
| Nguyên tắc | Chi tiết |
|---|---|
| REST + JSON | Giao thức chính; JWT bearer (ADR-008) |
| Contract-first | Định nghĩa ở `shared/contracts` (OpenAPI) → codegen client |
| Versioned | Prefix `/api/v{major}/` |
| Server-authoritative | Endpoint nhận **ý định**, server quyết kết quả (ADR-011) |
| Idempotency | Header `Idempotency-Key` cho command nhạy cảm (claim/summon/battle) |
| Consistent naming | Resource danh từ số nhiều (`/heroes`, `/battles`), động từ = HTTP method |
| Pagination | Cursor/limit cho list lớn (inventory) — `../mvp/08` |

---

## 2. Nhóm endpoint (MVP — minh hoạ, không phải hợp đồng cuối)

| Nhóm | Ví dụ | Loại |
|---|---|---|
| Auth | `POST /api/v1/auth/guest`, `/auth/refresh` | command |
| Profile | `GET /api/v1/profile` | query |
| Config | `GET /api/v1/config/{version}` | query (cache) |
| Heroes | `GET /api/v1/heroes` | query |
| Formation | `PUT /api/v1/teams/{id}` | command |
| Battle | `POST /api/v1/battles` | command (re-sim) |
| Summon | `POST /api/v1/summons` | command (idempotent) |
| Campaign | `GET /api/v1/campaign`, `POST /api/v1/campaign/{stage}/sweep` | query/command |
| Economy | `POST /api/v1/afk/claim`, `POST /api/v1/shop/purchase` | command (idempotent) |
| Mail | `GET /api/v1/mail`, `POST /api/v1/mail/{id}/claim` | query/command |

> Endpoint chính thức chốt ở `shared/contracts`; bảng trên định hướng.

---

## 3. Error contract (chuẩn hoá)

```json
{
  "error": {
    "code": "INSUFFICIENT_CURRENCY",
    "message": "Không đủ gem để summon.",
    "trace_id": "..."
  }
}
```
- Mã lỗi ổn định (enum), message hiển thị được, `trace_id` để tra log.
- HTTP status hợp lý (400 validate, 401/403 auth, 409 idempotency/conflict, 422 rule nghiệp vụ, 5xx server).

---

## 4. API Versioning
| Chủ đề | Chính sách |
|---|---|
| Major | `/api/vN`; breaking change → tăng major |
| Backward-compat | Trong cùng major chỉ thêm (additive), không phá |
| Deprecation | Thông báo + thời gian ân hạn trước khi bỏ version cũ |
| Client-server lệch phiên bản | Server hỗ trợ ≥1 major cũ trong giai đoạn chuyển |

## 5. DB & Schema Versioning
- DB: EF Core Migrations, additive-first (`infrastructure.md`).
- Profile save: version + migration (ADR-007).
- Config: `schema_version` + bundle version (ADR-005).
- Ba loại version **độc lập**: app version, API version, config version (`../conventions/naming.md`).

## 6. SignalR (optional)
- Chỉ cho realtime cần thiết (thông báo, Post-MVP guild/arena); bật theo feature flag (ADR-006).
- Không đưa core loop MVP phụ thuộc SignalR.

## 7. Liên kết
- ADR-008 (networking), ADR-005 (config), ADR-007 (save)
- Contracts: `shared/contracts` (`../architecture/project-structure.md`)
- Cross-cutting: `cross-cutting.md`
