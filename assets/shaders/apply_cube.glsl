#[compute]
#version 450
#define M_PI 3.14159265358979323846
#define FLT_EPSILON 1E-5

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;
const int kDistFac = 8;
const uint kDimLen = 32;
const uint kDimLen2 = kDimLen * kDimLen;
const uint kDimLen3 = kDimLen2 * kDimLen;

layout(std430, binding = 0) buffer Cells {
    int sb_cells[kDimLen3];
};

layout(std140, binding = 1) uniform Uniforms {
    mat4 u_inv_tsf;
    mat3 u_basis;
    vec4 u_pos;
    int u_block_id;
};

int btoi(int b) {
    return (b ^ 128) - 128;
}
int itob(int i) {
    return (i + 128) ^ 128;
}
int pack_polar(vec3 n) {
    float theta = atan(n.z, n.x);
    float phi = acos(n.y);
    int b2 = clamp(int(round(theta / M_PI * 128)), -128, 128);
    int b3 = clamp(int(round(phi / M_PI * 255)), 0, 255);
    return (b3 << 24) | (itob(b2 == 128 ? -128 : b2) << 16);
}
vec3 norm_or_up(vec3 v) {
    float l = length(v);
    return mix(v / max(l, FLT_EPSILON), vec3(0.0, 1.0, 0.0), float(l == 0));
}
vec3 safe_divide(vec3 v, float d) {
    return v / max(d, FLT_EPSILON);
}

int cube_sdf(vec3 pos) {
    vec4 local_pos4 = u_inv_tsf * vec4(pos, 1.0);
    vec3 local_pos = local_pos4.xyz / local_pos4.w;
    vec3 local_diff = abs(local_pos) - vec3(1.0);
    vec3 local_pos_near = max(local_diff, vec3(0.0));
    vec3 max_mask = max(local_diff.yzx, local_diff.zxy);
    max_mask = vec3(greaterThan(local_diff.xyz, max_mask));
    vec3 local_neg_near = max_mask * local_diff;
    bool is_neg = 0.0 > dot(max_mask, local_diff);
    vec3 local_near = mix(local_pos_near, local_neg_near, float(is_neg));
    //vec3 local_norm = mix(max_mask, normalize(local_pos_near), is_neg)
    //* (sign(local_pos) + vec3(equal(local_pos, vec3(0.0))));
    //vec4 norm4 = tsf * vec4(local_norm, 1.0);
    vec3 near = u_basis * local_near;
    //int cell = pack_polar(normalize(norm4.xyz / norm4.w));
    float dist = length(local_near) * (is_neg ? -1 : 1);
    return itob(int(clamp(round(dist * kDimLen), -127, 127)));
    //return cell;
}

void main() {
    uvec3 cell_pos = gl_GlobalInvocationID;
    uint ind = cell_pos.x * kDimLen2
            + cell_pos.y * kDimLen
            + cell_pos.z;
    int old_cell = sb_cells[ind];
    int old_norm_dist = old_cell & 0xffff00ff;
    int old_dist = btoi(old_cell & 0xff);
    int new_norm_dist = cube_sdf(u_pos.xyz + cell_pos);
    int new_norm = new_norm_dist & 0xffff0000;
    int new_dist = btoi(new_norm_dist & 0xff);
    new_dist = u_block_id == -1 ? -new_dist : new_dist;
    bool replace = (u_block_id != -1 && new_dist < old_dist)
            || (u_block_id == -1 && new_dist > old_dist);
    int res_norm_dist = replace ? new_norm | itob(new_dist) : old_norm_dist;

    bool replace_id = u_block_id != -1 && 0 < old_dist && 0 > new_dist;
    int res_id = replace_id ? u_block_id << 8 : (old_cell & 0xff00);
    sb_cells[ind] = res_norm_dist | res_id; //res_norm_dist | res_id;
}
