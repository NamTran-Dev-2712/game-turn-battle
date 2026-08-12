# `tools/config-validator/` — Config Validator (Phase 07, GATE thật)

Validate mọi file `config/**` theo `shared/config-schema/**`: **(1)** đúng JSON Schema (draft 2020-12),
**(2)** **referential integrity** giữa các file (hero↔skill↔stage↔reward↔gacha…), **(3)** `schema_version`
hợp lệ. Config sai → **exit ≠ 0** kèm vị trí chính xác. Là **CI gate bắt buộc** (`validate-config.yml`) để
config sai **không bao giờ** tới runtime (ADR-004/005).

> Ranh giới: đây là **validate lúc author-time / CI**. Loading & publishing bundle runtime là **Config
> Service (Phase 21)** — Phase 21 **tái dùng** chính core này (xem §Phase 21 reuse), KHÔNG hiện thực ở đây.

## Stack & vì sao

- **.NET 9 console** (SDK pin `global.json` = `9.0.306`) + **`JsonSchema.Net`** (json-everything) cho JSON
  Schema draft 2020-12. Chọn .NET để đồng nhất codebase server, tái dùng xUnit/CI, và để Config Service
  (Phase 21, cũng .NET) **project-reference** thẳng vào core lib. `$ref` (absolute URI
  `https://game-team/schema/...`) được giải **cục bộ** qua registry — **không fetch mạng**.
- CPM (ADR-010) qua `Directory.Packages.props` riêng; solution **tách rời** `server/GameTeam.sln`.

## Kiến trúc (CLI mỏng ↔ core tái dùng)

```
GameTeam.ConfigValidator        # CORE (thuần logic, KHÔNG Console/Exit) — ranh giới tái dùng Phase 21
  SchemaSet          # nạp + đăng ký MỌI schema MỘT LẦN (memo theo thư mục) → $ref giải cục bộ
  ConfigFileMapper   # map thư mục SỐ NHIỀU (heroes/) → schema SỐ ÍT (hero.schema.json)
  ConfigLoader       # duyệt config/**.json một lần, phân loại, parse (JSON001 / MAP001)
  IdIndex            # bảng ID theo loại, tra cứu O(1) cho referential integrity
  SchemaValidator    # validate draft 2020-12, gom MỌI lỗi (không dừng ở lỗi đầu) → SCH001
  ReferenceValidator # kiểm tham chiếu chéo theo đồ thị cố định → REF001 / REF002
  VersionValidator   # schema_version hiện diện + khớp phiên bản hỗ trợ → VER001 / VER002
  ConfigValidationRunner # điều phối: schema 1 lần → index 1 lần → gom lỗi → report ổn định
GameTeam.ConfigValidator.Cli    # CLI mỏng: parse args, in report, đặt exit code
GameTeam.ConfigValidator.Tests  # xUnit + FluentAssertions + cây fixture
run.sh                          # entrypoint GATE mà validate-config.yml gọi
```

## Yêu cầu

- **.NET SDK 9** (theo `global.json`). Không cần cài gói toàn cục — `dotnet` tự restore `JsonSchema.Net`.
- Chạy được trên Windows / Linux / macOS (CI = `ubuntu-latest`).

## Chạy local

Từ **gốc repo** (đường dẫn config/schema là tương đối nơi gọi):

```bash
# qua entrypoint GATE (giống hệt CI):
bash tools/config-validator/run.sh config shared/config-schema

# hoặc trực tiếp bằng dotnet:
dotnet run --project tools/config-validator/GameTeam.ConfigValidator.Cli -c Release -- config shared/config-schema
```

## Tham số & CLI

```
config-validator <config-dir> [schema-dir]
  <config-dir>   Thư mục config cần validate (vd: config)
  [schema-dir]   Thư mục JSON Schema (mặc định: shared/config-schema)
  -h | --help    In hướng dẫn
```

## Exit codes

| Code | Ý nghĩa |
|---|---|
| `0` | Hợp lệ — không lỗi. |
| `1` | Có lỗi validate (schema / reference / version / mapping / parse). |
| `2` | Sai tham số hoặc lỗi hạ tầng tool (vd thiếu thư mục schema). |

CI dùng exit code làm gate: khác `0` → job đỏ.

## Định dạng report & mã lỗi

Mỗi lỗi in một dòng: **`file:jsonpath:CODE message`** — đủ để định vị & sửa **mà không cần đọc mã validator**.
`jsonpath` là JSON Pointer tới vị trí lỗi (vd `/skills/0`, `/base_stats/hp`); gốc tài liệu hiển thị `/`.

