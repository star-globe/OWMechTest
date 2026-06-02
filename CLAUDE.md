# CLAUDE.md

## プロジェクト概要
WarGameProject
カスタマイズ可能な人型兵器を駆り、戦術によって勝利を得るアクションシミュレーター

## 仕様書
GitHubWikiをサブモジュールとして `unity/docs/wiki/` に格納しています。
wikiを参照するコマンドを実行する際は`unity/docs/wiki/`で`git pull`を実行してwikiを更新してください。

## 仕様書の索引
- 基本仕様: docs/wiki/基本仕様.md
- 技術仕様: docs/wiki/技術仕様.md
- プレイヤー操作: docs/wiki/プレイヤー操作.md
- レイヤー管理: docs/wiki/レイヤー管理.md

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
