# Hero System — Module Architecture

> Ranh giới & trách nhiệm module Hero. Nguồn: `../mvp/03` §2, `../mvp/05`. Không hiện thực logic; không đổi thiết kế.

---

## 1. Trách nhiệm module
- Định nghĩa **dữ liệu hero** (faction/class/element/role/rarity/base stats/skill refs) — data-driven (ADR-004).
- Quản lý **trạng thái hero của người chơi** (level, sao/ascension, gear lắp) — server-authoritative (ADR-007).
- Cung cấp **snapshot hero** cho combat sim (chỉ số tính từ config + trạng thái).

## 2. Không thuộc module này
- Logic combat (→ `combat-framework.md`).
- Logic skill effect (→ `skill-framework.md`).
- Quy tắc summon (→ `../mvp/03` §9, economy).

## 3. Dữ liệu (schema — data-driven)

| Nhóm | Nội dung | Nơi định nghĩa |
|---|---|---|
| Hero definition (tĩnh) | id, faction, class, element, role, rarity, base_stats, skill refs, asset refs | `config/heroes/` + schema |
| Hero instance (động, per-player) | hero_id, level, star/ascension, equipped gear, exp | DB profile (server) |
| Stat computation | Cách gộp base + level + sao + gear → stats cuối | Domain policy (mechanism), đọc config |

> Danh sách faction/class/element/role & số hero **chưa chốt** — `../mvp/10` GP2/GP4. Module thiết kế **mở** theo enum config, không cứng hoá số lượng.

## 4. Ranh giới client/server

| | Client | Server |
|---|---|---|
| Hiển thị hero, hoạt ảnh | ✅ | — |
| Xem stats (đọc cache) | ✅ | — |
| Thay đổi (level/sao/gear) | Gửi command | ✅ Quyết định + lưu |
| Snapshot cho combat | Dùng để hiển thị | ✅ Nguồn cho re-sim |

## 5. Tương tác module

```mermaid
flowchart LR
    Config[config/heroes + skills] --> Hero[Hero Module]
    Profile[(Player Profile)] --> Hero
    Hero --> Combat[Combat Framework - snapshot]
    Hero --> Progression[Progression - level/sao]
    Hero --> Equipment[Equipment - gear lắp]
    Hero -. HeroAscended event .-> Quest[Quest/Telemetry]
```

## 6. Mở rộng tương lai (chừa chỗ)
- Thêm faction/hero mới = thêm **config**, không sửa code (ADR-004).
- Skin/awakening (Future — `../mvp/04` F37): thêm trường config + trạng thái, không phá schema (versioning ADR-005).

## 7. Trạng thái hiện thực (Phase 27 — đã đóng)

Nền tảng Hero data-driven đã hiện thực (ADR-004/007). Chi tiết vận hành:

**Server (chân lý):**
- **HeroDefinition = config, KHÔNG hardcode.** Đọc qua port **`IConfigProvider.Get<HeroConfig>("hero", id)`**
  (`server/src/GameTeam.Application/Features/Heroes/HeroConfig.cs`) — faction/class/element/role/rarity/base_stats/
  skills/art từ `config/heroes/*.json` (schema phase 06, thêm field tuỳ chọn `art`). KHÔNG có nguồn hero thứ hai.
- **`OwnedHero`** (`GameTeam.Domain/Heroes/OwnedHero.cs`, `AggregateRoot<Guid>`): instance động gắn
  **`ProfileId`** (khoá ngoại `player_profiles`), `HeroId` (ref config), `Level`/`Stars` nền. Bảng `owned_heroes`
  (unique `(profile_id, hero_id)`). Chỉ số tĩnh KHÔNG lưu ở đây — đọc từ config.
- **Queries:** `GetMyHeroesQuery` (owner suy TỪ token `sub` qua `ICurrentUser` — chống IDOR; trả
  `MyHeroesResponse` bọc `OwnedHeroDto{heroId,level,stars}`) + `GetHeroDefinitionQuery` (definition từ config →
  `HeroDefinitionDto`). Endpoint: `GET /api/v1/heroes` (protected) + `GET /api/v1/heroes/{heroId}/definition`
  (public catalog).
- **Seed tạm:** guest login cấp toàn bộ hero trong config (`GetIds("hero")`) cùng transaction — **tạm tới
  phase 33 (summon)**, KHÔNG phải cơ chế nhận thật.

**Client (hiển thị, không chân lý):**
- **Hero List** (`client/src/ui/hero_list/`): GHÉP hero **owned** (`StateCache.get_heroes()`, server-authoritative)
  + **definition** (`ConfigProvider.get_hero(id)`, data-driven). Đổi config → định nghĩa đổi KHÔNG rebuild.
- **Hero Detail** (`client/src/ui/hero_detail/`): chi tiết một hero (id truyền qua `SceneRouter.route_context()`);
  **art tải LAZY** qua **`AssetLoader`** (`client/src/core/assets/asset_loader.gd`, autoload) — placeholder trước,
  art thật sau (KHÔNG chặn list — ADR-009), đường dẫn art từ config (field `art`), giải phóng khi rời màn.
