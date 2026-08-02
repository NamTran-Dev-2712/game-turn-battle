# ADR-004: Data-Driven Design (Thiết kế hướng dữ liệu)
- Status: Accepted
- Date: 2026-08-02
- Deciders: Lead Technical Architect
- Related: ADR-005, ADR-006, `../gameplay/configuration-and-data.md`, `../mvp/06`, `../mvp/07`

## Context
`../mvp/06` & `07` nhấn mạnh: kinh tế **chắc chắn sẽ sai** bản đầu → phải **tune được nhanh**; LiveOps sống nhờ nội dung config được. Đề bài cấm hardcode config gameplay và cấm switch để mở rộng gameplay.

## Decision
**Mọi thứ gameplay-related cuối cùng phải là dữ liệu, không phải code.** Cụ thể:
- Hero, skill, stage, gacha (rate/pity), shop, reward, economy curve, quest... định nghĩa trong **file config** (validate bằng JSON Schema ở `shared/config-schema`).
- Code chỉ chứa **cơ chế** (mechanism) đọc & thực thi dữ liệu, **không** chứa giá trị cân bằng.
- Mở rộng bằng **thêm dữ liệu / polymorphism / registry**, **không** bằng `switch`/`if` phình to.
- Client dùng `Resource` làm khuôn dữ liệu; backend đọc qua Configuration Service (ADR-005).

## Alternatives
| Phương án | Vì sao loại |
|---|---|
| Hardcode trong code | Không tune/LiveOps được — đề bài cấm |
| Switch/if theo loại hero/skill | Không mở rộng, vi phạm OCP — đề bài cấm |
| Chỉ data-driven một phần | Lệch nguồn sự thật, khó bảo trì |

## Trade-offs
- **Được:** tune không cần build, LiveOps mạnh, thêm content nhanh, testable bằng data vector.
- **Mất:** cần schema + validator + tooling; gián tiếp hoá làm debug khó hơn → bù bằng validation & tài liệu schema.

## Consequences
- `config/` + `shared/config-schema/` + `tools/config-validator` (`../architecture/project-structure.md`).
- Skill/effect theo mô hình **effect data-driven + handler registry** (`../gameplay/skill-framework.md`).
- Combat đọc chỉ số từ config (ADR-011).
- CI **fail** nếu config sai schema (`../testing/`).
