# `shared/codegen/` — Codegen client (Contracts → GDScript, Phase 08, GATE thật)

> **Sinh model client GDScript từ hợp đồng OpenAPI** — client KHÔNG gõ tay DTO/enum, không lệch server
> theo thời gian (ADR-008). Nguồn DUY NHẤT là `shared/contracts/openapi.json` (sinh từ
> `server/GameTeam.Contracts`). Đầu ra `client/src/data/generated/` **không sửa tay**.

```
server/GameTeam.Contracts  ──(build .NET, Phase 05)──▶  shared/contracts/openapi.json
                                                                │
                                                     shared/codegen (tool .NET 9)
                                                                │
                                                                ▼
                                                   client/src/data/generated/*.gd
```

## Nó làm gì

Đọc `openapi.json` → sinh **6 enum dùng chung** + **DTO nền** thành GDScript (Godot 4.7):
- Enum → `class_name <Name>` + `enum { … }` **giữ đúng giá trị số** của `GameTeam.Contracts`
  (vd `Rarity` = `0,3,4,5`, có "khoảng trống"). Wire serialize dạng **chuỗi** (JsonStringEnumConverter);
  giá trị số là để dùng nội bộ client.
- DTO → `class_name <Name> extends Resource` + biến typed theo property.

## Đầu vào

`shared/contracts/openapi.json` (OpenAPI 3.0.1) — **nguồn duy nhất**, KHÔNG sửa tay (CI `ci-server`
có OpenAPI drift guard). Enum trong spec được enrich `x-enum-varnames` + `x-enum-values` bởi
`server/src/GameTeam.Api/OpenApi/ContractEnumsDocumentTransformer.cs` để mang giá trị số qua spec.

## Đầu ra

`client/src/data/generated/` — mỗi schema một file `.gd` (snake_case), **committed** (drift check cần
file tracked). File có header `# AUTO-GENERATED — DO NOT EDIT` + đường dẫn nguồn.

## Cách chạy

Từ **gốc repo**:

```bash
bash shared/codegen/run.sh
# hoặc chỉ định tường minh:
bash shared/codegen/run.sh shared/contracts/openapi.json client/src/data/generated
```

`run.sh` build CLI (`-c Release`, log ra stderr) rồi chạy. Exit `0` = xong; `2` = sai tham số / thiếu
OpenAPI / contract chứa cấu trúc chưa hỗ trợ (in `schema:property:reason`).

## Khi nào chạy

> **Khi contract/DTO thay đổi thì bắt buộc regenerate.**

Quy trình: sửa `server/GameTeam.Contracts` → `dotnet build server/GameTeam.sln` (regenerate
`openapi.json`) → `bash shared/codegen/run.sh` → xem diff generated → commit.

## CI

`.github/workflows/codegen-check.yml` (GATE): checkout → setup-dotnet (theo `global.json`) → `run.sh`
→ `git diff --exit-code -- client/src/data/generated`. Generated bị lệch (stale) ⇒ **FAIL**. Import
Godot headless của model generated do `ci-client.yml` đảm nhiệm (`--headless --import` trên `client/**`).

## File generated

> **Không chỉnh sửa thủ công generated files.** Sửa tay sẽ bị ghi đè ở lần regenerate kế tiếp và bị CI
> drift check chặn. Đổi nội dung ⇒ sửa **contract** (`GameTeam.Contracts`), không sửa `.gd`.

## Kiến trúc

`GameTeam.Codegen` (core, không Console/Exit — ranh giới tái dùng) + `GameTeam.Codegen.Cli` (CLI mỏng)
+ `GameTeam.Codegen.Tests` (xUnit). **Không gói ngoài** — chỉ `System.Text.Json` built-in. Deterministic
(`Deterministic=true`, giữ thứ tự khai báo, sort file theo tên, LF + newline cuối, không timestamp) →
**idempotent**: cùng input ⇒ output byte-identical.

### Bảng map kiểu (OpenAPI/C# → GDScript)

| OpenAPI | GDScript |
|---|---|
| `string` | `String` |
| `integer` | `int` |
| `number` | `float` |
| `boolean` | `bool` |
| `array<T>` | `Array[T]` (phần tử untyped → `Array`) |
| `$ref` → DTO | kiểu class (`ConfigVersion`, …) |
| `$ref` → enum | `int` + ghi chú `## enum: <Name> (wire: string)` |
| `nullable` (nội trị) | biến untyped (Variant) + ghi chú `## nullable` |
| `nullable` (class) | giữ chú kiểu (Resource nhận `null` sẵn) |

Mỗi property có ghi chú `## wire: <jsonKey>` (camelCase) phục vụ parse ở Phase 15. Field GDScript là
snake_case.

## Giới hạn (chưa hỗ trợ — fail RÕ RÀNG, không sinh sai)

Generator ném lỗi `schema:property:reason` khi gặp:
- `oneOf`/`allOf`/`anyOf`/`not` (tổ hợp schema),
- `object` nội tuyến / map (`additionalProperties`) — hãy tách thành schema DTO ở `components`,
- `array` thiếu `items`,
- `$ref` treo (tới schema không tồn tại),
- enum thiếu `x-enum-values` (spec chưa enrich).

Kiểu phức tạp (dictionary/map, tuple, tổ hợp) là **nợ mở rộng dần**. Round-trip parse (client↔server) là
Phase 15 — Phase 08 chỉ sinh **read-model** (không logic/mạng).

## Mở rộng

Thêm DTO/enum vào `GameTeam.Contracts` → rebuild → chạy codegen: model mới **tự có** (pipeline
schema-driven, không có danh sách DTO hardcode). Thêm kiểu mới ⇒ mở rộng `GdTypeMapper` + test kèm.

## Liên kết

- Hợp đồng & versioning: `../../docs/backend/api-and-versioning.md` §4
- Resource client: `../../docs/godot/resources-and-assets.md`
- Dependency rule: `../../docs/architecture/dependency-graph.md` · ADR: `../../docs/adr/ADR-008-networking.md`,
  `../../docs/adr/ADR-002-godot-architecture.md`
- Phase: `../../docs/roadmap/08-codegen-pipeline.md` · Quyết định: `../../.memory/0006-codegen-pipeline-standardized.md`
