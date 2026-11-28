"""
共通リグ rig_mech のアニメーション（アクション）を作るスクリプト（#212）

今あるのは待機（idle）だけ。肘・膝を 150 度程度に曲げ、正面から見て腕と脚が
八の字になる「準備姿勢」を 2 フレームの静止ポーズとして書き込む（ロボットなので呼吸の揺れは入れない）。

ボーンの回転（rotation_euler、XYZ、ラジアン）と向きの関係（rig_mech の正面は -Y、機体の左は +X）:
  - X 軸: 負で前（-Y）へ、正で後ろへ振る（腕・脚とも）
  - Z 軸: 正で -X 側（機体の右）へ振る。左のボーンは負、右のボーンは正で外へ開く

使い方（Blender MCP の execute_blender_code など。rig_mech があるシーンで実行する）:
    import sys, importlib
    sys.path.insert(0, r"<リポジトリ>/tools/blender")
    import mech_animations as ma
    importlib.reload(ma)
    result = ma.build_idle()
"""
import math

import bpy
from mathutils import Vector

RIG_NAME = "rig_mech"
# 休止姿勢での足のボーンの向き（head → tail）
REST_FOOT_DIR = Vector((0.0, -0.8, -0.4))


def _deg(x, y=0.0, z=0.0):
    return (math.radians(x), math.radians(y), math.radians(z))


# 待機の準備姿勢。左右対称のボーンは左の値を書き、右は Z を反転して使う
# 脚: 腿を前へ 15 度・外へ 6 度、すねを後ろへ 30 度（膝の角度 150 度）、足首で足裏を水平に戻す
# 腕: 上腕を外へ 15 度・後ろへ 5 度、前腕を前へ 30 度（肘の角度 150 度）
IDLE_POSE = {
    "Spine": _deg(-3),
    "Chest": _deg(-2),
    "Neck": _deg(3),
    "Head": _deg(2),
    "UpperArm_L": _deg(5, 0, -15),
    "LowerArm_L": _deg(-30, 0, 0),
    "Hand_L": _deg(-5, 0, 5),
    "UpperLeg_L": _deg(-15, 0, -6),
    "LowerLeg_L": _deg(30, 0, 0),
    "Foot_L": _deg(-15, 0, 6),
}

IDLE_FRAMES = (0, 1)


def _mirrored(pose):
    out = dict(pose)
    for name, (x, y, z) in pose.items():
        if name.endswith("_L"):
            out[name[:-2] + "_R"] = (x, -y, -z)
    return out


def _fcurves(action):
    """Blender 4.4 以降の階層化アクションと従来のアクションの両方から F カーブを集める"""
    try:
        return list(action.fcurves)
    except AttributeError:
        return [c for layer in action.layers for strip in layer.strips
                for bag in strip.channelbags for c in bag.fcurves]


def _foot_min_z(rig):
    bpy.context.view_layer.update()
    pb = rig.pose.bones
    zs = []
    for n in ("Foot_L", "Foot_R"):
        zs.append((rig.matrix_world @ pb[n].head).z)
        zs.append((rig.matrix_world @ pb[n].tail).z)
    return min(zs)


def build_idle(rig_name=RIG_NAME, action_name="idle"):
    """idle アクションを準備姿勢で書き直し、確認用の数値を返す"""
    rig = bpy.data.objects[rig_name]
    action = bpy.data.actions[action_name]
    pose = _mirrored(IDLE_POSE)
    pb = rig.pose.bones

    ad = rig.animation_data_create()
    prev = ad.action
    ad.action = None

    # 地面からの足の高さが休止姿勢と同じになるよう、腰を下げる量を求める
    for b in pb:
        b.rotation_euler = (0, 0, 0)
        b.location = (0, 0, 0)
    rest_z = _foot_min_z(rig)
    for name, rot in pose.items():
        pb[name].rotation_euler = rot
    drop = _foot_min_z(rig) - rest_z
    # Hips は +Z を向いたボーンなので、ローカルの Y が世界の Z に当たる
    pb["Hips"].location = (0, -drop, 0)
    check_z = _foot_min_z(rig)

    values = {}
    for b in pb:
        for i in range(3):
            values[(f'pose.bones["{b.name}"].rotation_euler', i)] = b.rotation_euler[i]
            values[(f'pose.bones["{b.name}"].location', i)] = b.location[i]

    written = 0
    for c in _fcurves(action):
        key = (c.data_path, c.array_index)
        if key not in values:
            continue
        for k in c.keyframe_points:
            k.co.y = values[key]
            k.handle_left.y = values[key]
            k.handle_right.y = values[key]
        c.update()
        written += 1

    angles = _joint_angles(rig)
    for b in pb:
        b.rotation_euler = (0, 0, 0)
        b.location = (0, 0, 0)
    ad.action = prev
    bpy.context.view_layer.update()
    return {
        "written_fcurves": written,
        "hips_drop": round(drop, 3),
        "foot_min_z": [round(rest_z, 3), round(check_z, 3)],
        "angles": angles,
    }


def _joint_angles(rig):
    """現在のポーズでの肘・膝の角度（度）と、手先・足先の左右の位置を返す"""
    bpy.context.view_layer.update()
    pb = rig.pose.bones
    mw = rig.matrix_world

    def ang(a, b):
        u = (mw @ pb[a].tail) - (mw @ pb[a].head)
        v = (mw @ pb[b].tail) - (mw @ pb[b].head)
        return round(180.0 - math.degrees(u.angle(v)), 1)

    return {
        "elbow_L": ang("UpperArm_L", "LowerArm_L"),
        "elbow_R": ang("UpperArm_R", "LowerArm_R"),
        "knee_L": ang("UpperLeg_L", "LowerLeg_L"),
        "knee_R": ang("UpperLeg_R", "LowerLeg_R"),
        "hand_x": [round((mw @ pb[n].tail).x, 2) for n in ("Hand_L", "Hand_R")],
        "foot_x": [round((mw @ pb[n].head).x, 2) for n in ("Foot_L", "Foot_R")],
        # 足のボーンが休止姿勢の向きからずれた角度（0 に近いほど足裏が水平）
        "foot_tilt_deg": [round(math.degrees(((mw @ pb[n].tail) - (mw @ pb[n].head)).angle(REST_FOOT_DIR)), 1)
                          for n in ("Foot_L", "Foot_R")],
    }
