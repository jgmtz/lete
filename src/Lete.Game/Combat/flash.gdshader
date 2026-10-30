// Sprite de batalha: textura lisa (linear + mipmaps) ou pixelizada (nearest), tinta, piscada branca, fade
// perda de cor da Marca (só no protagonista) e desintegração (some pixel a pixel de cima para baixo, com uma borda clara onde está se desfazendo).
shader_type spatial;
render_mode blend_mix, cull_disabled, depth_prepass_alpha;

uniform sampler2D tex_nearest : source_color, filter_nearest;
uniform sampler2D tex_linear : source_color, filter_linear_mipmap;
uniform bool smooth_filter = false;
uniform vec4 tint : source_color = vec4(1.0);
uniform float desaturation : hint_range(0.0, 1.0) = 0.0;
uniform float flash : hint_range(0.0, 1.0) = 0.0;
uniform float fade : hint_range(0.0, 1.0) = 1.0;
uniform float dissolve : hint_range(0.0, 1.0) = 0.0;
uniform vec4 dissolve_edge : source_color = vec4(0.85, 0.8, 0.69, 1.0);
uniform float dissolve_edge_width = 0.06;

float hash(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

void fragment() {
	vec4 color = smooth_filter ? texture(tex_linear, UV) : texture(tex_nearest, UV);
	color *= tint;
	color.rgb = mix(color.rgb, vec3(dot(color.rgb, vec3(0.299, 0.587, 0.114))), desaturation);
	ALBEDO = mix(color.rgb, vec3(1.0), flash);
	EMISSION = vec3(flash);
	ALPHA = color.a * fade;

	if (dissolve > 0.0) {
		// Um valor por pixel da arte, para o pó sair em pixels inteiros.
		vec2 size = vec2(smooth_filter ? textureSize(tex_linear, 0) : textureSize(tex_nearest, 0));
		float key = 0.75 * UV.y + 0.25 * hash(floor(UV * size));
		float front = dissolve * (1.0 + dissolve_edge_width);
		if (key < front - dissolve_edge_width) {
			discard;
		}
		if (key < front) {
			ALBEDO = dissolve_edge.rgb;
			EMISSION = dissolve_edge.rgb * 0.6;
		}
	}
}
