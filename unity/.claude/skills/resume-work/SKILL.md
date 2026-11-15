---
name: resume-work
description: /suspend-work で中断した作業を再開する。Issue（または PR）のコメントから最新の引き継ぎの要約を読み、ブランチと Unity / Blender の状態を確かめて、次の一手を提示する。「続きをやって」「昨日の作業を再開して」と言われたとき、前のセッションが再開できなくなったときに使う。
argument-hint: "[Issue番号]"
---

# /resume-work

引数: 引き継ぎの要約がある Issue 番号（省略可）。省略したら、今のブランチ名 `feature/issue-{番号}-...` から番号を読む。それでもわからなければ、どの Issue の続きかを人間に尋ねる。

要約を読んで状態を確かめ、次の一手を提示するところで止まる。作業を進めるのは、人間の了承を得てから。

## 手順

1. **最新の要約を読む**
   - `gh issue view <番号> --repo star-globe/OWMechTest --json comments --jq '[.comments[] | select(.body | startswith("## 中断時の引き継ぎ"))] | last | .body'`
   - 見つからなければ、ブランチの PR のコメントを同じ条件で探す（`gh pr view <番号> --repo star-globe/OWMechTest --json comments --jq ...`）
   - どちらにも無ければ、`git log --oneline -n 10` の `WIP:` のコミットと、その本文（`git log -n 1 --format=%B <ハッシュ>`）から状況を推測する。推測であることを人間に伝える

2. **ブランチを合わせる**
   - `git status --short` で未コミットの変更が無いことを確かめる。変更があれば、切り替える前に内容を人間に伝えて判断を仰ぐ
   - 要約のブランチに `git switch <ブランチ名>` で切り替え、`git pull` する
   - 最後のコミットが要約と一致するかを `git log --oneline -n 3` で確かめる。要約より新しいコミットがあれば、その内容も読む（別のマシンやセッションで進んでいる場合）
   - 要約が「未 push」なら、中断したマシンでないとコミットが無い。どのマシンで中断したかを人間に確かめる

3. **アプリの状態を確かめる**（要約の「再開の手順」に書いてあるものだけ）
   - Unity: `unity status` で `ready` を確かめ（起動していなければ `wait-unity-ready.ps1`）、`console_status` でコンパイルエラーの件数を見る
   - Blender: 要約の `.blend` を開く必要があれば `/switch-app blender <.blend>` で切り替える。Unity と同時に起動しない

4. **次の一手を提示する**
   - 要約の「次の一手」と「判断待ち・注意点」を、上で確かめた現在の状態と合わせて短く示す（ずれていれば、そのことも書く）
   - 判断待ちの事項は、ここで人間に尋ねる
   - 進めてよいかを尋ね、了承を得てから作業を始める
