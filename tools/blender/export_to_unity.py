"""
Blender → Unity FBXエクスポートスクリプト

命名規約（docs/blender-unity-pipeline.md）に従ったファイル名で、
unity/Assets/Models/ 以下のカテゴリ別フォルダへ FBX をエクスポートする。
  body_standard → Parts/Body/body_standard.fbx
  arm_left_blade → Parts/Arms/arm_left_blade.fbx
  booster_standard → Parts/Booster/booster_standard.fbx
  rig_mech       → Rig/rig_mech.fbx（アニメーションを含めるのは rig_ だけ）
  env_building_small_a → Environment/Buildings/env_building_small_a.fbx
  env_prop_car   → Environment/Props/env_prop_car.fbx
スキンメッシュは、Armature モディファイアが参照するアーマチュアも同じ FBX に含める。
子孫の空オブジェクト（ノズルの出口などの目印）も FBX に含める。

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

使い方 C: コマンドライン（GUI を開かずに実行。ラッパーは tools/blender/export.sh）
  blender --background --factory-startup <file.blend> --python export_to_unity.py -- [オプション]
    --objects <名前...>   対象のオブジェクト（省略時は .blend に保存された選択状態）
    --all                 カテゴリ（body / arm / ...）で始まる名前のオブジェクトをすべて対象にする
    --validate-only       検証だけ行う
    --dry-run             出力先だけ確認する
    --base-path <path>    エクスポート先の Models フォルダ
    --json-out <path>     結果の JSON をファイルにも書く
  結果は "OWMECH_RESULT:" で始まる 1 行の JSON で標準出力に出す。
  終了コード: 0 = 成功、2 = 規約違反（error）があり何もしなかった、1 = その他のエラー

エクスポート先（Models フォルダ）は次の順で決まる:
  1. 関数の引数 base_path
  2. 環境変数 OWMECH_UNITY_MODELS
  3. このスクリプトの場所から求めたリポジトリの unity/Assets/Models
"""

import argparse
import json
import os
import re
import sys

import bpy

ENV_BASE_PATH = "OWMECH_UNITY_MODELS"

# 名前の先頭 → Models 以下のフォルダ
CATEGORY_MAP = {
    "body":   "Parts/Body",
    "arm":    "Parts/Arms",
    "leg":    "Parts/Legs",
    "head":   "Parts/Head",
    "booster": "Parts/Booster",
    "weapon": "Weapons",
    "rig":    "Rig",
    # 環境オブジェクト（#124）。カテゴリは名前の先頭 2 語
    "env_building": "Environment/Buildings",
    "env_prop":     "Environment/Props",
}

# アニメーションを書き出すカテゴリ（パーツの FBX には入れない。アニメーションは共通リグの FBX にまとめる）
ANIMATION_CATEGORIES = {"rig"}

# docs/blender-unity-pipeline.md の「FBXファイル名」
_VARIANT = r"[a-z0-9]+(?:_[a-z0-9]+)*"
NAME_PATTERNS = {
    "body":   re.compile(rf"^body_{_VARIANT}$"),
    "arm":    re.compile(rf"^arm_(?:left|right)_{_VARIANT}$"),
    "leg":    re.compile(rf"^leg_(?:left|right)_{_VARIANT}$"),
    "head":   re.compile(rf"^head_{_VARIANT}$"),
    "booster": re.compile(rf"^booster_{_VARIANT}$"),
    "weapon": re.compile(rf"^weapon_{_VARIANT}$"),
    "rig":    re.compile(rf"^rig_{_VARIANT}$"),
    "env_building": re.compile(rf"^env_building_(?:small|medium|large)_{_VARIANT}$"),
    "env_prop":     re.compile(rf"^env_prop_{_VARIANT}$"),
}

# docs/blender-unity-pipeline.md の「マテリアルスロット名」
ALLOWED_MATERIALS = ("Mat_Body", "Mat_Detail", "Mat_Visor", "Mat_Thruster")

EXPORT_OBJECT_TYPES = {"ARMATURE", "MESH"}

# FBX に書き出す種類。空オブジェクトは、エフェクトの発生位置（ブースターのノズルの出口など）の目印として
# メッシュの子に置いたものを Unity の Transform として書き出す
FBX_OBJECT_TYPES = EXPORT_OBJECT_TYPES | {"EMPTY"}

