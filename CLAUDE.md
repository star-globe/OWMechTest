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
GitHubWikiをサブモジュールとして `unity/docs/wiki/` に格納しています。
wikiを参照する前に、リポジトリルートで以下を実行して最新化してください（サブモジュールは detached HEAD のため、`unity/docs/wiki/` での素の `git pull` は失敗します）。

```
git submodule update --init --remote unity/docs/wiki
```

`unity/docs/wiki/` が空の場合（クラウド環境・新規 clone 直後）も同じコマンドで取得できます。

## 仕様書の索引
パスはリポジトリルート基準。
- 基本仕様: unity/docs/wiki/基本仕様.md
- 技術仕様: unity/docs/wiki/技術仕様.md
- プレイヤー操作: unity/docs/wiki/プレイヤー操作.md
- レイヤー管理: unity/docs/wiki/レイヤー管理.md
- ターゲットシステム: unity/docs/wiki/ターゲットシステム.md
- ユニット全般: unity/docs/wiki/ユニット全般.md
- プロトタイプの方針: unity/docs/wiki/プロトタイプの方針.md

その他のドキュメント（リポジトリルートの `docs/`）
- AI 開発の改善プラン: docs/ai-dev-improvement-plan.md
- Blender ↔ Unity パイプライン: docs/blender-unity-pipeline.md
- RenderGraph ラジアルブラー: docs/rendergraph-radial-blur-guide.md
- 作業ログ: docs/logs/issue-{番号}-log.md

## ディレクトリマップ

### `unity/Assets/` 直下
| フォルダ | 内容 |
|---|---|
| `Script/` | 自作 C# コード（下表） |
| `Editor/` | `SetupChecker.cs`（必須外部アセットの有無を確認するエディタ拡張） |
| `Resources/` | マスタ ScriptableObject（`EffectSettings/`, `MissionSettings/`, `PlayerSettings/`, `UnitSettings/`, `GlobalParamSettings`, `PlayerControllerConstSettings`） |
| `Prefabs/` | プレハブ。`Prefabs/UI/` は画面ごとのフォルダ（UI 実装ポリシー参照）。`Bullets/Resources/`, `Effect/Resources/`, `Parts/Resources/` にもマスタがある |
| `Scenes/` | ビルド対象は `MainScene`（起動シーン）, `SelectMenu`, `Briefing`, `Battle`, `Result`, `License` と `Fields/Field_XXX/*.unity`（`FieldSettings` は `Scenes/Fields/Resources/`）。`SceneManager` が読み込む `Customize` シーンは現状存在しない |
| `Models/`, `Textures/`, `Shader/`, `Fonts/`, `Terrain/`, `ProBuilder Data/` | アセット |
| `Plugins/UniRx/` | UniRx（サードパーティ。編集しない） |
| `UnityTechnologies/ParticlePack/`, `CleanFlatIcon/` | Asset Store の外部アセット（git 管理外。下記「注意事項」参照） |
| `MainControls.cs` / `.inputactions` | Input System のアクション定義と自動生成コード |

### `unity/Assets/Script/`
| フォルダ | 責務 |
|---|---|
| `Main/` | 起動処理（`InitializeObject`） |
| `State/`, `Scene/` | ゲーム状態の管理とシーン遷移 |
| `Master/` | マスタデータの ScriptableObject 定義と読み込み（`MasterManager`） |
| `Mission/`, `Field/` | ミッション定義、フィールド読み込み、スポーン地点 |
| `Player/` | プレイヤー機体の入力・移動・カメラ・脚 IK（`Player/Animation/`） |
| `Parts/` | 機体パーツ（ブースター、武器）の組み立て |
| `Npc/` | NPC と部隊 AI（`Npc/Squad/`） |
| `Unit/` | 戦車・ドローン・砲台などのユニット |
| `Bullet/`, `HitReceiver/`, `Target/` | 弾、被弾・撃破処理、目標オブジェクト |
| `Effect/` | エフェクトのプール管理と再生 |
| `Graphics/` | URP の Renderer Feature（アウトライン、ラジアルブラー）とグラフィック設定 |
| `Input/` | 入力管理（`PlayerInputManager`） |
| `UI/` | 画面ごとの UI（`Battle/`, `Briefing/`, `Customize/`, `MissionSelect/`, `FieldSelect/`, `License/`） |
| `Utils/` | シングルトン基底クラス、物理・入力ユーティリティ、`GameLayers` |
| `Editor/` | エディタ拡張（マスタデータビューア、CSV 入出力、`PlayMainSceneMenu`） |

## アーキテクチャ要約

- **namespace**: 原則 `AdvancedGears`。ただし `Utils/`・`Graphics/`・`UI/` の一部など約25ファイルは namespace なし（Issue #33 で統一予定）。新規ファイルは `AdvancedGears` に置く
- **アセンブリ**: 自作コードはすべて `Assembly-CSharp`（asmdef は UniRx のみ。Issue #33 で導入予定）
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
- **テスト**: 現時点ではテストは 0 件（テスト用 asmdef なし）
- **クラウド環境**: Unity が無いためコンパイルも動作も検証できない。その場合は PR 本文に「未検証（クラウド環境のため）」と明記する

## 禁止・注意事項

