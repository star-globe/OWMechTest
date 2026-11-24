# エフェクトの対応一覧

> 作成日: 2026-11-14。弾の命中エフェクトの不具合（#130）をきっかけに、必要なエフェクトを洗い出した。各項目は Issue にして、作業用セッションで 1 つずつ進める。
> 進捗が変わったら（Issue の作成、PR の作成・マージ、方針の変更）、その PR か、管理用セッションでこの表を更新する。

## 方針
- 弾まわりのエフェクトは、実弾をオレンジ、EN 弾を青にして、見た目で区別できるようにする
- エフェクトのプレハブは `Prefabs/Effect/` に置き、`Resources/EffectSettings/` の ID に設定する。マテリアルは #130 / PR #131 の `Mat_EffectAdditive`（発光）と `Mat_EffectSmoke`（煙）を使い回す
- 外部アセットの `UnityTechnologies/ParticlePack` には依存しない
- 単発のものは `EffectManager.PlayEffect` で再生する（プールで使い回す）。続けて出すもの（ブースターの炎、接地移動の火花）は、機体側にループする ParticleSystem を持たせ、`PlayerEffectPresenter` から再生と停止を切り替える
- 見た目をすぐに確認できないことがあるため、大きさ・色・持続時間は数値で確かめ、根拠を PR に書く

## 進捗（2026-11-25 時点。#183 の PR で更新）

状態: 未着手 / 進行中 / PR レビュー待ち / 完了 / 保留

| エフェクト | ID | 発生するタイミング（呼び出し元） | 実弾 / EN | 優先度 | 状態 | Issue / PR |
|---|---|---|---|---|---|---|
| 命中の火花（HitSpark） | 11 | 弾の命中（`Bullet.OnHit`） | 区別あり（今はオレンジのみ。青は #133） | 高 | 完了 | #130 / PR #131 |
| 被弾の小爆発（ExplosionSmall） | 13 | `HitReceiver` を持つ相手への被弾（`HitReceiver.Damage`） | 不要 | 高 | 完了 | #130 / PR #131 |
| 撃破の爆発（Explosion） | 12 | 撃破（`HitReceiver.Damage`） | 不要 | 高 | 完了 | #130 / PR #131 |
| マズルフラッシュ | 10 | 発射（`GunPart.Fire` / `UnitShooter.Fire`。弾の `muzzleEffectId`） | 区別あり（今はオレンジのみ。青は #133） | 中〜高 | 完了 | #132 / PR #141 |
| 着地の砂煙（LandingDust） | 20 | `TouchChecker` が `None` → `Ground` に変わったとき | 不要 | 中 | 完了 | #136 / PR #142 |
| 接地ブースト中の足元の火花（GroundBoostSpark） | 21 | 地上でのブースト移動中（`IsBoost && !isFloating`）。ループ | 不要 | 中 | 完了 | #135 / PR #143 |
| 壁を蹴って上るときのエフェクト（WallKick） | 22 | `PlayerController.StartJump` で `TouchState.Wall` のとき | 不要 | 中 | 完了 | #134 / PR #155 |
| ミサイルの発射（MissileLaunch） | 14 | 発射（`BulletManager.Fire`。弾 `Missile_001` の `muzzleEffectId`） | 実弾（オレンジ） | 中 | PR レビュー待ち | #183 |
| ミサイルの飛行中の噴射（Exhaust） | - | `Missile.prefab` の子のループする ParticleSystem と、煙の軌跡（`TrailRenderer`） | 実弾（オレンジ） | 中 | PR レビュー待ち | #183 |
| ミサイルの命中（MissileHit） | 15 | 命中（`Bullet.OnHit`。`hitEffectId`） | 実弾（オレンジ） | 中 | PR レビュー待ち | #183 |
| ミサイルの撃墜（MissileShotDown） | 16 | 撃ち落とされたとき（`Bullet` の撃墜。`shootDownEffectId`） | 実弾（オレンジ） | 中 | PR レビュー待ち | #183 |
| 実弾 / EN 弾の色分け（命中・マズル・軌跡） | 新規 | 弾の属性で切り替える | - | 中 | 保留（#114・#117・#132 待ち） | #133 |
| ジャンプの足元の噴射 | 4 | `StartJump`（今はクイックのブースター炎で代用） | 不要 | 低 | 未着手 | #137 |
| ハイパーブースト開始時の衝撃波（HyperBoostShockwave） | 5 | ハイパーブーストに入った瞬間（ラジアルブラーと併用） | 不要 | 低 | PR レビュー待ち | #158 |
| 撃破後の煙・炎 | - | `CharacterDeadComponent` の `hideDelay` の間 | 不要 | 低 | 未着手 | #159 |
| EN 武器のチャージ | - | #117 の仕様次第（チャージ射撃を想定するか） | EN のみ | 低 | 保留（#117 待ち） | #137 |

## 進め方
- 作業用セッションは #132 → #136 → #135 → #134 の順に、Issue ごとにブランチと PR を作る（2026-11-14 に指示）
- #133 は #114（攻撃の属性）と #117（EN 射撃武器）が終わってから着手する
- #137 はまとめた Issue で、着手するときに必要に応じて分ける。2026-11-16 に衝撃波を #158、撃破後の煙・炎を #159 に分けた。#158 → #159 → #137（ジャンプの噴射）の順に進め、EN チャージは #117 で決まってから #133 と一緒に扱う

## 対象外
- ロックの成立: `LockOnState` / `LockSiteDisplay` の UI で表示しているため、ワールド上のエフェクトは作らない（UI の演出として扱う）

## 関連
- PR #101: ブースターのエフェクト（ループする ParticleSystem の例）
- #66: 小さなタスク（チェックリスト）
