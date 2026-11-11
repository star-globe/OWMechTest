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


# light: 細身・軽装。heavy: 太く装甲が厚い。どちらも関節の位置（ボーン）は standard と同じ

def _head_light():
    # 細く縦長の頭部に、正面の単眼センサーと左右に開いたアンテナ
    return [
        box((0, 0, 7.4), (0.55, 0.7, 0.7), "Head", top=(0.7, 0.8)),
        box((0, -0.36, 7.42), (0.2, 0.06, 0.2), "Head", "Mat_Visor"),
        box((0.22, 0.05, 7.95), (0.04, 0.04, 0.5), "Head", "Mat_Detail", top=(0.5, 0.5)),
        box((-0.22, 0.05, 7.95), (0.04, 0.04, 0.5), "Head", "Mat_Detail", top=(0.5, 0.5)),
    ]


def _head_heavy():
    # 幅広で平たい頭部に、横長のバイザーと頬の装甲
    return [
        box((0, 0, 7.3), (0.9, 0.9, 0.5), "Head", top=(0.8, 0.85)),
        box((0, -0.46, 7.33), (0.75, 0.04, 0.1), "Head", "Mat_Visor"),
        box((0.5, -0.1, 7.2), (0.15, 0.6, 0.35), "Head", "Mat_Detail"),
        box((-0.5, -0.1, 7.2), (0.15, 0.6, 0.35), "Head", "Mat_Detail"),
    ]


def _body_light():
    return [
        box((0, 0, 6.8), (0.3, 0.3, 0.4), "Neck", "Mat_Detail"),
        box((0, 0, 6.05), (1.25, 0.85, 1.0), "Chest", top=(1.2, 1.05)),
        box((0, -0.45, 6.25), (0.9, 0.06, 0.15), "Chest", "Mat_Detail"),
        box((0, 0, 5.0), (0.55, 0.5, 0.8), "Spine", "Mat_Detail"),
        box((0, 0, 4.3), (1.05, 0.75, 0.55), "Hips", top=(1.1, 1.0)),
    ]


def _body_heavy():
    return [
        box((0, 0, 6.8), (0.45, 0.45, 0.4), "Neck", "Mat_Detail"),
        box((0, 0, 5.95), (1.95, 1.35, 1.35), "Chest", top=(0.9, 0.9)),
        box((0, -0.7, 5.9), (1.2, 0.08, 0.7), "Chest", top=(1.1, 1.0)),
        box((0, 0.85, 6.0), (1.1, 0.4, 1.0), "Chest", "Mat_Detail"),
        box((0, 0, 5.0), (1.0, 0.9, 0.8), "Spine", "Mat_Detail"),
        box((0, 0, 4.2), (1.5, 1.05, 0.8), "Hips", top=(1.1, 1.0)),
    ]


def _arm_left_light():
    # 肩アーマーなし。上腕・前腕とも細い
    return [
        box((0.9, 0, 6.3), (0.6, 0.35, 0.35), "Shoulder_L", "Mat_Detail"),
        box((1.3, 0, 6.3), (0.4, 0.4, 0.4), "Shoulder_L"),
        box((1.3, 0, 5.6), (0.28, 0.28, 1.2), "UpperArm_L", "Mat_Detail"),
        box((1.3, 0, 4.25), (0.36, 0.38, 1.2), "LowerArm_L", top=(1.2, 1.2)),
        box((1.3, 0, 3.35), (0.28, 0.25, 0.4), "Hand_L", "Mat_Detail"),
    ]


def _arm_left_heavy():
    # 大きな肩アーマーと太い前腕
    return [
        box((0.85, 0, 6.3), (0.5, 0.5, 0.5), "Shoulder_L", "Mat_Detail"),
        box((1.35, 0, 6.4), (1.0, 1.05, 0.8), "Shoulder_L", top=(0.7, 0.85)),
        box((1.3, 0, 5.55), (0.5, 0.5, 1.1), "UpperArm_L", "Mat_Detail"),
        box((1.3, 0, 4.25), (0.75, 0.8, 1.3), "LowerArm_L", top=(1.15, 1.1)),
        box((1.3, 0, 3.35), (0.45, 0.4, 0.5), "Hand_L", "Mat_Detail"),
    ]


def _leg_left_light():
    return [
        box((0.55, 0, 3.1), (0.42, 0.45, 1.7), "UpperLeg_L", "Mat_Detail"),
        box((0.55, -0.26, 2.2), (0.32, 0.15, 0.4), "LowerLeg_L"),
        box((0.55, 0, 1.35), (0.48, 0.55, 1.6), "LowerLeg_L", top=(1.25, 1.15)),
        box((0.55, -0.2, 0.125), (0.45, 1.1, 0.25), "Foot_L", top=(0.8, 0.9)),
    ]


def _leg_left_heavy():
    # 太いスネと足首の装甲、大きな足
    return [
        box((0.55, 0, 3.1), (0.7, 0.75, 1.7), "UpperLeg_L", "Mat_Detail"),
        box((0.55, -0.42, 2.2), (0.6, 0.25, 0.6), "LowerLeg_L"),
        box((0.55, 0, 1.4), (0.95, 1.05, 1.5), "LowerLeg_L", top=(0.8, 0.85)),
        box((0.55, 0, 0.6), (0.85, 0.9, 0.3), "Foot_L", "Mat_Detail"),
        box((0.55, -0.25, 0.175), (0.85, 1.5, 0.35), "Foot_L", top=(0.85, 0.9)),
    ]


PARTS = {
    "head_standard": _head_standard,
    "head_light": _head_light,
    "head_heavy": _head_heavy,
    "body_standard": _body_standard,
    "body_light": _body_light,
    "body_heavy": _body_heavy,
}
for _variant, _arm, _leg in (("standard", _arm_left_standard, _leg_left_standard),
                             ("light", _arm_left_light, _leg_left_light),
                             ("heavy", _arm_left_heavy, _leg_left_heavy)):
    PARTS[f"arm_left_{_variant}"] = _arm
    PARTS[f"arm_right_{_variant}"] = (lambda f: lambda: mirror(f()))(_arm)
    PARTS[f"leg_left_{_variant}"] = _leg
    PARTS[f"leg_right_{_variant}"] = (lambda f: lambda: mirror(f()))(_leg)


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
