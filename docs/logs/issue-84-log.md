---
Issue: #84 ロックした目標への射撃システムと二段階ロック（一次ロック・二次ロックの偏差射撃）の実装
実装日: 2026-11-12
変更ファイル:
- unity/Assets/Script/Shooting/（LeadCalculator、LockOnState、AimSolver、SecondaryLockTime。新規）
- unity/Assets/Script/BaseObject.cs
- unity/Assets/Script/Bullet/Bullet.cs、BulletManager.cs
- unity/Assets/Script/Master/PartsSettings/GunSettings.cs
- unity/Assets/Script/Parts/Weapon/Gun/GunPart.cs、GunAttach.cs
- unity/Assets/Script/Player/PlayerController.cs、MyPlayerController.cs、PlayerPartsContainer.cs
- unity/Assets/Script/Npc/NpcController.cs
- unity/Assets/Script/UI/Battle/LockSiteDisplay.cs、LockTargetImage.cs、CurrentLockStateInfo.cs（削除）
- unity/Assets/Script/Utils/PhysicsUtils.cs
- unity/Assets/Tests/EditMode/LeadCalculatorTests.cs、LockOnStateTests.cs（新規）
- docs/spec/ターゲットシステム.md、docs/spec/プレイヤー操作.md

実装方針:
- PR は #93 の 1 つ。#85（動く敵）でも使うため、偏差の計算・ロックの状態・照準の決定は、プレイヤー専用のフォルダではなく共通の `Script/Shooting/` に置いた
- 偏差の計算（`LeadCalculator`）は、撃った側から見た相対運動で `|D + V t| = s t` の正の最小解を求める（D = 目標 - マズル、V = 目標の速度 - 撃った側の速度）。等速を前提とし、加速度と重力は考えない。解がなければ目標の現在位置を返す。弾速と相対速度が等しい場合（一次方程式になる場合）も扱う
- 一次ロックと二次ロックは同じ計算を使い、目標の速度を 0 にするかどうかだけを変える（`AimSolver`）。一次ロックでも自機の速度の分は補正される
- ロックの状態（`LockOnState`）は目標と経過時間だけを持ち、二次ロックまでの時間は武器ごとに `GetLevel(secondaryLockTime)` へ渡す。左右・サブの武器で時間が違っても、状態は 1 つで済む
- 二次ロックまでの時間は `GunSettings.secondaryLockTime`（初期値 1.0 秒）。あとで計算の仕様が追加されたときのために、`SecondaryLockTime.Calc` を差し替える場所として分けた。既存の銃のアセットにはフィールドが無いため、初期値の 1.0 が使われる
- プレイヤーの目標は、ロック UI（`LockSiteDisplay`）が今の方法（ロックサークル内でいちばん近い目標）で選び、`PlayerController.SetLockTarget` で渡す（人間が選択。目標を選ぶ処理をプレイヤー側に移す案は、画面座標の判定を二重に持つことになるため採らなかった）。ロックの状態は機体（`PlayerController`）が持ち、UI はそれを表示するだけにした
- 目標の速度は `BaseObject.Velocity`（virtual）。物理で動く Rigidbody があれば `linearVelocity`、なければ `LateUpdate` で記録した位置の差分。狙う点はコライダーの中心（`AimPosition`）
- 弾は `Rigidbody.linearVelocity` に「方向 × 弾速 + 撃った側の速度」を代入する（`AddForce` だとプールから再利用したときに速度が積み上がるため）
- 照準の計算はマズルの位置と弾速を知っている `GunPart` で行い、`PlayerController` は `AimRequest`（目標・ロックの段階・ロックしていないときに狙う点・自機の速度）を渡すだけにした。`CheckXxxFire` は目標を返さず、射撃するかどうかだけを返す
- NPC は毎フレーム索敵した目標（`BaseObject`）をロックする。見つけた時点で一次ロック、同じ目標を狙い続けると二次ロック

詰まった点・解決策:
- `GunPart` の中で `SecondaryLockTime` という名前のプロパティを定義したため、同名の静的クラスを呼ぶときに `AdvancedGears.SecondaryLockTime.Calc` と修飾した
- Play 確認では、Mission 0 の Battle に `eval` で遷移し、`PlayerController.Fire` をリフレクションで呼んで弾の速度の向きを確かめた（一次ロックで目標の方向との内積が 1.0000）。ロックの目標はロック UI が毎フレーム設定し直すので、手で設定した目標はすぐ上書きされる

残課題・関連Issue:
- #85: 動く敵。`BaseUnit` に `LockOnState` を持たせ、`AimSolver.CalcAimDirection` と `BulletManager.Fire(bulletId, 位置, 方向, 自分の速度)` で撃つ。ユニットは銃を持たないので、二次ロックまでの時間はユニットの設定から渡す。移動を式で計算するなら `Velocity` を override する
- 未確認: 動く目標への二次ロックの命中（#85 の後）、NPC の射撃（Mission 0 に NPC がいない）、一次・二次ロックの UI の色
- #66 に追加: `BulletQueue.DequeueBullet` が常に null を返し、弾のプールが再利用されない／ロック UI が陣営 `None` の `FixedBaseObject` もロックし、射撃の目標にもなる
- 検討事項として残したもの: 照準点がロックサークルの外にはみ出した場合の扱い、NPC の偏差射撃の精度（難易度によるばらつき）
---
