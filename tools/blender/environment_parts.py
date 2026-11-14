"""
フィールドの環境オブジェクト（廃墟ビルと小物）のメッシュを作るスクリプト（#124）

座標は Blender の単位（m）。原点が地面での中心、+Z が上、正面は -Y。
1 つのモデルを 1 つのメッシュにまとめ、マテリアルは Mat_Body / Mat_Detail の単色だけを使う。

当たり判定の箱は空オブジェクト col_{名前から env_ を除いたもの}_{番号} としてメッシュの子に置く。
空オブジェクトは立方体の表示（-1〜+1）で、位置が箱の中心、スケールが箱の大きさの半分。
Unity ではこれに BoxCollider（size 2）を付ける。

廃墟ビルは柱・床・外壁（窓の穴の空いた壁）を箱で組み、次の方法で廃墟にする。
  - 崩れた上部: ランダムに選んだ角に近い区画ほど上の階を失う（collapse 階まで）
  - 抜けた窓・壁: 外壁の板を wall_loss の割合で抜き、骨組み（柱と床）を見せる
  - むき出しの骨組み: 隣より高い柱を、崩れた区画の上に spike m まで突き出す
  - がれき: 足元に低い箱を散らす（当たり判定なし）
形の違いは BUILDINGS の引数（大きさ・階数・seed など）で増やす。

使い方（Blender MCP の execute_blender_code など）:
  import sys, importlib
  sys.path.insert(0, r"<リポジトリ>/tools/blender")
  import environment_parts as ep
  importlib.reload(ep)
  ep.build_all()                        # BUILDINGS と PROPS をすべて作り直す
  ep.build("env_building_small_a")      # 1 つだけ作り直す
  ep.measure("env_building_small_a")    # 大きさ・面数・当たり判定の確認
"""

import math
import random

import bpy

MATERIAL_COLORS = {
    "Mat_Body":   (0.50, 0.49, 0.46, 1.0),
    "Mat_Detail": (0.20, 0.20, 0.21, 1.0),
}

COLLIDER_PREFIX = "col_"

# 廃墟ビルの部材の寸法（m）
COLUMN = 0.7    # 柱の太さ
SLAB = 0.4      # 床の厚さ
WALL = 0.3      # 外壁の厚さ
SILL = 1.0      # 窓の下の壁の高さ
LINTEL = 0.6    # 窓の上の壁の高さ
WINDOW = 0.6    # 壁の長さに対する窓の幅の割合
PARAPET = 1.0   # 崩れていない区画の屋上の手すり壁の高さ


# ── 形の定義 ─────────────────────────────────────

def box(center, size, mat="Mat_Body", top=(1.0, 1.0), rot=0.0):
    """箱。top は上面（+Z 側）の X・Y の縮尺、rot は Z 軸まわりの回転（ラジアン）"""
    return {"center": center, "size": size, "mat": mat, "top": top, "rot": rot}


