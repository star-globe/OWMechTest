---
Issue: #39 カスタマイズ結果のバトル反映とプレイヤー/NPC の機体データ分離
実装日: 2026-10-14
変更ファイル:
- unity/Assets/Script/Player/PlayerPartsContainer.cs
- unity/Assets/Script/Parts/PartsAssembleDataAsset.cs
- unity/Assets/Script/Parts/PlayerPartsAssembleDataAsset.cs
- unity/Assets/Script/Master/PartsSettings/GunSettings.cs
- unity/Assets/Script/Master/PartsSettings/BoosterSettings.cs
- unity/Assets/Resources/PlayerSettings/NpcPartsAssembleDataAsset.asset（新規）
- unity/Assets/Resources/PlayerSettings/PlayerNPC.prefab

実装方針:
- #40 と同じく `PlayerPartsAssembleDataAsset` の扱いを変える内容のため、1つの PR にまとめた（PR #44）
- `PlayerPartsContainer.Start()` で、アセットが `PlayerPartsAssembleDataAsset` なら PlayerPrefs の保存データを読み込んでから機体を組み立てる。再起動後もカスタマイズ結果が反映される
- NPC 用に `NpcPartsAssembleDataAsset.asset`（基底の `PartsAssembleDataAsset`、中身は従来の初期値）を作り、`PlayerNPC.prefab` の参照を差し替えてプレイヤーの保存データと分離
- 未装備（-1）の扱いを統一：保存時は負の ID をデータに含めない。`GunMaster.InstantiateGunPart` / `BoosterMaster.InstantiateBoosterPart` は負の ID なら `null` を返す。`GetPartId` はデータに無いスロットで -1 を返す
- 確認：保存データなし／Play 中に保存 → Play し直して反映（再起動相当）／NPC アセットが影響を受けないこと

詰まった点・解決策:
- 従来は `RemoveBoosters()` などが -1 を渡すと `There is no ...` のエラーが出ていた → 負の ID を「未装備」として扱い、エラーを出さないようにした
- 初回（保存データなし）にカスタマイズ画面を開くと警告が出ていた → 保存データが無いのは正常な状態なので、`Load()` は bool を返すだけにした
- NPC プレハブを Play 中に直接 `Instantiate` して確かめようとしたが、`PlayerManager.CreatePlayer` の初期化を通らず `PlayerController` で NullReferenceException が出た → テスト方法の問題のため、アセットの値の比較で分離を確認した

残課題・関連Issue:
- `PlayerNPC.prefab` は現状どこからも使われていない（生成されるのは `isSelf: true` のプレイヤーのみ）。NPC 機体を出すようになったときに動作を確認する
- 回帰テストは #45 の `PartsAssembleDataTests` に追加済み（別インスタンスが保存データを読み込めること）
---
