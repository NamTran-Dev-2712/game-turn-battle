# Template: Client Test (gdUnit4)

Guide for a client test. See `docs/testing/godot-testing.md`. Place under `client/tests/` mirroring
the source path.

```gdscript
extends GdUnitTestSuite

func test_action_emits_request() -> void:
	# Arrange
	var controller := FeatureController.new()
	add_child(controller)

	# Act / Assert — assert observable behavior (emitted signals, state), not private members.
	# await assert_signal(controller).is_emitted("action_requested")
	controller.free()
```

Combat sim tests use the shared **golden-vector** fixtures and must match the server bit-for-bit;
keep them deterministic (seeded RNG, injected clock). Treat golden vectors as a living spec.
