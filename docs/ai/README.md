# AI Collaboration Rules

> Quy tắc để nhiều **AI coding agent** (và dev người) làm việc trên dự án mà **không phá vỡ kiến trúc**. Đây là hợp đồng làm việc cho mọi prompt hiện thực tương lai.

## Danh mục
| File | Nội dung |
|---|---|
| [context-strategy.md](context-strategy.md) | Context Package, Context Loading Strategy, AI Memory Strategy |
| [coding-rules.md](coding-rules.md) | Prompt Rules, Coding Rules, Forbidden Patterns |
| [review-and-dod.md](review-and-dod.md) | Review Checklist, Refactor Rules, Testing Rules, Definition of Done |

## Nguyên tắc nền
| Nguyên tắc | Diễn giải |
|---|---|
| SSOT là luật | `../mvp/` (nghiệp vụ) + `../adr/` (kiến trúc) là nguồn quyết định; không tự phát minh |
| Ranh giới bất khả xâm phạm | Tuân dependency rule (`../architecture/dependency-graph.md`) |
| Data-driven | Không hardcode config gameplay (ADR-004) |
| Nhỏi khi mơ hồ | Không rõ → trỏ `../mvp/10-open-questions.md`, không đoán bừa |
| Nhỏ & kiểm chứng được | PR nhỏ, có test, có DoD |

## Vì sao cần tài liệu này
- AI agent mất ngữ cảnh giữa phiên (`../mvp/09` AI4); dễ "bịa" API/pattern (AI2); code thiếu nhất quán (AI1).
- Bộ quy tắc + context package giảm mạnh các rủi ro đó.

## Liên kết
- Conventions: `../conventions/` · Testing: `../testing/` · ADR: `../adr/`
