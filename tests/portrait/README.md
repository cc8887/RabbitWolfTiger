# Portrait integration validation

Run after a normal Godot C# build and resource import:

    godot --headless --path . tests/portrait/portrait_integration.tscn

The success marker is `PASS: ... portrait integration checks` with exit code 0.
Tests instantiate all eight traveler variants, exercise all five expressions,
validate independent eye materials and clipping parameters, extreme gaze, blink,
pause/resume, rapid expression replacement, part swaps, stable node/texture/material
identities, unchanged diet/predation state, and real gallery buttons/layout.
Resource identity checks plus source review cover reuse; this is not a managed-heap
allocation benchmark. Headless validates state and parameters, not rendered pixels.

Real OpenGL capture on an available X11 display:

    godot --display-driver x11 --rendering-method gl_compatibility --path . tests/portrait/portrait_integration.tscn -- --capture=/absolute/gallery.png --frames-dir=/absolute/portrait-frames

Headless/dummy capture is deliberately refused. The fixture uses the actual gallery,
requires a nonempty 1280×720 viewport image, and emits `PORTRAIT_CAPTURE:` and
`PORTRAIT_FRAMES:` markers. It writes 60 numbered PNG frames plus five expression
snapshots and sequence.json. The sequence is a controlled 12fps, five-second facial
animation sample: automatic `_Process` is disabled, and each output frame advances
the animator twice by 1/24 second. It includes all five expressions, gaze, and explicit
blinks. This is a deterministic animation demonstration, not a real-time input recording.
No GIF, simulated screenshot, or rendered-pixel claim is produced by headless tests.
