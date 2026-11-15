---
Issue: #134 壁を蹴って上るときのエフェクトを作る
実装日: 2026-11-15
変更ファイル:
- unity/Assets/Script/Effect/EffectID.cs
- unity/Assets/Script/Player/TouchChecker.cs
- unity/Assets/Script/Player/PlayerController.cs
- unity/Assets/Script/Player/Movement/PlayerEffectPresenter.cs
- unity/Assets/Tests/EditMode/PlayerMoveCalculatorTests.cs
- unity/Assets/Prefabs/Effect/WallKick.prefab
- unity/Assets/Resources/EffectSettings/WallKick.asset
- docs/effects-plan.md

実装方針:
- `EffectID.WallKick`（22）を追加し、着地の砂煙（#136）と命中の火花（#130）のプレハブをもとに `WallKick.prefab`（粉じん 10 個 + 火花 16 個）を作った
- `TouchChecker` の接触ごとの記録に接触点を加え、`CurrentPoint` で取れるようにした
- `PlayerController.StartJump` で `KickAndJump` と同じ接触状態・法線を使い、`TouchState.Wall` のときだけ接触点に法線の向きで再生する（真上に上るときも、壁から離れるときも出す）
- 判定を `PlayerEffectPresenter.IsWallKick` に分けてテストを追加した
- PR #155

詰まった点・解決策:
- `TouchChecker` は法線しか持っていなかったため、接触点を推定するより記録するほうが正確と判断し、辞書の値に接触点を加えた
- 見た目を確認できないため、`ParticleSystem.Simulate` で粒子の向き（全粒子が法線方向に飛ぶ）と広がりを数値で確かめた

残課題・関連Issue:
- 実際に機体を壁に当てて蹴る動作と、見た目は未確認
- #137 を分け、ハイパーブースト開始時の衝撃波を #158、撃破後の煙・炎を #159 にした
---
