# 許可確認（承認画面）の調査

Claude Code の作業中に出る承認画面を減らすため、手元のセッション記録から「どの呼び出しで承認画面が出たか」を集計し、改修の案をまとめる。2 台の作業マシンの結果がそろったら、改修用のセッションを新しく作って進める。

関連: PR #146（つないだ git コマンドを拒否するフック、許可リストの追加、作業ルールの見直し）

## 進捗

| マシン | 集計日 | 対象 | 状態 |
|---|---|---|---|
| マシン A（PR #146 の検証をしたマシン） | 2026-11-15 | 16 セッション（2026-11-08 以降） | 集計済み |
| マシン B | - | - | 未集計 |
| 改修用のセッション | - | - | 2 台の結果がそろってから作る |

## 集計の手順

PowerShell 7 で、リポジトリのルートから次を実行する。読み取りだけで、何も書き換えない。

```
pwsh -NoProfile -File tools/claude/survey-permission-prompts.ps1
```

- `~/.claude/projects/**/*.jsonl` のうち新しい順に 50 件（`-MaxFiles` で変更）を読む。記録は `cleanupPeriodDays`（ユーザー設定）の日数で消えるので、それより前の分は残っていない
- 許可リストは、ユーザー設定（`~/.claude/settings.json`、`settings.local.json`）、repo ルートの `.claude/settings.json`、`unity/.claude/settings.json`、`unity/.claude/settings.local.json` を読む。worktree から実行するときは、`-RepoRoot` に本体のフォルダを指定する（`settings.local.json` は git 管理外で worktree に無いため）
- 出力は「承認画面の原因」「許可リストに無い MCP ツール」「ツール別の呼び出し回数」の 3 つ。結果は下の「集計結果」にマシンごとの節を作って貼る

判定の限界:

- 記録には承認画面が実際に出たかどうかが残らないので、今の許可リストで判定し直した「見込み」の件数になる
- PowerShell のコマンドレット（`Get-Content`、`Select-Object` など）を Claude Code が自動で許可するかは確かめていない。PowerShell の件数は多めに出ている可能性がある
- 複数行の引用符（コミットメッセージや PR 本文）を含むコマンドは、行の途中を別のコマンドと誤判定することがある（「許可リストに無い: Closes」のような行）

## 集計結果

### マシン A（2026-11-15）

対象は 16 セッション。Bash 196 回 / PowerShell 85 回のうち、承認画面が出たと見込まれるのは Bash 63 回 / PowerShell 47 回。

| # | 原因 | 件数 | 例 | 対策の状況 |
|---|---|---|---|---|
| 1 | `cd` と git / gh をつないだ | 25 | `cd /c/.../OWMechTest && gh issue view 66 ...` | PR #146 のフックで拒否する |
| 2 | `git -C` | 17 | `git -C .. status --short; git -C .. branch ...` | PR #146 のフックで拒否する |
| 3 | `gh issue edit` / `gh pr edit` | 12 | `gh issue edit 66 --body-file ...` | 書き込みなので承認が出てよい |
| 4 | PowerShell の変数代入・here-string | 9 | `$d = git diff ...; ($d \| Where-Object ...)` | CLAUDE.md にルールはあるが、フックでは拒否していない |
| 5 | `Start-Sleep` での待機 | 7 | `Start-Sleep -Seconds 10` | 未対応 |
| 6 | 待機のループ・コマンド置換 | 7 | `for i in $(seq 1 40); do ... unity status ...` | 未対応 |
| 7 | シェルでのファイル書き込み（`>`、`sed -i`、heredoc） | 7 | `gh issue view 66 ... > issue66-body.md` | PR #146 で「Write / Edit を使う」ルールを追加 |
| 8 | PowerShell のパイプ（`Select-String`、`Select-Object`、`ConvertFrom-Json`、`Out-String`） | 約 8 | `git diff ... \| Select-String '^-'` | CLAUDE.md にルールはあるが、フックでは拒否していない |
| 9 | `git worktree add` | 4 | | PR #146 で許可リストに追加 |
| 10 | `unity open` / `unity command` | 2 | | PR #146 で許可リストに追加 |

