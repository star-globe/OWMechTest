---
Issue: #183 ミサイルランチャーとミサイルのモデル・エフェクトを作る
実装日: 2026-11-24
変更ファイル:
- tools/blender/weapon_parts.py（ランチャー `weapon_right_missilepod` とミサイル `bullet_missile` の定義、複数の発射口 `muzzles` に対応）
- tools/blender/export_to_unity.py（`bullet_` のカテゴリ。出力先 `Models/Weapons/Bullets/`）
- blender/weapon.blend
- docs/blender-unity-pipeline.md（命名と #183 の節）、docs/effects-plan.md（4 つのエフェクト）
- unity/Assets/Models/Weapons/weapon_right_missilepod.fbx、unity/Assets/Models/Weapons/Bullets/bullet_missile.fbx（新規）
- unity/Assets/Prefabs/Parts/Sub/MissileLauncher_001_part.prefab、unity/Assets/Prefabs/Bullets/Missile/Missile.prefab
- unity/Assets/Prefabs/Effect/MissileLaunch|MissileHit|MissileShotDown.prefab、unity/Assets/Resources/EffectSettings/（同名の設定。新規）
- unity/Assets/Prefabs/Bullets/Resources/BulletSettings/Missile_001.asset（エフェクト ID 14 / 15 / 16）
- unity/Assets/Prefabs/Mech/MechBody.prefab、unity/Assets/Resources/PlayerSettings/PlayerNPC.prefab（`Core/SubAttach` の位置だけ）
- unity/Assets/Script/EffectCheck/MissileEffectTester.cs（新規）、PartEffectTester.cs、EffectCheckCamera.cs
- unity/Assets/Scenes/Debug/EffectCheck.unity、unity/Assets/Prefabs/UI/EffectCheck/EffectCheckPanel.prefab、MissileEffectCheckPanel.prefab（新規）

実装方針:
- #192 の仮モデル（キューブ・カプセル）の子 `Model` だけを差し替え、ルートのコンポーネントと発射口の配列は残した
- モデルは `weapon_parts.py` のスクリプトで作った。ランチャーは幅 0.8・全長 1.3・高さ 0.6 m の箱に 2×2 の発射筒（φ0.24）、ミサイルは全長 1.2 m・胴体 φ0.2。Blender の発射口の空オブジェクトの位置に `Muzzle_1`〜`4` を合わせた
- エフェクトは既存のマズルフラッシュ・小爆発・撃破の爆発を元に、マテリアル（`Mat_EffectAdditive` / `Mat_EffectSmoke`）を使い回してオレンジ系にした。大きさは「小爆発 < 命中 < 機体の撃破」になるようにした
- 飛行中の噴射は `Missile.prefab` の子 `Exhaust`（ループするパーティクル）と、煙のマテリアルにした `TrailRenderer` で表した
- エフェクト確認シーンに `MissileEffectTester`（`PartEffectTester` を継承）を置き、発射・的の出し入れ・撃ち返しによる撃墜をボタンで確かめられるようにした。ブースターとミサイルでカメラの向け先をボタンで切り替える

詰まった点・解決策:
- 最初のセッションで Blender MCP が開始時に接続できず、そのセッションでは再接続できなかった。Issue に引き継ぎをコメントし、新しいセッションで続けた
- 作業用セッションから管理用セッションへの `send_message` が「信頼済みの端末の証明が必要」で失敗し続けた。報告は人間経由で受け取った
- Customize のデバッグ表示で、ランチャーが胴体と右肩の装甲にめり込んでいた（頂点 18 個が箱の内側）。`Core/SubAttach` を (0.9, 1.0, -0.4) → (0.9, 1.4, -0.4) に上げて重なりを 0 にした。PlayerNPC は MechBody を入れ子にしていないので、両方のプレハブを直した
- プールから再利用したミサイルで前の噴射の炎が残らないかを確かめ、発射直後のパーティクル数が 0 であることを確認した

残課題・関連Issue:
- Missile_001 の攻撃力（今は 5）を 500 程度に見直す（#116 の式に合わせる。#66 に追加済み）
- 手元のマシンから管理用セッションへの `send_message` が認証エラーで届かない原因は未確認
---
