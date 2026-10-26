// Abertura: água escura de um rio, correndo devagar. Sem reflexo real: o reflexo do rosto é um quad à parte.
shader_type spatial;
render_mode cull_disabled;

uniform vec4 deep : source_color = vec4(0.03, 0.05, 0.07, 1.0);
uniform vec4 shine : source_color = vec4(0.22, 0.30, 0.36, 1.0);
uniform float flow_speed = 0.12;
uniform float wave_scale = 3.0;

float wave(vec2 p) {
	return sin(p.x * 1.7 + sin(p.y * 0.9)) * 0.5 + sin(p.y * 2.3 - p.x * 0.6) * 0.5;
}

void fragment() {
	vec2 p = UV * wave_scale * 10.0;
	p.y += TIME * flow_speed * 10.0;
	float w = wave(p) * 0.5 + 0.5;
	float glints = pow(w, 8.0);
	ALBEDO = mix(deep.rgb, shine.rgb, glints * 0.6);
	ROUGHNESS = 0.15;
	METALLIC = 0.0;
	SPECULAR = 0.6;
}
