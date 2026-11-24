---
Issue: #114 攻撃に属性（実弾 / EN）を追加する
実装日: 2026-11-24
変更ファイル:
- unity/Assets/Script/Bullet/AttackInfo.cs（新規。`AttackAttribute` と `AttackInfo`）
- unity/Assets/Script/Master/BulletSettings.cs
- unity/Assets/Script/HitReceiver/HitReceiver.cs
- unity/Assets/Script/Bullet/Bullet.cs
- unity/Assets/Tests/EditMode/BulletSettingsTests.cs
- docs/spec/武装.md（「攻撃の属性」の節を追加）

実装方針:
- 攻撃の属性 `AttackAttribute { Physical = 0, Energy = 1 }` と、攻撃力と属性をまとめた `readonly struct AttackInfo` を追加した
- 属性は武器ではなく弾のマスタ（`BulletSettings`）に持たせた。攻撃力が弾にあるため、属性も一緒に持たせると値が 2 か所に分かれない。武器の属性は使う弾の属性になる（#117 は EN 属性の弾を武器に設定する）
- `HitReceiver.Damage(int)` を `Damage(AttackInfo)` に変え、`Bullet.OnHit` から属性まで渡すようにした。ダメージは今までどおり攻撃力のまま
- 爆発など弾以外の攻撃も、`AttackInfo` を作って `Damage` を呼ぶ形で扱う（新しい仕組みは足していない）

詰まった点・解決策:
- 既存の弾のアセット（Bullet_001 / Missile_001）を変更すると、#183（ミサイルのモデル・エフェクト）が同じ Missile_001 を変更するので衝突する。enum の既定値 0 を実弾にして、アセットを変更せずに済ませた。既定値が実弾であることはテストで確かめている
- 作業用セッションから管理用セッションへの `send_message` が、信頼済みのデバイスでないという理由で送れなかった。報告は人間に伝えてもらった

残課題・関連Issue:
- #115（装甲の属性）、#116（ダメージ計算式。`HitReceiver.Damage` の中で、攻撃力をそのままダメージにしている 1 行を計算クラスに差し替える）
- #117（EN 射撃武器。EN 属性の弾のマスタを追加する）、#133（実弾と EN 弾のエフェクトの色分け）
- 戦闘で弾が当たってダメージが入るところは、Play では確かめていない（Main Scene の起動までを確認した）
---
