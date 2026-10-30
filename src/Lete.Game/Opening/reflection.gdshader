// Abertura: o reflexo do rosto no rio. Com "black" = 1 é uma mancha negra sem traços (a silhueta do busto, com um
// halo pálido para se destacar da água). Na escolha, as cores aparecem, mas turvas: a água passa por cima ("murk"),
// com os brilhos da correnteza, e ondula mais quando o jogador troca de personagem ("stir"). Ao confirmar, clareia.
shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix, depth_draw_never;

uniform sampler2D bust : source_color, filter_linear;
uniform float black : hint_range(0.0, 1.0) = 1.0;
uniform float blur : hint_range(0.0, 0.08) = 0.04;
uniform float darken : hint_range(0.0, 1.0) = 0.25;
uniform float ripple = 0.018;
uniform float ripple_speed = 1.6;
uniform float stir = 0.0;
uniform float murk : hint_range(0.0, 1.0) = 0.45;
uniform float alpha : hint_range(0.0, 1.0) = 1.0;
// Mesma água do rio (water.gdshader): fundo escuro e brilhos que correm.
uniform vec4 water_deep : source_color = vec4(0.03, 0.05, 0.07, 1.0);
uniform vec4 water_shine : source_color = vec4(0.22, 0.30, 0.36, 1.0);
uniform float flow_speed = 0.12;
uniform float wave_scale = 3.0;
// A mancha negra precisa se destacar da água escura: um halo pálido em volta da silhueta.
uniform vec4 halo : source_color = vec4(0.32, 0.40, 0.46, 1.0);
uniform float halo_spread = 2.5;
// O halo também fica na escolha: o corpo da sombra é escuro como a água e sumiria sem ele.
uniform float halo_keep : hint_range(0.0, 1.0) = 0.0;
// Bordas que se dissolvem na água: lados e alto (fração da imagem) e o corte dos ombros embaixo, mais longo.
uniform float edge_fade = 0.12;
uniform float bottom_fade = 0.35;
// A cor do personagem vista pela água: apagada (menos saturação) e puxada para o azul do rio.
uniform float desaturate : hint_range(0.0, 1.0) = 0.55;
uniform vec4 water_tint : source_color = vec4(0.45, 0.62, 0.80, 1.0);
uniform float tint : hint_range(0.0, 1.0) = 0.7;

float wave(vec2 p) {
	return sin(p.x * 1.7 + sin(p.y * 0.9)) * 0.5 + sin(p.y * 2.3 - p.x * 0.6) * 0.5;
}

vec4 sample_blurred(vec2 uv) {
	// Desfoque em caixa simples (9 amostras), suficiente para apagar os traços.
	vec4 sum = vec4(0.0);
	for (int x = -1; x <= 1; x++) {
		for (int y = -1; y <= 1; y++) {
			sum += texture(bust, uv + vec2(float(x), float(y)) * blur);
		}
	}
	return sum / 9.0;
}

void fragment() {
	// Reflexo de quem se olha na água: espelhado, com a cabeça para o lado de longe; ondulando com a correnteza.
	float amp = ripple * (1.0 + stir);
	vec2 uv = vec2(1.0 - UV.x, UV.y);
	uv.x += sin(uv.y * 40.0 + TIME * ripple_speed) * amp;
	uv.y += sin(uv.x * 25.0 + TIME * ripple_speed * 0.7) * amp * 0.5;
	vec4 c = sample_blurred(uv);
	vec3 body = c.rgb;
	float gray = dot(body, vec3(0.299, 0.587, 0.114));
	body = mix(body, vec3(gray), desaturate);
	body = mix(body, body * water_tint.rgb, tint);
	vec3 rgb = mix(body * (1.0 - darken), vec3(0.0), black);

	// Água por cima: a cor do fundo do rio e os brilhos, na mesma correnteza do rio.
	vec2 p = UV * wave_scale * 10.0;
	p.y += TIME * flow_speed * 10.0;
	float w = wave(p) * 0.5 + 0.5;
	vec3 water = mix(water_deep.rgb, water_shine.rgb, pow(w, 8.0) * 0.6);
	rgb = mix(rgb, water, murk) + water_shine.rgb * pow(w, 12.0) * murk * 0.5;

	// Halo: o contorno borrado mais largo, forte enquanto é mancha negra e mais fraco na escolha ("halo_keep").
	float wide = 0.0;
	for (int x = -1; x <= 1; x++) {
		for (int y = -1; y <= 1; y++) {
			wide += texture(bust, uv + vec2(float(x), float(y)) * blur * halo_spread).a;
		}
	}
	wide /= 9.0;
	float ring = clamp(wide - c.a, 0.0, 1.0) * max(black, halo_keep);
	ALBEDO = mix(rgb, halo.rgb, ring);
	// Nas bordas o reflexo se dissolve na água, sem recorte duro (o corte dos ombros some aos poucos).
	float fade = smoothstep(0.0, edge_fade, UV.x) * smoothstep(0.0, edge_fade, 1.0 - UV.x)
		* smoothstep(0.0, edge_fade, UV.y) * smoothstep(0.0, bottom_fade, 1.0 - UV.y);
	ALPHA = max(c.a, ring) * alpha * fade;
}
