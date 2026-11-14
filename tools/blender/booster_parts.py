"""
ブースターパーツのメッシュを作るスクリプト（#70）

ブースターは BoosterPartConnector の子に置かれる（リグは使わない）。
座標は Blender の単位（m）。原点が取り付け位置（コネクター）、+Z が噴射の向き（Unity ではパーツの +Y）。
原点より -Z 側は機体に差し込む取り付け部で、胴体の大きさの違い（背面の位置の差）を吸収する。
ノズルの出口に空オブジェクト nozzle_exit_{バリアント名} をメッシュの子として置き、
Unity ではこれを基準に噴射のエフェクトを配置する。
ノズルの内側は Mat_Thruster。Unity の BoosterPart.SetBaseColor が _EmissionColor を基本色で上書きする。

使い方（Blender MCP の execute_blender_code など）:
  import sys, importlib
  sys.path.insert(0, r"<リポジトリ>/tools/blender")
  import booster_parts as bp
  importlib.reload(bp)
  bp.build_all()                    # PARTS のブースターをすべて作り直す
  bp.build_part("booster_standard") # 1 つだけ作り直す
"""

import math

import bpy

# 色は Unity に取り込んだときの初期値。Mat_Thruster の発光色は Unity 側で基本色に置き換わる
MATERIAL_COLORS = {
    "Mat_Body":     ((0.42, 0.45, 0.48, 1.0), None),
    "Mat_Detail":   ((0.12, 0.13, 0.15, 1.0), None),
    "Mat_Thruster": ((1.00, 1.00, 1.00, 1.0), 1.0),
}

NOZZLE_EXIT_PREFIX = "nozzle_exit_"

SEGMENTS = 16


# ── 形の定義 ─────────────────────────────────────

def box(center, size, mat="Mat_Body", top=(1.0, 1.0)):
    """箱。top は上面（+Z 側）の X・Y の縮尺"""
    return {"kind": "box", "center": center, "size": size, "mat": mat, "top": top}


def tube(z0, z1, r0, r1, mat="Mat_Body", cap0=True, cap1=True, inside=False):
    """Z 軸に沿った円すい台。inside=True なら面を内向きにする（ノズルの内側）"""
    return {"kind": "tube", "z": (z0, z1), "r": (r0, r1), "mat": mat,
            "caps": (cap0, cap1), "inside": inside}


def ring(z, r_in, r_out, mat="Mat_Detail"):
    """+Z を向いた平たい輪（ノズルの縁）"""
    return {"kind": "ring", "z": z, "r": (r_in, r_out), "mat": mat}


def nozzle(z_throat, z_exit, r_throat, r_exit, thickness=0.03):
    """外側（Mat_Detail）・内側（Mat_Thruster）・縁・奥の面からなるノズル"""
    t = thickness
    return [
        tube(z_throat, z_exit, r_throat + t, r_exit + t, "Mat_Detail", cap0=True, cap1=False),
        tube(z_throat + t, z_exit, r_throat, r_exit, "Mat_Thruster", cap0=False, cap1=False, inside=True),
        ring(z_throat + t, 0.0, r_throat, "Mat_Thruster"),
        ring(z_exit, r_exit, r_exit + t, "Mat_Detail"),
    ]


def _booster_standard():
    """標準: 円筒の本体に、細めのノズル。長さ 1.55 m（取り付け部 0.35 m を含む）"""
    return {
        "shapes": [
            box((0, 0, -0.15), (0.3, 0.26, 0.4), "Mat_Detail"),
            tube(0.0, 0.85, 0.24, 0.24, "Mat_Body"),
            tube(0.70, 0.85, 0.27, 0.27, "Mat_Detail"),
            box((0.27, 0, 0.35), (0.06, 0.12, 0.5), "Mat_Detail", top=(1.0, 0.5)),
            box((-0.27, 0, 0.35), (0.06, 0.12, 0.5), "Mat_Detail", top=(1.0, 0.5)),
            *nozzle(0.85, 1.2, 0.15, 0.25),
        ],
        "nozzle_exit": (0, 0, 1.2),
    }


def _booster_heavy():
    """重量: 角張った太い本体に、大きなノズル。長さ 1.5 m（取り付け部 0.35 m を含む）"""
    return {
        "shapes": [
            box((0, 0, -0.15), (0.45, 0.36, 0.4), "Mat_Detail"),
            box((0, 0, 0.45), (0.64, 0.56, 0.9), "Mat_Body", top=(0.85, 0.85)),
            box((0.34, 0, 0.4), (0.06, 0.46, 0.7), "Mat_Detail"),
            box((-0.34, 0, 0.4), (0.06, 0.46, 0.7), "Mat_Detail"),
            box((0, 0.3, 0.5), (0.3, 0.06, 0.5), "Mat_Detail"),
            *nozzle(0.9, 1.3, 0.22, 0.34, thickness=0.04),
        ],
        "nozzle_exit": (0, 0, 1.3),
    }


