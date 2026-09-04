class_name CombatInputResolver
## Lớp **data-driven** (adapter, không phải lõi sim thuần) dựng [BattleInput] từ `ConfigProvider`
## (phase 16/22) — song ánh server `GameTeam.Application/Combat/CombatInputResolver.cs`. Đọc
## hero/skill/stage theo type/id (KHÔNG hardcode số cân bằng; ADR-004); `combat_rules` lấy từ **stage
## config** đúng như server. `config_version` khớp version ConfigProvider đang kích hoạt.
##
## Ranh giới: chỉ chạm `ConfigProvider` (config, display-only) — KHÔNG UI/network/scene. Lõi sim
## ([BattleSimulator]) vẫn thuần, nhận [BattleInput] đã dựng.
extends RefCounted

const HERO_TYPE: StringName = &"hero"
const SKILL_TYPE: StringName = &"skill"
const STAGE_TYPE: StringName = &"stage"
const TEAM_ALLY: String = "ally"
const TEAM_ENEMY: String = "enemy"


## Dựng BattleInput từ một request + ConfigProvider.
## `request` = { seed:int, stage_id:String, ally:Array[{ actor_id, hero_id, slot }] }.
## `config_provider` = autoload `ConfigProvider` (hoặc giả trong test) — cần get_entry/current_version.
func resolve(request: Dictionary, config_provider: Node) -> BattleInput:
	var stage_id := str(request.get("stage_id", ""))
	var stage: Dictionary = config_provider.get_entry(STAGE_TYPE, stage_id)
	assert(not stage.is_empty(), "COMBAT_STAGE_CONFIG_NOT_FOUND: %s" % stage_id)

	var max_rounds := int(stage.get("max_rounds", 0))
	var basic_skill_id := str(stage.get("basic_skill_id", "skill_basic"))
	var skill: Dictionary = config_provider.get_entry(SKILL_TYPE, basic_skill_id)
	assert(not skill.is_empty(), "COMBAT_SKILL_CONFIG_NOT_FOUND: %s" % basic_skill_id)

	var rules := CombatRules.from_dict(stage.get("combat_rules", {}), max_rounds)

	var ally := _build_ally(request.get("ally", []), config_provider)
	var enemy := _build_enemies(stage.get("enemies", []), config_provider)

	var basic_skill := SkillDef.make(
		basic_skill_id,
		int(skill.get("coeff_fixed", 0)),
		str(skill.get("target_rule", "default")),
		_build_effects(skill))

	var input := BattleInput.new()
	input.config_version = "config@v%d" % config_provider.current_version()
	input.seed = int(request.get("seed", 0))
	input.stage = StageInfo.make(stage_id, max_rounds)
	input.ally = ally
	input.enemy = enemy
	input.rules = rules
	input.basic_skill = basic_skill
	return input


func _build_ally(members: Array, config_provider: Node) -> Array[UnitSnapshot]:
	var units: Array[UnitSnapshot] = []
	for member in members:
		var m := member as Dictionary
		units.append(_build_unit(
			str(m.get("actor_id", "")), str(m.get("hero_id", "")), TEAM_ALLY, int(m.get("slot", 0)), config_provider))
	return units


func _build_enemies(enemies: Array, config_provider: Node) -> Array[UnitSnapshot]:
	var units: Array[UnitSnapshot] = []
	for enemy in enemies:
		var e := enemy as Dictionary
		units.append(_build_unit(
			str(e.get("actor_id", "")), str(e.get("hero_id", "")), TEAM_ENEMY, int(e.get("slot", 0)), config_provider))
	return units


func _build_unit(actor_id: String, hero_id: String, team: String, slot: int, config_provider: Node) -> UnitSnapshot:
	var hero: Dictionary = config_provider.get_entry(HERO_TYPE, hero_id)
	assert(not hero.is_empty(), "COMBAT_HERO_CONFIG_NOT_FOUND: %s" % hero_id)
	# Chấp nhận cả hai hình dạng chỉ số: lồng `base_stats` (schema hero phase 16) hoặc phẳng (combat).
	var stats_src: Dictionary = hero.get("base_stats", hero)
	var u := UnitSnapshot.new()
	u.actor_id = actor_id
	u.hero_id = hero_id
	u.team = team
	u.slot = slot
	u.stats = UnitStats.from_dict(stats_src)
	return u


# effects của skill: list rỗng ⇒ mặc định một `damage` (khớp server). Mỗi phần tử là String
# (effect_type) hoặc Dictionary { effect_type, params }.
func _build_effects(skill: Dictionary) -> Array[EffectDef]:
	var raw: Array = skill.get("effects", [])
	var effects: Array[EffectDef] = []
	if raw.is_empty():
		effects.append(EffectDef.make(DamageEffectHandler.TYPE_NAME))
		return effects
	for t in raw:
		if t is String:
			effects.append(EffectDef.make(t))
		elif t is Dictionary:
			var d := t as Dictionary
			effects.append(EffectDef.make(str(d.get("effect_type", "")), d.get("params", {})))
	return effects
