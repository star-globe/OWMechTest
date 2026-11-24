# CLAUDE.md

## プロジェクト概要
WarGameProject
カスタマイズ可能な人型兵器を駆り、戦術によって勝利を得るアクションシミュレーター

## 技術スタック

| 項目 | バージョン / 詳細 |
|------|-----------------|
| Unity | **6000.6.3f1**（Unity 6.6） |
| レンダーパイプライン | **URP**（Universal Render Pipeline） |
| スクリプティングバックエンド | Mono / .NET Standard 2.1（apiCompatibilityLevel: 6） |
| 言語 | C# |
| 主要ライブラリ | UniRx、ProBuilder、NavMesh |

### コード記述上の注意
- **Unity 6 対応の API を使用すること**（Unity 5 / 2019-2022 系の deprecated API は使わない）
- `switch` 式・パターンマッチング等の C# 8.0 以降の構文は使用可能
- `Rigidbody.velocity` は Unity 6 で **`linearVelocity`** に変更されている（`velocity` は廃止）
- Physics API など Unity バージョンで挙動が変わる箇所は必ず Unity 6 のドキュメントを参照すること
- 実装前に既存コードのパターンを確認し、プロジェクト内の記述スタイルに合わせること

## 仕様書
仕様書の原本は本体リポジトリの `docs/spec/` に置いている。GitHub Wiki（`star-globe/OWMechTest.wiki`）は表示用の複製で、main への push 時に GitHub Actions（`.github/workflows/sync-wiki.yml`）が `docs/spec/` の内容で上書きする。

### 仕様の変更
- **wiki を直接編集しない**。仕様の変更は `docs/spec/` を編集し、コードの変更と同じ PR に含める（クラウド環境でも同じ）
- 会話の中で仕様が変わったら、その場で「`docs/spec/` のどのページをどう変えるか」の差分案を示し、人間の了承を得てから編集する。決まった内容を後からまとめて書き起こさない
- 相談だけで PR がない場合も、`docs/spec/` の変更だけの PR を作る
- PR 本文の「仕様変更」欄（`.github/pull_request_template.md`）に、あり / なしと変更したページを書く
- ページの書式は GitHub Wiki に合わせる（ファイル名がページ名、ページ間のリンクは `[[ページ名]]`、サイドバーは `_Sidebar.md`、フッターは `_Footer.md`）。ページを追加・改名したら `_Sidebar.md` と下の索引も更新する
- 反映のワークフローは、wiki が前回の `docs/spec/` から変わっている（直接編集された）と失敗する。そのときは wiki の差分を `docs/spec/` に取り込んでから、Actions の画面で `force` を付けて手動実行する

## 仕様書の索引
パスはリポジトリルート基準。
- 基本仕様: docs/spec/基本仕様.md
- 技術仕様: docs/spec/技術仕様.md
- プレイヤー操作: docs/spec/プレイヤー操作.md
- レイヤー管理: docs/spec/レイヤー管理.md
- ターゲットシステム: docs/spec/ターゲットシステム.md
- ユニット全般: docs/spec/ユニット全般.md
- 武装: docs/spec/武装.md
- 撃破と任務の結果: docs/spec/撃破と任務の結果.md
- アートディレクション: docs/spec/アートディレクション.md
- プロトタイプの方針: docs/spec/プロトタイプの方針.md

その他のドキュメント（リポジトリルートの `docs/`）
- AI 開発の改善プラン: docs/ai-dev-improvement-plan.md
- Blender ↔ Unity パイプライン: docs/blender-unity-pipeline.md
- モデリング作業のポリシー（トークン使用量の削減）: docs/modeling-work-policy.md
- RenderGraph ラジアルブラー: docs/rendergraph-radial-blur-guide.md
- 管理用セッションのトークン節約ポリシー: docs/management-session-policy.md
- クラウドセッションの基本方針: docs/cloud-session-policy.md
- Remote Control での起動バッチファイル（ひな形）: docs/remote-claude-launch.md
- エフェクトの対応一覧（進捗の管理）: docs/effects-plan.md
- 許可確認（承認画面）の調査: docs/permission-prompt-survey.md（集計スクリプト: `tools/claude/survey-permission-prompts.ps1`）
- 作業ログ: docs/logs/issue-{番号}-log.md

## ディレクトリマップ

