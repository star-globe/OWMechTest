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

## PR #144 NPC の部隊をミッション 2 に出し、経路に沿って動いて交戦できるようにする

---
Issue: #66 小さなタスク（チェックリスト）（PR #144）
実装日: 2026-11-15
変更ファイル:
- unity/Assets/Script/Npc/NpcController.cs、NpcManager.cs、NpcDecider.cs（`NpcBoostDecider.cs` を改名）、NpcSpawner.cs（新規）、NpcCharacter.cs（削除）
- unity/Assets/Script/Parts/Booster/BoosterContainer.cs、Player/PlayerController.cs、Utils/PhysicsUtils.cs
- unity/Assets/Resources/PlayerSettings/PlayerNPC.prefab、NpcPlayerSettings.asset（新規）
- unity/Assets/Scenes/Fields/Field_002/Field_002_2.unity
- unity/Assets/Tests/EditMode/NpcDeciderTests.cs（新規）、EnergyPerformanceTests.cs
- docs/spec/ユニット全般.md

実装方針:
- 対象の項目は 3 つ（NPC が出てこない、`BoosterContainer.cs:29` の NullReferenceException、NPC が接地するたびにジャンプする）。#66 は閉じない（`Refs #66`）
- NPC が出せなかった原因は、#66 に書いてあったもののほかにもあった:
  - `PlayerNPC.prefab` のコネクターが基底の `PartConnector` で、`BoosterPartConnector[]` の参照が切れていた（NRE の原因）
  - プレハブに `NpcCharacter` が無く、`CreateNpc` は必ず失敗していた
  - ルートのレイヤーが Default で、ロックも被弾もされなかった
  - NPC の索敵が `Player` タグで絞っていたが、機体はどれも Untagged だった
  - `FollowCamera` が付いていて、メインカメラを動かしてしまう状態だった
- プレハブの直し方:
  - Core を Player.prefab から複製した（6 つの `BoosterPartConnector`）
  - NavMeshAgent（Mech、baseOffset 4）を付けた
  - レイヤーを NonPlayer にし、`camTarget` を外した
  - NPC の機体は `PlayerCharacter`（IsSelf = false）で扱い、使われていない `NpcCharacter` を削除した
- 判断の外部化（人間の指示）:
  - `INpcDecider`（ブースト・ジャンプ・クイック・ハイパーブースト・上昇・射撃）と、判断の材料 `NpcDecisionContext` を作った
  - `NpcController.Decider` で差し替えられるようにし、後の AI の作り込みでここを入れ替える
  - 既定の `NpcDecider` は、ブーストの判定を `NpcBoostDecider` から引き継ぐ。ジャンプ・クイック・ハイパーブーストは当面使わない
- 移動: NavMeshAgent は経路を求めるだけにした（`updatePosition` / `updateRotation` は false）。経路の次の曲がり角（`steeringTarget`）の方向を、機体から見た移動の入力（`UpdateXZ`）と旋回の入力（`GetCameraPos`）に変えるので、移動はプレイヤーと同じ Rigidbody の処理になる
- スポーン:
  - `NpcSpawner` は `FixedUnitSpawner` に倣い、シーンに置くと自分の位置から部隊を 1 つ出す
  - プレハブはマスタの `PlayerType.NPC`（`NpcPlayerSettings`）から取る
  - Mech の NavMesh があるのは Field_002 だけなので、ミッション 2 の (140, 0, 420) に敵の部隊（リーダー・偵察・支援）を置いた。目的地はプレイヤーのスポーン地点で、経路は約 470 m

詰まった点・解決策:
- `PlayerController.Start` が private なので、派生クラスに `Start` を書くと基底の `Start`（エネルギー性能の設定）が呼ばれなくなる。Agent の設定は毎フレームの `SyncAgentPosition` で行った
- 最初の Play では出現と前進まではできたが、近づいてもロックしなかった。原因はタグで絞っていたこと。砲台（`TurretController`）と同じくタグを指定しないようにし、`PhysicsUtils.CheckOverlapShpereOthers` のタグを `TagHandle?` にした
- eval で `GetComponent<T>() ?? AddComponent<T>()` と書くと、Unity の null 判定と合わずに例外になった。`== null` で判定して書き直した
- 途中で、人間用の別のクローンの準備のため Unity Editor が止まり、待機した
- Play 確認（`WarGame/Play Main Scene` → eval でミッション 2）:
  - 3 機が経路に沿って約 18 m/s で前進した
  - プレイヤーに近づくと 3 機とも二次ロックして射撃した
  - 偵察を AP 0 にすると、停止し、Agent が切れ、部隊から外れ（3 → 2）、演出の後に非表示になった
  - リーダーを撃破すると `Leader` が null になり、残った支援は交戦を続けた

残課題・関連Issue:
- #66 に追加した項目:
  - Field_000 と Field_001 の NavMesh が Humanoid でベイクされていて、Mech の NPC を出せない
  - プレイヤーは撃っていないのに NPC の AP が少し減っていて、同じ陣営の弾が NPC に当たっているらしい（未調査）
  - フォントに「墟」が無い
- 部隊の AI（リーダーが撃破された後の再編、偵察の哨戒、支援の役割など）はまだ暫定。AI を作り込むときは `INpcDecider` と `NpcController.UpdateAgentDestination` を差し替える
- 目的地に着いた後は止まったまま撃ち続ける。距離を取る・回り込むなどの動きは無い
---
