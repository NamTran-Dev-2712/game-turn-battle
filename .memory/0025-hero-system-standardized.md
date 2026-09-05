# 0025 — Hero System (data-driven) standardized (Phase 27)

- **Trạng thái:** Đã chốt & verify cục bộ (2026-09-05). **Mở Nhóm 6 (Gameplay Vertical Slice).** Nền Hero
  data-driven (ADR-004) + server-authoritative (ADR-007): definition từ config, owned gắn profile, client
  hiển thị list/detail, art lazy (ADR-009). Không đổi spec combat/config; mở rộng additive schema + contracts.
- **Bối cảnh:** Trước Phase 27 có `client/src/ui/hero_list/` là màn MẪU config e2e (Phase 22) và POCO
  `HeroCombatConfig` (lát cắt combat). Chưa có `OwnedHero`, chưa có Hero Detail, chưa có bộ nạp asset,
  chưa có DTO hero. Phase 27 hiện thực Hero System thật (foundation cho 28/29/30/33/35).

## Quyết định (user-approved)

- **Hero Detail = scene riêng** + mở rộng `SceneRouter.goto_scene(path, context)` (additive) + `route_context()`
  để truyền `hero_id` (SceneRouter không truyền tham số qua ctor scene).
- **Art path lấy từ config**: thêm field **tuỳ chọn `art`** vào `hero.schema.json` (additive, KHÔNG bump
  `schema_version`); `AssetLoader` lazy-load + placeholder.
- **`GetMyHeroes` trả lean** `OwnedHeroDto{heroId,level,stars}` (bọc `MyHeroesResponse`); client ghép
  definition từ `ConfigProvider`. `GetHeroDefinition` trả definition từ config riêng (proof data-driven server).

## Thành phần

- **Server Domain** `GameTeam.Domain/Heroes/`: `OwnedHero : AggregateRoot<Guid>` (ProfileId/HeroId/Level/Stars,
  `Grant`/`Restore`, event `OwnedHeroGranted`). Chỉ số tĩnh KHÔNG lưu ở entity.
- **Server Application** `GameTeam.Application/Features/Heroes/`: `HeroConfig` POCO (đọc `IConfigProvider.Get<HeroConfig>("hero",id)`),
  `GetMyHeroesQuery`/`GetHeroDefinitionQuery` + handlers, `HeroErrors`, `HeroMapping` (`HeroMapping.ConfigType="hero"`;
  parse chuỗi config→enum contract, không khớp⇒None). `IOwnedHeroRepository` (Abstractions).
- **Server Infrastructure**: `OwnedHeroConfiguration` (bảng `owned_heroes`, unique `(profile_id,hero_id)` + FK
  cascade), `OwnedHeroRepository`, `DbSet<OwnedHero>`, DI, migration `AddOwnedHeroes`.
- **Seed tạm**: `CreateGuestAccountCommandHandler` cấp `GetIds("hero")` cùng transaction (tới phase 33).
- **Contracts** `GameTeam.Contracts/Hero/`: `OwnedHeroDto`, `MyHeroesResponse` (bọc — tránh mảng trần vì
  NetworkClient chỉ nhận Dictionary), `HeroDefinitionDto` (faction=string GP2; class/element/role/rarity=enum),
  `HeroBaseStatsDto`. Endpoint `/api/v1/heroes` (protected) + `/api/v1/heroes/{heroId}/definition` (public).
- **Codegen**: sinh 3 DTO GDScript; **fix escape từ khoá GDScript** (`GdNaming.ToFieldName`: wire `class`→var
  `class_`, giữ `## wire: class`) + `ContractEnumSchemaTransformer` (làm giàu enum được DTO tham chiếu) +
  `ContractEnumsDocumentTransformer` chỉ force-publish enum CHƯA tham chiếu (self-maintaining, tránh trùng khoá).
- **Client**: `hero_list` (nâng cấp: owned StateCache + definition ConfigProvider + intent open_hero),
  `hero_detail/*` (mới), `core/assets/asset_loader.gd` (autoload lazy async + placeholder + release; config-driven
  path), `SceneRouter` (context), `auth_profile_flow.gd` (fetch `/heroes` → snapshot {profile,heroes}),
  `response_parser.parse_my_heroes`. KHÔNG thêm event EventBus (tái dùng `config_updated`/`state_refreshed`).

## Chống tự-vẽ (binding)

- Definition data-driven từ `IConfigProvider` — **KHÔNG hardcode chỉ số**, KHÔNG nguồn hero thứ hai, KHÔNG lưu
  stats ở `owned_heroes`. Ownership từ token `sub` (`ICurrentUser`) — chống IDOR; client KHÔNG tự thêm/đổi.
- Tái dùng `AssetLoader`/`ConfigProvider`/`StateCache`/`NetworkClient` — KHÔNG bộ nạp/cache/HTTP thứ hai. Art path
  từ config; list KHÔNG chặn art (ADR-009). KHÔNG hand-edit `client/src/data/generated`.
- Ngoài scope: skill (28), formation (29), battle (30), summon (33), upgrade (35/39), art thật + atlas/pool (52).

## Verify

- `dotnet test`: Domain 88 / Application 51 / Contracts 36 / Infrastructure 44 (Testcontainers pg16) / Api 56
  (Testcontainers). Data-driven: `GetHeroDefinition` atk 220→999 (KHÔNG sửa code). Ownership từ token.
- codegen tool 41; config-validator 45 + `run.sh` exit 0 (6 file, hero→skill integrity).
- Godot 4.7.1 `--headless --import` exit 0; gdUnit4 **113/113 pass, 0 orphan**.
- `has-pending-model-changes` sạch; no openapi/generated drift ngoài additive hero.
- **CI-pending:** `ci-server.yml`/`ci-client.yml`/`codegen-check.yml`/`validate-config.yml` trên Actions.

## Liên kết

- CLAUDE.md §4.6 (Hero System block) · [[0017-profile-persistence-standardized]] (save root mở rộng) ·
  [[0019-config-service-standardized]] (IConfigProvider) · [[0006-codegen-pipeline-standardized]] (codegen) ·
  [[0020-client-config-bundle-e2e-standardized]] (ConfigProvider client).
- Docs: `docs/gameplay/hero-system.md` §7, `docs/backend/domain-and-application.md`, `docs/backend/infrastructure.md`
  §1.3, `docs/backend/api-and-versioning.md`, `docs/godot/resources-and-assets.md` §2.1,
  `docs/godot/scene-architecture.md` §4.1, `docs/gameplay/configuration-and-data.md` §2b.
