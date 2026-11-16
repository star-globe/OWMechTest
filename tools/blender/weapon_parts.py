"""
手持ちの銃のメッシュを作るスクリプト（#154）

銃は手のボーンの GunConnector_L / GunConnector_R の子に置かれる（リグは使わない）。
座標は Blender の単位（m）。原点がグリップ（手で握る位置）、-Y が銃口の向き、+Z が上
（Unity では +Z が銃口の向き、+Y が上）。機体の 8 m 基準で、手の大きさは 0.3〜0.45 m。
左用（weapon_left_*）は弾倉やセンサーを外側（+X）に寄せる。右用（weapon_right_*）は左用を
mirror() で X に反転して作る（腕と同じく左右で鏡像）。
銃口に空オブジェクト muzzle_{left|right}_{種類} をメッシュの子として置き、
Unity ではこの位置に GunPart の発射位置（muzzle）を置く。

使い方（Blender MCP の execute_blender_code など）:
  import sys, importlib
  sys.path.insert(0, r"<リポジトリ>/tools/blender")
  import weapon_parts as wp
  importlib.reload(wp)
  wp.build_all()                      # PARTS の銃をすべて（左用・右用）作り直す
  wp.build_part("weapon_right_rifle") # 1 つだけ作り直す
  wp.measure("weapon_left_rifle")     # 大きさとマズルの位置を数値で確かめる
"""

import math

import bpy

MATERIAL_COLORS = {
    "Mat_Body":   ((0.42, 0.45, 0.48, 1.0), None),
    "Mat_Detail": ((0.12, 0.13, 0.15, 1.0), None),
    "Mat_Visor":  ((0.10, 0.80, 1.00, 1.0), 3.0),
}

MUZZLE_PREFIX = "muzzle_"

SEGMENTS = 16


# ── 形の定義 ─────────────────────────────────────

def box(center, size, mat="Mat_Body", top=(1.0, 1.0)):
    """箱。top は上面（+Z 側）の X・Y の縮尺"""
    return {"kind": "box", "center": center, "size": size, "mat": mat, "top": top}


def barrel(y0, y1, r0, r1, x=0.0, z=0.0, mat="Mat_Detail"):
    """Y 軸に沿った円すい台（銃身）。y0 が根元、y1 が先端（-Y が前なので y1 < y0）"""
    return {"kind": "barrel", "y": (y0, y1), "r": (r0, r1), "x": x, "z": z, "mat": mat}


def mirror(definition):
    """左用（外側が +X）の定義を右用（外側が -X）に反転する。形はどれも自分の中心で対称なので、中心の X だけ反転すればよい"""
    shapes = []
    for s in definition["shapes"]:
        if s["kind"] == "box":
            cx, cy, cz = s["center"]
            shapes.append(dict(s, center=(-cx, cy, cz)))
        else:
            shapes.append(dict(s, x=-s["x"]))
    mx, my, mz = definition["muzzle"]
    return {"shapes": shapes, "muzzle": (-mx, my, mz)}


def _rifle():
    """ライフル（標準）: 全長 2.6 m（グリップの後ろ 0.6 / 前 2.0）、高さ 0.75 m、幅 0.32 m、銃身 φ0.14"""
    return {
        "shapes": [
            box((0, -0.2, 0.25), (0.22, 1.2, 0.3)),                         # 機関部
            box((0, 0.05, -0.12), (0.16, 0.22, 0.46), "Mat_Detail"),        # グリップ
            box((0, 0.45, 0.24), (0.2, 0.3, 0.28), top=(1.0, 0.8)),         # 銃床
            box((0, -1.05, 0.28), (0.2, 0.5, 0.2), top=(0.9, 1.0)),         # ハンドガード
            barrel(-0.8, -2.0, 0.07, 0.07, z=0.30),                         # 銃身
            barrel(-1.85, -2.0, 0.09, 0.09, z=0.30),                        # 銃口
            box((0.16, -0.45, 0.2), (0.10, 0.3, 0.36), "Mat_Detail"),       # 外側の弾倉
            box((0.13, -0.95, 0.30), (0.06, 0.2, 0.08), "Mat_Visor"),       # 外側のセンサー
        ],
        "muzzle": (0, -2.0, 0.30),
    }


def _smg():
    """短機関銃（軽量）: 全長 1.6 m（後ろ 0.3 / 前 1.3）、高さ 0.6 m、幅 0.28 m、銃身 φ0.10"""
    return {
        "shapes": [
            box((0, -0.25, 0.18), (0.2, 0.9, 0.24)),                        # 機関部
            box((0, 0.0, -0.1), (0.14, 0.2, 0.4), "Mat_Detail"),            # グリップ
            box((0, 0.25, 0.2), (0.12, 0.1, 0.16), "Mat_Detail"),           # 折りたたんだ銃床
            box((0, -0.85, 0.2), (0.16, 0.3, 0.16)),                        # 銃身のカバー
            box((0, -0.6, -0.05), (0.1, 0.12, 0.22), "Mat_Detail"),         # 前のグリップ
            barrel(-0.7, -1.3, 0.05, 0.05, z=0.22),                         # 銃身
            box((0.14, -0.35, 0.12), (0.08, 0.18, 0.3), "Mat_Detail"),      # 外側の弾倉
            box((0.11, -0.65, 0.26), (0.04, 0.12, 0.06), "Mat_Visor"),      # 外側のセンサー
        ],
        "muzzle": (0, -1.3, 0.22),
    }