| Code | Nhóm | Nghĩa | Cách sửa |
|---|---|---|---|
| `JSON001` | parse | File không phải JSON hợp lệ. | Sửa cú pháp JSON của file. |
| `MAP001` | mapping | File config ở thư mục không map được loại. | Đặt file dưới đúng thư mục (`heroes/…quests/`); metadata dùng `liveops/`, `_versions/`. |
| `SCH001` | schema | Vi phạm JSON Schema (kèm keyword + path). | Sửa field theo schema loại tương ứng ở `shared/config-schema/`. Combat = integer (ADR-011). |
| `VER001` | version | Thiếu `schema_version` hoặc không phải số nguyên. | Thêm `"schema_version": 1` (số nguyên) ở gốc file. |
| `VER002` | version | `schema_version` không được hỗ trợ. | Dùng phiên bản hiện tại (`1`). Bump phiên bản là quy trình có migration — xem `shared/config-schema/_versions/`. |
| `REF001` | reference | ID được tham chiếu không tồn tại. | Sửa ID cho khớp một entity có thật, hoặc thêm entity đích. |
| `REF002` | reference | Tham chiếu sai loại đích (vd `reward_type=currency` nhưng `ref_id` không thuộc `gold\|gem\|ticket`). | Sửa `ref_id` theo `reward_type`. |

Ví dụ đọc lỗi:

```
config/heroes/ignis.json:/skills/0:REF001 skill id 'skill_ghost' được tham chiếu nhưng không tồn tại.
config/heroes/ignis.json:/base_stats/hp:SCH001 type: Value is "number" but should be "integer"
```

## Danh mục validate & đồ thị tham chiếu (lấy nguyên văn từ schema — KHÔNG phát minh)

| Loại | Schema | Tham chiếu kiểm (referential integrity) |
|---|---|---|
| hero | `hero.schema.json` | `skills[]` → skill |
| skill | `skill.schema.json` | (không tham chiếu id chéo) |
| stage | `stage.schema.json` | `enemies[].hero_id` → hero; `rewards[]` → reward; `requirements.prerequisite_stage_id` → stage |
| gacha | `gacha.schema.json` | `pool[]` → hero |
| shop | `shop.schema.json` | `items[].reward_ref` → reward |
| reward | `reward.schema.json` | `entries[].ref_id` đa hình theo `reward_type` |
| economy | `economy.schema.json` | (không tham chiếu id chéo) |
| quest | `quest.schema.json` | `reward_refs[]` → reward |

**`reward.entries[].ref_id` đa hình:** `currency` → phải thuộc `gold\|gem\|ticket` (else `REF002`);
`hero` → phải tồn tại trong index hero (else `REF001`).

### Giới hạn có chủ đích (Known limitations)

- `reward_type` = **`fragment`** / **`item`**: hiện **chưa có** loại config tương ứng trong repo → validator
  **chỉ kiểm định dạng, KHÔNG kiểm tồn tại** `ref_id`. Đây là giới hạn có chủ đích để **không phát minh**
  quan hệ chưa có trong schema/gameplay. Khi loại config đó xuất hiện, mở rộng `ReferenceValidator` + thêm test.
- **`schema_version` chỉ hỗ trợ `1`** (chưa có migration; xem `shared/config-schema/_versions/`). Validator
  **không** thực hiện migrate — phiên bản lạ là `VER002`. Migration là quy trình riêng khi có breaking change.
- Cross-file referential integrity là việc của validator **này**; JSON Schema đơn lẻ chỉ ràng buộc *định dạng*
  ID (prefix), không kiểm *tồn tại*.

## Test

```bash
dotnet test tools/config-validator/ConfigValidator.sln -c Release
```

Bộ test (xUnit) phủ: valid tree pass; schema/ref/version fail đúng mã; gom nhiều lỗi (không dừng ở lỗi đầu);
mapping thư mục; index ID; MAP001/JSON001; report actionable; và **tái dùng fixture Phase 06**
(`shared/config-schema/fixtures/*.valid|invalid.json`) làm test vector.

## Debug khi CI đỏ

1. Chạy local đúng lệnh CI: `bash tools/config-validator/run.sh config shared/config-schema`.
2. Đọc từng dòng `file:jsonpath:CODE message`; tra mã ở bảng trên.
3. `SCH001` → mở schema loại tương ứng ở `shared/config-schema/` xem ràng buộc.
4. `REF001/REF002` → kiểm ID đích có tồn tại / đúng loại không.
5. `VER00x` → kiểm `schema_version` ở gốc file.

## Phase 21 reuse (ranh giới)

Toàn bộ logic ở **`GameTeam.ConfigValidator`** (core, không phụ thuộc Console/CLI). Config Service (Phase 21)
khi publish bundle sẽ **project-reference core này** và gọi `ConfigValidationRunner.Run(...)` để validate
**trước khi** publish — KHÔNG viết lại logic. Phase 07 **không** hiện thực: Config Service, bundle publishing,
runtime config loading, migration execution.

Chi tiết quyết định: `../../.memory/0005-config-validator-standardized.md`.
Bám: `../../docs/gameplay/configuration-and-data.md`, `../../docs/adr/ADR-005-configuration-strategy.md`.
