# PreToolUse hook（Bash / PowerShell）: git / gh をほかのコマンドとつないだ呼び出しを、承認画面を出さずに拒否する。
# つないだコマンドは許可ルールに合わず承認待ちになり、cd と git の組み合わせは「常に許可」も選べないため、
# 拒否の理由を AI に返して 1 つずつ実行し直させる（CLAUDE.md「作業上のルール」）。
# 対象外のコマンドは何も出力せずに終了し、通常の許可判定に任せる。
$ErrorActionPreference = 'SilentlyContinue'
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$command = [string]$payload.tool_input.command
if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }

# 引用符の中（コミットメッセージやパスなど）は判定から外す
$stripped = ($command -replace "'[^']*'", "''" -replace '"[^"]*"', '""').Trim()

# コマンドとしての git / gh（行頭、区切り文字、空白、括弧の直後）。".git/" や "GitHub" は対象外
$usesGit = $stripped -cmatch '(^|[\s;&|(])(git|gh)(\s|$)'
if (-not $usesGit) { exit 0 }

$reason = $null
if ($stripped -cmatch '(^|[\s;&|(])git\s+-C\s') {
    $reason = '`git -C` は使わないでください。git は unity/ の中でも動くので、repo ルートのファイルは ../docs/xxx.md のように相対パスで指定してください。'
} elseif ($stripped -match '(^|[\s;&|(])(cd|Set-Location|pushd|Push-Location)(\s|$)') {
    $reason = '`cd` と git / gh を同じ呼び出しにしないでください（Claude Code が毎回承認を求めます）。cd は不要です。git は unity/ の中でも動くので、相対パスで指定してください。'
} elseif ($stripped -match '&&|\|\||;|\||\r|\n') {
    $reason = 'git / gh をほかのコマンドと && / ; / | / 改行でつながず、1 回の呼び出しで 1 つずつ実行してください。ファイルの読み書きは Read / Write / Edit ツール、出力の絞り込みは --oneline / -n / --json / --jq などのオプションを使ってください。'
}
if (-not $reason) { exit 0 }

$result = @{
    hookSpecificOutput = @{
        hookEventName            = 'PreToolUse'
        permissionDecision       = 'deny'
        permissionDecisionReason = "[block-compound-git] $reason"
    }
}
$result | ConvertTo-Json -Depth 3 -Compress
exit 0
