# `shared/` — Hợp đồng & schema dùng chung 2 phía

> Nguồn **duy nhất** cho hợp đồng client ↔ server và schema config data-driven. Tránh lệch tay giữa Godot và .NET.

| Mục | Nội dung |
|---|---|
| **Purpose** | Chứa hợp đồng API, JSON Schema config, và định nghĩa/đầu ra codegen dùng cho cả hai phía. |
| **Responsibilities** | `contracts/` đặc tả API + enum/hằng chung; `config-schema/` JSON Schema validate mọi config; `codegen/` sinh model client từ contracts. |
| **Allowed** | OpenAPI/JSON Schema, file định nghĩa codegen, hằng chia sẻ. |
| **Not allowed** | ❌ mã runtime của game; ❌ số cân bằng gameplay (thuộc `../config/`). |
| **Dependencies** | `server/GameTeam.Contracts` là nguồn để sinh; `client/src/data` đồng bộ theo `config-schema`. |
| **Owner** | Platform/architecture team. |
| **Future expansion** | Thêm phiên bản hợp đồng (versioned), thêm schema khi thêm loại config. |

## Cấu trúc
```text
shared/
├── contracts/       # OpenAPI + enum/hằng dùng chung (nguồn hợp đồng API)
├── config-schema/   # JSON Schema cho mọi file config data-driven
└── codegen/         # Định nghĩa & output sinh mã (client model từ contracts)
```

Chi tiết: `../docs/architecture/project-structure.md` §5, `../docs/adr/ADR-008-networking.md`, `../docs/adr/ADR-005-configuration-strategy.md`.
