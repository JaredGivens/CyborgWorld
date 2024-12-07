shader_type spatial;
uniform float speed; 
uniform vec2 direction;
uniform sampler2D energy : repeat_enable;
void fragment() {
	vec2 move = direction * TIME * speed;
	ALBEDO = texture(energy, UV + move).rgb;
}
//void light() {
	// Called for every pixel for every light affecting the material.
	// Uncomment to replace the default light processing function with this one.
//}
