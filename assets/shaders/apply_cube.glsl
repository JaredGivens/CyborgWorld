#[compute]
#version 450
#define M_PI 3.14159265358979323846

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;
const int kDistFac = 8;
const uint kSDimLen = 32;

layout(std430, binding = 0) buffer CellBuf {
    int cell_buf[];
};

layout(std140, binding = 1) uniform Uniforms {
    mat4 tsf;
    vec4 pos;
    int block_id;
};

int ternary(bool condition, int true_value, int false_value) {
    return -int(condition) & true_value | -int(!condition) & false_value;
}
int btoi(int b) {
    return (b ^ 128) - 128;
}
int itob(int i) {
    return (i + 128) ^ 128;
}
int pack_polar(vec3 n) {
    float theta = atan(n.z, n.x);
    float phi = acos(n.y);
    int b2 = int(round(theta / M_PI * 128));
    int b3 = int(round(phi / M_PI * 255));
    return (b3 << 24) | (itob(ternary(b2 == 128, -128, b2)) << 16);
}
int cube_sdf(vec3 pos) {
    vec4 local_pos4 = inverse(tsf) * vec4(pos, 1.0);
    vec3 local_pos = local_pos4.xyz / local_pos4.w;
    vec3 local_diff = abs(local_pos) - vec3(1.0);
    vec3 local_pos_near = max(local_diff, vec3(0.0));
    vec3 max_mask = vec3(greaterThanEqual(local_diff.xyz,
                max(local_diff.yzx, local_diff.zxy)));
    vec3 local_neg_near = max_mask * local_diff;
    float is_neg = float(all(greaterThanEqual(vec3(0.0), local_neg_near)));
    vec3 local_near = mix(local_neg_near, local_pos_near, is_neg);
    vec3 local_norm = mix(max_mask, normalize(local_pos_near), is_neg)
            * (sign(local_pos) + vec3(equal(local_pos, vec3(0.0))));
    vec4 norm4 = tsf * vec4(local_norm, 1.0);
    vec4 near4 = tsf * vec4(local_near, 1.0);
    int cell = pack_polar(normalize(norm4.xyz / norm4.w));
    float dist = length(near4.xyz / near4.w) * sign(dot(max_mask, local_near));
    itob(int(clamp(round(dist * kSDimLen), -127, 127)));
    return cell;
}
void main() {
    uvec3 cell_pos = gl_GlobalInvocationID;
    uint ind = cell_pos.x * kSDimLen * kSDimLen
            + cell_pos.y * kSDimLen
            + cell_pos.z;
    int old_cell = cell_buf[ind];
    int old_norm_dist = old_cell & 0xffff00ff;
    int old_dist = btoi(old_cell & 0xff);
    int new_norm_dist = cube_sdf(pos.xyz + cell_pos);
    int new_dist = btoi(new_norm_dist & 0xff);
    new_dist = ternary(block_id == -1, -new_dist, new_dist);
    bool replace = (block_id != -1 && new_dist < old_dist)
            || (block_id == -1 && new_dist > old_dist);
    int res_norm_dist = itob(ternary(replace, new_norm_dist, old_norm_dist));

    bool replace_id = 0 >= old_dist && 0 < new_dist;
    int res_id = ternary(replace_id, old_cell & 0xff00, block_id << 8);
    cell_buf[ind] = res_norm_dist | res_id;
}