許可リストに無い MCP ツール:

| ツール | 件数 | 読み取り系か |
|---|---|---|
| `mcp__claude-code-remote__send_message` | 21 | いいえ（メッセージを送る） |
| `mcp__claude-code-remote__create_session` | 9 | いいえ（セッションを作る） |
| `mcp__claude-code-remote__get_session` | 5 | はい |
| `mcp__claude-code-remote__list_environments` | 2 | はい |
| `mcp__claude-code-remote__list_events` | 2 | はい |
| `mcp__claude-code-remote__list_sessions` | 1 | はい |

わかったこと:

- 承認画面の大半は、許可リストに無いコマンドではなく、つなぎ方（`cd` と git、`git -C`、変数代入、パイプ）とシェルでの書き込みが原因。許可リストに足して効く読み取り系のコマンドは、ほぼ無い
- `git -C` は、Bash の単独の呼び出しならユーザー設定の `Bash(git *)` で通る。承認が出るのは、ほかのコマンドとつないだときと PowerShell のとき
- 待機（`Start-Sleep`、`unity status` のループ）は、Unity の起動やテストの完了を待つときに出ている

### マシン B

（未集計。上の手順で集計して、マシン A と同じ形で書く）

## 設定で気になった点（マシン A）

承認画面とは逆に、確認なしで通ってしまう設定。

| 設定 | 場所 | 気になる点 |
|---|---|---|
| `Bash(git *)` | ユーザー設定、`unity/.claude/settings.local.json` | `git branch -D`、`git clean`、`git checkout -- .`、main 以外への push も確認なしで通る。ask のルールで止まるのは `--force`、`-f`、`reset --hard`、`push origin main` だけ |
| `Bash(python3 -c ' *)` | ユーザー設定の `settings.local.json` | 任意のコードが実行できる |
| `mcp__blender__execute_blender_code` | `unity/.claude/settings.local.json` | 任意のコードが実行できる（Blender のファイルを書き換えられる） |
| `PowerShell(Remove-Item *)` | `unity/.claude/settings.local.json` の allow | プロジェクトの設定では ask。ask が優先されるので確認は出るが、どちらにするか決めておく |
| `mcp__UnityMCP__*`（12 件）、`mcp__unity-mcp__Unity_RunCommand` | `unity/.claude/settings.local.json` | 旧 UnityMCP プラグインのツール名で、今は使わない |
| その場限りの許可（特定の `gh pr create`、`xargs grep -l "<GUID>"`、`Get-ChildItem ...` の完全一致など） | `settings.local.json` の各所 | 二度と一致しないので不要 |

## 改修の案

2 台の結果を見てから、改修用のセッションで優先順を決める。

1. **PR #146 のフックと許可リストを、worktree の中でも効く場所に移す**
   - `EnterWorktree` で入ると作業フォルダが worktree のルートになり、`unity/.claude/settings.json` が読まれない（PR #146 の検証で確認済み）
   - repo ルートの `.claude/settings.json` に移し、フックのパスも worktree の中で解決できるようにする
2. **PowerShell のつなぎ方もフックで拒否する**
   - 変数代入、here-string、`Select-String` / `Select-Object` などのパイプを、git / gh と組み合わせたときに拒否し、理由を返す（#4・#8）
3. **待機のやり方をルールにする**
   - `Start-Sleep` やループの代わりに `Monitor` ツールか、既存のスクリプト（`switch-to-unity.ps1` など）を使う（#5・#6）
4. **読み取り系の MCP ツールを許可リストに足す**
   - `mcp__claude-code-remote__get_session`、`list_sessions`、`list_events`、`list_environments`（管理用セッションの状態確認）
5. **確認なしで通る設定を見直す**
   - `Bash(git *)` を読み取りと決まった書き込み（`add`、`commit`、`fetch`、`switch`、`worktree` など）に分ける、または `git branch -D` / `git clean` / `git checkout --` を ask に足す
   - 任意のコードが実行できる許可（`python3 -c`、`execute_blender_code`）を残すか決める
   - `settings.local.json` の古いエントリとその場限りのエントリを消す（各マシンで行う。git 管理外のため）
