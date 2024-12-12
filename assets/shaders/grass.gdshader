shader_type spatial;
render_mode cull_disabled, unshaded;

uniform float wind_speed = 0.2;
uniform float wind_strength = 2.0;
// How big, in world space, is the noise texture
// wind will tile every wind_texture_tile_size
uniform float wind_texture_tile_size = 20.0;
uniform float wind_vertical_strength = 0.3;
uniform vec2 wind_horizontal_direction = vec2(1.0,0.5);
uniform vec3 scale = vec3(1.0,1.0, 1.0);


uniform sampler2D color_ramp : hint_default_black;
// we need a tiling noise here!
uniform sampler2D wind_noise : hint_default_black;

//uniform vec3 character_position;
//uniform float character_radius = 3.0;
//uniform sampler2D character_distance_falloff_curve : hint_default_black;
//uniform float character_push_strength = 1.0;

varying float debug_wind;

void vertex() {
	mat4 world_mat = MODEL_MATRIX;
	VERTEX *= scale;

	vec3 world_vert = (world_mat * vec4(VERTEX, 1.0)).xyz;

	vec2 normalized_wind_direction = normalize(wind_horizontal_direction);
	vec2 world_uv = world_vert.xz / wind_texture_tile_size + normalized_wind_direction * TIME * wind_speed;
	// we displace only the top part of the mesh
	// note that this means that the mesh needs to have UV in a way that the bottom of UV space
	// is at the top of the mesh
	float displacement_affect = (1.0 - UV.y);
	float wind_noise_intensity =
        (textureLod(wind_noise, world_uv , 0.0).r - 0.5);
       // + sin(world_uv.x + world_uv.y) + 1.0) / 3.0;


	// We convert the direction of the wind into vertex space from world space
	// if we used it directly in vertex space, rotated blades of grass wouldn't behave properly
	vec2 vert_space_horizontal_dir = (inverse(world_mat) * vec4(wind_horizontal_direction, 0.0,0.0)).xy;
	vert_space_horizontal_dir = normalize(vert_space_horizontal_dir);

	vec3 bump_wind = vec3(
		wind_noise_intensity * vert_space_horizontal_dir.x,
		1.0 - wind_noise_intensity,
		wind_noise_intensity * vert_space_horizontal_dir.y
	);
	normalize(bump_wind);
	bump_wind *= vec3(
		wind_strength,
		wind_vertical_strength,
		wind_strength
	);
	VERTEX += bump_wind * displacement_affect;


	// At the moment the blades are pushed away in a perfectly circular manner.
	// We could distort the distance to the character based on a noise, to break a bit the
	// circular shape. We could distort the falloff by sampling in a noise based on the xz coordinates.
	// The task is left to the reader

	//vec3 pos_diff = character_position - world_mat[3].xyz;
	// uncomment the following line to have a horizontal only character push
	//pos_diff.y = 0.0;
	//float diff_len = length(pos_diff);
	//float falloff = smoothstep(1.0, 0.0, diff_len /character_radius);
	// Because we operate in vertex space, we need to convert the direction to the character
	// in vertex space. Otherwise, it wouldn't work for rotated blades of grass.
	// comment the next line to observe how the blades are not all facing away from the character.
	//pos_diff = (inverse(MODEL_MATRIX) * vec4(pos_diff, 0.0)).xyz;
	//pos_diff = normalize(pos_diff);

	// sample the curve based on how far we are from the character, in normalized coordinates
	//float falloff_curve = texture(character_distance_falloff_curve, vec2(falloff)).x;
	// direction to character is inverted because we want to point away from it
	//VERTEX += normalize(-pos_diff) * falloff_curve * character_push_strength * displacement_affect;

}

void fragment() {
	ALBEDO = texture(color_ramp, vec2(1.0 - UV.y, 0)).rgb ;
}
