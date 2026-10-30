// Sprite de mapa do protagonista com a Marca: o mesmo que o material padrão do AnimatedSprite3D (billboard,
// iluminado, corte de alfa, nearest) mais a perda de cor por faixa. Só entra quando há perda de cor.
shader_type spatial;
render_mode cull_disabled;

uniform sampler2D sprite_texture : source_color, filter_nearest;
uniform float desaturation : hint_range(0.0, 1.0) = 0.0;

void vertex() {
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(INV_VIEW_MATRIX[0], INV_VIEW_MATRIX[1], INV_VIEW_MATRIX[2], MODEL_MATRIX[3]);
	MODELVIEW_NORMAL_MATRIX = mat3(MODELVIEW_MATRIX);
}

void fragment() {
	vec4 color = texture(sprite_texture, UV);
	ALBEDO = mix(color.rgb, vec3(dot(color.rgb, vec3(0.299, 0.587, 0.114))), desaturation);
	ALPHA = color.a;
	ALPHA_SCISSOR_THRESHOLD = 0.5;
}
