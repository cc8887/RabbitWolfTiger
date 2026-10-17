shader_type canvas_item;
// Both true source layers use the same eye-space aperture. No fabricated eye pixels.
uniform sampler2D sclera_mask : filter_linear_mipmap, repeat_disable;
uniform bool pupil_layer = false;
uniform vec2 relative_size = vec2(1.0);
uniform vec2 gaze_uv = vec2(0.0);
uniform float openness : hint_range(0.0, 1.0) = 1.0;
uniform float slant : hint_range(-0.5, 0.5) = 0.0;
void fragment() {
    vec2 eye_uv = pupil_layer ? (UV - vec2(0.5)) * relative_size + vec2(0.5) + gaze_uv : UV;
    // Incoming COLOR already includes texture, vertex color and inherited modulate.
    vec4 source = COLOR;
    float upper = 0.5 - openness * 0.5 + slant * openness * (eye_uv.x - 0.5);
    float lower = 0.5 + openness * 0.5;
    float aperture = openness <= 0.001 ? 0.0 : smoothstep(upper, upper + 0.025, eye_uv.y) * (1.0 - smoothstep(lower - 0.025, lower, eye_uv.y));
    float mask = texture(sclera_mask, eye_uv).a;
    float bounds = step(0.0, eye_uv.x) * step(eye_uv.x, 1.0) * step(0.0, eye_uv.y) * step(eye_uv.y, 1.0);
    // Threshold only the clipping mask to keep source background alpha noise from leaking pupils.
    source.a *= aperture * bounds * (pupil_layer ? smoothstep(0.08, 0.65, mask) : 1.0);
    COLOR = source;
}
