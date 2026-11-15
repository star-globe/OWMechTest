# 許可確認（承認画面）の調査

Claude Code の作業中に出る承認画面を減らすため、手元のセッション記録から「どの呼び出しで承認画面が出たか」を集計し、改修の案をまとめる。2 台の作業マシンの結果がそろったら、改修用のセッションを新しく作って進める。

関連: PR #146（つないだ git コマンドを拒否するフック、許可リストの追加、作業ルールの見直し）

## 進捗

| マシン | 集計日 | 対象 | 状態 |
|---|---|---|---|
| マシン A（PR #146 の検証をしたマシン） | 2026-11-15 | 16 セッション（2026-11-08 以降） | 集計済み |
| マシン B | 2026-11-15 | 29 セッション（2026-11-10 以降） | 集計済み |
| 改修 | 2026-11-15 | プロジェクトの設定・フック・ルール | PR を作成（「改修の内容」）。各マシンの片付けは未着手 |

## 集計の手順

PowerShell 7 で、リポジトリのルートから次を実行する。読み取りだけで、何も書き換えない。

PowerShell 7 は通常版（`winget install --id Microsoft.PowerShell --installer-type wix --source winget`）を入れる。Microsoft Store 版を入れると、Claude Code の PowerShell ツールが実行前の構文チェックで「コマンド ラインが長すぎます」と失敗し、コマンドが実行されなくなる（PR #150 のマシン B の検証）。

```
pwsh -NoProfile -File tools/claude/survey-permission-prompts.ps1
```

- `~/.claude/projects/**/*.jsonl` のうち新しい順に 50 件（`-MaxFiles` で変更）を読む。記録は `cleanupPeriodDays`（ユーザー設定）の日数で消えるので、それより前の分は残っていない
- 許可リストは、ユーザー設定（`~/.claude/settings.json`、`settings.local.json`）、repo ルートの `.claude/settings.json`、`unity/.claude/settings.json`、`unity/.claude/settings.local.json` を読む。worktree から実行するときは、`-RepoRoot` に本体のフォルダを指定する（`settings.local.json` は git 管理外で worktree に無いため）。ただし本体のフォルダが main より古いブランチにあると、新しい許可リストで判定されない。そのときは `unity/.claude/settings.local.json` を worktree にコピーし、`-RepoRoot` を付けずに worktree から実行する
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

### マシン B（2026-11-15）

対象は 29 セッション（2026-11-10 以降）。Bash 435 回 / PowerShell 240 回のうち、承認画面が出たと見込まれるのは Bash 261 回 / PowerShell 112 回。記録の多くは PR #146 のマージ前のもの。集計したセッション自身の呼び出し（PowerShell 7 を探す・起動する数回）も含む。

| # | 原因 | 件数 | 例 | 対策の状況 |
|---|---|---|---|---|
| 1 | `cd` と git / gh をつないだ | 144 | `cd /c/.../OWMechTest && git status --short && git fetch origin main` | PR #146 のフックで拒否する |
| 2 | `git -C` | 39（PowerShell 29 / Bash 10） | `git -C C:/.../wt-bc add CLAUDE.md ...` | PR #146 のフックで拒否する |
| 3 | ループ（`for` で Issue をまとめて読むなど） | 33 | `for n in 130 132 133 ...; do gh issue view $n ...; done` | 未対応 |
| 4 | PowerShell の変数代入・here-string（`$env:Path` を含む） | 29 | `$b = gh issue view 66 ... ; $b = $b.Replace(...)` | CLAUDE.md にルールはあるが、フックでは拒否していない |
| 5 | PowerShell のパイプ・コマンドレット（`Get-Content`、`Select-Object`、`Out-String`、`Out-File`、`Test-Path` など） | 約 28 | `Get-Content ...cs.meta \| Select-String guid` | CLAUDE.md にルールはあるが、フックでは拒否していない |
| 6 | Bash のシェル変数の代入・`export`（長いパスを変数に入れる） | 24 | `SP="/c/Users/.../scratchpad"; ...`、`export PATH=...; gh issue list ...` | 未対応 |
| 7 | git / gh の書き込み（`gh issue edit` / `create`、`gh pr create`、PowerShell での `git push` / `git checkout`） | 16 | `gh issue edit 66 --body-file ...` | 書き込みなので承認が出てよい。複数行の本文の誤判定（「許可リストに無い: Closes」など）を 7 件含む |
| 8 | シェルでのファイル書き込み（`>`、`sed -i`、`cat > ... <<EOF`、`mkdir`、`rm`、`cp`） | 15 | `gh issue view 66 ... > issue66.md` | PR #146 で「Write / Edit を使う」ルールを追加 |
| 9 | スクリプト・別のシェルの起動（`powershell -File`、`uv run python`、`pwsh`） | 18 | `powershell -NoProfile -ExecutionPolicy Bypass -File .claude/scripts/switch-to-blender.ps1`（PowerShell ツールから） | Bash からの `switch-to-*.ps1` だけ許可リストにある |
| 10 | プロセスの確認・待機（`tasklist`、`time`、`Start-Sleep`、UI Automation） | 10 | `unity status ...; tasklist //FI "IMAGENAME eq Unity.exe"` | 未対応 |
| 11 | コマンド置換 `$(...)` | 9 | `g=$(grep guid ...cs.meta \| awk ...); grep -rl "$g" Assets` | 未対応 |

