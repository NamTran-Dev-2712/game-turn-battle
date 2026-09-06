# `shared/combat-vectors/` — Golden vector combat (định dạng + mẫu)

> **Hợp đồng kiểm** cho combat deterministic (ADR-011). Một golden vector là cặp **input → output kỳ vọng**: cùng
> `(config_version, team_snapshot, stage, seed)` thì **cả hai hiện thực** (server .NET phase 24, client GDScript phase 25)
> phải sinh **cùng `event_log` + cùng `result`, từng bit**. Đây là **định dạng chuẩn**; canon cơ chế nằm ở
> [`../../docs/gameplay/combat-framework.md`](../../docs/gameplay/combat-framework.md) (§9–§20) + [`ADR-011`](../../docs/adr/ADR-011-combat-authority-and-determinism.md).
>
> **Lịch sử:** định dạng + vector mẫu = Phase 23; bộ 9 vector + CI gate cross-impl = Phase 26; **vector skill (heal/buff/
> debuff/ultimate/multi-effect) + định dạng skill riêng của unit = Phase 28** (§23). Không code sim ở đây.

## Nội dung

| File | Mô tả |
|---|---|
| `vector_01_basic_hit.json` | 1v1 full-auto, **luôn trúng** (`accuracy_bp=10000`) + **không bao giờ crit** (`crit_rate_bp=0`). Kiểm: turn-order theo `spd`, damage fixed-point (divisive DEF-ratio), stream RNG, điều kiện VICTORY. Kết quả **kiểm được bằng tay**. |
| `vector_02_crit_ko.json` | 1v1 full-auto, **luôn trúng + luôn crit** (`crit_rate_bp=10000`, `crit x1.5`). Kiểm: nhân hệ số crit **sau** mitigation, KO trong 2 round, stream RNG với seed khác. |

## Định dạng (schema)

Mỗi vector là **một object JSON UTF-8**. Số **integer thô** (đơn vị `combat_int`); **không `float`** ở bất kỳ trường nào.
Khoá `snake_case`. Thứ tự phần tử trong `event_log` là **có nghĩa** (canonical) và được neo bằng `seq`.

### Top-level

| Trường | Kiểu | Bắt buộc | Ý nghĩa |
|---|---|---|---|
| `format_version` | int | ✔ | Phiên bản định dạng vector (hiện `1`). Đổi cấu trúc ⇒ tăng. |
| `name` | string | ✔ | Định danh vector (khớp tên file, không đuôi). |
| `description` | string | ✔ | Mục đích + điều kiện đang kiểm. |
| `input` | object | ✔ | Đầu vào sim (xem dưới). |
| `expected` | object | ✔ | Kết quả kỳ vọng (`event_log` + `result`). |

### `input`

| Trường | Kiểu | Bắt buộc | Ý nghĩa |
|---|---|---|---|
| `config_version` | string | ✔ | Nhãn `config@vN` (ADR-005). Ở production, số liệu balance được resolve từ bundle theo version này. |
| `seed` | int (uint64) | ✔ | Seed trận, **server sinh** (ADR-011); input tường minh của PCG (§11). |
| `stage` | object | ✔ | `{ id: string, max_rounds: int }`. |
| `team_snapshot` | object | ✔ | `{ ally: Unit[], enemy: Unit[] }` (§9). |
| `config_excerpt` | object | ✔ (ở mẫu) | **Lát cắt balance** cần để chạy vector **tự chứa** (không cần Config Service). Ở production các giá trị này đến từ `config_version`; ở đây nhúng để 24–25 kiểm sơ bộ. |

**Skill riêng của unit (§23, tuỳ chọn — Phase 28):** một `Unit` có thể mang `"skills": { "basic": "<skill_id>", "ultimate": "<skill_id>"? }`
tham chiếu vào `config_excerpt.skills` (bảng skill). **Vắng ⇒ dùng `skill_basic` dùng chung** (đòn thường 1 effect `damage`) —
tương thích ngược 9 vector Phase 26. Mỗi entry trong `config_excerpt.skills` (lát cắt combat):

```jsonc
"skills": {
  "skill_hero_ult": {
    "coeff_fixed": 2500,          // hệ số damage (nếu có effect damage)
    "target_rule": "single_enemy", // single_enemy(mặc định) | single_ally | self
    "energy_cost": 100,            // §15: năng lượng cần để cast (ultimate); 0 = đòn thường
    "cooldown_rounds": 3,          // §15: hồi chiêu sau cast; 0 = không
    "effects": [ { "effect_type": "damage" } ]   // effect-data: { effect_type, target?, params? }
  }
}
```

**`Unit`** (một phần tử trong `ally`/`enemy`):

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `actor_id` | string | **Định danh ổn định, duy nhất toàn trận** — khoá tie-break cuối cùng (§13). |
| `hero_id` | string | id hero trong config (tham chiếu, không dùng cho tie-break). |
| `team` | string | `"ally"` \| `"enemy"`. |
| `slot` | int | Chỉ số vị trí formation `0..5` (§7/§14). |
| `stats` | object | `{ hp, atk, def, spd }` — `combat_int` (integer ≥ 0). |