### `unity/Assets/` 直下
| フォルダ | 内容 |
|---|---|
| `Script/` | 自作 C# コード（下表） |
| `Editor/` | `SetupChecker.cs`（必須外部アセットの有無を確認するエディタ拡張）。asmref で `AdvancedGears.Editor` に含まれる |
| `Tests/EditMode/` | EditMode テスト（asmdef: `AdvancedGears.Tests.EditMode`） |
| `Resources/` | マスタ ScriptableObject（`EffectSettings/`, `MissionSettings/`, `PlayerSettings/`, `UnitSettings/`, `GlobalParamSettings`, `PlayerControllerConstSettings`） |
| `Prefabs/` | プレハブ。`Prefabs/UI/` は画面ごとのフォルダ（UI 実装ポリシー参照）。`Bullets/Resources/`, `Effect/Resources/`, `Parts/Resources/` にもマスタがある |
| `Scenes/` | ビルド対象は `MainScene`（起動シーン）, `SelectMenu`, `Briefing`, `Customize`, `Battle`, `Result`, `License` と `Fields/Field_XXX/*.unity`（`FieldSettings` は `Scenes/Fields/Resources/`）。`Debug/` はデバッグ用のシーン（ビルド対象外。`EffectCheck`: パーツのエフェクト確認） |
| `Models/`, `Textures/`, `Shader/`, `Fonts/`, `Terrain/`, `ProBuilder Data/` | アセット |
| `Plugins/UniRx/` | UniRx（サードパーティ。編集しない） |
| `UnityTechnologies/ParticlePack/`, `CleanFlatIcon/` | Asset Store の外部アセット（git 管理外。下記「注意事項」参照） |
| `MainControls.inputactions` | Input System のアクション定義。生成コードは `Script/Input/Generated/MainControls.cs` |

### `unity/Assets/Script/`
| フォルダ | 責務 |
|---|---|
| `Main/` | 起動処理（`InitializeObject`） |
| `State/`, `Scene/` | ゲーム状態の管理とシーン遷移 |
| `Master/` | マスタデータの ScriptableObject 定義と読み込み（`MasterManager`） |
| `Mission/`, `Field/` | ミッション定義、フィールド読み込み、スポーン地点 |
| `Player/` | プレイヤー機体の入力・移動・カメラ・脚 IK（`Player/Animation/`）。移動の計算は `Player/Movement/`（`PlayerMoveCalculator` など。Unity に依存せずテストできる） |
| `Parts/` | 機体パーツ（ブースター、武器）の組み立て |
| `Npc/` | NPC と部隊 AI（`Npc/Squad/`） |
| `Unit/` | 戦車・ドローン・砲台などのユニット |
| `Bullet/`, `HitReceiver/`, `Target/` | 弾、被弾・撃破処理、目標オブジェクト |
| `Effect/` | エフェクトのプール管理と再生 |
| `EffectCheck/` | エフェクト確認シーンのパーツ呼び出しと操作（`PartEffectTester` を種類ごとに継承する。初期化は `Utils/DebugBootstrap`） |
| `Graphics/` | URP の Renderer Feature（アウトライン、ラジアルブラー）とグラフィック設定 |
| `Input/` | 入力管理（`PlayerInputManager`）。`Input/Generated/MainControls.cs` は自動生成コード |
| `UI/` | 画面ごとの UI（`Battle/`, `Briefing/`, `Customize/`, `MissionSelect/`, `FieldSelect/`, `License/`） |
| `Utils/` | シングルトン基底クラス、物理・入力ユーティリティ、`GameLayers` |
| `Editor/` | エディタ拡張（マスタデータビューア、CSV 入出力、`PlayMainSceneMenu`） |

## アーキテクチャ要約

- **namespace**: `AdvancedGears`（エディタ拡張の一部は `AdvancedGears.Editor`）。新規ファイルも `AdvancedGears` に置く。`AdvancedGears.Editor` namespace があるため、namespace 内で UnityEditor の `Editor` クラスを継承するときは `UnityEditor.Editor` と完全修飾する。例外は自動生成の `MainControls`（グローバル namespace）
- **アセンブリ**: `AdvancedGears.Runtime`（`Script/`）と `AdvancedGears.Editor`（`Script/Editor/` と、asmref 経由の `Assets/Editor/`、Editor 専用）。Runtime の参照先は UniRx、Unity.InputSystem、Unity.TextMeshPro、UnityEngine.UI、URP Core/Universal、Unity.Animation.Rigging。新しいパッケージを使うときは asmdef の references に追加する。NUnit はテスト専用なので Runtime では使わない（アサーションは `UnityEngine.Assertions`）
- **シングルトン**
  - `SingletonMonoBehaviour<T>`（`Utils/`）: Manager 系。`Instance` に初めてアクセスした時点で、`SingletonMonoObject` の GameObject に `AddComponent` して生成し、`DontDestroyOnLoad` で常駐させる。そのため参照するだけで生成されるので、初期化順に注意する
  - 対象: `StateManager`, `SceneManager`, `MasterManager`, `FieldManager`, `PlayerManager`, `NpcManager`, `SquadManager`, `BulletManager`, `EffectManager`, `BattleUIManager`, `TimerManager`, `PlayerInputManager`
  - `Singleton<T>`: MonoBehaviour でない純粋クラス用。`XxxMaster`（マスタのコンテナ）で使う
