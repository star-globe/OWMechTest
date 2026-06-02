"""
Blender → Unity FBXエクスポートスクリプト

使い方:
1. Blenderの Text Editor でこのファイルを開く
2. EXPORT_BASE_PATH を自分の環境に合わせて編集する
3. エクスポートしたいオブジェクトを選択して Run Script

命名規約に従ったファイル名でエクスポートされます:
  body_standard.fbx / arm_left_blade.fbx など
"""

import bpy
import os

# ====== 設定 ======
# プロジェクトルートからの相対パス（絶対パスでも可）
EXPORT_BASE_PATH = "/path/to/OWMechTest/unity/Assets/Models"

CATEGORY_MAP = {
    "BODY":   "Parts/Body",
    "ARM":    "Parts/Arms",
    "LEG":    "Parts/Legs",
    "HEAD":   "Parts/Head",
    "WEAPON": "Weapons",
}
# ==================

def get_export_path(obj_name: str) -> str:
    """オブジェクト名のプレフィックスからエクスポート先を決定する"""
    upper = obj_name.upper()
    for key, subdir in CATEGORY_MAP.items():
        if upper.startswith(key):
            return os.path.join(EXPORT_BASE_PATH, subdir, obj_name.lower() + ".fbx")
    return os.path.join(EXPORT_BASE_PATH, obj_name.lower() + ".fbx")


def export_selected():
    selected = bpy.context.selected_objects
    if not selected:
        print("[export_to_unity] オブジェクトが選択されていません")
        return

    for obj in selected:
        path = get_export_path(obj.name)
        os.makedirs(os.path.dirname(path), exist_ok=True)

        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj

        bpy.ops.export_scene.fbx(
            filepath=path,
            use_selection=True,
            global_scale=0.01,
            apply_scale_options='FBX_SCALE_ALL',
            axis_forward='-Z',
            axis_up='Y',
            object_types={'ARMATURE', 'MESH'},
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
        print(f"[export_to_unity] エクスポート完了: {path}")


export_selected()
