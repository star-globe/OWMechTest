---
name: issue-start
description: CLAUDE.md の開発フロー「1. 着手前の確認」を実行する。Issue 番号を受け取り、内容の確認・実装方針の提示・他 Issue との競合確認・承認後のブランチ作成までを行う。Issue に着手するとき、「#XX をやって」「Issue XX に取りかかる」と言われたときに使う。
argument-hint: <Issue番号>
---

# /issue-start

引数: Issue 番号（`$ARGUMENTS`）。無ければオープンな Issue を一覧して、どれに着手するか尋ねる。

## 手順

1. **Issue を読む**
   - `gh issue view $ARGUMENTS --repo star-globe/OWMechTest --comments`
   - Issue 本文に完了条件があれば控えておく（実装後の自己チェックで使う）

2. **関連する仕様とコードを確認する**
   - 仕様書を使う場合は先に `git submodule update --init --remote unity/docs/wiki` で wiki を最新化する
   - CLAUDE.md の「仕様書の索引」「ディレクトリマップ」「アーキテクチャ要約」から関係する箇所を特定し、既存コードのパターンを読む

3. **他の Issue・PR との競合を調べる**
   - `gh issue list --repo star-globe/OWMechTest --state open` と `gh pr list --repo star-globe/OWMechTest --state open`
   - 同じファイル・同じ仕組みを触るもの、先に終わっている必要があるもの（依存）を洗い出す

4. **実装方針を提示して承認を得る**
   - 変更するファイル、方針、検証方法（Unity でのコンパイル・Play 確認が要るか）、競合・依存をまとめて提示する
   - AskUserQuestion で承認を取る。承認されるまで実装・ブランチ作成はしない

5. **ブランチを作る**
   - 未コミットの変更があれば内容を確認する（Unity が自動で変更した `unity/ProjectSettings/` は作業と無関係なら残してよい）
   - `git switch main && git pull` で最新化してから `git switch -c feature/issue-{番号}-{内容を表す短い英語}`
   - 例: `feature/issue-7-squad-ai`

承認後の実装・検証・PR 作成は CLAUDE.md の「開発フロー」2〜3 に従う。C# を変更したら `/unity-verify` で確認する。