- **起動の流れ**: `MainScene` の `InitializeObject.Start()` → `MasterManager.LoadAll()` → `SceneManager.Register()` → `FieldManager.Register()` → `StateManager.SetStart()`。他のシーンから直接 Play すると、この初期化を通らない
- **状態とシーン遷移**: `StateManager` が `GameState`（Start → Select → Briefing → Battle → Result → Select、`Customize` は `GoToState` で任意遷移）を持つ。状態変更時に UniRx の Subject で通知し、`SceneManager` が対応するシーンを読み込む。`Battle` は読み込み後に `FieldManager.LoadPendingMission()` でフィールドシーンを Additive で読み込む
- **マスタデータ**: ScriptableObject を `Resources/` に置き、`MasterManager.LoadAll()` が `Resources.LoadAll<T>` で各 `XxxMaster.Instance`（`IDBasedMasterContainer<T>`、ID をキーにした辞書）へ読み込む。参照は `XxxMaster.Instance.GetSettings(id)`。エディタでは `MasterDataEditorWindow`（一覧表示）と `MasterCsvIO`（CSV 入出力）で確認・編集する

## 検証方法

- **コンパイル確認**: コードを変更したら、Unity CLI MCP の `console_status` / `console` でコンパイルエラーが 0 件であることを確認する。反映されていなければ `recompile` を実行する
- **動作確認**: 「デバッグ実行」の手順で `WarGame/Play Main Scene` から Play し、`console` でエラーと例外を確認する
- **テスト**: EditMode テストを Unity CLI MCP の `run_tests`（`mode: editor`、`filter_type: assembly`、`filter: AdvancedGears.Tests.EditMode`）で実行し、全件 green であることを確認する
  - テストは `unity/Assets/Tests/EditMode/`（asmdef: `AdvancedGears.Tests.EditMode`、Editor 専用、`UNITY_INCLUDE_TESTS` 定義時のみコンパイル）。Runtime と Editor の両アセンブリを参照できる
  - Unity に依存しない純粋なロジック（データクラス、パラメーター計算、マスタのコンテナなど）を変更・追加したらテストも追加する
  - PlayerPrefs など Editor の外部状態に触れるテストは、`SetUp` で退避して `TearDown` で元に戻す（例: `PartsAssembleDataTests`）
- **Editor を起動していないとき**（Blender 作業中など）: リポジトリルートで `bash tools/unity/compile-check.sh`（コンパイル）と `bash tools/unity/run-tests.sh [フィルタ]`（EditMode テスト）を実行する。Unity をバッチモードで起動するため、Editor がプロジェクトを開いている間は使えない（案内を出して終了する）。ログは `unity/Logs/compile-check.log` / `run-tests.log`
  - スクリプトに変更がないと Unity は再コンパイルしないため、`compile-check.sh` の警告一覧は変更があったときだけ表示される
- **クラウド環境**: Unity が無いためコンパイルも動作も検証できない。その場合は PR 本文に「未検証（クラウド環境のため）」と明記する（GameCI による CI は保留中: #54）

## 禁止・注意事項

- `unity/Assets/Script/Input/Generated/MainControls.cs` は `MainControls.inputactions` から自動生成されるため、手で編集しない（アクションの変更は `.inputactions` 側で行う。生成先は `.inputactions` のインポート設定 `wrapperCodePath` で指定済み）
- 新規アセット・スクリプトの `.meta` は Unity に生成させ、必ずコミットに含める。手で `.meta` を作ったり GUID を書き換えたりしない（参照切れの原因になる）
- `UnityTechnologies/ParticlePack` と `CleanFlatIcon` は Asset Store のアセットで git に含まれていない。新規環境では Asset Store から導入し、`SetupChecker` で有無を確認する。自作コードはこれらのスクリプトに依存していない（`SetupChecker` が文字列でパスを見ているだけ）。依存を増やさないこと
- `Plugins/UniRx/` は編集しない
- `unity/.ignore` で、UniRx とフォントの SDF アセット（約 35 MB）を Grep の全体検索から除外している。検索するときは Grep の `path` に UniRx のフォルダ、または SDF のファイルを直接指定する（Glob は `.ignore` の影響を受けない）
- Unity が自動で変更した `unity/ProjectSettings/` のファイルは、作業内容と無関係ならコミットに含めない

## Blender 連携

- **モデリング（Blender でのモデル作成と Unity への取り込み）を始める前に、`docs/modeling-work-policy.md` を読んで従う**（切り替え回数・ツールの戻り値を最小にする、形は数値で確かめる、モデルはスクリプトで作る、など）
- パイプラインの手順は `docs/blender-unity-pipeline.md`、エクスポート用スクリプトは `tools/blender/export_to_unity.py`
- Blender MCP からエクスポートするときは、`tools/blender` を `sys.path` に追加して `export_to_unity` を import し、`validate_objects()` で命名規約を確認してから `export_objects([...])` / `export_selected()` を呼ぶ（`dry_run=True` で出力先だけ確認できる）。使い方はパイプラインの手順書の「エクスポート用スクリプト」
  - MCP のコードには UI のコンテキストが無い（`bpy.context.object` は使えず、`bpy.ops.mesh.primitive_*` の後に作ったオブジェクトを取れない）。オブジェクトの作成や参照は `bpy.data` を使う
  - 試しにエクスポートするときは `base_path` に scratchpad などを指定し、`unity/Assets/Models` に不要な FBX を作らない（Unity が取り込んで `.meta` ができる）
