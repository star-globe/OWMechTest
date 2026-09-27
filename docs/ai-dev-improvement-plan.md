# AI開発速度向上のための改善提案

> 調査日: 2026-09-27。各項目は Issue 化して逐次実行する。

## Context
AI（Claude Code）での開発速度を上げるため、CLAUDE.md・CLI検証環境・MCP連携・コード構造を調査した。本ドキュメントは調査結果と提案をまとめたもので、各提案は別途 Issue/PR で逐次実行する。

## 調査で分かった事実
- **バージョン不一致**: CLAUDE.md は `6000.4.6f1` だが、`unity/ProjectSettings/ProjectVersion.txt` は **`6000.5.4f1`**。
- **wiki の参照先が壊れている**: `unity/docs/wiki` のサブモジュールが未初期化（クラウド環境では中身が空）。索引のパス `docs/wiki/...` がリポジトリルート基準では存在しない（実体は `unity/docs/wiki/...`）。
- **CLI での検証手段がない**: `.github/workflows` が無い。コンテナに Unity が無い。テストも 0 件（`com.unity.test-framework` は入っているのにテスト用 asmdef が無い）。コンパイル確認はローカルの Unity＋UnityMCP 経由でしかできない。
- **asmdef**: `Plugins/UniRx/Scripts/UniRx.asmdef` だけ。自作コード約125ファイルはすべて `Assembly-CSharp` にまとまっている。
- **namespace が混在**: 98ファイルが `AdvancedGears`、約25ファイル（Utils/Graphics/UI の一部）は namespace 無し。
- **シングルトン依存**: `SingletonMonoBehaviour<T>.Instance` がアクセス時に自動で AddComponent する方式。`StateManager.Instance` は16箇所、`FieldManager` は11箇所で参照されていて、暗黙の依存が多い。
- **巨大クラス**: `Player/PlayerController.cs`（638行）が入力判定・ジャンプ/ブースト・移動物理・エフェクト・回転を1クラスで持っている。次に大きいのは `FootIK.cs`（344行）、`PlayerInput.cs`（267行）。
- **UnityMCP**: `com.coplaydev.unity-mcp`（MCPForUnity, `#main` 追従）がインストール済み。
- **Blender MCP**: リポジトリ内に設定（`.mcp.json` 等）が無い。ローカルの Claude Desktop/Code 側だけで設定していると思われる。パイプラインは `docs/blender-unity-pipeline.md` と `tools/blender/export_to_unity.py`（手動実行、`EXPORT_BASE_PATH` がプレースホルダのまま）。
- その他: `.claude/settings.json` には env だけで、permissions/hooks が無い。`docs/logs/` が無い（フロー上は作ることになっている）。`SetupChecker` が必須とする有料/外部アセット（ParticlePack, CleanFlatIcon）は git に含まれていないので、クラウドではコンパイルが再現できない可能性がある。

## プラグイン / MCP でできること
| ツール | できること | 開発速度への効果 |
|---|---|---|
| UnityMCP (MCPForUnity) | シーン/GameObject/Prefab/Material/Shader の作成・編集、スクリプトの作成・検証、**`read_console`（コンパイルエラー取得）**、**`run_tests`（EditMode/PlayMode テスト実行）**、メニュー実行、Play 制御 | ローカルで「編集→コンパイル→エラー修正→テスト」のループを AI が自走できる |
| Blender MCP (ahujasid/blender-mcp 想定) | シーン/オブジェクト情報の取得、任意の Python 実行（`execute_blender_code`）、ビューポートのスクショ、PolyHaven/Sketchfab/Hyper3D からのアセット取得 | `export_to_unity.py` の命名規約エクスポートを AI から直接実行し、そのまま UnityMCP で再インポートまで連結できる |
| Recorder / ProfileAnalyzer / Performance Test | 録画、プロファイル比較、性能テスト | パフォーマンス回帰の検出に使える |

## 提案（優先度順）

### P1. CLAUDE.md の修正・拡充 ― 効果: 大 / 作業量: 小（30分〜1時間）
- Unity バージョンを `6000.5.4f1` に修正する。
- 仕様書の索引をルート基準の `unity/docs/wiki/*.md` にし、「空なら `git submodule update --init` する」手順を追記する。
- **ディレクトリマップ**（`Script/` 以下の各フォルダの責務、`Resources/*Settings` とマスタ SO の対応）を追加する。
- **アーキテクチャ要約**: namespace `AdvancedGears`、Manager 系シングルトンの一覧と生成方式（`SingletonMonoObject` に自動 Add）、State/Scene の遷移、Master データ（ScriptableObject＋`MasterCsvIO`）の流れ。
- **検証方法**: 「編集後は UnityMCP の `read_console` でエラー 0 を確認し、`run_tests` を実行する」「クラウドでは検証できないので PR 本文にその旨を書く」。
- **禁止・注意事項**: `MainControls.cs` は自動生成なので手で編集しない。`*.meta` の扱い（新規ファイルには Unity が meta を生成する。クラウドで作る場合の GUID の扱い）。Asset Store 依存アセットは git に無いこと。
- **Blender 連携**節: `docs/blender-unity-pipeline.md` への参照と MCP の使い方。

