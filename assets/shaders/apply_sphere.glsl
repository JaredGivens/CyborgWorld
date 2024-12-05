#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;

layout(std430, binding = 0) buffer CellBuf {
    int cell_buf[];
};

layout(std140, binding = 1) uniform Uniforms {
    mat4 inv_tsf;
    vec4 pos;
    int id;
};

int ternary(bool condition, int true_value, int false_value) {
    return -int(condition) & true_value | -int(!condition) & false_value;
}

const int dist_fac = 8;
const uint dim_len = 32;
int cube_sdf(vec3 pos) {
    vec4 local = inv_tsf * vec4(pos, 1.0);
    vec3 dv = abs(local.xyz / local.w) - vec3(1.0);
    float d = max(dv.x, max(dv.y, dv.z));
    d = mix(d, length(max(dv, vec3(0.0))), 0 < d);
    return int(clamp(round(d * dist_fac), -127, 127));
}
int int_sbyte(int b) {
    return (b ^ 0x80) - 0x80;
}
int sbyte_int(int i) {
    return (i + 0x80) ^ 0x80;
}
void main() {
    uvec3 cell_pos = gl_GlobalInvocationID;
    uint ind = cell_pos.x * dim_len * dim_len
            + cell_pos.y * dim_len
            + cell_pos.z;

    int cell = cell_buf[ind];
    int old_dist = int_sbyte(cell & 0xff);
    int new_dist = cube_sdf(pos.xyz + cell_pos);
    int res_dist = sbyte_int(ternary(new_dist < old_dist, new_dist, old_dist));

    int res_id = ternary(0 >= old_dist && 0 < new_dist, (cell >> 16) & 0xff, id);
    cell_buf[ind] = res_dist | (res_id << 16);
}