**`config_excerpt`** (ở vector mẫu) — mọi hệ số fixed-point lưu ở **đơn vị FIXED_SCALE** (`1.0→1000`, `1.5→1500`),
tỉ lệ lưu **basis points** (`[0..10000]`), còn lại integer:

```jsonc
{
  "skill_basic": { "coeff_fixed": 1000 },          // hệ số damage skill thường (1.0)
  "combat_rules": {
    "def_constant_k": 300,                          // K trong K/(K+def)
    "min_damage": 1,                                // sàn damage
    "crit_multiplier_fixed": 1500,                  // x1.5 khi crit
    "accuracy_bp": 10000,                           // 100% trúng
    "crit_rate_bp": 0,                              // 0% crit
    "max_rounds": 30,
    "energy": { "initial": 0, "on_attack": 0, "on_hit": 0, "ultimate_cost": 100, "max": 100 }
  }
}
```

### `expected.event_log` — mảng sự kiện

- **`seq`**: int tăng dần từ `0`, liên tục — neo thứ tự tuyệt đối (bắt drift ordering).
- **`type`**: tên sự kiện. Thứ tự canonical trong một action gây damage (§16/§18):
  `RoundStarted → ActionStarted → TargetSelected → RandomRoll(hit) → (Miss | Hit → RandomRoll(crit) → [Crit] →
  DamageApplied → [Death]) → [EnergyChanged…] → ActionCompleted → … → RoundEnded → … → BattleEnded`.

| `type` | Trường kèm | Ghi chú |
|---|---|---|
| `RoundStarted` / `RoundEnded` | `round:int` | Mốc round. |
| `ActionStarted` / `ActionCompleted` | `actor:actor_id` | Bao một action. |
| `TargetSelected` | `actor`, `target` | Mục tiêu đã resolve (§14). |
| `RandomRoll` | `purpose:"hit"\|"crit"`, `bound:int`, `value:int` | **Giá trị roll [0,bound)** — bắt drift RNG. Thứ tự tiêu thụ cố định (§16). |
| `Miss` / `Hit` | `actor`, `target` | Kết quả roll hit vs `accuracy_bp`. |
| `Crit` | `actor`, `target` | Chỉ phát khi crit (roll < `crit_rate_bp`). |
| `DamageApplied` | `actor`, `target`, `amount:int`, `target_hp_after:int`, `crit:bool` | Damage integer cuối (§17). |
| `Death` | `unit:actor_id` | Ngay sau `DamageApplied` khiến `hp==0`, trước `ActionCompleted`. |
| `EnergyChanged` | `unit`, `energy_after:int` | §15: nạp on_attack (người đánh)/on_hit (người trúng còn sống) hoặc spend khi cast ultimate. **Chỉ phát khi giá trị đổi** (gain=0 ⇒ không phát). |
| `Healed` | `unit`, `amount:int`, `target_hp_after:int` | §23: hồi máu (`amount` = lượng thực sau kẹp MaxHp). |
| `BuffApplied` | `unit`, `source`, `stat:"atk"\|"def"\|"spd"`, `amount:int` (có dấu), `duration:int` | §23: áp modifier chỉ số (buff dương / debuff âm). |
| `BuffExpired` | `unit`, `source`, `stat` | §23: modifier hết hạn, phát tại `RoundStarted` theo thứ tự (stat: atk,def,spd; rồi source). |
| `BattleEnded` | — | Sự kiện cuối, trước `result`. |

### `expected.result`

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `outcome` | string | `"VICTORY"` \| `"DEFEAT"` \| `"DRAW"` (góc nhìn đội `ally`, §19). |
| `winner_team` | string\|null | `"ally"` \| `"enemy"` \| `null` (draw). |
| `rounds` | int | Số round đã chạy. |
| `final_hp` | object | `{ actor_id: int }` HP cuối mỗi unit. |

## Quy tắc ổn định (serialization)

- Chỉ **integer** cho số (không float); chuỗi `actor_id`/`type` cố định; so sánh `actor_id` theo **byte/UTF-8** (không locale).
- `event_log` **có thứ tự** (neo bằng `seq`); so khớp golden = so **toàn bộ chuỗi + từng trường**, không chỉ `result`.
- Vector **bất biến theo mục đích**: đổi cơ chế sim ⇒ **cập nhật vector có chủ đích** trong cùng thay đổi + giải thích WHY
  (agent [`combat-determinism`](../../.claude/agents/combat-determinism.md)). Không sửa vector để "cho CI xanh".

## Kiểm tham chiếu (Phase 23)

