# PreToolUse hook（Bash / PowerShell）: 承認画面の原因になる書き方を、承認画面を出さずに拒否する。
# 拒否の理由（代わりのやり方）を AI に返して書き直させる（CLAUDE.md「作業上のルール」、docs/permission-prompt-survey.md）。
# 対象外のコマンドは何も出力せずに終了し、通常の許可判定に任せる。
# 逃げ道: コマンドの末尾に「# 承認が必要: <理由>」のコメントがあれば判定せず、通常の許可判定（承認画面）に回す。
$ErrorActionPreference = 'SilentlyContinue'
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$tool = [string]$payload.tool_name
$command = [string]$payload.tool_input.command
if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }

# 逃げ道（理由が空のものは認めない）
if ($command -match '#\s*承認が必要\s*[:：]\s*\S') { exit 0 }

$isPowerShell = $tool -eq 'PowerShell'

# 引用符の中（コミットメッセージやパスなど）は判定から外す
$noSingle = $command -replace "'[^']*'", "''"
$stripped = ($noSingle -replace '"[^"]*"', '""').Trim()

# コマンドの先頭（行頭、区切り文字、括弧の直後）
$head = '(^|[;&|(\r\n{]\s*)'

$reason = $null

# --- git / gh（PR #146） ---
$usesGit = $stripped -cmatch '(^|[\s;&|(])(git|gh)(\s|$)'
if ($usesGit -and $stripped -cmatch '(^|[\s;&|(])git\s+-C\s') {
    $reason = '`git -C` は使わないでください。git は unity/ の中でも動くので、repo ルートのファイルは ../docs/xxx.md のように相対パスで指定してください。'
} elseif ($usesGit -and $stripped -match '(^|[\s;&|(])(cd|Set-Location|pushd|Push-Location)(\s|$)') {
    $reason = '`cd` と git / gh を同じ呼び出しにしないでください（Claude Code が毎回承認を求めます）。cd は不要です。git は unity/ の中でも動くので、相対パスで指定してください。'
} elseif ($usesGit -and $stripped -match '&&|\|\||;|\||\r|\n') {
    $reason = 'git / gh をほかのコマンドと && / ; / | / 改行でつながず、1 回の呼び出しで 1 つずつ実行してください。ファイルの読み書きは Read / Write / Edit ツール、出力の絞り込みは --oneline / -n / --json / --jq などのオプションを使ってください。'
}

# --- 待機 ---
elseif ($stripped -match "$head(sleep|Start-Sleep|timeout)\s") {
    $reason = 'sleep / Start-Sleep で待たないでください。Unity の起動を待つときは unity/.claude/scripts/wait-unity-ready.ps1 を、それ以外の完了待ちは Monitor ツール（until ループ）を使ってください。'
}

# --- ループ ---
elseif ($stripped -match "$head(for|while|until|foreach|do)(\s|\()" -or ($isPowerShell -and $stripped -match '\|\s*(ForEach-Object|%)\s')) {
    $reason = 'ループを使わず、1 回の呼び出しで 1 つずつ実行してください。互いに依存しない呼び出しは 1 つのメッセージで並べて呼べます。Issue や PR をまとめて読むときは gh issue list / gh pr list の --json / --jq を使ってください。'
}

# --- 変数・コマンド置換 ---
elseif ((-not $isPowerShell) -and ($noSingle -match '\$\(' -or $noSingle -match '`')) {
    $reason = 'コマンド置換 $(...) / `...` を使わず、先に単独で実行して結果を見てから、値を直接書いて次のコマンドを呼んでください。'
} elseif ((-not $isPowerShell) -and $stripped -cmatch "$head(export\s+)?[A-Za-z_][A-Za-z0-9_]*=") {
    $reason = 'シェル変数の代入・export を使わず、パスや値は毎回そのまま書いてください。'
} elseif ($isPowerShell -and ($stripped -match "$head\`$[\w:]+\s*[-+*/]?=(?!=)" -or $command -match '@[''"]\s*\r?\n')) {
    $reason = 'PowerShell の変数代入・here-string を使わず、値は直接書いてください。長い本文は Write ツールでファイルに書き、--body-file / -F で渡してください。'
} elseif ($isPowerShell -and $stripped -match '\$\(') {
    $reason = 'PowerShell の部分式 $(...) を使わず、先に単独で実行して結果を見てから、値を直接書いて次のコマンドを呼んでください。'
}

# --- PowerShell のパイプ ---
elseif ($isPowerShell -and $stripped -match '\|\s*(Select-String|Select-Object|Where-Object|Sort-Object|Measure-Object|Out-String|Out-Null|Out-File|ConvertFrom-Json|ConvertTo-Json|Format-\w+|\?)(\s|$)') {
    $reason = 'PowerShell のパイプ（Select-String / Select-Object / Where-Object / Out-* / ConvertFrom-Json など）を使わないでください。検索・読み取りは Grep / Read ツール、出力の絞り込みは --json / --jq / --oneline / -n などのオプションを使ってください。'
}

# --- シェルでのファイル書き込み ---
elseif ($stripped -match '(?<![<>=-])>{1,2}(?!&)\s*(?!/dev/null|\$null|nul\b)[^\s&|;]' -or
        $command -match '<<-?\s*[''"]?\w' -or
        $stripped -match "$head(sed\s+(-\w+\s+)*-i|tee\s|Set-Content|Add-Content|Out-File|New-Item)") {
    $reason = 'シェルでファイルに書き込まないでください（> / >> / heredoc / sed -i / tee / Set-Content / Out-File など）。書き込みは Write / Edit ツール、コミットメッセージや本文は Write で scratchpad に書いて -F / --body-file で渡してください。'
}

if (-not $reason) { exit 0 }

$result = @{
    hookSpecificOutput = @{
        hookEventName            = 'PreToolUse'
        permissionDecision       = 'deny'
        permissionDecisionReason = "[enforce-command-style] $reason 必要な場合だけ、末尾に「# 承認が必要: <理由>」を付けると承認画面に回ります。"
    }
}
$result | ConvertTo-Json -Depth 3 -Compress
exit 0
