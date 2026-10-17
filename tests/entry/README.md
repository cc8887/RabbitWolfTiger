# Configured entry acceptance

After building/importing, first smoke-test the ordinary project launch with no scene argument:

    godot --headless --path . --quit-after 10

Require `[GALLERY] Ready: 8 portraits`, and reject `[LEGACY] Movement sandbox ready`.
Then run the configured-entry/navigation fixture:

    godot --headless --path . tests/entry/default_entry_integration.tscn

The fixture reads `ProjectSettings application/run/main_scene` and asks SceneTree to load it.
It does not instantiate a hardcoded substitute gallery. It checks eight traveler portraits,
loaded textures, interactive expression controls, F3 staying home, rules/home round trips
via buttons and F2, and explicit Legacy/F4 routes returning via Home/F3.
Its success marker is `PASS: ... configured-entry/navigation checks` with exit 0.
This is real Button.Pressed signaling and Input.ParseInputEvent dispatch, not physical input.

For an actual image of the configured default entry, before test navigation:

    godot --display-driver x11 --rendering-method gl_compatibility --path . tests/entry/default_entry_integration.tscn -- --capture-entry=/absolute/default-entry.png

Require `ENTRY_CAPTURE:` and a nonempty 1280x720 PNG. Headless/dummy capture is refused.
Headless state checks alone do not satisfy final visual acceptance; inspect the real PNG.
