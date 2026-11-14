---
Issue: #124 8 m の機体に合わせた廃墟の都市のフィールドを新しいシーンで作る（廃墟ビル大・中・小と小物）
実装日: 2026-11-14
変更ファイル:
- PR #125（モデルとプレハブ）
  - tools/blender/environment_parts.py（新規）、tools/blender/export_to_unity.py
  - blender/environment.blend（新規）
  - unity/Assets/Models/Environment/{Buildings,Props}/*.fbx（新規 6）
  - unity/Assets/Prefabs/Environment/{Buildings,Props}/*.prefab（新規 6）
  - docs/blender-unity-pipeline.md
- PR #128（フィールドとミッション）
  - unity/Assets/Scenes/Fields/Field_002/（Field_002_1.unity、Field_002_2.unity、FieldTerrain_002.asset、NavMesh-Field_002.asset。すべて新規）
  - unity/Assets/Scenes/Fields/Resources/FieldSettings/FieldSettings_2.asset（新規）
  - unity/Assets/Resources/MissionSettings/Mission_002.asset（新規）
  - unity/Assets/Prefabs/Environment/Buildings/*.prefab（NavMeshModifierVolume を追加）
  - unity/ProjectSettings/EditorBuildSettings.asset、docs/blender-unity-pipeline.md

実装方針:
- PR を 2 つに分けた（#125 モデル・プレハブ、#128 フィールド・ミッション）。Unity ↔ Blender の切り替えは 2 往復。1 往復目で代表の 2 モデル（小ビル・廃車）だけをエクスポートして、縮尺・向き・当たり判定を Unity で確かめてから残りを出した
- 廃墟ビルは `ruined_building()` で柱・床・窓の穴の空いた外壁を箱で組み、角に近い区画ほど上の階が崩れる・外壁が抜ける・柱が突き出る・足元にがれき、で廃墟にした。引数（大きさ・階数・seed など）でバリアントを増やせる
- 高さ（崩れていない屋上）: 小 8 m、中 28 m（崩れた区画 21 m）、大 98.8 m（同 83.6 m）。小物は実寸（廃車 4.5 m、トラック 9 m、標識 3 m）。小物にもすべて当たり判定を付けた（着手時の決定）
- 当たり判定は空オブジェクト `col_*` として FBX に入れ、Unity のプレハブ（FBX のバリアント）で BoxCollider を付けた。建物は区画ごとの箱（屋上の床の上面まで）
- フィールドは 70 m 間隔・幅 20 m の道路の格子に 8 × 8 区画。広場 3 か所（戦車・ドローン）、大ビル 3 か所、最奥の目標（`BuildGoalObject`）。配置は seed 124 の乱数で eval から生成した
- 敵は既存のユニット ID（砲台 1、戦車 101〜105、ドローン 201〜203）の `FixedUnitSpawner`。移動パターンの経路を評価し、建物・小物にぶつからない位置と向きを決めた

詰まった点・解決策:
- 環境プレハブのルートは FBX のルートで、立てるための回転 (270, 0, 0) を持つ。配置で回転を `Euler(0, yaw, 0)` にしたら横倒しになった（小物とビルの重なりが 77 件と出て気づいた）。`Euler(270, yaw, 0)` に直し、手順書に書いた
- NavMesh のベイク（Recast）は当たり判定の箱の中を埋めないため、高さがエージェント（8 m）を超える中・大ビルの中の地面が NavMesh になった。建物の `col_*` に `NavMeshModifierVolume`（Not Walkable）を付けて解消した（1,107 箱すべてで中の地面が NavMesh から外れたことを確認）
- 四角形に動く戦車 104 の経路が、広場の外周の道路の廃車に当たっていた。広場の範囲（外周の道路の中心線の内側）の小物を除いた
- Play での確認は、ミッション選択と出撃のボタンと同じ `SetPendingMission(2)` / `NextState` を eval から呼んで行った。1 回目の `NextState` の直後はまだシーン遷移中なので、状態を確かめてから次へ進める

残課題・関連Issue:
- 完了条件の「NPC と部隊が NavMesh の上を移動できる」は未確認。NPC を出現させるコード（`NpcManager.CreateNpc` の呼び出し元）がどのフィールドにも無い。#66 の既存の項目に追記した
- 弾が当たるたびに `effectId=11` のプレハブ未設定の警告が出る（もとからある問題）。管理セッションで新しい Issue にする
- 建物は種類ごとに 1 バリアントだけ。見た目の目視確認は人間の確認待ち
---
