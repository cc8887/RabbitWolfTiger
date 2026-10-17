shader_type canvas_item;
uniform vec2 opening_center = vec2(0.5, 0.77);
uniform vec2 opening_radius = vec2(0.295, 0.285);
uniform bool keep_inside = false;
void fragment() {
    vec4 source = COLOR;
    float radius = length((UV - opening_center) / opening_radius);
    float inside = 1.0 - smoothstep(0.98, 1.02, radius);
    source.a *= keep_inside ? inside : (1.0 - inside);
    // Original hood contains a baked peach patch. Mask it in the shell only;
    // the separate face layer supplies the current gameplay face color.
    vec3 raw = texture(TEXTURE, UV).rgb;
    float baked_skin = step(0.5, raw.r) * step(raw.g * 1.1, raw.r) * step(raw.b * 1.05, raw.g);
    if (!keep_inside) source.a *= 1.0 - baked_skin;
    COLOR = source;
}
