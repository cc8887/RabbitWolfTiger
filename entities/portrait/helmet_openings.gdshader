shader_type canvas_item;
// Reusable runtime cutouts of the ORIGINAL helmet PNG's two baked dark eye windows.
// Coordinates are measured in source pixels (80 x 87); no replacement helmet is drawn.
uniform vec2 left_center = vec2(27.0, 74.25);
uniform vec2 right_center = vec2(55.5, 74.25);
uniform vec2 half_size = vec2(9.75, 8.5);
float window_mask(vec2 pixel, vec2 center) {
    vec2 d = abs(pixel - center) - half_size;
    float edge = max(d.x, d.y);
    return 1.0 - smoothstep(-0.35, 0.35, edge);
}
void fragment() {
    vec4 source = COLOR;
    vec2 pixel = UV * vec2(80.0, 87.0);
    float openings = max(window_mask(pixel, left_center), window_mask(pixel, right_center));
    source.a *= 1.0 - openings;
    COLOR = source;
}