- **保存済みの `.blend` の検証・エクスポートは、GUI を開かずに `bash tools/blender/export.sh <file.blend> [--objects <名前...> | --all] [--validate-only | --dry-run] [--base-path <path>]` で行える**。標準出力に結果の JSON、終了コードは 0 = 成功 / 2 = 規約違反 / 1 = その他のエラー。GUI で編集中の内容は保存してから実行する
  - 使い分け: モデリングなど状態を見ながら進める作業は MCP、エクスポート・検証は `export.sh`、GUI を開かずに `.blend` を調べるのは MCP の `_for_cli` ツール（パイプラインの手順書の「MCP・コマンドラインの使い分け」）
  - **Unity Editor の起動中は実行しない**（メモリ不足になるため。`export.sh` は Editor が起動中なら実行せずに終了する）。Unity を閉じてから使う
- Blender MCP の導入手順は `README.md` の「開発環境セットアップ > Blender MCP」

### Unity と Blender の切り替え

- **メモリ節約のため、Unity Editor と Blender は同時に起動しない**（Unity だけで約 3.5 GB 使う）
- 切り替えは `/switch-app <blender|unity>` で行う（スクリプト: `unity/.claude/scripts/switch-to-blender.ps1` / `switch-to-unity.ps1`）
  - Unity → Blender: 未保存シーンを片付け（「デバッグ実行」の手順）→ Unity を quit → Blender を起動 → MCP アドオン（`localhost:9876`）の応答を待つ
  - Blender → Unity: （必要なら FBX をエクスポート）→ スクリプトが MCP アドオン経由で `.blend` を保存 → Blender を閉じる → Unity を起動 → `ready` を待つ
  - Unity の未保存シーン、Blender の無題シーンがあるとスクリプトは中断する。**強制終了（`Stop-Process` など）はしない**
- **Blender 作業は開始時に名前付きの `.blend` として保存しておく**。MCP（Python）で行った変更は `bpy.data.is_dirty` に反映されず、閉じても保存確認が出ないまま失われるため、`is_dirty` で保存の要否を判断しない
- Unity の終了に `quit` コマンドは使わない（`com.unity.pipeline` の不具合で Editor モードでは失敗し、Editor の登録だけが外れる）。スクリプトはウィンドウを閉じる方法で終了する
- SessionStart フックは、Blender が起動中なら Unity を自動起動しない
- Blender 作業中は unity-mcp のツールが使えない（Editor 未起動のため。コンパイルとテストは `tools/unity/*.sh` で確認できる）。逆に Unity 作業中は Blender MCP のツールが使えない

## Unity Editor の操作（Unity CLI）

Unity Editor の操作には **Unity CLI の MCP**（`unity mcp`、MCP サーバー名 `unity-mcp`）を使用する。旧 UnityMCP プラグイン（`com.coplaydev.unity-mcp`）は使用しない。

- MCP 接続は自動化済み: `unity/.mcp.json` で `unity mcp --project-path .` を登録し、`unity/.claude/settings.json` の SessionStart フック（`.claude/hooks/ensure-unity-editor.ps1`）が Editor 未起動時に自動で `unity open` する。Editor が ready になるとツールが有効になる（Claude Code は `unity/` で起動すること）
- Blender MCP も `unity/.mcp.json` に `blender` として登録済み（`uv --directory C:/blender_mcp/mcp run blender-mcp`、環境変数 `BLENDER_MCP_DIR` / `BLENDER_MCP_PORT` で変更可）。ツール呼び出し時に `localhost:9876` のアドオンへ接続するため、Blender を後から起動しても再接続は不要（ただしセッション開始時に MCP サーバー自体の起動に失敗していた場合は、`/mcp` から再接続が必要）。Unity と同時に起動しないこと（「Unity と Blender の切り替え」参照）。SessionStart フック（`check-blender-mcp.ps1`）が接続可否を通知する。Blender 側は MCP アドオンの Auto Start を有効にしておく
- Editor 操作はプロジェクトの Pipeline パッケージ（`com.unity.pipeline`）経由で行う
- MCP ツールが使えない場合は、ターミナルから `unity command <コマンド名> --caller plugin --skill unity-cli` で同等の操作を行う
- 作業前に `unity status` で Editor が `ready` であることを確認する。Editor が起動していなければ `powershell -NoProfile -ExecutionPolicy Bypass -File .claude/scripts/wait-unity-ready.ps1`（`unity/` で実行）で起動して `ready` まで待つ。`sleep` / `Start-Sleep` やループで `unity status` を繰り返し呼ばない（呼ぶたびに承認画面が出る）
- 利用できるコマンドは `unity command`（一覧）で確認し、名前を推測しない
- よく使うコマンド
  - コンパイル・コンソール確認: `console_status` / `console`
  - シーン構成の確認: `get_scene_hierarchy`
  - Play モード: `editor_stop` / `editor_status`（開始は下記「デバッグ実行」に従う）
- Editor に接続できる間は `.unity` / `.prefab` / `.asset` の YAML を直接編集しない
- 接続できない場合はコンパイルエラーによる Safe Mode を疑い、`unity pipeline list` で確認する

### デバッグ実行

