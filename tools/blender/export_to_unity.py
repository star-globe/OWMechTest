"""
Blender → Unity FBXエクスポートスクリプト

命名規約（docs/blender-unity-pipeline.md）に従ったファイル名で、
unity/Assets/Models/ 以下のカテゴリ別フォルダへ FBX をエクスポートする。
  body_standard → Parts/Body/body_standard.fbx
  arm_left_blade → Parts/Arms/arm_left_blade.fbx

使い方 A: Blender の Text Editor
  1. このファイルを開く
  2. エクスポートしたいオブジェクトを選択して Run Script
     （規約違反があればエクスポートせず、System Console に内容を表示する）

使い方 B: Python から（Blender MCP の execute_blender_code など）
  import sys, importlib
  sys.path.insert(0, r"<リポジトリ>/tools/blender")
  import export_to_unity as ex
  importlib.reload(ex)                      # スクリプトを編集した後に読み直す場合
  ex.validate_objects()                     # 選択中のオブジェクトを検証（問題の一覧を返す）
  ex.export_selected(dry_run=True)          # 出力先だけ確認
  ex.export_selected()                      # エクスポート
  ex.export_objects(["body_standard"])      # 名前を指定してエクスポート

エクスポート先（Models フォルダ）は次の順で決まる:
  1. 関数の引数 base_path
  2. 環境変数 OWMECH_UNITY_MODELS
  3. このスクリプトの場所から求めたリポジトリの unity/Assets/Models
"""

import os
import re

import bpy

ENV_BASE_PATH = "OWMECH_UNITY_MODELS"

# 名前の先頭 → Models 以下のフォルダ
CATEGORY_MAP = {
    "body":   "Parts/Body",
    "arm":    "Parts/Arms",
    "leg":    "Parts/Legs",
    "head":   "Parts/Head",
    "weapon": "Weapons",
}

# docs/blender-unity-pipeline.md の「FBXファイル名」
_VARIANT = r"[a-z0-9]+(?:_[a-z0-9]+)*"
NAME_PATTERNS = {
    "body":   re.compile(rf"^body_{_VARIANT}$"),
    "arm":    re.compile(rf"^arm_(?:left|right)_{_VARIANT}$"),
    "leg":    re.compile(rf"^leg_(?:left|right)_{_VARIANT}$"),
    "head":   re.compile(rf"^head_{_VARIANT}$"),
    "weapon": re.compile(rf"^weapon_{_VARIANT}$"),
}

# docs/blender-unity-pipeline.md の「マテリアルスロット名」
ALLOWED_MATERIALS = ("Mat_Body", "Mat_Detail", "Mat_Visor", "Mat_Thruster")

EXPORT_OBJECT_TYPES = {"ARMATURE", "MESH"}

FBX_SETTINGS = dict(
    use_selection=True,
    global_scale=0.01,
    apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z',
    axis_up='Y',
    object_types=EXPORT_OBJECT_TYPES,
    use_mesh_modifiers=True,
    mesh_smooth_type='FACE',
    primary_bone_axis='Y',
    secondary_bone_axis='X',
    armature_nodetype='NULL',
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_step=1.0,
    bake_anim_simplify_factor=1.0,
    add_leaf_bones=False,
)


# ── パス ─────────────────────────────────────────

MODELS_SUBPATH = os.path.join("unity", "Assets", "Models")


def _find_models_dir(start):
    """start から親フォルダをたどり、unity/Assets/Models を持つリポジトリを探す"""
    d = os.path.abspath(start)
    while True:
        candidate = os.path.join(d, MODELS_SUBPATH)
        if os.path.isdir(candidate):
            return candidate
        parent = os.path.dirname(d)
        if parent == d:
            return None
        d = parent


def resolve_base_path(base_path=None):
    """エクスポート先の Models フォルダを返す（引数 → 環境変数 → リポジトリ内の既定の場所）"""
    if base_path:
        return os.path.abspath(base_path)
    env = os.environ.get(ENV_BASE_PATH)
    if env:
        return os.path.abspath(env)

    # Text Editor から実行すると __file__ が実際の場所にならないことがあるので、
    # テキストのファイルパスと .blend の場所も候補にする
    starts = [os.path.dirname(os.path.abspath(__file__))]
    starts += [os.path.dirname(bpy.path.abspath(t.filepath)) for t in bpy.data.texts
               if t.filepath and os.path.basename(t.filepath) == "export_to_unity.py"]
    if bpy.data.filepath:
        starts.append(os.path.dirname(bpy.data.filepath))
    for start in starts:
        found = _find_models_dir(start)
        if found:
            return found
    raise ValueError(
        f"unity/Assets/Models が見つかりません。base_path 引数か環境変数 {ENV_BASE_PATH} で指定してください")


def get_category(obj_name):
    """名前の先頭からカテゴリ（body / arm / ...）を返す。該当しなければ None"""
    head = obj_name.lower().split("_", 1)[0]
    return head if head in CATEGORY_MAP else None


def get_export_path(obj_name, base_path=None):
    """オブジェクト名からエクスポート先の FBX のパスを決める"""
    base = resolve_base_path(base_path)
    category = get_category(obj_name)
    filename = obj_name.lower() + ".fbx"
    if category is None:
        return os.path.join(base, filename)
    return os.path.join(base, *CATEGORY_MAP[category].split("/"), filename)


# ── 検証 ─────────────────────────────────────────

def _iter_tree(obj):
    yield obj
    for child in obj.children:
        yield from _iter_tree(child)