def _cannon():
    """大口径砲（重量）: 全長 3.4 m（後ろ 1.0 / 前 2.4）、高さ 0.9 m、幅 0.5 m、銃身 φ0.30、マズルブレーキ φ0.40"""
    return {
        "shapes": [
            box((0, -0.1, 0.3), (0.34, 1.4, 0.4)),                          # 機関部
            box((0, 0.8, 0.31), (0.3, 0.4, 0.38), top=(0.9, 0.9)),          # 後ろの釣り合いのおもり
            box((0, 0.1, -0.15), (0.18, 0.24, 0.5), "Mat_Detail"),          # グリップ
            barrel(-0.8, -1.0, 0.18, 0.18, z=0.30),                         # 銃身の根元
            barrel(-1.0, -2.15, 0.15, 0.15, z=0.30),                        # 銃身
            barrel(-2.15, -2.4, 0.20, 0.20, z=0.30),                        # マズルブレーキ
            box((0.25, -0.1, 0.25), (0.16, 0.6, 0.4), "Mat_Detail"),        # 外側の弾薬箱
            box((0.20, -0.7, 0.45), (0.06, 0.1, 0.06), "Mat_Visor"),        # 外側のセンサー
        ],
        "muzzle": (0, -2.4, 0.30),
    }


PARTS = {}
for _variant, _left in (("rifle", _rifle), ("smg", _smg), ("cannon", _cannon)):
    PARTS[f"weapon_left_{_variant}"] = _left
    PARTS[f"weapon_right_{_variant}"] = (lambda f: lambda: mirror(f()))(_left)


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


def _circle(x, y, z, r):
    """XZ 平面の円（Y 軸まわり）"""
    return [(x + r * math.cos(2 * math.pi * i / SEGMENTS), y, z + r * math.sin(2 * math.pi * i / SEGMENTS))
            for i in range(SEGMENTS)]


def _barrel_geometry(s, offset):
    (y0, y1), (r0, r1), x, z = s["y"], s["r"], s["x"], s["z"]
    n, o = SEGMENTS, offset
    verts = _circle(x, y0, z, r0) + _circle(x, y1, z, r1)
    faces = []
    for i in range(n):
        j = (i + 1) % n
        faces.append((o + i, o + j, o + n + j, o + n + i))  # 外向き
    faces.append(tuple(o + i for i in reversed(range(n))))  # 根元（+Y 向き）
    faces.append(tuple(o + n + i for i in range(n)))        # 先端（-Y 向き）
    return verts, faces


GEOMETRY = {"box": _box_geometry, "barrel": _barrel_geometry}


def _remove_object(name):
    old = bpy.data.objects.get(name)
    if old is None:
        return
    old_data = old.data
    bpy.data.objects.remove(old, do_unlink=True)
    if old_data is not None and old_data.users == 0:
        bpy.data.meshes.remove(old_data)


def muzzle_name(part_name):
    """weapon_left_rifle → muzzle_left_rifle"""
    return MUZZLE_PREFIX + part_name[len("weapon_"):]


def build_part(name):
    """PARTS[name] のメッシュと銃口の空オブジェクトを作る（同じ名前のオブジェクトがあれば作り直す）"""
    definition = PARTS[name]()
    m_name = muzzle_name(name)

    _remove_object(m_name)
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

    muzzle = bpy.data.objects.new(m_name, None)
    muzzle.empty_display_type = 'SINGLE_ARROW'
    muzzle.empty_display_size = 0.3
    muzzle.location = definition["muzzle"]
    muzzle.rotation_euler = (math.radians(90), 0, 0)  # 矢印（+Z）を銃口の向き（-Y）に向ける
    muzzle.parent = obj
    bpy.context.scene.collection.objects.link(muzzle)
    return obj


def build_all():
    return [build_part(n).name for n in PARTS]


def measure(name):
    """形の確認用: 頂点の範囲・大きさ・マテリアルごとの面数・銃口の位置・法線が外向きかを返す"""
    obj = bpy.data.objects[name]
    me = obj.data
    xs, ys, zs = zip(*(v.co for v in me.vertices))
    counts = {}
    for p in me.polygons:
        m = me.materials[p.material_index].name
        counts[m] = counts.get(m, 0) + 1
    muzzle = bpy.data.objects.get(muzzle_name(name))
    # 体積が正なら面の向きがそろって外向き（閉じた形の和）
    volume = 0.0
    for p in me.polygons:
        vs = [me.vertices[i].co for i in p.vertices]
        for k in range(1, len(vs) - 1):
            volume += vs[0].dot(vs[k].cross(vs[k + 1])) / 6.0
    return {
        "min": [round(min(xs), 3), round(min(ys), 3), round(min(zs), 3)],
        "max": [round(max(xs), 3), round(max(ys), 3), round(max(zs), 3)],
        "size": [round(max(xs) - min(xs), 3), round(max(ys) - min(ys), 3), round(max(zs) - min(zs), 3)],
        "faces": counts,
        "muzzle": [round(c, 3) for c in muzzle.location] if muzzle else None,
        "volume": round(volume, 4),
    }