PARTS = {
    "booster_standard": _booster_standard,
    "booster_heavy": _booster_heavy,
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


def _box_geometry(s, offset):
    cx, cy, cz = s["center"]
    sx, sy, sz = (v * 0.5 for v in s["size"])
    tx, ty = s["top"]
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


def _circle(z, r):
    return [(r * math.cos(2 * math.pi * i / SEGMENTS), r * math.sin(2 * math.pi * i / SEGMENTS), z)
            for i in range(SEGMENTS)]


def _tube_geometry(s, offset):
    (z0, z1), (r0, r1) = s["z"], s["r"]
    n, o = SEGMENTS, offset
    verts = _circle(z0, r0) + _circle(z1, r1)
    faces = []
    for i in range(n):
        j = (i + 1) % n
        faces.append((o + i, o + j, o + n + j, o + n + i))  # 外向き
    cap0, cap1 = s["caps"]
    if cap0:
        faces.append(tuple(o + i for i in reversed(range(n))))  # -Z 向き
    if cap1:
        faces.append(tuple(o + n + i for i in range(n)))  # +Z 向き
    if s["inside"]:
        faces = [tuple(reversed(f)) for f in faces]
    return verts, faces


def _ring_geometry(s, offset):
    z, (r_in, r_out) = s["z"], s["r"]
    n, o = SEGMENTS, offset
    if r_in <= 0.0:
        return _circle(z, r_out), [tuple(o + i for i in range(n))]
    verts = _circle(z, r_in) + _circle(z, r_out)
    faces = [(o + i, o + n + i, o + n + (i + 1) % n, o + (i + 1) % n) for i in range(n)]
    return verts, faces


GEOMETRY = {"box": _box_geometry, "tube": _tube_geometry, "ring": _ring_geometry}


def _remove_object(name):
    old = bpy.data.objects.get(name)
    if old is None:
        return
    old_data = old.data
    bpy.data.objects.remove(old, do_unlink=True)
    if old_data is not None and old_data.users == 0:
        bpy.data.meshes.remove(old_data)


def build_part(name):
    """PARTS[name] のメッシュとノズルの出口の空オブジェクトを作る（同じ名前のオブジェクトがあれば作り直す）"""
    definition = PARTS[name]()
    variant = name[len("booster_"):]
    exit_name = NOZZLE_EXIT_PREFIX + variant

    _remove_object(exit_name)
    _remove_object(name)

    verts, faces, face_mats, mat_names = [], [], [], []
    for s in definition["shapes"]:
        v, f = GEOMETRY[s["kind"]](s, len(verts))
        verts += v
        faces += f
        if s["mat"] not in mat_names:
            mat_names.append(s["mat"])
        face_mats += [mat_names.index(s["mat"])] * len(f)

    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    for m in mat_names:
        mesh.materials.append(get_material(m))
    for poly, mi in zip(mesh.polygons, face_mats):
        poly.material_index = mi
    mesh.update()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)

    exit_obj = bpy.data.objects.new(exit_name, None)
    exit_obj.empty_display_type = 'SINGLE_ARROW'  # 矢印（+Z）が噴射の向き
    exit_obj.empty_display_size = 0.3
    exit_obj.location = definition["nozzle_exit"]
    exit_obj.parent = obj
    bpy.context.scene.collection.objects.link(exit_obj)
    return obj


def build_all():
    return [build_part(n).name for n in PARTS]


def measure(name):
    """形の確認用: 頂点の範囲・マテリアルごとの面数・ノズルの出口の位置を返す"""
    obj = bpy.data.objects[name]
    xs, ys, zs = zip(*(v.co for v in obj.data.vertices))
    counts = {}
    for p in obj.data.polygons:
        m = obj.data.materials[p.material_index].name
        counts[m] = counts.get(m, 0) + 1
    exit_obj = next((c for c in obj.children if c.name.startswith(NOZZLE_EXIT_PREFIX)), None)
    return {
        "min": [round(min(xs), 3), round(min(ys), 3), round(min(zs), 3)],
        "max": [round(max(xs), 3), round(max(ys), 3), round(max(zs), 3)],
        "faces": counts,
        "nozzle_exit": [round(c, 3) for c in exit_obj.location] if exit_obj else None,
    }
