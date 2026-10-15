---
Issue: #45 テスト基盤の導入（改善プラン P3）
実装日: 2026-10-15
変更ファイル:
- unity/Assets/Tests/EditMode/AdvancedGears.Tests.EditMode.asmdef（新規）
- unity/Assets/Tests/EditMode/PartsAssembleDataTests.cs（新規）
- unity/Assets/Tests/EditMode/MasterContainerTests.cs（新規）
- unity/Assets/Tests/EditMode/ParamTests.cs（新規）
- unity/Assets/Tests/EditMode/PlayerControllerInfoTests.cs（新規）
- unity/Assets/Tests/EditMode/SquadDataTests.cs（新規）
- unity/Assets/Tests/EditMode/MasterCsvIOTests.cs（新規）
- unity/.claude/skills/unity-verify/SKILL.md
- unity/.claude/settings.json
- CLAUDE.md
- docs/ai-dev-improvement-plan.md

実装方針:
- EditMode テスト用の asmdef を作成（PR #46）。Editor 専用・`UNITY_INCLUDE_TESTS` 定義時のみコンパイルし、ビルドには含めない。`AdvancedGears.Runtime` と `AdvancedGears.Editor` を参照、NUnit は precompiled reference
- Unity に依存しない純粋なロジックから 34 件のテストを追加：機体データ（#39 / #40 の回帰テストを含む 8 件）、マスタのコンテナ 4 件、`CharacterParam` / `UnitParam` 9 件、`JumpInfo` / `HyperBoostInfo` 6 件、`SquadData` 5 件、`MasterCsvIO` 2 件
- テストの実行方法と書き方の方針を CLAUDE.md の「検証方法」と `/unity-verify`（手順 4）に追加し、`mcp__unity-mcp__run_tests` を許可リストに追加
- 確認：`run_tests`（mode: editor、filter_type: assembly、filter: AdvancedGears.Tests.EditMode）で 34/34 passed

詰まった点・解決策:
- 初回は「保存してもアセットが dirty にならない」テストが失敗した。テストデータを `SerializedObject` で書き込んだ時点でアセットが dirty になっていたため → 初期値の書き込み後に `EditorUtility.ClearDirty` を呼ぶ
- PlayerPrefs を使うテストが実際の保存データを壊さないよう、`SetUp` で退避して `TearDown` で元に戻す。実行後にキーが元の状態に戻ることを確認した
- 未登録 ID のテストは意図してエラーログを出すため、`LogAssert.Expect` で想定済みにした

残課題・関連Issue:
- 改善プランにある「`PlayerController` の移動計算を POCO に切り出してテストする」作業はリファクタリングを伴うため P6 に移した
- PR ごとの自動実行は P4（CI）で行う。Unity ライセンスの Secret と、git 管理外の有料アセット（ParticlePack / CleanFlatIcon）への対処が必要
---
