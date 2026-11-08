---
Issue: #48 SpawnPointContainer の非推奨 API（FindObjectsOfType）を置き換える
実装日: 2026-10-15
変更ファイル:
- unity/Assets/Script/Field/Spawn/SpawnPointContainer.cs

実装方針:
- `CorrectPoints()` の `FindObjectsOfType<SpawnPoint>()` を `FindObjectsByType<SpawnPoint>().OrderBy(p => p.ID)` に置き換えた（PR #49）。`using System.Linq` を追加
- `FindObjectsByType` は順序を保証しないため、ID で並べて以前と同じ順にした
- 確認：Field_000_1 と Field_001_1 で、置き換え前と同じ結果（ID 0 の 1 件）になることを確認した

詰まった点・解決策:
- Unity 6.6 では `FindObjectsOfType` に加えて、`FindObjectsSortMode` を取る `FindObjectsByType` の形も非推奨になっていた → 引数なしの `FindObjectsByType<T>()` を使い、並べ替えは LINQ で行う

残課題・関連Issue:
- なし
---