def ruined_building(width, depth, floors, floor_h=3.6, bay=6.0, seed=0,
                    collapse=2, wall_loss=0.25, spike=1.5, rubble=8):
    """廃墟ビル。width × depth の床を bay 程度の区画に分け、区画ごとに残る階数を変える

    返す高さ（崩れていない区画の屋上）は floors × floor_h。
    """
    rng = random.Random(seed)
    nx, ny = max(1, round(width / bay)), max(1, round(depth / bay))
    bx, by = width / nx, depth / ny
    x0, y0 = -width / 2, -depth / 2

    # 区画ごとに残る階数。崩れの中心にする角から遠い区画ほど残る
    cx, cy = rng.choice((0, nx)), rng.choice((0, ny))
    diag = math.hypot(nx, ny)
    top = [[0] * ny for _ in range(nx)]
    for i in range(nx):
        for j in range(ny):
            near = 1.0 - math.hypot(i + 0.5 - cx, j + 0.5 - cy) / diag
            lost = round(collapse * near + rng.uniform(-0.4, 0.4))
            top[i][j] = floors - max(0, min(collapse, lost))

    shapes, colliders = [], []

    # 床（区画ごと。屋上も含む）
    for i in range(nx):
        for j in range(ny):
            for k in range(1, top[i][j] + 1):
                shapes.append(box((x0 + (i + 0.5) * bx, y0 + (j + 0.5) * by, k * floor_h - SLAB / 2),
                                  (bx + COLUMN, by + COLUMN, SLAB), "Mat_Body"))

    # 柱。隣り合う区画のうち最も高いものまで立て、低い区画に面していれば上に突き出す
    for i in range(nx + 1):
        for j in range(ny + 1):
            tops = [top[a][b] for a in (i - 1, i) for b in (j - 1, j) if 0 <= a < nx and 0 <= b < ny]
            h = max(tops) * floor_h
            if min(tops) < floors:
                h += rng.uniform(0.3, 1.0) * spike
            shapes.append(box((x0 + i * bx, y0 + j * by, h / 2), (COLUMN, COLUMN, h), "Mat_Detail"))

    # 外壁（外周の区画の辺ごと、階ごとの板）
    edges = []  # (中心 x, 中心 y, 長さ, X に沿うか, 区画の残る階数)
    for i in range(nx):
        edges.append((x0 + (i + 0.5) * bx, y0, bx, True, top[i][0]))
        edges.append((x0 + (i + 0.5) * bx, -y0, bx, True, top[i][ny - 1]))
    for j in range(ny):
        edges.append((x0, y0 + (j + 0.5) * by, by, False, top[0][j]))
        edges.append((-x0, y0 + (j + 0.5) * by, by, False, top[nx - 1][j]))
    for ex, ey, length, along_x, t in edges:
        span = length - COLUMN
        for f in range(t):
            p = wall_loss * (0.5 + f / max(1, t)) + (0.3 if f == t - 1 and t < floors else 0.0)
            if rng.random() < p:
                continue  # 板が抜けている
            z0 = f * floor_h
            clear = floor_h - SLAB
            broken = rng.random() < 0.15  # 窓の下の壁だけ残る
            parts = [(0.0, span, z0 + SILL / 2, SILL)]
            if not broken:
                pier = span * (1.0 - WINDOW) / 2
                win_h = clear - SILL - LINTEL
                parts.append((0.0, span, z0 + clear - LINTEL / 2, LINTEL))
                parts.append((-(span - pier) / 2, pier, z0 + SILL + win_h / 2, win_h))
                parts.append(((span - pier) / 2, pier, z0 + SILL + win_h / 2, win_h))
            for offset, w, zc, h in parts:
                if along_x:
                    shapes.append(box((ex + offset, ey, zc), (w, WALL, h), "Mat_Body"))
                else:
                    shapes.append(box((ex, ey + offset, zc), (WALL, w, h), "Mat_Body"))
        if t == floors:  # 崩れていない区画の屋上の手すり壁
            zc = floors * floor_h + PARAPET / 2
            size = (span, WALL, PARAPET) if along_x else (WALL, span, PARAPET)
            shapes.append(box((ex, ey, zc), size, "Mat_Body"))

    # がれき（足元の低い箱。当たり判定なし）
    for _ in range(rubble):
        side = rng.randrange(4)
        u = rng.uniform(-0.5, 0.5)
        out = rng.uniform(0.5, 3.0)
        if side < 2:
            px, py = u * width, (depth / 2 + out) * (1 if side == 0 else -1)
        else:
            px, py = (width / 2 + out) * (1 if side == 2 else -1), u * depth
        s = rng.uniform(1.0, 3.0)
        h = rng.uniform(0.4, 1.2)
        shapes.append(box((px, py, h / 2), (s, s * rng.uniform(0.5, 1.0), h), "Mat_Detail",
                          top=(0.6, 0.6), rot=rng.uniform(0, math.pi)))

    # 当たり判定: 区画ごとの箱を、X 方向に同じ高さが続く分だけまとめる
    for j in range(ny):
        i = 0
        while i < nx:
            k = i
            while k + 1 < nx and top[k + 1][j] == top[i][j]:
                k += 1
            h = top[i][j] * floor_h
            w = (k - i + 1) * bx
            colliders.append(((x0 + i * bx + w / 2, y0 + (j + 0.5) * by, h / 2), (w, by, h)))
            i = k + 1

    return {"shapes": shapes, "colliders": colliders}


def _car():
    """廃車（乗用車）: 全長 4.5 m、全幅 1.8 m、全高 1.5 m"""
    shapes = [
        box((0, 0, 0.65), (1.8, 4.5, 0.6), "Mat_Body"),
        box((0, 0.2, 1.225), (1.6, 2.4, 0.55), "Mat_Detail", top=(0.85, 0.7)),
    ]
    for x in (-0.8, 0.8):
        for y in (-1.4, 1.4):
            shapes.append(box((x, y, 0.35), (0.25, 0.7, 0.7), "Mat_Detail"))
    return {"shapes": shapes, "colliders": [((0, 0, 0.75), (1.8, 4.5, 1.5))]}


def _truck():
    """トラック: 全長 9 m、全幅 2.5 m、全高 3 m（運転台と荷台）"""
    shapes = [
        box((0, -3.5, 1.7), (2.4, 2.0, 2.4), "Mat_Body", top=(1.0, 0.8)),     # 運転台 z 0.5〜2.9
        box((0, 1.1, 2.0), (2.5, 6.8, 2.0), "Mat_Body"),                      # 荷台 z 1.0〜3.0
        box((0, 0, 0.75), (1.6, 8.6, 0.5), "Mat_Detail"),                     # 車台
    ]
    for x in (-1.05, 1.05):
        for y in (-3.4, 1.6, 3.1):
            shapes.append(box((x, y, 0.5), (0.35, 1.0, 1.0), "Mat_Detail"))
    colliders = [
        ((0, -3.5, 1.45), (2.4, 2.0, 2.9)),
        ((0, 1.1, 1.5), (2.5, 6.8, 3.0)),
    ]
    return {"shapes": shapes, "colliders": colliders}


