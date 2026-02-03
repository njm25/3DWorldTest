shader_type spatial;

uniform sampler2D grass_texture;
uniform sampler2D sand_texture;

uniform float height_threshold : hint_range(0.0, 10.0) = 0.1;
uniform float texture_scale = 10.0; // controls zoom

varying vec3 world_pos;

void vertex() {
	world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
	vec2 uv = UV * texture_scale;

	vec4 grass_color = texture(grass_texture, uv);
	vec4 sand_color  = texture(sand_texture, uv);

	float blend_factor = smoothstep(height_threshold - 0.5, height_threshold + 0.5, world_pos.y);

	ALBEDO = mix(sand_color.rgb, grass_color.rgb, blend_factor);
}
