---
name: dispatch-machines
description: クラウドの管理用セッションで、Issue の状況と docs/issue-order.md を確かめ、Remote Control で起動中の各マシンの管理用セッションに着手する Issue を割り振って指示を送る。「各マシンにタスクを割り振って」「イシュー状況を見てリモートマシンに実行させて」と言われたとき、または進み具合を聞かれたとき（引数 status）に使う。
argument-hint: "[status]"
---

# /dispatch-machines

クラウドの管理用セッション（3 層の 1 層目）の手順。運用の決まりは `docs/management-session-policy.md` の「10. クラウド → マシン → 作業用セッションの 3 層で進める」。

- 引数なし: 割り振りと指示（下の 1〜6）
- 引数 `status`: 状況の確認だけ（下の「status」）

このセッションでは実装・調査・Unity / Blender の操作をしない。大きな出力を読み込まない（ポリシーの「3」）。

## 1. 自分の ID を確かめる

- `get_session` を引数なしで呼び、自分のセッション ID を控える

## 2. Issue の順番と状況を読む

- `git fetch origin main` の後、`git show origin/main:docs/issue-order.md` で順番を読む（作業ブランチのファイルは使わない）
- 表に出てくる Issue の状態とラベルを 1 回で取る:
  `gh api "repos/star-globe/OWMechTest/issues?state=all&per_page=100&sort=updated" --jq '.[] | select(.pull_request|not) | "\(.number) \(.state) [\(.labels|map(.name)|join(","))]"'`
- オープンな PR: `gh api "repos/star-globe/OWMechTest/pulls?state=open&per_page=50" --jq '.[] | "PR \(.number) \(.head.ref) \(.title)"'`

## 3. 起動中のマシンのセッションを調べる

- `list_sessions`（`mine: true`、`limit` は 10 程度）で、`environment_kind` が `bridge`、タイトルが `OWMechTest-<担当>-<識別名>` の、最近作られたセッションを探す
- 担当（`Logic` / `Design` / `Both`）はタイトルだけで判断する。読めないときは推測せず、人間に尋ねる
- `parent:` タグの付いたセッションは作業用セッション（3 層目）。そのマシンで動いている作業用セッションが承認待ち・作業中なら、そのマシンには新しく割り振らない

## 4. 割り振りを決める

`docs/issue-order-policy.md` の「2. 順番を読む」に従い、マシンごとに次を満たす最初の Issue を選ぶ。

1. 「先に終わっている必要がある Issue」がすべてクローズしている
2. `InProgress` ラベルが付いていない、その Issue のオープンな PR も無い
3. 表の「環境」がそのマシンの担当と合う（`Both` はどちらでも可）
4. 段階の番号が小さいもの、同じ段階なら上の行から

- 1 台のマシンには 1 つの Issue だけを割り振る。同じ Issue を 2 台に割り振らない
- 候補が無い、または迷うときは人間に尋ねる。割り振りを人間に示してから送る必要は無い（人間が「割り振って」と頼んだとき）。頼まれたのが確認だけなら、案を示して止まる

## 5. マシンの管理用セッションに指示を送る

`send_message` で、下のひな形の `<>` を埋めて送る。

- 初回は `elevated_session_untrusted_device` で拒否されることがある。そのときは、拒否に含まれる `content_digest` で `confirm_repository_trust` を呼び、人間が許可したら送り直す（許可すると、このセッションは信頼済みの端末専用になる。元に戻せない）

```
報告先: <自分の ID>（クラウドの管理用セッション）

このセッション（<マシンのセッション名>）を、<担当>担当のマシンの管理用セッションとして使ってください。`docs/management-session-policy.md` の「10」に従い、このセッションでは実装・調査・Unity / Blender の操作をせず、ブランチも切り替えません。`/run-worker <Issue 番号>` の手順で進めてください。

## 依頼
#<番号> <タイトル>（依存なし・InProgress なし・オープンな PR なしを確認済み）。このマシンの次の順は <次の Issue 番号の並び> です。

- 作業用セッションは、方針を提示したら人間の承認を待つ。承認後は PR の作成まで（マージはしない）
- <Design の場合: Design の PR なのでスクリーンショットを 1〜3 枚載せる。Blender に送るコードの先頭に目的を 1〜2 行のコメントで書く>

## 報告のしかた
クラウドのセッションへの `send_message` は届かないので送らないでください。人間に報告文の貼り付けも頼まないでください。作業用セッションを作ったとき、作業用セッションから報告を受けたときは、要点を 3〜5 行でこのセッションの最後の返答に書いてください。クラウドのセッションが読みに行きます。
```

## 6. 人間に報告する

- 割り振りの表（マシン / Issue / 次の順）と、指示を送ったかどうかを短く書く
- 作業用セッションの承認待ちには、人間が claude.ai でそのセッションを開いて答えることを伝える

## status

1. `list_sessions`（`mine: true`、`limit` は 10 程度）で、マシンの管理用セッションと作業用セッション（`parent:` タグ）の `status_bucket` と `post_turn_summary` を見る
2. 要約で足りないときだけ、マシンの管理用セッションの `list_events` を `kinds: ["assistant"]`、`limit: 5` で読み、最後の返答の報告を取り出す
3. 人間には、Issue ごとに「担当のセッション / 状態 / 待っていること」を表で示す。承認待ちなら、どのセッションを開けばよいかを書く
4. 作業が終わったマシン（PR 作成済み・作業用セッションが完了）があれば、次の割り振りを提案する（送るのは人間に頼まれてから）