許可リストに無い MCP ツール:

| ツール | 件数 | 読み取り系か |
|---|---|---|
| `mcp__blender__execute_blender_code` | 27 | いいえ（任意のコードを実行する） |
| `mcp__claude-code-remote__get_session` | 18 | はい |
| `mcp__claude-code-remote__create_session` | 12 | いいえ（セッションを作る） |
| `mcp__claude-code-remote__send_message` | 11 | いいえ（メッセージを送る） |
| `mcp__claude-code-remote__list_events` | 10 | はい |
| `mcp__claude-code-remote__archive_session` | 7 | いいえ（セッションをアーカイブする） |
| `mcp__unity-mcp__simulate_key` | 3 | いいえ（Play 中に入力を送る） |
| `mcp__claude-code-remote__list_environments` | 2 | はい |
| `mcp__claude-code-remote__list_sessions` | 2 | はい |
| `mcp__claude-code-remote__interrupt_session` | 2 | いいえ |
| `mcp__unity-mcp__delete_asset`、`quit`、`mcp__blender__execute_blender_code_for_cli` | 各 1 | いいえ |

わかったこと:

- マシン A と同じく、承認画面の大半はつなぎ方が原因。`cd` と git / gh、`git -C` の 2 つで 183 件（承認画面の約半分）あり、PR #146 のフックが効けば次からは拒否と理由の表示に変わる
- マシン A に無かった原因として、ループで Issue をまとめて読む（33 件）、scratchpad などの長いパスをシェル変数に入れる（24 件）が多い。どちらも呼び出しを 1 つずつに分ければ承認画面は出ない
- PowerShell の `git -C`（29 件）は、管理用セッションが worktree を操作するときに出ている
- `mcp__blender__execute_blender_code` は、マシン A では `settings.local.json` で許可しているが、マシン B では許可リストに無く、27 回承認している。「設定で気になった点」の任意のコードを実行できる許可と合わせて、どちらにそろえるか決める
- `claude-code-remote` の読み取り系（`get_session`、`list_events`、`list_sessions`、`list_environments`）は計 32 回で、改修の案 4 がそのまま効く

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

## 改修の内容

2 台の結果をもとに、次のように決めて改修した（PR #146 で済んでいた 1 を除く）。

### 決めたこと

| 項目 | 決定 |
|---|---|
| `mcp__blender__execute_blender_code` | 2 台とも承認画面を出す（任意のコードを実行でき、管理用セッションの「Blender のコマンドを評価して解説する」流れも承認画面を前提にしているため）。回数はモデリングのポリシーどおり 1 回の呼び出しにまとめて減らす |
| `claude-code-remote` の書き込み系 | `send_message` だけ許可する（送り先は自分のセッション）。`create_session`（コストがかかる）、`archive_session`（元に戻しにくい）、`interrupt_session` は承認画面を残す |
| `gh issue edit` / `gh pr edit` | すべての Issue / PR で許可する。変えられるのはタイトル・本文・ラベルなどで、本文は GitHub の編集履歴から戻せる。クローズ・削除・移動は別のコマンドなので許可しない |

### プロジェクトの設定で行ったこと