### P2. Assembly Definition の導入 ― 効果: 大 / 作業量: 中（半日）
- 最小構成は `AdvancedGears.Runtime`（`Script/`）、`AdvancedGears.Editor`（`Script/Editor`＋`Assets/Editor`）、`AdvancedGears.Tests.EditMode`。
- 効果: 再コンパイルが速くなる、Editor/Runtime の境界がはっきりする、テストが書けるようになる。
- 注意: `MainControls.cs` と UniRx への参照を設定すること。Editor フォルダの移動・参照追加が必要。namespace の無い約25ファイルもこのタイミングで `AdvancedGears.*` にそろえる。

### P3. テスト基盤とロジックの切り出し ― 効果: 大 / 作業量: 中（初期1日＋継続）
- EditMode テスト用の asmdef を作り、まず純粋ロジックから始める（`PhysicsUtils`, `InputUtils`, `UnitParam`, `CharacterParam`, `MasterCsvIO`, `SquadData`）。
- `PlayerController` の `JumpInfo`/`BoostInfo`（ファイル冒頭の struct 群）や移動計算を POCO に切り出して単体テストできるようにする。
- AI がテストで仕様を確かめながら変更でき、リグレッションも検出できる。

### P4. CLI / CI でのコンパイル・テスト自動化 ― 効果: 大 / 作業量: 中〜大（1日、ライセンス次第）
- **ローカル**: `tools/unity/run-tests.sh`（`Unity -batchmode -projectPath unity -runTests -testPlatform EditMode -testResults ...`）と `compile-check.sh`（`-quit -logFile -` で Error を grep）を用意し、CLAUDE.md に記載する。
- **GitHub Actions**: GameCI（`game-ci/unity-test-runner`）で PR ごとにコンパイルとテストを回す。Unity ライセンスの Secret が必要（Personal でも可）。問題になるのは有料アセット（ParticlePack/CleanFlatIcon）で、git に無いとコンパイルが通らない。対策は、これらへのコード依存を無くすか、別 asmdef/define で切り離すこと。
- これでクラウドの Claude セッションでも CI 結果をもとに自己修正できるようになる。

### P5. `.claude/` 設定の整備 ― 効果: 中 / 作業量: 小（1〜2時間）
- `settings.json` に permissions を追加する（git/grep 等の読み取り系と UnityMCP の読み取り系ツールを allow）。
- `.mcp.json`（プロジェクト共有）で UnityMCP / Blender MCP の接続設定をリポジトリに含める。
- カスタムスキル/コマンドを追加する: `/issue-start`（ブランチ作成・方針提示のフロー）、`/unity-verify`（console 確認→テスト）、`/blender-export`。CLAUDE.md の開発フローを手順として実行できる形にする。
- クラウド用に SessionStart hook で `git submodule update --init unity/docs/wiki` を実行する。

### P6. 責務分割のリファクタ ― 効果: 中 / 作業量: 大（段階的に数日）
- `PlayerController`（638行）を入力解釈・移動物理（ジャンプ/ブースト/キック）・エフェクト制御に分割する。
- シングルトンの暗黙生成（`Instance` アクセスで AddComponent）をやめ、初期化順を明示する（`InitializeObject` 経由でのブート）。少なくともテスト時に差し替えられる interface を用意する。
- `MainControls.cs` を `Assets/Script/Input/Generated/` へ移動する（asmdef 導入とあわせて）。
- 各ファイルが小さく依存が明示的になり、AI が読む範囲が減って変更の影響も予測しやすくなる。

### P7. Blender パイプラインの MCP 対応 ― 効果: 中（アセット作業時） / 作業量: 小〜中
- `export_to_unity.py` の `EXPORT_BASE_PATH` を、環境変数か `.blend` からの相対パスで解決するようにする。
- 関数化して、Blender MCP の `execute_blender_code` から呼べる API にする（例: `export_selected(base_path)`）。
- マテリアルスロット名などの命名規約チェックを関数として用意し、AI がエクスポート前に検証できるようにする。

### P8. 軽微な整理 ― 効果: 小 / 作業量: 小
- `docs/logs/` を作成する。`.github/issues-draft.md` が Issue 化済みなら削除またはアーカイブする。
- `.claudeignore` に `unity/Assets/Plugins/UniRx`、`*.meta`、`Terrain`/`ProBuilder Data` 等の巨大な YAML を追加し、検索ノイズを減らす。

## 推奨する着手順
P1 → P5 → P2 → P3 → P4 → P7 → P6（P1/P5 はすぐ効果が出る。P2 以降は依存順）。

## 検証方法（各提案の実装時）
- P1/P5: 新しいクラウドセッションで wiki が読めること、索引パスが解決することを確認する。
- P2/P3: ローカルで UnityMCP の `read_console` がエラー 0、`run_tests` が green になることを確認する。
- P4: テスト PR で GitHub Actions が compile/test を実行し、結果を報告すること。
