#[compute]
#version 450
#define M_PI 3.14159265358979323846
#define FLT_MAX 3.402823466e+38

layout(local_size_x = 8, local_size_y = 8, local_size_z = 8) in;
const int kDistFac = 8;
const int kSdfRange = 128 / kDistFac;
const int kSDimLen = 32;
const int kCDimLen = kSDimLen + 2 * kSdfRange;
const int kCDimLen2 = kCDimLen * kCDimLen;
const int kCDimLen3 = kCDimLen2 * kCDimLen;
const int kNDimLen = kSDimLen + 2 + 2 * kSdfRange;
const int kNDimLen2 = kNDimLen * kNDimLen;
const int kNDimLen3 = kNDimLen2 * kNDimLen;
const ivec3[6] kDirs = {
        ivec3(1.0, 0.0, 0.0),
        ivec3(-1.0, 0.0, 0.0),
        ivec3(0.0, 1.0, 0.0),
        ivec3(0.0, -1.0, 0.0),
        ivec3(0.0, 0.0, 1.0),
        ivec3(0.0, 0.0, -1.0),
    };

layout(std430, binding = 0) buffer Cells {
    int cell_buf[kCDimLen3];
};
layout(std430, binding = 1) buffer readonly Noise {
    float noise_buf[kNDimLen3];
};
int btoi(int b) {
    return (b ^ 128) - 128;
}
int itob(int i) {
    return (i + 128) ^ 128;
}
int ternary(bool condition, int true_value, int false_value) {
    return -int(condition) & true_value | -int(!condition) & false_value;
}

vec3 unpack_polar(int cell) {
    float theta = float(btoi((cell >> 16) & 0xff)) * M_PI / 128;
    float phi = float((cell >> 24) & 0xff) * M_PI / 255;
    float x = sin(phi) * cos(theta);
    float y = cos(phi);
    float z = sin(phi) * sin(theta);
    return vec3(x, y, z);
}
int pack_polar(vec3 n) {
    float theta = atan(n.z, n.x);
    float phi = acos(n.y);
    int b2 = int(round(theta / M_PI * 128));
    int b3 = int(round(phi / M_PI * 255));
    return (b3 << 24) | (itob(ternary(b2 == 128, -128, b2)) << 16);
}

ivec3 unflatten(int f, int dim_len) {
    return ivec3(f / dim_len / dim_len, f / dim_len % dim_len, f % dim_len);
}
int flatten(ivec3 v, int dim_len) {
    return v.x * dim_len * dim_len + v.y * dim_len + v.z;
}
bool in_bounds(ivec3 point, int dim_len) {
    return all(greaterThanEqual(point, ivec3(0)))
        && all(lessThan(point, ivec3(dim_len)));
}

int compute_boundry(ivec3 v0) {
    ivec3 nv0 = v0 + ivec3(1);
    int ni0 = flatten(nv0, kNDimLen);
    float n0 = noise_buf[ni0];
    float ratio = 0.0f;
    int s0 = int(sign(n0));
    vec3 norm = normalize(vec3(
                noise_buf[ni0 + kNDimLen2] - noise_buf[ni0 - kNDimLen2],
                noise_buf[ni0 + kNDimLen] - noise_buf[ni0 - kNDimLen],
                noise_buf[ni0 + 1] - noise_buf[ni0 - 1]));
    if (s0 == 0) {
        return pack_polar(norm);
    }
    int inter_count = 0;
    vec3 inter = vec3(FLT_MAX);
    for (int ax = 0; ax < 3; ++ax) {
        for (int s = -1; s < 2; s += 2) {
            vec3 dir = vec3(0);
            dir[ax] = s;
            float n1 = noise_buf[flatten(nv0 + ivec3(dir), kNDimLen)];
            int s1 = int(sign(n1));
            if (dot((norm * -s0), dir) > 0 && s0 != s1) {
                inter[ax] = s * n0 / (n0 - n1);
                inter_count += 1;
            }
        }
    }
    if (0 == inter_count) {
        return itob(-128);
    }
    vec3 planeN = norm;
    bvec3 mask = lessThanEqual(inter.xyz, min(inter.yzx, inter.zxy));
    vec3 planeC = inter * vec3(mask);
    if (inter_count == 3) {
        planeN = normalize(vec3(inter.y * inter.z,
                    inter.x * inter.z,
                    inter.x * inter.y));
    }
    ratio = dot(planeN, planeC) / dot(norm, planeN);
    int cell = pack_polar(norm);
    cell |= itob(int(round(kDistFac * -ratio)));
    return cell;
}

int compare(int i0, int diri, int jumpi) {
    int jump = kSdfRange >> jumpi;
    int c0 = cell_buf[i0];
    int d0 = btoi(c0 & 0xff);
    ivec3 dir = kDirs[diri];
    ivec3 v1 = dir * jump + unflatten(i0, kCDimLen);
    if (!in_bounds(v1, kCDimLen)) {
        return c0;
    }
    int i1 = flatten(v1, kCDimLen);
    int c1 = cell_buf[i1];
    int d1 = btoi(c1 & 0xff);
    if (d1 == -128) {
        return c0;
    }
    int s1 = -int(sign(dot(unpack_polar(c1), vec3(dir))));
    if (s1 == 0 || s1 != int(sign(d1))) {
        return c0;
    }
    int diff = jump * kDistFac * s1;
    d1 = clamp(diff + d1, -127, 127);
    if (abs(d0) > abs(d1)) {
        return (c1 & 0xffffff00) | itob(d1);
    } else {
        return c0;
    }
}

void main() {
    int ind = flatten(ivec3(gl_GlobalInvocationID), kCDimLen);
    cell_buf[ind] = compute_boundry(ivec3(gl_GlobalInvocationID));
    memoryBarrier();
    for (int i = 0; i < 5; ++i) {
        for (int j = 0; j < 6; ++j) {
            cell_buf[ind] = compare(ind, j, i);
            memoryBarrier();
        }
    }
}