- AI エージェントがデバッグで Play する際は、必ず `PlayMainSceneMenu.PlayMainScene()`（`unity/Assets/Script/Editor/PlayMainSceneMenu.cs`、メニュー `WarGame/Play Main Scene`）から実行する
  - Unity CLI のメニュー実行コマンドで `WarGame/Play Main Scene` を呼ぶ（コマンド名は `unity command` の一覧で確認し、推測しない）
  - `editor_play` でシーンから直接 Play を開始しない（MainScene からの初期化経路を通らず、正しく再現できないため）。デバッグ用のシーンも下のメニューから起動する
- **Play の直前に、毎回シーンの保存状態を確認する**（未保存のシーンがあると `SaveCurrentModifiedScenesIfUserWantsTo` の「Scene(s) Have Been Modified」ダイアログで Editor が止まり、以降の MCP コマンドがすべてタイムアウトする）
  1. `list_open_scenes` で `isDirty` を確認する。再コンパイルやシーン操作の後は、触っていないシーンも dirty になることがある（Unity のバージョンアップに伴う自動再シリアライズなど）
  2. dirty なシーンがあれば、変更内容が作業で意図したものかを判断する。判断できないときは `EditorSceneManager.SaveScene(scene, "Temp/<名前>.unity", true)` でコピーを保存し、元ファイルと diff を取る
     - 意図した変更 → `save_scene` で保存する
     - 意図しない変更（自動再シリアライズなど） → 保存せず、`EditorSceneManager.OpenScene(path)` で開き直して破棄する。作業と無関係なシーン差分をコミットに混ぜない
  3. `isDirty` がすべて false になってから `WarGame/Play Main Scene`（またはデバッグ用のシーンのメニュー）を実行する
- Play 開始後に MCP コマンドがタイムアウトし続ける場合は、モーダルダイアログで止まっていることを疑う。人間に Editor の画面確認を依頼する
- 実行後は `editor_status` で Play 中であることを確認し、ログは `console` で確認する。停止は `editor_stop` で行う
- 例外: 特定シーン単体の検証を人間が明示的に指示した場合のみ、直接 Play してよい
- デバッグ用のシーン（MainScene を通らずに起動する前提で作ったもの）は、次のメニューから直接 Play してよい（人間の確認は要らない）。メニューは `WarGame/Play Main Scene` と同じく、Play の停止 → 未保存シーンの確認 → シーンを開く → Play を行う
  - `WarGame/Play Customize (Debug)`: `Scenes/Customize.unity`。カスタマイズ画面のデバッグモード（モデルの結合の確認）
  - `WarGame/Play Effect Check`: `Scenes/Debug/EffectCheck.unity`。パーツのエフェクト確認
  - メニューの前にも上の保存状態の確認を行う。終わったら `editor_stop` で止め、Play 中に dirty になったシーンは保存せずに開き直して破棄する
  - デバッグ用のシーンを追加したら、`PlayMainSceneMenu.cs` にメニューを足し、この一覧にも書く

## 作業上のルール

- **スクリーンショットは、明示的に指示された場合と、`Design` ラベルの作業で PR に載せる場合だけ撮影する**（`capture_game_view` / `capture_scene_view` 等）。それ以外の確認目的での自動撮影は行わない
  - `Design` の PR には、変更した見た目のスクリーンショットを 1〜3 枚載せる（エフェクトは Play 中に再生して `editor_pause` で止めてから撮る。モデルは Blender のビューポートか Unity のシーンビュー）
  - 画像は `gh pr create --attach '<ファイル>#<説明>'`（作成後なら `gh pr edit <番号> --attach ...`）で載せる。本文には画像の参照（`![](./xxx.png)`）を書かない（置き換わらずに残ることがある。PR #161）。画像は本文の末尾に足されるので、位置を変えたいときだけ後から `gh pr edit` で本文を書き直す。画像はリポジトリにコミットしない
  - `capture_game_view` などの `save_path` は、プロジェクトの外（scratchpad など）の絶対パスで指定する。相対パスは `Assets/` 基準になり、Unity が取り込んで `.meta` ができる
  - 解像度は横 640 px 程度にする（スマートフォンで見て確認できる大きさ）。作業用セッションが自分で見て確かめるのは PR に載せる画像だけにする
