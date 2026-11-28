---
Issue: #225 起伏のある砂漠と傾いた建造物のフィールドを追加する
実装日: 2026-11-28
変更ファイル:
- PR #247（モデルとプレハブ）
  - tools/blender/environment_parts.py（塔・床の板・柱の定義を追加）、blender/environment.blend
  - unity/Assets/Models/Environment/Buildings/env_building_medium_tower_a.fbx、Props/env_prop_slab_a.fbx、Props/env_prop_pillar_a.fbx（新規）
  - unity/Assets/Prefabs/Environment/Buildings/Building_Medium_Tower_A.prefab、Props/Prop_Slab_A.prefab、Props/Prop_Pillar_A.prefab（新規）
  - docs/blender-unity-pipeline.md
- PR #248（フィールドとミッション）
  - unity/Assets/Scenes/Fields/Field_003/（Field_003_1.unity、Field_003_2.unity、FieldTerrain_003.asset、NavMesh-Field_003.asset、SandLayer.terrainlayer、Sand_Albedo.png。すべて新規）
  - unity/Assets/Scenes/Fields/Resources/FieldSettings/FieldSettings_3.asset、unity/Assets/Resources/MissionSettings/Mission_003.asset（新規）
  - unity/ProjectSettings/EditorBuildSettings.asset

実装方針:
- #124 と同じく PR を 2 つに分けた（#247 はモデルとプレハブ、#248 はフィールドとミッション）。Unity ↔ Blender の切り替えは 1 往復で済んだ。箱で組むモデルは #124 で縮尺と向きを確かめ済みなので、3 種類をまとめて作ってエクスポートした
- 新しいモデル（塔・崩れ落ちた床の板・倒れた柱）は傾けずに作り、傾きはシーンでの配置の回転で付けた。当たり判定の `col_*` も一緒に傾く。回転は `Euler(0, yaw, 0) * Euler(傾き, 0, 0) * Euler(270, 0, 0)` の順にかけた。マテリアルは #229 の Mat_Main / Mat_Sub
- 地形は 1200 m 四方で、高さは eval から `TerrainData.SetHeights` で書き込んだ
  - 砂丘は、風上がなだらかで風下が急な形の波に、Perlin ノイズの大きな起伏を重ねた
  - 外周は持ち上げて囲んだ
- #213・#214 の確認用に、スポーン地点の周りに角度を決めた斜面を置いた
  - 西: 地形の土手 5 本（10/20/30/40/50°、高さ 12 m）
  - 東: 床の板を 10/20/30/40° に傾けて並べた。下の端で板の上面が地面と同じ高さになるよう、`Collider.Raycast` で位置を合わせた
- 敵・NPC は Field_002_2 のオブジェクトを複製して作った。NPC は親ごと複製して、目的地の参照を保った
  - 戦車の位置と向きは、移動パターン（`UnitMovePatternSettings.CreatePattern()`）の経路を評価して探した。条件は、高低差が出発地点の ±14 m 以内、建造物から 35 m 以上離れること、経路に斜面が含まれること
- NavMeshSurface は Field_002 から設定を複製した。ベイクは eval の `BuildNavMesh()` で行い、結果を `NavMesh-Field_003.asset` として保存した

詰まった点・解決策:
- 戦車の地面を探すレイ（`UnitMover.groundRayHeight` = 20 m）は、経路の高さから 20 m 上から撃つ。地面がそれより高いと埋まるので、経路の高低差を ±14 m 以内に収めた
- 砲台が戦車の経路から 11 m・19 m の位置になった。戦車の経路から 30 m 以上離れた、範囲の中で最も高い地点へ移した
- unity-mcp の `bake_navmesh_surfaces` は NavMeshSurface に対応していなかった（not_implemented）。eval で `BuildNavMesh()` を呼び、`AssetDatabase.CreateAsset` で保存した
- `EditorBuildSettings.scenes` を変えても、`AssetDatabase.SaveAssets()` を呼ぶまではファイルに書き出されなかった
- Play での確認中に、プレイヤーを塔の屋上に置いている間に撃破されて Result へ進んだ。撮影は出撃し直してから行った

残課題・関連Issue:
- #213・#214: 斜面の試験場で歩行とブーストを確かめる。入力での操作による確認はまだしていない
- 戦車（`UnitMover`）は斜面でも車体が水平のまま。必要なら別の Issue にする
- プレイヤーは 40° の地形の斜面で滑り落ちる（30° の床の板では止まる）。#213・#214 で斜面の扱いを決めるときの参考
- #226・#227（地形のフィールド）は、同じ方法（eval の SetHeights、経路の評価による戦車の配置）で作れる
---
