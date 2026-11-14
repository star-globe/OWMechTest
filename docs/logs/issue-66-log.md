# Issue #66 小さなタスク（チェックリスト）の作業ログ

#66 はピン留めのチェックリストで、閉じずに項目ごとに PR で対応する。PR ごとに下に節を追記する。

## PR #110 弾の当たり判定・プール・被弾と、エネルギーの端数の不具合を直す

---
Issue: #66 小さなタスク（チェックリスト）（PR #110）
実装日: 2026-11-13
変更ファイル:
- unity/Assets/Script/Bullet/Bullet.cs、BulletManager.cs
- unity/Assets/Script/HitReceiver/CharacterHitReceiver.cs（新規）、HitReceiver.cs
- unity/Assets/Script/Shooting/AimSolver.cs、Parts/Weapon/Gun/GunPart.cs、Unit/UnitShooter.cs
- unity/Assets/Script/Player/CharacterParam.cs、PlayerController.cs
- unity/Assets/Script/UI/Battle/ArmorDisplay.cs
- unity/Assets/Script/Utils/GameLayers.cs
- unity/Assets/Prefabs/Bullets/Normal/NormalBullet.prefab
- unity/Assets/Resources/PlayerSettings/Player.prefab、PlayerNPC.prefab、Resources/UnitSettings/Turret/Turret.prefab
- unity/Assets/Tests/EditMode/ParamTests.cs

実装方針:
- チェックリストのうち、弾と被弾・エネルギーに関わる項目をまとめて直した。#66 は閉じない（`Refs #66`）
- 弾の当たり判定（入力なしでプレイヤーが流れていく）: 弾が質量 1 の Rigidbody とトリガーでない SphereCollider を持ち、当たった相手に運動量が移っていた。衝突判定も Discrete で、300 m/s では 1 ステップに 6 m 進んですり抜けていた。弾のプレハブからコライダーを外し、`Bullet.FixedUpdate` で前の位置から今の位置まで SphereCast して判定する（半径 `hitRadius` 0.1 m）
- 撃った本人（`BaseObject`）のコライダーには当たらない。撃った本人は `AimRequest.Shooter` → `BulletManager.Fire(..., shooter)` で渡す。`HitReceiver` は子のコライダーからも拾えるよう `GetComponentInParent` で探す
- 弾のプール: `BulletQueue.DequeueBullet` が取り出した弾を捨てて null を返していたのを直した。シーンの切り替えで破棄された弾は飛ばす。命中した弾は `BulletManager.Vanish` ですぐプールに戻す
- 敵の弾がプレイヤーにダメージを与えない: `GameLayers.HitLayerMask` に `MyPlayer` を追加し、`CharacterHitReceiver`（`CharacterParam.SetDamage` に反映する）を `Player.prefab` / `PlayerNPC.prefab` に付けた
- 砲台を撃破できない: `Turret.prefab` のルートに `UnitHitReceiver` と `DeadComponent` を付けた
- `HitReceiver` は撃破した後の被弾を無視する（プレイヤーは撃破されても消えないので、爆発のエフェクトが繰り返されないようにするため）
- 回復量の毎フレームの (int) 切り捨て（#104 で追加）: `CharacterParam` はエネルギーを内部では float で持ち、`Energy` では切り捨てた値を返す。ハイパーブーストと上昇の消費も同じように直した。テスト `Recovery_AccumulatesFractionPerFrame`、`ConsumeHyperBoostEnergy_AccumulatesFractionPerFrame` を追加した
- 作業中に見つけた不具合: `ArmorDisplay` の AP ゲージが整数の割り算で 0 か 1 にしかならなかったのを直した

詰まった点・解決策:
- `NormalBullet.prefab` は保存するときに古い形式から再シリアライズされ、差分が大きい（`Rigidbody` の `m_Drag` → `m_LinearDamping` など）。内容の変更はコライダーを外したことと `hitRadius` の追加だけ
- Play 確認は Battle（mission 0）で行った。敵の弾で AP が減り、入力なしで機体が (0, 4, 0) に止まったまま（速度 0）。砲台に 200 発撃つとすべて命中し、AP を 0 にすると撃破されて非アクティブになった。200 発の後に 150 発撃っても弾のインスタンスは 200 個のまま（プールから再利用）
- 撃った本人の除外は、プレイヤーの後ろから機体を貫く向きに 20 発撃って確かめた（除外すると AP の減りは敵の弾の分だけ、除外しないと 20 発ちょうど減った）

残課題・関連Issue:
- #108: 機体の AP が 0 になったときの撃破・任務失敗の処理（今回の対象外として分けた）
- 未確認: 実際に操作しての射撃（自機の銃の弾が敵に当たること。経路は上の確認と同じ）、NPC の被弾（mission 0 に NPC がいないため）
- 以前からある警告: エフェクトのプレハブ未設定（effectId=11, 13）
- #98: オーバーヒートも `CharacterParam` のエネルギー処理を変える（float で持つようにした変更と合わせる）
---
