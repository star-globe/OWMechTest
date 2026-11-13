---
Issue: #90 プレイヤー機体を 8 m 基準にする倍率（worldSizeRate）を決め、速度系のパラメーターとカメラの距離を調整する
実装日: 2026-11-13
変更ファイル:
- unity/Assets/Script/Master/GlobalParamSettings.cs
- unity/Assets/Resources/GlobalParamSettings.asset
- unity/Assets/Resources/PlayerSettings/PlayerControllerSettings.asset
- unity/Assets/Resources/PlayerSettings/Player.prefab、PlayerNPC.prefab
- unity/Assets/Resources/UnitSettings/Tank/TankParameterSettings.asset、TankParameterSettings_NoFire.asset
- unity/Assets/Resources/UnitSettings/Drone/DroneParameterSettings.asset、DroneParameterSettings_NoFire.asset
- unity/Assets/Prefabs/Bullets/Resources/BulletSettings/Bullet_001.asset
- docs/spec/技術仕様.md

実装方針:
- PR は #102 の 1 つ
- 組み立て後の機体（Standard 構成）を Bounds で測ると、脚の下端 約 0 〜 頭頂 8.05 単位（Light 8.2 / Heavy 7.55）。カプセルコライダーの高さも 8。Blender で 1 単位 = 1 m・全高 8 m で作っているため、倍率は 8 / 8.05 ≈ 1 として `worldSizeRate` を 5 → 1 にした。`OnValidate` の条件は `> 1` から `> 0` に緩めた
- m 単位で持つ値（`lockLength` 250、`searchRadius`、移動パターン）は値を変えていない。Unity 上の距離はこれまでの 5 倍になる
- Unity 単位で持つ速度は、機体が約 3 倍（旧 約 2.75 → 8）になった分を目安に ×3 にした
  - Player: `vecSpeed` 9、`quickSpeed` 15、`hyperBoostSpeed` 30、`ascendSpeed` 6、`ascendAccel` 24
  - Tank `forwardSpeed` 15、Drone 水平 24 / 垂直 12、弾速 300
- `jumpSpeed` だけは ×3（15）にせず 8.5 にした。重力を 9.81 のまま ×3 にすると、最高点が機体の 1.4 倍・滞空 約 3 秒になるため。最高点が機体の約半分（3.7 m、旧来と同じ比率）になる値を選んだ
- 倍率・時間・角速度（`horizontalBoostRate`、`floatRate`、`rotSpeed`、`accelerateTime`、`quickTime` など）はスケールに依存しないので変えていない
- カメラ（`FollowCamera`）は `targetLength` 30、`cameraDiffMin` 0.3、`camDeltaRadius` 3 にした（Player / PlayerNPC）。`camTarget` の位置 (0, 6, -16) は #68 で新しいリグに合わせてあったので変えていない（Play で機体から約 17 m）
- 仕様書 `技術仕様.md` に「スケール（Unity の単位）」の節を追加し、m 単位で持つ値と Unity 単位で持つ値の区別を書いた

詰まった点・解決策:
- 機体のモデルは実行時にパーツから組み立てるため、Player プレハブの Bounds では全高が測れなかった。フレームパーツのプレハブ（`Prefabs/Parts/Frame/*_part.prefab`）をそれぞれ `LoadPrefabContents` で開いて Bounds を測り、Play 中の組み立て後の機体でも確認した
- `Bullet_001.speed` は int なので、`SerializedProperty.floatValue` では書き換えられなかった（`intValue` を使う）
- 値を書き換えたときに、Assert（`worldSizeRate > 1`）を直す前のコードで `OnValidate` が走って AssertionException が出た。先にコードを直して再コンパイルしてからアセットを変更する

残課題・関連Issue:
- 移動・ジャンプ・ブーストの速度とカメラの体感は、人間のテストプレイでの確認待ち
- #91: 今のフィールドは機体に対して m 換算で 1/5 の広さになるため、地形・配置物の拡大が必要。敵の索敵範囲（500 m）、`NpcController.radius` 100、`ascendHeight` 5 などフィールドに依存する値も一緒に見直す
- ブースターのエフェクト（#70）の大きさが、速くなった移動に合っているかの確認
---