- **人間が読む文章はすべて日本語で書くこと**。会話の返答・作業報告・質問、PR / Issue の本文とコメント、コミットメッセージ、ドキュメント、コードのコメントが対象。思考の過程は英語でもよいが、出力する前に日本語にする。コード中の識別子・コマンド・エラーメッセージの原文はそのままでよい。英語で出力してしまったら、指摘を待たずに日本語で出し直す。
- **ファイルの検索・読み取りは Grep / Glob / Read を使う**。PowerShell の `Select-String` パイプ（`Get-ChildItem ... | Select-String ...` など）は許可ルールがあっても毎回承認が必要になり、作業が止まるので使わない（GUID でプレハブ・シーンを探すときも Grep の `glob: *.{prefab,unity}` で指定できる）
- **git / gh のコマンドは 1 回の呼び出しで 1 つずつ実行する**。PowerShell と Bash は `;` / `&&` / パイプでつないだコマンドを 1 つずつ判定し、許可リストに無いもの（`Select-String`、`Select-Object`、`Out-Null`、変数への代入など）が 1 つでも混ざると全体が承認待ちになる
  - コミットメッセージと PR / Issue の本文は、Write で scratchpad のファイルに書いてから `git commit -F <ファイル>`、`gh pr create --body-file <ファイル>` で渡す。`$body = @'...'@` のような here-string と変数は使わない
  - 出力を絞るために `Select-String` / `Select-Object` をつながない。`gh` は `--json` / `--jq`（`-q`）、git は `--oneline` / `--short` / `-n` などのオプションで絞る
  - **`cd` と `git -C` は使わない**。`cd` と git をつないだコマンドは、Claude Code が安全確認で毎回承認を求める（「常に許可」も選べない）。git は `unity/` の中でもそのまま動くので、repo ルートのファイルは `git add ../docs/xxx.md` のように相対パスで指定する
  - push は `git push -u origin <ブランチ名>` の形で書く（許可ルールに合わせるため。`git push` だけや `git push -q` は承認待ちになる）
  - ブランチの作成・切り替えは `git switch -c <ブランチ名>` / `git switch <ブランチ名>` で行い、`git checkout` は使わない（`git switch` は許可済み。`git checkout` は許可リストに無く承認待ちになる。`git checkout .` などで変更を捨てられるため、許可にも追加しない）
- **ファイルの書き込みは Write / Edit を使う**。`printf ... > ファイル`、`echo ... >`、`sed -i` などシェルでの書き込みや、`sed -n` / `cat` での読み取りはしない
- **承認画面の原因になる書き方をしない**。次の書き方は PreToolUse フック（`unity/.claude/hooks/enforce-command-style.ps1`）が承認画面を出さずに拒否し、代わりのやり方を返すので、それに従って書き直す（調査: `docs/permission-prompt-survey.md`）
  - git / gh をほかのコマンドとつなぐ、`cd` と git の組み合わせ、`git -C`
  - ループ（`for` / `while` / `ForEach-Object`）: 1 回に 1 つずつ呼ぶ。互いに依存しない呼び出しは 1 つのメッセージで並べて呼ぶ。Issue をまとめて読むときは `gh issue list --json ... --jq ...`
  - 変数の代入（Bash の `X=...`・`export`、PowerShell の `$x = ...`・`$env:...`）、here-string、コマンド置換 `$(...)`: パスや値は毎回そのまま書く
  - PowerShell のパイプ（`Select-String`、`Select-Object`、`Where-Object`、`Out-*`、`ConvertFrom-Json` など）: Grep / Read ツールや `--json` / `--jq` を使う
  - シェルでのファイル書き込み（`>` / `>>`、heredoc、`sed -i`、`tee`、`Set-Content`、`Out-File`）: Write / Edit を使う
  - `sleep` / `Start-Sleep` での待機: Unity の起動は `wait-unity-ready.ps1`、それ以外は `Monitor` ツール
  - どうしても必要なときだけ、コマンドの末尾に `# 承認が必要: <理由>` のコメントを付ける。フックは判定せず通常の許可判定に回す（許可リストにあるコマンドや `sleep` のように確認なしで通るコマンドはそのまま実行され、それ以外は承認画面が出る）。理由は人間が承認画面で読むので具体的に書く
- **別のブランチの作業は `EnterWorktree` / `ExitWorktree` ツールで行う**（`git worktree` を手で使うより承認が少ない）
  - 新しいブランチ: `EnterWorktree`（`name` に短い名前）で repo ルートの `.claude/worktrees/<名前>/` に `origin/main` から作られる。ブランチ名は自動で付くので、作った直後に `git branch -m feature/issue-{番号}-{内容}` で命名規則に合わせる。作業が終わって push したら `ExitWorktree`（`action: remove`）で片付ける（未 push の変更があると拒否される）
  - 既存のブランチ（作業中の PR など）: `git worktree add ../.claude/worktrees/<名前> <ブランチ名>` で作り、`EnterWorktree` の `path` で入る。この worktree は `ExitWorktree` では消えないので、`ExitWorktree`（`action: keep`）で戻ってから `git worktree remove ../.claude/worktrees/<名前>` で片付ける
  - worktree の中で Unity Editor を開く作業はしない（Library の再生成と Windows のパス長制限のため）。docs・設定ファイル・C# の数か所の修正に使う
- **許可設定は 2 か所にある**。`unity/.claude/settings.json` は `unity/` で起動した通常のセッション用で、repo ルートの `.claude/settings.json` は worktree 用（worktree に入ると作業フォルダが worktree のルートになり、こちらだけが読まれる）。repo ルートには git / gh の規則、`EnterWorktree` / `ExitWorktree`、`claude-code-remote` の MCP の許可、PreToolUse フックだけを置き、Unity / Blender の MCP の許可は置かない
  - git / gh と `claude-code-remote` の allow・ask・deny とフックを変えるときは、両方のファイルを揃える
  - フックのスクリプトは `unity/.claude/hooks/` の 1 つだけ。repo ルートの設定からは `$CLAUDE_PROJECT_DIR/unity/.claude/hooks/...` で参照する