def _resolve_objects(objects):
    """None → 選択中のオブジェクト。名前の文字列も受け付ける"""
    if objects is None:
        objects = list(bpy.context.selected_objects)
    resolved = []
    for o in objects:
        if isinstance(o, str):
            found = bpy.data.objects.get(o)
            if found is None:
                raise ValueError(f"オブジェクトが見つかりません: {o}")
            o = found
        resolved.append(o)
    return resolved


def get_export_roots(objects=None):
    """エクスポートの単位になるオブジェクトを返す。祖先も指定されているものは祖先の FBX に含まれるので除く"""
    objs = _resolve_objects(objects)
    selected = set(objs)
    roots = []
    for o in objs:
        parent = o.parent
        while parent is not None and parent not in selected:
            parent = parent.parent
        if parent is None:
            roots.append(o)
    return roots


def _issue(obj, level, message):
    return {"object": obj.name, "level": level, "message": message}


def validate_objects(objects=None):
    """命名規約などを検証し、問題の一覧を返す（空なら問題なし）

    各要素は {"object": 名前, "level": "error" | "warning", "message": 内容}。
    error があると export_objects はエクスポートしない。
    """
    issues = []
    for root in get_export_roots(objects):
        category = get_category(root.name)
        if category is None:
            issues.append(_issue(root, "error",
                f"名前の先頭がカテゴリ（{', '.join(CATEGORY_MAP)}）と一致しない"))
        elif not NAME_PATTERNS[category].match(root.name.lower()):
            issues.append(_issue(root, "error",
                f"名前が規約（{NAME_PATTERNS[category].pattern}）に合わない"
                "（Blender が付ける .001 などの連番も不可）"))
        if root.name != root.name.lower():
            issues.append(_issue(root, "warning", "名前に大文字が含まれる（ファイル名は小文字になる）"))

        tree = list(_iter_tree(root))
        if not any(o.type in EXPORT_OBJECT_TYPES for o in tree):
            issues.append(_issue(root, "error", "メッシュもアーマチュアも含まれない"))

        for o in tree:
            if o.type not in EXPORT_OBJECT_TYPES:
                continue
            if any(s < 0 for s in o.scale):
                issues.append(_issue(o, "warning", f"スケールが負（{tuple(o.scale)}）。法線が反転するおそれがある"))
            if o.type != "MESH":
                continue
            if len(o.material_slots) == 0:
                issues.append(_issue(o, "warning", "マテリアルスロットが無い"))
            for i, slot in enumerate(o.material_slots):
                if slot.material is None:
                    issues.append(_issue(o, "error", f"マテリアルスロット {i} が空"))
                elif slot.material.name not in ALLOWED_MATERIALS:
                    issues.append(_issue(o, "error",
                        f"マテリアル名 '{slot.material.name}' が規約（{', '.join(ALLOWED_MATERIALS)}）に無い"))
    return issues


def format_issues(issues):
    return "\n".join(f"[{i['level']}] {i['object']}: {i['message']}" for i in issues)


# ── エクスポート ───────────────────────────────────

def export_objects(objects=None, base_path=None, validate=True, dry_run=False):
    """オブジェクトを1つずつ（子孫を含めて）FBX にエクスポートし、結果の一覧を返す

    objects: オブジェクトか名前のリスト。None なら選択中のオブジェクト
    validate: True なら先に validate_objects を実行し、error があれば何もせず ValueError を出す
    dry_run: True ならエクスポートせず、出力先だけを返す
    戻り値: [{"object": 名前, "path": FBX のパス, "objects": 含めたオブジェクト名, "exported": bool}]
    """
    roots = get_export_roots(objects)
    if not roots:
        raise ValueError("エクスポートするオブジェクトがありません（選択されていません）")

    if validate:
        errors = [i for i in validate_objects(roots) if i["level"] == "error"]
        if errors:
            raise ValueError("命名規約の違反があるためエクスポートしません:\n" + format_issues(errors))

    results = []
    for root in roots:
        tree = list(_iter_tree(root))
        results.append({
            "object": root.name,
            "path": get_export_path(root.name, base_path),
            "objects": [o.name for o in tree],
            "exported": False,
        })
    if dry_run:
        return results

    view_layer = bpy.context.view_layer
    prev_active = view_layer.objects.active
    prev_selected = list(bpy.context.selected_objects)
    if bpy.context.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')

    try:
        for root, result in zip(roots, results):
            os.makedirs(os.path.dirname(result["path"]), exist_ok=True)
            bpy.ops.object.select_all(action='DESELECT')
            for o in _iter_tree(root):
                o.select_set(True)
            view_layer.objects.active = root
            bpy.ops.export_scene.fbx(filepath=result["path"], **FBX_SETTINGS)
            result["exported"] = True
            print(f"[export_to_unity] エクスポート完了: {result['path']}")
    finally:
        # 選択状態を元に戻す
        bpy.ops.object.select_all(action='DESELECT')
        for o in prev_selected:
            if o.name in bpy.data.objects:
                o.select_set(True)
        view_layer.objects.active = prev_active
    return results


def export_selected(base_path=None, validate=True, dry_run=False):
    """選択中のオブジェクトをエクスポートする（export_objects の簡易版）"""
    return export_objects(None, base_path=base_path, validate=validate, dry_run=dry_run)


if __name__ == "__main__":
    # Text Editor の Run Script から実行したとき
    try:
        for issue in validate_objects():
            if issue["level"] == "warning":
                print(f"[export_to_unity] {format_issues([issue])}")
        export_selected()
    except ValueError as e:
        print(f"[export_to_unity] {e}")
