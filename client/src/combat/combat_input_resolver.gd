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

	var basic_skill := _build_skill(basic_skill_id, skill)

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


# Địch bám stage config gameplay (chỉ hero_id + slot?): định danh actor_id suy ra "enemy_{i}" (khớp server
# CombatInputResolver.cs — tất định), slot lấy từ config hoặc chỉ số thứ tự.
func _build_enemies(enemies: Array, config_provider: Node) -> Array[UnitSnapshot]:
	var units: Array[UnitSnapshot] = []
	for i in enemies.size():
		var e := enemies[i] as Dictionary
		units.append(_build_unit(
			"enemy_%d" % i, str(e.get("hero_id", "")), TEAM_ENEMY, int(e.get("slot", i)), config_provider))
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
	u.skills = _build_unit_skills(hero, config_provider)
	return u


# Bộ skill riêng của unit (§23) — ánh xạ từ gameplay `hero.skills[]` (phase 30, khớp server): resolve tất cả
# skill; ULTIMATE = skill đầu tiên tốn năng lượng (trigger.type=energy ⇒ energy_cost>0); BASIC = skill đầu tiên
# không tốn năng lượng (fallback: skill đầu). Không skill ⇒ null (dùng basic dùng chung của màn).
func _build_unit_skills(hero: Dictionary, config_provider: Node) -> UnitSkillSet:
	var skill_ids: Array = hero.get("skills", [])
	if skill_ids.is_empty():
		return null
	var resolved: Array[SkillDef] = []
	for sid in skill_ids:
		resolved.append(_resolve_skill(str(sid), config_provider))
	var basic: SkillDef = null
	var ultimate: SkillDef = null
	for s in resolved:
		if basic == null and s.energy_cost == 0:
			basic = s
		if ultimate == null and s.energy_cost > 0:
			ultimate = s
	if basic == null:
		basic = resolved[0]
	return UnitSkillSet.make(basic, ultimate)


func _resolve_skill(skill_id: String, config_provider: Node) -> SkillDef:
	var skill: Dictionary = config_provider.get_entry(SKILL_TYPE, skill_id)
	assert(not skill.is_empty(), "COMBAT_SKILL_CONFIG_NOT_FOUND: %s" % skill_id)
	return _build_skill(skill_id, skill)


# Dựng SkillDef từ gameplay skill config (khớp server): target→target_rule; trigger.type=energy ⇒
# energy_cost=trigger.value; cooldown→cooldown_rounds; NÂNG coeff của effect `damage` (params.coeff_fixed) lên
# cấp skill vì sim tiêu thụ coeff ở cấp skill (§17).
func _build_skill(skill_id: String, skill: Dictionary) -> SkillDef:
	var coeff := 0
	var raw: Array = skill.get("effects", [])
	for e in raw:
		if e is Dictionary and str((e as Dictionary).get("effect_type", "")) == DamageEffectHandler.TYPE_NAME:
			var params: Dictionary = (e as Dictionary).get("params", {})
			if params.has("coeff_fixed"):
				coeff = int(params["coeff_fixed"])
				break
	var trigger: Dictionary = skill.get("trigger", {})
	var energy_cost := 0
	if str(trigger.get("type", "")) == "energy":
		energy_cost = int(trigger.get("value", 0))
	return SkillDef.make(
		skill_id,
		coeff,
		str(skill.get("target", "single_enemy")),
		_build_effects(skill),
		energy_cost,
		int(skill.get("cooldown", 0)))


# effects của skill: list rỗng ⇒ mặc định một `damage` (khớp server). Mỗi phần tử là String
# (effect_type) hoặc Dictionary { effect_type, target?, params }.
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
			effects.append(EffectDef.make(
				str(d.get("effect_type", "")), d.get("params", {}), str(d.get("target", ""))))
	return effects