- `unity/Assets/MainControls.cs` は `MainControls.inputactions` から自動生成されるため、手で編集しない（アクションの変更は `.inputactions` 側で行う）
- 新規アセット・スクリプトの `.meta` は Unity に生成させ、必ずコミットに含める。手で `.meta` を作ったり GUID を書き換えたりしない（参照切れの原因になる）
- `UnityTechnologies/ParticlePack` と `CleanFlatIcon` は Asset Store のアセットで git に含まれていない。新規環境では Asset Store から導入し、`SetupChecker` で有無を確認する。クラウド環境ではこれらが無いため、コンパイルが通らない可能性がある
- `Plugins/UniRx/` は編集しない
- Unity が自動で変更した `unity/ProjectSettings/` のファイルは、作業内容と無関係ならコミットに含めない

## Blender 連携

- パイプラインの手順は `docs/blender-unity-pipeline.md`、エクスポート用スクリプトは `tools/blender/export_to_unity.py`
- Blender MCP の導入手順は `README.md` の「開発環境セットアップ > Blender MCP」

## Unity Editor の操作（Unity CLI）

Unity Editor の操作には **Unity CLI の MCP**（`unity mcp`、MCP サーバー名 `unity-mcp`）を使用する。旧 UnityMCP プラグイン（`com.coplaydev.unity-mcp`）は使用しない。

- MCP 接続は自動化済み: `unity/.mcp.json` で `unity mcp --project-path .` を登録し、`unity/.claude/settings.json` の SessionStart フック（`.claude/hooks/ensure-unity-editor.ps1`）が Editor 未起動時に自動で `unity open` する。Editor が ready になるとツールが有効になる（Claude Code は `unity/` で起動すること）
- Blender MCP も `unity/.mcp.json` に `blender` として登録済み（`uv --directory C:/blender_mcp/mcp run blender-mcp`、環境変数 `BLENDER_MCP_DIR` / `BLENDER_MCP_PORT` で変更可）。ツール呼び出し時に `localhost:9876` のアドオンへ接続するため、Blender を後から起動しても再接続は不要。SessionStart フック（`check-blender-mcp.ps1`）が接続可否を通知する。Blender 側は MCP アドオンの Auto Start を有効にしておく
- Editor 操作はプロジェクトの Pipeline パッケージ（`com.unity.pipeline`）経由で行う
- MCP ツールが使えない場合は、ターミナルから `unity command <コマンド名> --caller plugin --skill unity-cli` で同等の操作を行う
- 作業前に `unity status` で Editor が `ready` であることを確認する。Editor が起動していなければ `unity open .`（`unity/` で実行）で起動する
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
  - `editor_play` で任意のシーンから直接 Play を開始しない（MainScene からの初期化経路を通らず、正しく再現できないため）
- **Play の直前に、毎回シーンの保存状態を確認する**（未保存のシーンがあると `SaveCurrentModifiedScenesIfUserWantsTo` の「Scene(s) Have Been Modified」ダイアログで Editor が止まり、以降の MCP コマンドがすべてタイムアウトする）
  1. `list_open_scenes` で `isDirty` を確認する。再コンパイルやシーン操作の後は、触っていないシーンも dirty になることがある（Unity のバージョンアップに伴う自動再シリアライズなど）
  2. dirty なシーンがあれば、変更内容が作業で意図したものかを判断する。判断できないときは `EditorSceneManager.SaveScene(scene, "Temp/<名前>.unity", true)` でコピーを保存し、元ファイルと diff を取る
     - 意図した変更 → `save_scene` で保存する
     - 意図しない変更（自動再シリアライズなど） → 保存せず、`EditorSceneManager.OpenScene(path)` で開き直して破棄する。作業と無関係なシーン差分をコミットに混ぜない
  3. `isDirty` がすべて false になってから `WarGame/Play Main Scene` を実行する
- Play 開始後に MCP コマンドがタイムアウトし続ける場合は、モーダルダイアログで止まっていることを疑う。人間に Editor の画面確認を依頼する
- 実行後は `editor_status` で Play 中であることを確認し、ログは `console` で確認する。停止は `editor_stop` で行う
- 例外: 特定シーン単体の検証を人間が明示的に指示した場合のみ、直接 Play してよい

## 作業上のルール

- **スクリーンショットは明示的に指示された場合のみ撮影すること**（`capture_game_view` / `capture_scene_view` 等）。確認目的での自動撮影は行わない。

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
- Issueの内容を読み、実装方針を提示する
- 他のオープンなIssueとの競合・依存関係を予測して報告する
- 人間の承認を得てから実装に進む
- IssueごとにBranchを作成する。ブランチ名はIssueの内容を予測できる文言にすること
  - 命名規則: `feature/issue-{番号}-{内容を表す短い英語}`
  - 例: `feature/issue-7-squad-ai`, `feature/issue-9-briefing-screen`

### 2. 実装後の自己チェック
- Issueの完了条件をすべて満たしているか確認する
- 意図しないファイルへの変更がないか確認する

### 3. コミット・PR作成
- 問題がなければコミットしてPRを作成する
- PRの説明にはIssue番号（Closes #XX）と実装概要を記載する

### 4. マージ後の記録・クローズ
- マージ完了後、以下の形式で `docs/logs/issue-{番号}-log.md` を作成する
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