FBX_SETTINGS = dict(
    use_selection=True,
    # FBX_SCALE_ALL と組み合わせると Blender の 1 m が Unity の 1 になる（0.01 だと 1/100 になる。#68 で確認）
    global_scale=1.0,
    apply_scale_options='FBX_SCALE_ALL',
    axis_forward='-Z',
    axis_up='Y',
    object_types=FBX_OBJECT_TYPES,
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
    """名前の先頭からカテゴリ（body / arm / env_building / ...）を返す。該当しなければ None

    先頭 2 語のカテゴリ（env_building など）を先に調べる。
    """
    words = obj_name.lower().split("_")
    for n in (2, 1):
        head = "_".join(words[:n])
        if len(words) >= n and head in CATEGORY_MAP:
            return head
    return None


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


def _skinning_armatures(obj):
    """メッシュの Armature モディファイアが参照するアーマチュアを返す"""
    if obj.type != "MESH":
        return []
    return [m.object for m in obj.modifiers if m.type == 'ARMATURE' and m.object is not None]


def get_export_tree(root):
    """FBX に含めるオブジェクト（root とその子孫、スキニングに使うアーマチュア）を返す

    パーツのメッシュは共通リグのアーマチュアでスキニングするが、アーマチュアはパーツの子ではないので、
    ここで加えないとボーンの無い FBX になる。
    """
    tree = list(_iter_tree(root))
    for o in list(tree):
        for arm in _skinning_armatures(o):
            if arm not in tree:
                tree.append(arm)
    return tree


def get_fbx_settings(root):
    """root のカテゴリに合わせた FBX の設定を返す（アニメーションはリグの FBX だけに入れる）"""
    settings = dict(FBX_SETTINGS)
    settings["bake_anim"] = get_category(root.name) in ANIMATION_CATEGORIES
    return settings


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

        tree = get_export_tree(root)
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
        tree = get_export_tree(root)
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
            for o in get_export_tree(root):
                o.select_set(True)
            view_layer.objects.active = root
            bpy.ops.export_scene.fbx(filepath=result["path"], **get_fbx_settings(root))
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


# ── コマンドライン ─────────────────────────────────

RESULT_PREFIX = "OWMECH_RESULT:"
EXIT_OK = 0
EXIT_ERROR = 1
EXIT_VIOLATION = 2


def get_all_targets():
    """名前がカテゴリで始まるオブジェクトをすべて返す（カメラやライトなどは含めない）"""
    return [o for o in bpy.data.objects if get_category(o.name) is not None]


def _parse_cli_args(argv):
    parser = argparse.ArgumentParser(
        prog="export_to_unity.py",
        description="命名規約を検証して FBX を unity/Assets/Models にエクスポートする")
    target = parser.add_mutually_exclusive_group()
    target.add_argument("--objects", nargs="+", metavar="NAME", help="対象のオブジェクト名（省略時は選択中のオブジェクト）")
    target.add_argument("--all", action="store_true", help="カテゴリで始まる名前のオブジェクトをすべて対象にする")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--validate-only", action="store_true", help="検証だけ行う")
    mode.add_argument("--dry-run", action="store_true", help="エクスポートせず出力先だけ確認する")
    parser.add_argument("--base-path", help="エクスポート先の Models フォルダ")
    parser.add_argument("--json-out", help="結果の JSON を書き出すファイル")
    return parser.parse_args(argv)


def run_cli(argv):
    """コマンドラインの処理。結果の dict と終了コードを返す"""
    args = _parse_cli_args(argv)
    mode = "validate" if args.validate_only else "dry_run" if args.dry_run else "export"
    result = {"blend_file": bpy.data.filepath, "mode": mode, "ok": False,
              "issues": [], "exports": [], "error": None}
    try:
        objects = args.objects if args.objects else get_all_targets() if args.all else None
        roots = get_export_roots(objects)
        if not roots:
            raise ValueError("対象のオブジェクトがありません（--objects か --all で指定するか、選択して保存してください）")
        result["issues"] = validate_objects(roots)
        if any(i["level"] == "error" for i in result["issues"]):
            result["error"] = "命名規約の違反があるため処理しません"
            return result, EXIT_VIOLATION
        if mode != "validate":
            result["exports"] = export_objects(roots, base_path=args.base_path,
                                               validate=False, dry_run=(mode == "dry_run"))
        result["ok"] = True
        return result, EXIT_OK
    except Exception as e:
        result["error"] = f"{type(e).__name__}: {e}"
        return result, EXIT_ERROR
    finally:
        if args.json_out:
            with open(args.json_out, "w", encoding="utf-8") as f:
                json.dump(result, f, ensure_ascii=False, indent=2)


def _main_cli():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    try:
        result, code = run_cli(argv)
    except SystemExit as e:
        # argparse の --help や引数の誤り
        code = e.code if isinstance(e.code, int) else EXIT_ERROR
        sys.stdout.flush()
        sys.exit(code if code == 0 else EXIT_ERROR)
    print(RESULT_PREFIX + json.dumps(result, ensure_ascii=False))
    sys.stdout.flush()
    sys.exit(code)


def _main_text_editor():
    # Text Editor の Run Script から実行したとき
    try:
        for issue in validate_objects():
            if issue["level"] == "warning":
                print(f"[export_to_unity] {format_issues([issue])}")
        export_selected()
    except ValueError as e:
        print(f"[export_to_unity] {e}")


if __name__ == "__main__":
    if bpy.app.background:
        _main_cli()
    else:
        _main_text_editor()