Hai vector mẫu được sinh bởi **reference calculator** bám đúng pseudo-code spec (SplitMix64→PCG32, `pcg_bounded`,
fixed-point round-half-up, divisive DEF-ratio). Damage kiểm được bằng tay, ví dụ `vector_01` (ally→enemy):

```
raw   = fixed_mul(to_fixed(200), 1000) = 200000
ratio = fixed_div(to_fixed(300), to_fixed(300)+to_fixed(80)) = fixed_div(300000, 380000) = 789
dmg_f = fixed_mul(200000, 789) = 157800
dmg   = from_fixed(157800) = 158    → enemy hp 500-158 = 342   ✓ (khớp event seq 6)
```

## Bộ vector đa kịch bản (Phase 26 — đã hiện thực)

Baseline (`expected`) của MỌI vector sinh từ **sim server** bằng `tools/combat-baseline`
(`bash tools/combat-baseline/run.sh generate`) — KHÔNG viết tay. Bộ hiện tại (9 vector):

| File | Kịch bản |
|---|---|
| `vector_01_basic_hit` | 1v1 luôn trúng / không crit — VICTORY |
| `vector_02_crit_ko` | 1v1 luôn crit (×1.5) — VICTORY nhanh |
| `vector_03_miss` | accuracy_bp=5000 ⇒ có **Miss** xen kẽ (miss = 1 hit-roll, 0 crit-roll) |
| `vector_04_defeat` | enemy mạnh + nhanh ⇒ **DEFEAT** (winner=enemy) |
| `vector_05_draw` | sát thương tối thiểu + def cao ⇒ **DRAW** ở max_rounds |
| `vector_06_multi_unit` | 2v2 — turn order `(-spd, actor_id)` + **tie-break** + đổi target khi front chết |
| `vector_07_mixed_crit` | crit_rate_bp=5000 ⇒ **crit lẫn thường** trong cùng trận |
| `vector_08_boundary_lethal` | damage **==** HP ⇒ `target_hp_after=0` + Death (biên chết) |
| `vector_09_boundary_survive` | damage **<** HP ⇒ `target_hp_after=1`, KHÔNG Death (biên sống) |
| `vector_10_heal` | Skill **heal** (single_ally): healer hồi máu tank bị đánh ⇒ **Healed** (§28) |
| `vector_11_buff` | Skill **buff** (apply_buff atk, ultimate cooldown-gated) ⇒ **BuffApplied**→sát thương tăng→**BuffExpired** (§28) |
| `vector_12_debuff` | Skill **debuff** (apply_debuff def) ⇒ BuffApplied delta âm→xuyên def→BuffExpired (§28) |
| `vector_13_ultimate_energy` | **Ultimate theo năng lượng** (§15): nạp `on_attack` tới ngưỡng ⇒ cast ultimate (**EnergyChanged** + hồi chiêu) (§28) |
| `vector_14_multi_effect` | Một skill **nhiều effect** `[damage, apply_debuff]` cùng action — thứ tự effect cố định (§28) |

Test hai phía **tự khám phá** mọi `*.json` (server `GoldenVectorTests` `[MemberData]`; client
`CombatVectorLoader.list_vector_files()`) ⇒ thêm vector = KHÔNG sửa code test. Gate CI `golden-vector`
(server: `run.sh check` + golden tests; client: gdUnit4) so cả hai với CÙNG baseline ⇒ **server ≡ client ≡ baseline**.

**Cập nhật baseline CÓ CHỦ ĐÍCH:** xem `tools/combat-baseline/README.md` (đổi công thức → regenerate → review diff →
ghi WHY → doc-sync). Không sửa baseline âm thầm.

## Skill/effect vectors (Phase 28 — đã hiện thực)

`vector_10`..`vector_14` phủ **skill framework** (§23): heal (Healed), buff/debuff (BuffApplied/BuffExpired + modifier chỉ số),
ultimate theo năng lượng (§15: EnergyChanged + cooldown), và skill nhiều effect. Cơ chế energy/ultimate **config-gated, mặc
định TẮT** (gain=0) ⇒ 9 vector cũ **byte-identical**; vector Phase 28 bật energy/buff bằng `config_excerpt.skills` + `team_snapshot[].skills`.
Cơ chế canon ở [`combat-framework.md`](../../docs/gameplay/combat-framework.md) §23 + [`skill-framework.md`](../../docs/gameplay/skill-framework.md).

## Ngoài phạm vi

- `shield` handler + hệ điều kiện tổng quát (generic conditions) = **nợ tài liệu** (Post-MVP); `trigger` (energy/cooldown) là cơ chế điều kiện hiện tại.
- Số liệu balance ultimate/energy (CB4) vẫn `[OPEN]` — cơ chế đã canon, giá trị production do product/tuning.
- Signed/secure vector, nén, delta = Post-MVP.
- Battle endpoint + wiring `config/skills` thật theo `hero.skills[]` = **phase 30**; nội dung skill đầy đủ = phase sau.
