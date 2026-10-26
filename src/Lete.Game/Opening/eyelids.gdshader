// Abertura: a visão de quem acorda. Duas pálpebras (cima e baixo) fecham conforme "open", a tela fica turva
// (desfoque pela textura da tela com mipmaps) e escurece nas bordas (vinheta).
shader_type canvas_item;

uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;
uniform float open : hint_range(0.0, 1.0) = 1.0;
uniform float blur : hint_range(0.0, 6.0) = 0.0;
uniform float vignette : hint_range(0.0, 1.0) = 0.5;
uniform vec4 lid_color : source_color = vec4(0.02, 0.02, 0.03, 1.0);
// Borda suave da pálpebra (em fração da tela).
uniform float lid_softness = 0.08;
// Com "open" = 1 as pálpebras saem de toda a tela (a curva chega a ~1,35 nos cantos).
uniform float fully_open = 1.6;

void fragment() {
	vec3 scene = textureLod(screen_tex, SCREEN_UV, blur).rgb;

	// Pálpebras: a abertura vai do centro para fora; curvas (mais fechadas nos cantos), como um olho.
	float center = abs(SCREEN_UV.y - 0.5) * 2.0;
	float curve = center + pow(abs(SCREEN_UV.x - 0.5) * 2.0, 2.0) * 0.35;
	float o = open * fully_open;
	float lid = smoothstep(o - lid_softness, o + lid_softness, curve);

	float edge = distance(SCREEN_UV, vec2(0.5)) * 1.4;
	float dark = clamp(edge * edge * vignette, 0.0, 1.0);

	vec3 color = mix(scene * (1.0 - dark), lid_color.rgb, lid);
	COLOR = vec4(color, 1.0);
}
