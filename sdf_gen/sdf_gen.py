import bpy
import numpy as np
import math
from mathutils import Vector
import bmesh
import mathutils
from mathutils.bvhtree import BVHTree
from Fb import Prefab
import flatbuffers
import sys
import os

# Ensure that the script is called with a file path
if len(sys.argv) > 1:
    # The first argument after the script name is the file path
    file_path = sys.argv[1]
    
    # Open the .blend file using the bpy operator
    bpy.ops.wm.open_mainfile(filepath=file_path)
else:
    print("No file path provided. Please provide a .blend file path as an argument.")

    
DIST_FAC = 8
SDF_RANGE = 128 // DIST_FAC
SCALE = 2
def is_inside(ray_origin: Vector, obj: bpy.types.Object):

    # the matrix multiplations and inversions are only needed if you
    # have unapplied transforms, else they could be dropped. but it's handy
    # to have the algorithm take them into account, for generality.
    ray_direction = Vector((1, 0, 0))
    hit, loc, normal, face_idx = obj.ray_cast(ray_origin, ray_direction)

    if not hit:
        return False
    
    max_expected_intersections = 1000
    fudge_distance = 0.0001
    
    i = 1
    while (face_idx != -1):
        
        loc += ray_direction * fudge_distance;    
        hit, loc, normal, face_idx = obj.ray_cast(loc,  ray_direction)
        if not hit:
            break
        i += 1
        if i > max_expected_intersections:
            break

    return (i & 1) == 1

BVHTreeList = list[tuple[BVHTree, bpy.types.Object]]
def find_signed_distance(point: Vector, bvh_data: BVHTreeList):
    """Find the nearest point and the object it belongs to."""
    closest_distance = float('inf')
    closest_info: tuple[float,int,Vector] | None = None

    for bvh_tree, obj in bvh_data:
        result = bvh_tree.find_nearest(point)
        if result is not None:
            location, normal, index, distance = result
            if distance < closest_distance:
                closest_distance = distance
                signed_distance = distance  - 1 / 2 / SCALE
                if signed_distance < 0:
                    block_id = int(obj.material_slots.values()[0].name) 
                else: 
                    block_id = 0
                closest_info = (signed_distance, block_id, normal)

    return closest_info

def get_bvh_data(collection: bpy.types.Collection) -> BVHTreeList:
    bvh_data = []
    for obj in collection.objects:
        if obj.type != 'MESH':
            continue
        print(f"building object {obj.name} bvh");
        # Create BVH for each object
        bm: BMesh = bmesh.new()
        bm.from_mesh(obj.data)
        bm.transform(obj.matrix_world)  # Apply object transforms to the BVH
        bvh_tree = BVHTree.FromBMesh(bm)
        bm.free()

            # Store BVH and reference to object
        bvh_data.append((bvh_tree, obj))
    return bvh_data

def compute_prefab_size(collection: bpy.types.Collection):
    max_top = None
    min_bot = None
    for obj in collection.objects:
        if obj.type != 'MESH':
            continue
        bot = np.array(obj.matrix_world @ Vector(obj.bound_box[0]))
        bot = bot.round().astype(np.int32)
        top = np.array(obj.matrix_world @ Vector(obj.bound_box[6]))
        top = top.round().astype(np.int32)
        if min_bot is None:
            min_bot = bot
        else:
            min_bot = np.minimum(min_bot, bot)
        if max_top is None:
            max_top = top
        else:
            max_top = np.maximum(max_top, top)

    if max_top is None or min_bot is None:
        return None
    print(max_top, min_bot)
    padded_top = max_top + 3
    padded_bot = min_bot - 2
    dimensions = padded_top - padded_bot
    return (padded_bot, dimensions)

def itob(i: int) -> int:
    return (i + 128) ^ 128;
def pack_polar( n: Vector) -> int:
    theta = math.atan2(n.y, n.x)
    phi = math.acos(n.z)
    b2 = int(round(theta / math.pi * 128));
    b3 = int(round(phi / math.pi * 255));
    return (b3 << 24) | (itob(-128 if b2 == 128 else b2) << 16);

for collection in bpy.context.scene.collection.children:
    print(f"processing: {collection.name}")
    
    res= compute_prefab_size(collection)
    if res is None:
        continue
    location, dim = res
    # Iterate over objects in each collection
    bvh_data = get_bvh_data(collection)
    dim *= SCALE
    cells = np.zeros(dim.prod(), dtype=np.uint32)

    for x in range(dim[0]):
        for y in range(dim[1],):
            for z in range(dim[2], ):
                world_x = location[0] + x 
                world_y = location[1] + y
                world_z = location[2] + z
                
                world_point = Vector((world_x, world_y, world_z)) / SCALE
                res = find_signed_distance(world_point, bvh_data)
                if res is None:
                    continue
                signed_distance, block_id, normal= res
                
                # Clamp the distance to [-127, 127] and store it
                signed_distance = round(signed_distance * DIST_FAC)
                signed_distance = itob(max(-127, min(127, signed_distance)))
                block_id <<= 8;

                flat = x * dim[1] * dim[2] + z * dim[1] + y
                cells[flat] = pack_polar(normal) | block_id | signed_distance

    builder = flatbuffers.Builder(dim.prod() * 4)
    cell_vector = builder.CreateNumpyVector(cells)
    Prefab.Start(builder)
    Prefab.AddWidth(builder, dim[0])
    Prefab.AddHeight(builder, dim[2])
    Prefab.AddDepth(builder, dim[1])
    Prefab.AddCells(builder, cell_vector)
    fb_prefab = Prefab.End(builder)
    builder.Finish(fb_prefab)
    buf = builder.Output()
    fn = f"../assets/models/sdfs/{collection.name}.fb"

    current_file_path = os.path.dirname(os.path.realpath(__file__))
    file_path = os.path.join(current_file_path, fn)
    with open(file_path, "wb") as f:
        f.write(buf)
        print(f"wrote {file_path}")
