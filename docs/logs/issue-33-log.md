---
Issue: #33 Assembly Definition の導入と namespace の統一（改善プラン P2）
実装日: 2026-10-12
変更ファイル:
- unity/Assets/Script/AdvancedGears.Runtime.asmdef
- unity/Assets/Script/Editor/AdvancedGears.Editor.asmdef
- unity/Assets/Editor/AdvancedGears.Editor.asmref
- unity/Assets/MainControls.cs → unity/Assets/Script/Input/Generated/MainControls.cs（移動、GUID 維持）
- unity/Assets/MainControls.inputactions.meta（wrapperCodePath）
- unity/Assets/Script/ 以下の namespace なし 25 ファイル（`namespace AdvancedGears` で囲む）
- unity/Assets/Script/Editor/LockTargetImageEditor.cs
- unity/Assets/Script/Master/GlobalParamSettings.cs

実装方針:
- `docs/ai-dev-improvement-plan.md` の P2 を実施（PR #36）
- `AdvancedGears.Runtime`（`Script/`）と Editor 専用の `AdvancedGears.Editor`（`Script/Editor/`、`Assets/Editor/` は asmref）を作成
- asmdef を付けられない `Assets/` 直下の `MainControls.cs` を `Script/Input/Generated/` に移動し、`.inputactions` の生成先も同じパスに設定
- 事前に変更前の動作（Play → Start → Select 遷移、エラー 0 件）を記録し、変更後に Start → Select → Briefing → Battle まで同じ結果になることを確認

詰まった点・解決策:
- namespace で囲むと、既存の `AdvancedGears.Editor` namespace と衝突して `Editor` 基底クラスが解決できない → `UnityEditor.Editor` と完全修飾
- 実行時コードで NUnit の `Assert` を使っていた（テスト専用でランタイムの asmdef から参照不可）→ `UnityEngine.Assertions.Assert` に置き換え
- シリアライズ済み UnityEvent の型名が `..., Assembly-CSharp` のまま残るが、Unity がターゲットの型にフォールバックするため正常に呼ばれることを確認
- namespace だけの変更でいったんコンパイルし、asmdef 追加前に問題を切り分けた

残課題・関連Issue:
- テスト用 asmdef（`AdvancedGears.Tests.EditMode`）は改善プラン P3 で追加する
- `MissionSelectPanel.prefab` に呼び出し先のない UnityEvent が残っている（動作への影響なし）
---
