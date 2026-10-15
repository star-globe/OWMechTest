---
Issue: #40 Editor の Play 中にカスタマイズを保存すると git 管理下のアセットが書き換わるおそれがある
実装日: 2026-10-14
変更ファイル:
- unity/Assets/Script/Parts/PartsAssembleDataAsset.cs
- unity/Assets/Script/Parts/PlayerPartsAssembleDataAsset.cs

実装方針:
- #39 と同じ PR #44 で対応
- `PartsAssembleDataAsset` から、シリアライズ済みデータを書き換える `SetData()` を削除。アセットのデータは「保存データが無いときの初期値」としてだけ使う
- 参照するデータを `protected virtual CurrentData` で切り替えられるようにし、`PlayerPartsAssembleDataAsset` は読み込んだ・保存したデータを `[NonSerialized] savedData` に持って優先する
- 確認：Play 中に `Save()` してもアセットは dirty にならない。Play を止めて `SetDirty` + `AssetDatabase.SaveAssets()` で強制保存しても `.asset` に git diff が出ない

詰まった点・解決策:
- `[NonSerialized]` のフィールドは Play の開始・終了で値がリセットされるため、「Play し直す」ことで再起動と同じ状態を作って #39 の反映も確認できた

残課題・関連Issue:
- 回帰テストは #45 の `PartsAssembleDataTests` に追加済み（保存してもシリアライズ済みデータが変わらず、dirty にならないこと）
---
