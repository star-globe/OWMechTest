"""
機体本体（頭部・胴体・腕・脚）のパーツを、共通リグ rig_mech にスキニングしたメッシュとして作るスクリプト（#68）

パーツは箱の組み合わせ（プロトタイプ用）。箱ごとに 1 本のボーンへウェイト 100% で割り当てる。
メッシュはアーマチュアの子にせず、Armature モディファイアだけで rig_mech を参照する
（子にすると rig_mech の FBX にパーツのメッシュが入ってしまうため）。
座標は Blender の単位（m）、Z 上、機体の正面は -Y、機体の左は +X。機体の高さは約 8 m。

使い方（Blender MCP の execute_blender_code など。rig_mech があるシーンで実行する）:
  import sys, importlib
  sys.path.insert(0, r"<リポジトリ>/tools/blender")
  import mech_frame_parts as mp
  importlib.reload(mp)
  mp.build_all()                 # PARTS のパーツをすべて作り直す
  mp.build_part("head_standard") # 1 つだけ作り直す
"""

import bpy

RIG_NAME = "rig_mech"

MATERIAL_COLORS = {
    "Mat_Body":     ((0.42, 0.45, 0.48, 1.0), None),
    "Mat_Detail":   ((0.12, 0.13, 0.15, 1.0), None),
    "Mat_Visor":    ((0.10, 0.80, 1.00, 1.0), 3.0),
    "Mat_Thruster": ((1.00, 0.45, 0.10, 1.0), 3.0),
}


def box(center, size, bone, mat="Mat_Body", top=(1.0, 1.0)):
    """箱 1 つ。top は上面（+Z 側）の X・Y の縮尺（台形にしてデザインに差をつける）"""
    return dict(center=center, size=size, bone=bone, mat=mat, top=top)


def mirror(boxes):
    """左（+X）の箱を右（-X）に反転し、ボーン名の _L を _R にする"""
    result = []
    for b in boxes:
        cx, cy, cz = b["center"]
        result.append(dict(b, center=(-cx, cy, cz), bone=b["bone"].replace("_L", "_R")))
    return result


# ── パーツの定義 ───────────────────────────────────

def _head_standard():
    return [
        box((0, 0, 7.35), (0.7, 0.8, 0.6), "Head", top=(0.9, 0.9)),
        box((0, -0.41, 7.4), (0.55, 0.04, 0.15), "Head", "Mat_Visor"),
        box((0.25, 0.1, 7.85), (0.05, 0.05, 0.4), "Head", "Mat_Detail"),
    ]


def _body_standard():
    return [
        box((0, 0, 6.8), (0.35, 0.35, 0.4), "Neck", "Mat_Detail"),
        box((0, 0, 6.0), (1.6, 1.1, 1.2), "Chest", top=(1.1, 1.0)),
        box((0, -0.56, 5.8), (0.6, 0.04, 0.3), "Chest", "Mat_Detail"),
        box((0, 0, 5.0), (0.8, 0.7, 0.8), "Spine", "Mat_Detail"),
        box((0, 0, 4.25), (1.3, 0.9, 0.7), "Hips", top=(1.1, 1.0)),
    ]


def _arm_left_standard():
    return [
        box((0.85, 0, 6.3), (0.5, 0.4, 0.4), "Shoulder_L", "Mat_Detail"),
        box((1.25, 0, 6.35), (0.7, 0.8, 0.6), "Shoulder_L", top=(0.8, 0.9)),
        box((1.3, 0, 5.6), (0.4, 0.4, 1.2), "UpperArm_L", "Mat_Detail"),
        box((1.3, 0, 4.25), (0.5, 0.55, 1.2), "LowerArm_L", top=(1.1, 1.1)),
        box((1.3, 0, 3.35), (0.35, 0.3, 0.45), "Hand_L", "Mat_Detail"),
    ]


def _leg_left_standard():
    return [
        box((0.55, 0, 3.1), (0.55, 0.6, 1.7), "UpperLeg_L", "Mat_Detail"),
        box((0.55, -0.32, 2.2), (0.45, 0.2, 0.5), "LowerLeg_L"),
        box((0.55, 0, 1.35), (0.65, 0.75, 1.6), "LowerLeg_L", top=(1.1, 1.1)),
        box((0.55, -0.25, 0.15), (0.6, 1.3, 0.3), "Foot_L", top=(0.8, 0.9)),
    ]


PARTS = {
    "head_standard": _head_standard,
    "body_standard": _body_standard,
    "arm_left_standard": _arm_left_standard,
    "arm_right_standard": lambda: mirror(_arm_left_standard()),
    "leg_left_standard": _leg_left_standard,
    "leg_right_standard": lambda: mirror(_leg_left_standard()),
}


# ── 作成 ───────────────────────────────────────

def get_material(name):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
    color, emission = MATERIAL_COLORS[name]
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = color
        if emission is not None:
            bsdf.inputs["Emission Color"].default_value = color
            bsdf.inputs["Emission Strength"].default_value = emission
    return mat


def _box_geometry(b, offset):
    cx, cy, cz = b["center"]
    sx, sy, sz = (s * 0.5 for s in b["size"])
    tx, ty = b["top"]
    verts = []
    for z, kx, ky in ((cz - sz, 1.0, 1.0), (cz + sz, tx, ty)):
        for x, y in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            verts.append((cx + x * sx * kx, cy + y * sy * ky, z))
    o = offset
    faces = [
        (o + 0, o + 3, o + 2, o + 1),  # 下
        (o + 4, o + 5, o + 6, o + 7),  # 上
        (o + 0, o + 1, o + 5, o + 4),
        (o + 1, o + 2, o + 6, o + 5),
        (o + 2, o + 3, o + 7, o + 6),
        (o + 3, o + 0, o + 4, o + 7),
    ]
    return verts, faces


def build_part(name, rig_name=RIG_NAME):
    """PARTS[name] のメッシュを作る（同じ名前のオブジェクトがあれば作り直す）"""
    rig = bpy.data.objects.get(rig_name)
    if rig is None or rig.type != "ARMATURE":
        raise ValueError(f"アーマチュア {rig_name} がありません")

    boxes = PARTS[name]()

    old = bpy.data.objects.get(name)
    if old is not None:
        old_mesh = old.data
        bpy.data.objects.remove(old, do_unlink=True)
        if old_mesh is not None and old_mesh.users == 0:
            bpy.data.meshes.remove(old_mesh)

    verts, faces, face_mats, groups = [], [], [], []
    mat_names = []
    for b in boxes:
        v, f = _box_geometry(b, len(verts))
        groups.append((b["bone"], list(range(len(verts), len(verts) + len(v)))))
        verts += v
        faces += f
        if b["mat"] not in mat_names:
            mat_names.append(b["mat"])
        face_mats += [mat_names.index(b["mat"])] * len(f)

    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    for m in mat_names:
        mesh.materials.append(get_material(m))
    for poly, mi in zip(mesh.polygons, face_mats):
        poly.material_index = mi
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    for bone, indices in groups:
        vg = obj.vertex_groups.get(bone) or obj.vertex_groups.new(name=bone)
        vg.add(indices, 1.0, 'REPLACE')
    mod = obj.modifiers.new("Armature", 'ARMATURE')
    mod.object = rig
    return obj


def build_all(rig_name=RIG_NAME):
    return [build_part(n, rig_name).name for n in PARTS]
