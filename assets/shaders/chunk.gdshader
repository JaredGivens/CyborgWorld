shader_type spatial;
uniform sampler2D albedo_atlas: filter_nearest;
uniform sampler2D orm_atlas: filter_nearest;
uniform sampler2D nh_atlas: filter_nearest;
uniform int atlas_height; // Number of tiles in the atlas (e.g., 4x4 tiles)
uniform int atlas_width; // Number of tiles in the atlas (e.g., 4x4 tiles)
uniform float chunk_size;
varying vec2 uv;
varying vec3 norm2;
varying flat vec2 uvx;
varying flat vec2 uvy;
varying flat vec2 uvz;

int xtexi(int texi, int mapping, vec3 norm) {
  switch (mapping) {
    case 3:
      if (norm.x >= 0.0) {
        texi += 1;
      }
      break;
    case 4:
      if (norm.x < 0.0) {
        texi += 1;
      }
      break;
  }
  return texi;
}
int ytexi(int texi, int mapping, vec3 norm) {
  switch (mapping) {
    case 1:
    case 2:
    case 3:
    case 4:
      if (norm.y >= 0.0) {
        texi += 2;
      }
      break;
  }
  return texi;
}
int ztexi(int texi, int mapping, vec3 norm) {
  switch (mapping) {
    case 1:
      if (norm.z >= 0.0) {
        texi += 1;
      }
      break;
    case 2:
      if (norm.z < 0.0) {
        texi += 1;
      }
      break;
  }
  return texi;
}
vec2 texiuv(int texi) {
  return vec2( float(texi % atlas_width), float(texi / atlas_width));
}
const vec2 uvs[] = {
	vec2(0, 0),
	vec2(1, 1),
	vec2(1, 0),
	vec2(0, 0),
	vec2(0, 1),
	vec2(1, 1)
};
void vertex() {
  uv = uvs[VERTEX_ID % 6];
  norm2 = NORMAL  * NORMAL;
  uvx = texiuv(xtexi(int(UV.x), int(UV.y), NORMAL));
  uvy = texiuv(ytexi(int(UV.x), int(UV.y), NORMAL));
  uvz = texiuv(ztexi(int(UV.x), int(UV.y), NORMAL));
}
void fragment() {
  vec2 size = vec2(ivec2(atlas_width, atlas_height));
  vec4 albedo = texture(albedo_atlas, (uvx + uv) / size) * norm2.x;
  albedo += texture(albedo_atlas, (uvy + uv) / size) * norm2.y;
  albedo += texture(albedo_atlas, (uvz + uv) / size) * norm2.z;
  vec4 orm = texture(orm_atlas, (uvx + uv) / size) * norm2.x;
  orm += texture(orm_atlas, (uvy + uv) / size) * norm2.y;
  orm += texture(orm_atlas, (uvz + uv) / size) * norm2.z;
  vec4 nh = texture(nh_atlas, (uvx + uv) / size) * norm2.x;
  nh += texture(nh_atlas, (uvy + uv) / size) * norm2.y;
  nh += texture(nh_atlas, (uvz + uv) / size) * norm2.z;

  ALBEDO = albedo.rgb;
  AO = orm.r;
  ROUGHNESS = orm.g;
  METALLIC = orm.b;
  NORMAL_MAP = nh.xyz;
  NORMAL_MAP_DEPTH = nh.a;
  //ALPHA = color.a;
}

//void light() {
// Called for every pixel for every light affecting the material.
// Uncomment to replace the default light processing function with this one.
//}