- Contract→codegen: DTO hero ở `GameTeam.Contracts/Hero/*` → `openapi.json` → GDScript generated (DO-NOT-EDIT).

**Ranh giới quyền:** client KHÔNG tự thêm hero / đổi owner / level / sao / chỉ số. Definition từ config; ownership
từ server/profile. Ngoài phạm vi Phase 27: skill (28), formation (29), battle (30), summon (33), nâng cấp (35/39).

## 8. Formation & Team (Phase 29 — đã đóng)

Đội hình **6 hero + vị trí (formation)** — lựa chọn tactical duy nhất trong combat full-auto (A02/A05/A12).
Lưu **server-authoritative** (ADR-007); vị trí đi vào combat sim, ảnh hưởng target/aggro (ADR-011,
`combat-framework.md` §7/§14). Client chỉ gửi **intent** — server validate + quyết định.

**Server (chân lý):**
- **`Team`** (`GameTeam.Domain/Teams/Team.cs`, `AggregateRoot<Guid>`): gắn **`ProfileId`** (khoá ngoại
  `player_profiles`, **unique** — một đội/profile MVP) + các **`TeamSlot`** (`SlotIndex` 0-based, `HeroId` ref
  config). Bảng `teams` + owned collection `team_slots` (PK `(team_id, slot_index)`), migration `AddTeams`.
  Bất biến **cấu trúc** ở Domain (≥1 ô, không trùng ô, không trùng hero); ràng buộc **phụ thuộc config** ở Application.
- **Lưới = config, KHÔNG hardcode.** Loại config **`formation`** (`shared/config-schema/formation.schema.json`,
  `config/formation/formation_default.json`: `rows`/`cols`) đọc qua **`IConfigProvider.Get<FormationConfig>("formation",
  "formation_default")`** — **số ô (team size) = rows×cols**. Client đọc lưới từ config bundle để dựng UI.
- **`SaveTeamCommand`** (`Features/Teams/Commands/`, `ITransactionalRequest`): owner suy TỪ token `sub`
  (`ICurrentUser` — chống IDOR), validate **server-authoritative**: đúng số ô (`TEAM_INVALID_SIZE`), slot trong lưới
  không trùng (`TEAM_INVALID_SLOT`), không trùng hero (`TEAM_DUPLICATE_HERO`), **mọi hero thuộc sở hữu**
  (`IOwnedHeroRepository`, `TEAM_HERO_NOT_OWNED`) → upsert (`Create`/`Replace`). **`GetMyTeamQuery`**: đọc đội theo
  token; chưa lưu ⇒ đội rỗng (lưới trống). Endpoint: `GET`+`POST /api/v1/team` (protected).
- **Team snapshot** (`Features/Teams/TeamSnapshotFactory.cs`): đội đã lưu → `IReadOnlyList<CombatTeamMember>` bất
  biến (feed `CombatInputResolver`/`BattleRequest.Ally`, phase 30); `SlotIndex` → `slot` combat. Bản sao giá trị,
  KHÔNG giữ tham chiếu profile/team mutable ⇒ sim tất định (ADR-011).

**Client (hiển thị, không chân lý):**
- **Formation** (`client/src/ui/formation/`): `FormationView` (BaseView, network-free) dựng lưới (rows×cols từ
  `ConfigProvider`) + roster hero owned (`StateCache.get_heroes()`); `FormationPresenter` giữ **bản nháp cục bộ**
  (chọn hero → đặt ô → đổi vị trí), khi Lưu → **`NetworkClient.post_json("/team", …)`** (intent) → hiển thị lại theo
  **đội server trả về** (bản nháp bị thay). Mở màn → `GET /team` nạp đội đã lưu. KHÔNG lưu cục bộ rồi coi như server nhận.
- Contract→codegen: DTO team ở `GameTeam.Contracts/Team/*` (`TeamDto`/`TeamSlotDto`/`SaveTeamRequest`) → `openapi.json`
  → GDScript generated (DO-NOT-EDIT); parser `NetworkResponseParser.parse_team`.

**Ranh giới quyền:** client KHÔNG tự quyết ownership / hợp lệ / trùng / slot / đội đã lưu — server validate tất cả.
Ngoài phạm vi Phase 29: battle thật (30), nhiều đội/preset (Post-MVP), bonus vị trí (config/tuning), aggro nâng cao
(CB3, `../mvp/10`).

## 9. Liên kết
- Combat: `combat-framework.md` · Skill: `skill-framework.md`
- Progression: `progression-and-economy.md` · Config: `configuration-and-data.md` · Assets: `../godot/resources-and-assets.md`
- Nguồn: `../mvp/03`, `../mvp/05` · Roadmap: `../roadmap/27-hero-system.md`