def _sign():
    """標識: 高さ 3 m の支柱と、0.9 × 0.7 m の板"""
    shapes = [
        box((0, 0, 1.5), (0.12, 0.12, 3.0), "Mat_Detail"),
        box((0, -0.08, 2.55), (0.9, 0.04, 0.7), "Mat_Body"),
    ]
    colliders = [
        ((0, 0, 1.5), (0.2, 0.2, 3.0)),
        ((0, -0.08, 2.55), (0.9, 0.1, 0.7)),
    ]
    return {"shapes": shapes, "colliders": colliders}


BUILDINGS = {
    # 小: 2 階建て。崩れていない区画の屋上 8 m（機体と同じくらい）
    "env_building_small_a": lambda: ruined_building(12, 10, 2, floor_h=4.0, bay=6.0, seed=11,
                                                    collapse=1, wall_loss=0.3, spike=0.8, rubble=5),
    # 中: 8 階建て。屋上 28 m、崩れた区画でも 21 m
    "env_building_medium_a": lambda: ruined_building(24, 18, 8, floor_h=3.5, bay=6.0, seed=23,
                                                     collapse=2, wall_loss=0.3, spike=1.5, rubble=8),
    # 大: 26 階建て。屋上 98.8 m のランドマーク
    "env_building_large_a": lambda: ruined_building(36, 28.8, 26, floor_h=3.8, bay=7.2, seed=37,
                                                    collapse=4, wall_loss=0.25, spike=2.5, rubble=12),
}

PROPS = {
    "env_prop_car": _car,
    "env_prop_truck": _truck,
    "env_prop_sign": _sign,
}

MODELS = {**BUILDINGS, **PROPS}


# ── 作成 ───────────────────────────────────────

def get_material(name):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
    color = MATERIAL_COLORS[name]
    mat.diffuse_color = color
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = color
    return mat


def _box_geometry(s, offset):
    cx, cy, cz = s["center"]
    sx, sy, sz = (v * 0.5 for v in s["size"])
    tx, ty = s["top"]
    c, sn = math.cos(s["rot"]), math.sin(s["rot"])
    verts = []
    for z, kx, ky in ((cz - sz, 1.0, 1.0), (cz + sz, tx, ty)):
        for x, y in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
            lx, ly = x * sx * kx, y * sy * ky
            verts.append((cx + lx * c - ly * sn, cy + lx * sn + ly * c, z))
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


def _collider_prefix(name):
    return COLLIDER_PREFIX + name[len("env_"):] + "_"


def _remove_object(name):
    old = bpy.data.objects.get(name)
    if old is None:
        return
    for child in list(old.children):
        _remove_object(child.name)
    old_data = old.data
    bpy.data.objects.remove(old, do_unlink=True)
    if old_data is not None and old_data.users == 0:
        bpy.data.meshes.remove(old_data)


def build(name):
    """MODELS[name] のメッシュと当たり判定の空オブジェクトを作る（同じ名前があれば作り直す）"""
    definition = MODELS[name]()
    _remove_object(name)

    verts, faces, face_mats, mat_names = [], [], [], []
    for s in definition["shapes"]:
        v, f = _box_geometry(s, len(verts))
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

    prefix = _collider_prefix(name)
    for n, (center, size) in enumerate(definition["colliders"]):
        col_name = f"{prefix}{n:02d}"
        _remove_object(col_name)
        col = bpy.data.objects.new(col_name, None)
        col.empty_display_type = 'CUBE'
        col.empty_display_size = 1.0
        col.location = center
        col.scale = tuple(v / 2 for v in size)
        col.parent = obj
        bpy.context.scene.collection.objects.link(col)
    return obj


def build_all():
    return [build(n).name for n in MODELS]


def measure(name):
    """形の確認用: 頂点の範囲・三角形の数・マテリアルごとの面数・当たり判定の箱の数と範囲を返す"""
    obj = bpy.data.objects[name]
    xs, ys, zs = zip(*(v.co for v in obj.data.vertices))
    counts = {}
    for p in obj.data.polygons:
        m = obj.data.materials[p.material_index].name
        counts[m] = counts.get(m, 0) + 1
    cols = [c for c in obj.children if c.name.startswith(COLLIDER_PREFIX)]
    col_top = [c.location.z + c.scale.z for c in cols]
    return {
        "min": [round(min(xs), 2), round(min(ys), 2), round(min(zs), 2)],
        "max": [round(max(xs), 2), round(max(ys), 2), round(max(zs), 2)],
        "tris": sum(len(p.vertices) - 2 for p in obj.data.polygons),
        "faces": counts,
        "colliders": len(cols),
        "collider_top": [round(min(col_top), 2), round(max(col_top), 2)] if cols else None,
    }
