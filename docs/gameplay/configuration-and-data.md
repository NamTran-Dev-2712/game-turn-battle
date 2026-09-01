# Configuration & Data — Gameplay Data Model

> Ranh giới dữ liệu data-driven cho gameplay và quan hệ với Configuration Service (ADR-004/005). Mô tả **schema boundary**, không phải giá trị cân bằng (tuning — `../mvp/10` EC).

---

## 1. Nguyên tắc
- **Code phụ thuộc schema, không phụ thuộc giá trị** (ADR-004).
- Nguồn sự thật runtime = **Configuration Service** (server), phân phối bundle versioned cho client (ADR-005).
- Mọi file config validate bằng **JSON Schema** (`shared/config-schema/`) ở CI (`../testing/`).

## 2. Danh mục config gameplay

| Config | Nội dung | Dùng bởi |
|---|---|---|
| `heroes/` | Hero definition | Hero, Combat |
| `skills/` | Skill + effect | Skill, Combat |
| `stages/` | Campaign/tower stage (địch, thưởng, yêu cầu) | Campaign, Combat |
| `gacha/` | Banner + rate + pity | Economy/Summon |
| `shop/` | Shop items + giá | Economy/Shop |
| `rewards/` | Reward tables (AFK, quest, first-clear) | Economy, Quest |
| `economy/` | Đường cong cost (level/sao), energy params | Progression, Economy |
| `quests/` | Quest definition | Quest |
| `liveops/` | Event/season/flag (schedule) — Post-MVP | LiveOps |

## 2b. Ánh xạ schema (phase 06)

Mỗi loại config có một JSON Schema (draft 2020-12) ở `../../shared/config-schema/` — định nghĩa **cấu trúc/kiểu**, không chứa giá trị balance. `common.schema.json` giữ `$defs` dùng lại (id prefix, `combat_int`, enum khớp `GameTeam.Contracts`).

| Config type | Schema | Nguồn gameplay | Tham chiếu chính | Ghi chú |
|---|---|---|---|---|
| hero | `hero.schema.json` | `hero-system.md` | `skills` → skill id | `base_stats` integer; `faction` chuỗi (GP2 chưa chốt) |
| skill | `skill.schema.json` | `skill-framework.md` | `effects[].effect_type` (registry) | effect_type: damage/heal/apply_buff/apply_debuff/shield; `params` mở |
| stage | `stage.schema.json` | `progression-and-economy.md` | `enemies[].hero_id` → hero; `rewards[]` → reward | `energy_cost` integer |
| reward | `reward.schema.json` | `progression-and-economy.md` | `entries[].ref_id` (currency/hero/fragment/item) | `amount` integer |
| gacha | `gacha.schema.json` | `progression-and-economy.md` | `pool[]` → hero; `rates[].rarity` | rate/pity **cấu trúc**, không giá trị |
| shop | `shop.schema.json` | `progression-and-economy.md` | `items[].reward_ref` → reward; `cost.currency` | `cost.amount` integer |
| economy | `economy.schema.json` | `progression-and-economy.md` | `cost_curves`, `energy` | bước đường cong integer, không cố định |
| quest | `quest.schema.json` | `quest-system.md` | `reward_refs[]` → reward; `condition_type` | condition_type: battles_won/summons_done/login |

> **Cấp độ tham chiếu:** JSON Schema chỉ ràng buộc **định dạng/cấu trúc** của ref (prefix id, kiểu). **Kiểm tồn tại id chéo file** (hero→skill…) là việc của validator (phase 07 — §3, §6), không phải schema đơn. Fixture pass/fail ở `../../shared/config-schema/fixtures/`; quy tắc migration ở `../../shared/config-schema/_versions/`.

## 3. Quan hệ id (referential integrity)

```mermaid
flowchart LR
    Hero[hero.skills -> skill id] --> Skill
    Stage[stage.enemies -> hero/enemy id] --> Hero
    Stage[stage.rewards -> reward id] --> Reward
    Gacha[gacha.pool -> hero id] --> Hero
    Shop[shop.items -> item/reward id] --> Reward
```

- Validator kiểm **id tham chiếu tồn tại** (không trỏ id không có) — chống lỗi config khi live.

## 4. Versioning
- Mỗi file có `schema_version`; bundle có version `config@vN` (ADR-005).
- Đổi giá trị → publish version mới. Đổi cấu trúc → tăng schema_version + migration/compat.
- Client cache theo version; chỉ tải khi có version mới. **Client-side (Phase 16):** autoload
  `ConfigProvider` cache `config@vN` **bất biến** xuống đĩa (`user://config_cache/`, ghi-một-lần),
  nạp khi boot, phát `config_updated` khi đổi version — **không rebuild client**. Chi tiết:
  `../godot/resources-and-assets.md` §1.1.

### 4.1 Luồng config end-to-end (Phase 22)
Vòng data-driven đầy-đủ từ Configuration Service (server, phase 21) tới UI client:

```text
Client boot
  → GET /api/v1/config/current          (ConfigBundleDto: version.bundle = N hiện hành)
  → so N với version cache đĩa
  ├─ bằng   → dùng bundle cache (offline-view)
  └─ mới hơn → GET /api/v1/config/bundle?bundleVersion=N   (bundle NGUYÊN VĂN, bất biến)
       → ConfigProvider.apply_bundle (validate envelope)
       → cache đĩa config@vN (ghi-một-lần, KHÔNG ghi đè version cũ)
       → phát config_updated
  → Feature query qua ConfigProvider.get_all(&"hero") / get_entry(type,id)
  → màn mẫu (HeroListView) hiển thị dữ liệu hero từ config
```

- **Hình dạng `data` server phát:** bundle `data` là **map theo id** — `data.{type}.{id} = entry`
  (KHÔNG phải mảng). `ConfigProvider._build_index` index theo `entry.id`; chấp nhận cả map (server)
  lẫn mảng (fixture cũ) để tương thích ngược.
- **Endpoint & tham số THẬT (phase 21):** `GET /api/v1/config/current` +
  `GET /api/v1/config/bundle?bundleVersion=N`. Tham số tên **`bundleVersion`** (KHÔNG `version` —
  trùng token `{version:apiVersion}` phía server). Endpoint config là **public** (`.AllowAnonymous`) —
  bundle là nội dung chia sẻ, không nhạy cảm; client chỉ đọc.
- **Version là bất biến & so bắt buộc:** client KHÔNG tự quyết version mới — luôn hỏi
  `/config/current` rồi so. Mỗi `config@vN` cache dưới file riêng, không bao giờ ghi đè.

### 4.2 Fallback (KHÔNG im lặng)
Khi tải/áp bundle mới thất bại, client suy giảm có kiểm soát và **báo rõ** (ADR-005; không che lỗi):

- **Có cache cũ (stale):** giữ `config@v(N-1)` đang dùng, `ConfigProvider.is_stale()` = true +
  `last_error_code()`; boot ghi `push_warning`; màn feature hiện **banner "đang dùng cache cũ"** +
  nút **Thử lại**. KHÔNG bịa dữ liệu.
- **Không có cache:** màn feature hiện **empty state + nút Thử lại** (retry gọi lại
  `check_for_update()` qua đúng abstraction `NetworkClient` — không vòng lặp vô hạn).
- Boot vẫn **best-effort** với config (không chặn boot) để giữ offline-view (phase 20); UI feature là
  nơi lộ trạng thái stale + retry cho người dùng.

### 4.3 Chứng minh "đổi config → KHÔNG rebuild client"
Cùng một binary/scene client, chỉ đổi dữ liệu config phía server:

```text
config@v1: hero_sample.rarity = 3   → client hiển thị "hero_sample · rarity 3"
   (sửa config/heroes/hero_sample.json phía server → publish)
config@v2: hero_sample.rarity = 5   → reload/retry client → hiển thị "hero_sample · rarity 5"
   → client KHÔNG build lại; chỉ ConfigProvider nạp version mới.
```

Kiểm chứng tự động: gdUnit4 mock (`tests/core/config/config_provider_test.gd`,
`tests/ui/hero_list/hero_list_presenter_test.gd`) — nhận→query→hiển thị, version bump, lỗi→fallback,
no-cache→retry. Seed config mẫu (`config/heroes/hero_sample.json` + `config/skills/skill_sample_basic.json`,
số 0 — KHÔNG balance) cho phép server thật phát `data.hero` khác rỗng để demo e2e.

## 5. Ranh giới với backend
- Domain/Application đọc config qua `IConfigProvider` (port), **không** đọc file trực tiếp (`../backend/`).
- Combat sim đọc chỉ số từ config version cụ thể (đảm bảo re-sim tất định — ADR-011).
- **Configuration Service (Phase 21 — đã hiện thực):** `IConfigProvider` = `RuntimeConfigProvider` (Infrastructure) phục
  vụ **bundle bất biến hiện hành** (`config@vN`) từ snapshot bộ nhớ — `Get<T>(type,id)`/`GetIds(type)`/`CurrentVersion`.
  Backend nạp/validate (tái dùng validator phase 07)/version/publish bundle; đổi giá trị config → **version mới, không
  rebuild**. Chi tiết: `../backend/infrastructure.md §3.1`; endpoint & boundary: `../liveops/remote-config.md §4.1`.

## 6. Tooling
- `tools/config-validator` (Phase 07 — **GATE CI bắt buộc**, .NET 9): validate schema (draft 2020-12) +
  referential integrity + `schema_version` cho `config/**`. Chạy: `bash tools/config-validator/run.sh config
  shared/config-schema`. Report `file:jsonpath:CODE`; mã lỗi (`JSON001`/`MAP001`/`SCH001`/`VER001`/`VER002`/`REF001`/`REF002`)
  ở `../../tools/config-validator/README.md`. Core lib **được Config Service (Phase 21) tái dùng** qua ProjectReference
  (`ConfigValidationRunner.Run` + `ConfigLoader.Load`) — một nguồn validate, không fork validator thứ 2.
- `tools/content-importer` (Post-MVP): import bảng (csv/xlsx) → config json.

## 7. Liên kết
- ADR-004 (data-driven), ADR-005 (config strategy)
- Conventions JSON: `../conventions/data-and-docs-conventions.md`
- Backend config service: `../backend/infrastructure.md`
- LiveOps: `../liveops/remote-config.md`
