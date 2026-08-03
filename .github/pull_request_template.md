<!-- PR template — tuân docs/ai/review-and-dod.md & docs/conventions/git-conventions.md -->

## Mục tiêu (WHY)
<!-- Vì sao thay đổi này? Liên kết SSOT/ADR/issue. -->
- Liên quan: <!-- #issue / docs/mvp/* / docs/adr/ADR-0xx -->

## Thay đổi (WHAT)
<!-- Tóm tắt ngắn gọn nội dung. -->

## Loại thay đổi
- [ ] feat  - [ ] fix  - [ ] docs  - [ ] refactor  - [ ] test  - [ ] chore  - [ ] ci

## Definition of Done (docs/ai/review-and-dod.md §4)
- [ ] Đáp ứng acceptance của task/phase
- [ ] Tuân dependency rule; không God Object/giant manager (SRP)
- [ ] Không hardcode config gameplay (data-driven — ADR-004); quyết định nhạy cảm ở server (ADR-007/011)
- [ ] Combat (nếu chạm sim): integer/fixed-point + seeded RNG; golden vector cập nhật
- [ ] Có test cho logic mới (nhất là combat/kinh tế); **CI xanh**
- [ ] Không secret trong code
- [ ] Không vi phạm Forbidden Patterns (docs/ai/coding-rules.md §3)
- [ ] Điểm mơ hồ đã ghi docs/mvp/10-open-questions.md (nếu có)
- [ ] Đặt tên theo docs/conventions/naming.md; commit theo Conventional Commits

## Ghi chú review
<!-- Điểm cần chú ý, rủi ro, cách test. -->