## UI 実装ポリシー

UI を実装・編集する際は以下のルールに従うこと。

- `Assets/Prefabs/UI/` 以下に各画面用のフォルダを作成し、プレハブを格納する  
  例: `Assets/Prefabs/UI/Briefing/BriefingPanel.prefab`
- シーン上の Canvas は `Assets/Prefabs/UI/UICanvas.prefab` をベースとして配置し、その子に各 UI パーツを置く
- **各 UI パネルはプレハブとしてシーンに配置すること**。シーン上のオブジェクトはプレハブインスタンスとして参照を持った状態にし、プレハブ参照が切れた（Unpacked / Missing Prefab）状態で放置しない
  - 正しい手順: プレハブファイルを `Assets/Prefabs/UI/` に作成（`create_prefab`）→ シーンへはそのプレハブをドラッグまたは Unity CLI でインスタンスとして配置
  - プレハブへの変更は「プレハブを編集してシーンに反映」で行い、シーン上のインスタンスを直接 Unpack してから編集する方法は避ける
- テキストには `TextMeshProUGUI` を使用する
- `TextMeshProUGUI` のフォントは **`Assets/Fonts/NotoSansJP/NotoSansJP-VariableFont_wght SDF.asset`**（GUID: `255d0acb36bcebb44a76ca265a789374`）をデフォルトとして指定すること
- GameObject を作成した際は Scale を確認し、**特に指定がない限り Scale は (1, 1, 1) に設定すること**
- 実際の UI 編集作業は **Unity CLI の MCP**（または `unity command`）で Editor を操作して実施する（シーン・プレハブのファイルを直接編集しない）

---

## 開発フロー

Issueに着手する際は必ず以下のフローに従うこと。

### 1. 着手前の確認
- `/issue-start <番号>` スキルでこの手順を実行できる
- 就寝前などに作業を中断するときは `/suspend-work`、再開するときは `/resume-work` を使う（引き継ぎの要約を Issue にコメントするので、セッションが再開できなくても新しいセッションで続けられる）
- Issueの内容を読み、実装方針を提示する
- 他のオープンなIssueとの競合・依存関係を予測して報告する
- 人間の承認を得てから実装に進む
- IssueごとにBranchを作成する。ブランチ名はIssueの内容を予測できる文言にすること
  - 命名規則: `feature/issue-{番号}-{内容を表す短い英語}`
  - 例: `feature/issue-7-squad-ai`, `feature/issue-9-briefing-screen`

### 2. 実装後の自己チェック
- Issueの完了条件をすべて満たしているか確認する
- C# やアセットを変更した場合は `/unity-verify`（動作確認が必要なら `/unity-verify play`）でコンパイルと実行時エラーを確認する
- 意図しないファイルへの変更がないか確認する（Unity が自動で変更した `unity/ProjectSettings/` や、自動再シリアライズだけの差分を混ぜない）

### 3. コミット・PR作成
- 問題がなければコミットしてPRを作成する
- PRの説明にはIssue番号（Closes #XX）と実装概要を記載する（`.github/pull_request_template.md` に沿う。仕様変更があれば `docs/spec/` の変更を含める）
- PRには Issue と同じ分野ラベルを付ける（「ラベル」参照）。作業ログの PR だけは例外で、分野ラベルではなく `WorkLog` だけを付ける（「4. マージ後の記録・クローズ」）

### 4. マージ後の記録・クローズ
- マージ完了後、以下の形式で `docs/logs/issue-{番号}-log.md` を作成する
- ログの PR は `docs/logs/issue-{番号}-log.md` だけを変更し、`WorkLog` ラベルを付ける。条件を満たせば GitHub Actions（`.github/workflows/auto-merge-worklog.yml`）が自動でマージする。条件は、作者が許可リストに含まれる、このリポジトリのブランチから出された PR、作業ログのファイルの追加か変更だけ、1 ファイル 200 行以内。満たさないときは理由が PR にコメントされるので、人が確認してマージする
- Issueをクローズする

#### ログのフォーマット
---
Issue: #XX タイトル
実装日: YYYY-MM-DD
変更ファイル:
- 

実装方針:

詰まった点・解決策:

残課題・関連Issue:
---

### 5. 次のIssueの提案
- マイルストーンと依存関係を考慮して次に着手すべきIssueを提案する

### ラベル

Issue と PR は分野ラベルで区別する。作業の中心になるものを原則 1 つ付ける（デザインとコードが半々のときだけ 2 つ付けてよいが、できれば Issue を分ける）。既定のラベル（`bug`、`enhancement` など）は種類を表すラベルとして併用する。

| ラベル | 対象 | 作業できる環境 |
|---|---|---|
| `GameLogic` | コードの修正で完結するもの（C#、テスト、マスタデータ） | ロジック担当のマシン。簡素なコード修正だけクラウドでも可（「クラウドセッション」参照） |
| `Design` | モデリング、エフェクト、UI の見た目、シェーダー | デザイン担当のマシンのみ（Blender / Unity Editor が必要） |
| `Management` | CLAUDE.md、スキル、フック、CI、ドキュメント | クラウドでも可 |
| `WorkLog` | 作業ログ（`docs/logs/issue-{番号}-log.md`）の PR 専用。分野ラベルの代わりにこれだけを付ける。条件を満たせば自動でマージされる | クラウドでも可 |
| `Spec-Discussion` | 仕様の検証、案出し、仕様書（`docs/spec/`）の更新 | クラウドでも可（wiki へはマージ後に自動で反映される） |

