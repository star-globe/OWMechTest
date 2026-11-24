---
Issue: #182 ミサイル（誘導兵器）をサブ武装として実装する
実装日: 2026-11-24
変更ファイル:
- docs/spec/武装.md（サブのスロットに、射出しないサブ装備を今後追加することを追記）
- unity/Assets/Script/Parts/Weapon/Sub/ISubPart.cs, SubPartAttach.cs（新規。サブ装備の取り付けと使用）
- unity/Assets/Script/Parts/PartSlotRule.cs（新規。スロットに付けられるカテゴリ）
- unity/Assets/Script/Shooting/MissileGuidance.cs, BurstSchedule.cs（新規。誘導の計算、一斉発射の予定）
- unity/Assets/Script/Bullet/Bullet.cs, BulletManager.cs（誘導、撃ち落とし、弾ごとの命中エフェクト）
- unity/Assets/Script/Master/BulletSettings.cs, PartsSettings/GunSettings.cs, PartsSettings/PartSettings.cs（マスタの項目、PartCategoryType.Missile）
- unity/Assets/Script/Parts/Weapon/Gun/GunPart.cs（一斉発射、発射口の順送り、ISubPart の実装）
- unity/Assets/Script/Parts/MechPartsAssembler.cs, Parts/Frame/MechBody.cs, Player/PlayerPartsContainer.cs（サブを SubPartAttach に組み付ける）
- unity/Assets/Script/Parts/PartsAssembleDataAsset.cs, UI/Customize/CustomizeManager.cs, CustomizeEnergyText.cs, PartsSlotButton.cs（スロットのカテゴリの判定と表示）
- unity/Assets/Script/Unit/UnitShooter.cs（ユニットのミサイルにロックの目標を渡す）
- unity/Assets/Script/Utils/GameLayers.cs, Effect/EffectID.cs（Missile レイヤー、エフェクト ID 14〜16 の予約）
- unity/Assets/Tests/EditMode/MissileGuidanceTests.cs, BurstScheduleTests.cs, PartSlotRuleTests.cs（新規）
- 仮のアセット: Prefabs/Bullets/Missile/Missile.prefab, Prefabs/Parts/Sub/MissileLauncher_001_part.prefab, マスタ Missile_001（弾 ID 2）, MissileLauncher_001（パーツ ID 311001）
- MechBody.prefab, Player.prefab, PlayerNPC.prefab（右肩の SubAttach）, PlayerPartsAssembleDataAsset.asset（初期のサブにランチャー）
- ProjectSettings/TagManager.asset（Missile レイヤー 17）, DynamicsManager.asset（Missile レイヤーの衝突をすべて無効）

実装方針:
- 当初はサブを銃（GunAttach）のまま扱う案だったが、今後のバフなど射出しないサブ装備を考えて、サブを「サブ装備」（ISubPart / SubPartAttach）として銃から切り離した。スロットに付けられるカテゴリは PartSlotRule で決める（腕 → Gun、サブ → Missile）
- 射出するミサイルは GunPart に ISubPart を実装させ、腕の銃と発射の処理を共通にした。一斉発射の数・間隔は GunSettings に汎用で持たせ、ライフルは既定値の 1 発
- 誘導は弾のマスタのパラメーター（誘導開始までの時間・誘導時間・旋回性能）で有無を決める。発射時のロックの目標と段階を弾に渡し、旋回の上限つきで向きを変える。二次ロックの未来位置は LeadCalculator を使う
- 撃ち落としは Missile レイヤーのコライダーで判定する。衝突マトリクスで物理の衝突を無効にし、判定はほかの弾の SphereCast に任せる
- #183（モデル・エフェクト）とは、仮の見た目を子の Model に入れたプレハブと、予約したエフェクト ID で分担した（差し替え先は PR #192 の本文と #183 のコメントに記載）

詰まった点・解決策:
- 弾とミサイルが正面からすれ違うと、1 ステップでお互いを飛び越えて撃ち落とせないことがあった（相対速度 420 m/s で 1 ステップ 8.4 m、10 組中 2 組がすり抜けた）。ミサイルの当たり判定をカプセルにし、前のステップの位置まで後ろへ伸ばして解決した
- 伸ばした判定に、弾の判定の開始時点で重なっていると「撃った直後の自機」として除外されていた。Missile レイヤーは開始時点の重なりでも当たりとし、当たった位置が (0, 0, 0) になる問題も判定の開始位置を使って直した（直した後は 20 組中 20 組を撃ち落とせた）
- テストで境界ちょうどの時刻（0.2 秒など）を比べると、浮動小数の誤差で失敗した。2 進数で正確に表せる値（0.25 秒など）に変えた
- 衝突マトリクスの保存で、Unity が DynamicsManager.asset を古い形式から新しい形式に書き直し、差分が大きくなった（衝突マトリクスの値の末尾に Unity が書き出した `u` が 1 文字付いている）
- 管理用セッションへの send_message が、信頼済み端末の制限で送れなかった。報告は人間に手で渡してもらった

残課題・関連Issue:
- #183: ミサイルとランチャーのモデル・エフェクトに差し替える（予約したエフェクト ID 14〜16 を弾のマスタに設定する）
- 実際のキー入力でのサブ射撃と、ロックサークルにミサイルが入らないことは、画面では未確認
- ロック表示 UI へのサブの追加（ターゲットシステム.md）、NPC の AI によるサブ射撃は範囲外
- #114（攻撃の属性）が入ったら、ミサイルの属性を実弾に設定する
---
