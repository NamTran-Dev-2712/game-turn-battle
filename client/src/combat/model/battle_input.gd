class_name BattleInput
## Input **thuần** (không I/O) của sim (§9): mọi thứ cần để tái hiện trận bit-for-bit với server —
## `config_version`, `seed` (uint64), stage, hai đội, luật combat, skill cơ bản. Xây từ ConfigProvider
## qua [CombatInputResolver] (đường thật) hoặc từ golden vector (test).
extends RefCounted

var config_version: String = ""
var seed: int = 0
var stage: StageInfo = null
var ally: Array[UnitSnapshot] = []
var enemy: Array[UnitSnapshot] = []
var rules: CombatRules = null
var basic_skill: SkillDef = null