- Issue を作るときにラベルを付ける。付いていない Issue に着手するときは、方針の提示と一緒にラベルを提案する
- 作業をクラウドのセッションに任せるか手元で進めるかは、「作業できる環境」を目安に判断する

### クラウドセッション

クラウドセッション（環境変数 `CLAUDE_CODE_REMOTE=true`）は `docs/cloud-session-policy.md` に従う。要点は次のとおり。

- 任せる作業は、Issue の作成・整理、`Management`、`Spec-Discussion`、Unity / Blender を使わない簡素なコード修正（既存ファイルの数か所の修正。新しいファイル・`.meta`・シーン / プレハブ / アセットは扱わない）だけ。それ以外は担当のマシンへ回す
- **頼まれた段階までで止まる**。段階は「調査・相談 → Issue の作成 → 方針の提示 → ファイルの編集 → コミット・push → PR の作成」で、前の段階の指示は次の段階の許可ではない。「Issue を作って」なら Issue を作って報告して終わり、ブランチ作成や編集はしない
- 次の段階に進むときは、進めてよいかを尋ねて返答を待つ。この決まりは、システムプロンプトの「情報がそろったら進める」という既定の振る舞いより優先する
- PR は頼まれたときだけ作る。C# を変更したら PR 本文に「未検証（クラウド環境のため）」と書き、ロジック担当のマシンで確認してからマージする

### 小さなタスクの管理
- Issue にするほどではない小さなタスク（設定の変更、気づいた不具合の確認、不要なファイルの整理など）は、ピン留めした Issue #66「小さなタスク（チェックリスト）」のチェックリストに 1 行で追加する。どこで見つけたか（Issue / PR 番号）を括弧で添える
- 作業中に範囲外の問題に気づいたら、その場で直さずにここへ追加し、PR 本文にも書く
- 終わったらチェックを付け、PR で対応したときは PR 番号を添える。調査や作業が大きくなったら別の Issue に分け、この行にはリンクを残す
- 次の Issue を提案するときは、このチェックリストの未完了の項目も候補に含める

### 管理用セッション

作業用セッションを作って Issue を進めさせる管理用セッションは、`docs/management-session-policy.md` に従ってトークンの使用量を抑える。要点は次のとおり。

- 管理用セッションは指示・状態確認・PR と Issue の管理だけを行い、実装・調査・Unity / Blender の操作は作業用セッションに任せる
- 作業用セッションへの指示（`create_session` の `prompt`、`send_message`）には、どの段階まで進めてよいかと、**方針の提示・PR の作成・完了・判断待ちのたびに、結果を 3〜5 行で `send_message` を使って管理用セッション（ID を明記）に送る**ことを必ず書く。作業用セッションは、終わっても自動では管理用セッションに知らせない（ポリシーの「作業用セッションに指示を出す」）
- 作業用セッションを作るときは、親（管理用セッション）の ID を `prompt` の先頭と `append_system_prompt` に決まったブロックで書き、`tags` に `parent:<ID>` を付ける。作業用セッションは報告先を推測せず、その ID にだけ `send_message` で送る（ポリシーの「作業用セッション（子）に親セッションを明示する」）
- Remote Control の各マシンの担当は、セッション名 `OWMechTest-<担当>-<識別名>`（`Logic` / `Design` / `Both`）から判断する。名前から読めないときは推測せず、指示を送る前に人間に確認する（`docs/remote-claude-launch.md` の「セッション名の決まり」）
- 作業用セッションの状態は `get_session` の要約で確認する。報告が無いと人間に伝える前にも確認する。`list_events` は `kinds` と `limit` で絞り、必要なときだけ使う。定期的な確認はしない
- 作業用セッションが承認を求める Blender のコマンド（`execute_blender_code` のコードや `export.sh` など）について人間に頼まれたら、管理用セッションが内容を評価し、何をするか・書き込むファイル・元に戻せない操作・承認してよいかを解説する（ポリシーの「Blender のコマンドを評価して解説する」）
- 大きな出力（ファイル全体、diff 全体、シーン階層、コンソール）を読み込まない
- 作業の区切りで、新しいセッションに引き継ぐよう人間に提案する（引き継ぎの形式はポリシーを参照）
- 複数の Issue・セッションにまたがる作業は、`docs/` に進捗の表を持つドキュメントを作って管理し、状態が変わるたびに更新する（例: `docs/effects-plan.md`。ポリシーの「複数のセッションにまたがる作業は docs で進捗を管理する」）
- 作業用セッションと作業フォルダを共有しているときは、ブランチを切り替えない。別の PR は `EnterWorktree` / `ExitWorktree` で worktree を作って行う（「作業上のルール」参照）
