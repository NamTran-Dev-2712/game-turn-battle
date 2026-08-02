# Review Checklist, Refactor Rules, Testing Rules & Definition of Done

> Chuẩn để một thay đổi được coi là "xong" và an toàn merge. Áp dụng cho cả AI agent và người.

---

## 1. Review Checklist

### Kiến trúc & ranh giới
- [ ] Tuân dependency rule (`../architecture/dependency-graph.md`); không phụ thuộc ngược
- [ ] Không God Object/giant manager; SRP
- [ ] Không feature client import chéo (Event Bus/signals)
- [ ] Domain thuần (không EF/HTTP/`DateTime.Now`)

### Data-driven & server authority
- [ ] Không hardcode config gameplay (ADR-004)
- [ ] Quyết định nhạy cảm ở server (ADR-007/011)
- [ ] Combat: integer/fixed-point + seeded RNG (nếu chạm sim)

### Chất lượng
- [ ] Đặt tên theo `../conventions/naming.md` + glossary
- [ ] Hàm nhỏ, rõ ý định; không magic number
- [ ] Xử lý lỗi/biên (`edge-case-hunter`)
- [ ] Không secret trong code

### Test & CI
- [ ] Có test cho logic mới (nhất là combat/kinh tế)
- [ ] Golden vector cập nhật nếu đổi sim
- [ ] Config validate (nếu đổi config/schema)
- [ ] CI xanh toàn bộ

### Traceability
- [ ] PR liên kết ADR/`docs/mvp/*` liên quan
- [ ] Điểm mơ hồ đã ghi `open-questions` (nếu có)

## 2. Refactor Rules
| Rule | Chi tiết |
|---|---|
| Không đổi hành vi khi refactor | Có test chứng minh (regression) |
| Refactor tách khỏi feature | PR riêng, dễ review |
| Xoá code chết & flag chết | Giảm nợ kỹ thuật |
| Cải thiện theo boundary | Không "refactor" phá ranh giới |
| Nhỏ & tăng dần | Tránh big-bang (`refactoring` skill) |

## 3. Testing Rules
| Rule | Chi tiết |
|---|---|
| Logic nhạy cảm phải có test | Combat, gacha, AFK, currency, save |
| Test hành vi, không nội bộ dễ vỡ | Bền vững |
| Deterministic | Inject clock/RNG seeded |
| Golden vector là đặc tả sống | Đổi sim ⇒ cập nhật có chủ đích |
| Không giảm coverage vùng rủi ro | `../testing/` |

## 4. Definition of Done (DoD)
Một thay đổi **Done** khi:
1. Đáp ứng acceptance của task/phase (`../roadmap/`).
2. Tuân toàn bộ Review Checklist (§1).
3. Có test phù hợp; CI xanh (build/test/golden/architecture/config/smoke).
4. Không vi phạm Forbidden Patterns (`coding-rules.md` §3).
5. Tài liệu/README module cập nhật nếu ranh giới/đổi hành vi công khai.
6. PR nhỏ, mô tả WHY + liên kết SSOT/ADR.
7. Không để lại open-question chưa ghi nhận.

## 5. Liên kết
- Coding rules: `coding-rules.md` · Context: `context-strategy.md`
- Testing: `../testing/README.md` · Git/PR: `../conventions/git-conventions.md`
