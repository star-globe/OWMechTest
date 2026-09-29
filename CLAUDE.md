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
wikiを参照するコマンドを実行する際は`unity/docs/wiki/`で`git pull`を実行してwikiを更新してください。

## 仕様書の索引
- 基本仕様: docs/wiki/基本仕様.md
- 技術仕様: docs/wiki/技術仕様.md
- プレイヤー操作: docs/wiki/プレイヤー操作.md
- レイヤー管理: docs/wiki/レイヤー管理.md

## Unity Editor の操作（Unity CLI）

Unity Editor の操作には **Unity CLI の MCP**（`unity mcp`、MCP サーバー名 `unity-mcp`）を使用する。旧 UnityMCP プラグイン（`com.coplaydev.unity-mcp`）は使用しない。

- Editor 操作はプロジェクトの Pipeline パッケージ（`com.unity.pipeline`）経由で行う
- MCP ツールが使えない場合は、ターミナルから `unity command <コマンド名> --caller plugin --skill unity-cli` で同等の操作を行う
- 作業前に `unity status` で Editor が `ready` であることを確認する。Editor が起動していなければ `unity open .`（`unity/` で実行）で起動する
- 利用できるコマンドは `unity command`（一覧）で確認し、名前を推測しない
- よく使うコマンド
  - コンパイル・コンソール確認: `console_status` / `console`
  - シーン構成の確認: `get_scene_hierarchy`
  - Play モード: `editor_play` / `editor_stop` / `editor_status`
- Editor に接続できる間は `.unity` / `.prefab` / `.asset` の YAML を直接編集しない
- 接続できない場合はコンパイルエラーによる Safe Mode を疑い、`unity pipeline list` で確認する

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