1. **フックを広げる**（`unity/.claude/hooks/block-compound-git.ps1` を `enforce-command-style.ps1` に改名）
   - git / gh が無くても、次の書き方を承認画面なしで拒否し、代わりのやり方を返す
     - ループ（A #6、B #3）
     - Bash のシェル変数の代入・`export`（B #6）
     - PowerShell の変数代入・here-string（A #4、B #4）
     - コマンド置換 `$(...)`（A #6、B #11）
     - PowerShell のパイプ（A #8、B #5）
     - シェルでのファイル書き込み（A #7、B #8）
     - `sleep` / `Start-Sleep` での待機（A #5、B #10）
   - 逃げ道: コマンドの末尾に `# 承認が必要: <理由>` があれば判定せず、通常の許可判定に回す。許可リストにあるコマンドや `sleep` のように確認なしで通るコマンドはそのまま実行され、それ以外は承認画面が出る（PR #150 のマシン B の検証）
2. **待機用のスクリプトを追加する**: `unity/.claude/scripts/wait-unity-ready.ps1`
   - Unity が起動していなければ起動し、`ready` まで待つ。Blender が起動中なら起動しない
   - 許可リストに追加し、CLAUDE.md と `/unity-verify` から案内する
3. **許可リストに足す**（`unity/.claude/settings.json` と repo ルートの `.claude/settings.json`）
   - `mcp__claude-code-remote__get_session`、`list_sessions`、`list_events`、`get_event`、`list_environments`、`send_message`
   - `gh issue edit *`、`gh pr edit *`、`git push -u origin docs/*`、PowerShell の `gh pr checks *`
   - PowerShell ツールからの `switch-to-blender.ps1` / `switch-to-unity.ps1` / `wait-unity-ready.ps1`（`unity/` のみ）
4. **ask に足す**（ユーザー設定の `Bash(git *)` などより優先される）
   - `git branch -D`、`git clean`、`git checkout --`、`git restore`、`git stash drop`
   - `git push *origin main*`（それまでの `git push origin main*` は `git push -u origin main` に当てはまらなかった）

### 各マシンで行う片付け

git 管理外の設定なので、それぞれのマシンで行う。終わったらチェックを付ける。

| 片付け | 場所 | マシン A | マシン B |
|---|---|---|---|
| `mcp__blender__execute_blender_code` の allow を消す | `unity/.claude/settings.local.json` | 該当なし（片付けの時点で既に無かった） | 該当なし |
| `Bash(git *)` を消す（必要な git 操作はプロジェクトの許可リストで足りる） | ユーザー設定（`~/.claude/settings.json`）、`unity/.claude/settings.local.json` | [x] | [ ]（あれば） |
| `Bash(python3 -c ' *)` を消す | `~/.claude/settings.local.json` | 該当なし（片付けの時点で既に無かった） | [ ]（あれば） |
| `PowerShell(Remove-Item *)` の allow を消す（プロジェクトの設定で ask） | `unity/.claude/settings.local.json` | [x] | [ ]（あれば） |
| 旧 UnityMCP のツール名（`mcp__UnityMCP__*`、`mcp__unity-mcp__Unity_RunCommand`）を消す | `unity/.claude/settings.local.json` | [x] | [ ]（あれば） |
| その場限りの完全一致の許可（特定の `gh pr create`、GUID を含む `grep`、`Get-ChildItem ...` など）を消す | `settings.local.json` の各所 | [x] | [ ] |

マシン A の片付け（2026-11-15）で消したもの: `Bash(git *)`（2 か所）、`PowerShell(Remove-Item *)`、旧 UnityMCP のツール名 14 件（`mcp__UnityMCP__*` 13 件と `mcp__unity-mcp__Unity_RunCommand`）、その場限りの許可 11 件（`gh pr create --title 'feat: SelectMenu→Briefing→Field …'`、`xargs grep -l "EntityId"`、`xargs grep -l "<GUID>"`、`docker --version` / `node --version` と対の `if ($?) …` 2 件、`Get-ChildItem …` の完全一致 4 件）。残したもの: `PowerShell(New-Item *)`、`PowerShell(claude mcp *)`、`Skill(update-config)`、`WebFetch(domain:api.github.com)`、`WebSearch`、Blender MCP の読み取り 2 件、GitHub MCP 4 件、`Read(...)` のパス指定。

片付けの後、しばらく作業してから「集計の手順」で集計し直すと、改修の効果を確かめられる。
