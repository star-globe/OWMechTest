---
Issue: #211 ブースターパーツをコアパーツのアニメーションに連動させる
実装日: 2026-11-28
変更ファイル:
- unity/Assets/Script/Parts/Frame/MechRig.cs（FollowBone を追加）
- unity/Assets/Script/Parts/MechPartsAssembler.cs（ブースター・サブの組み付けで Core を Chest ボーンへ付け替える）

実装方針:
- Issue の案 A（コードだけで完結）を採った。プレハブは変更していない
- ブースターの取り付け位置 Core（BoosterContainer、子にサブの SubAttach）が、リグのボーンの外に置かれていた。MechBody.prefab と、MechBody を持たない PlayerNPC.prefab は同じ構成だった
- MechRig.FollowBone は、対象がすでにスケルトンの子なら何もしない。そうでなければ animator.Update(0) で現在の状態（待機）の姿勢を評価し、worldPositionStays で付け替える。プレハブ上の配置は待機姿勢を基準にしているため
- MechPartsAssembler を通すので、バトル（Player / PlayerNPC）とカスタマイズのプレビューの両方に効く。噴射のエフェクトはコネクターの子なので、向きもコアと一緒に回る
- 武器の GunConnector_L/R は、すでに Hand ボーンの子で問題はなかった。サブ（肩）は Core の子なので、一緒に直った

詰まった点・解決策:
- プレハブに保存されたリグの姿勢（Root が X 90°）は、実行時の姿勢と違う。そのため、Awake の時点で付け替えるとずれる。Animator を評価してから付け替えるようにした
- カスタマイズ画面のプレビューは、MechPreview.Update が毎フレーム Speed を上書きする。歩行の確認では、eval で OnClickMotion を呼んで歩行にした
- バトルの確認は、eval で FieldManager.SetPendingMission(0) と StateManager.GoToState(Battle) を呼んで進めた

残課題・関連Issue:
- #66 に追加: PlayerNPC での Play 確認（Mission_000 に出てこなかった）／PR #236（#212、マージ済み）で待機姿勢が変わったので、ブースターの位置がずれていないかの確認
---
