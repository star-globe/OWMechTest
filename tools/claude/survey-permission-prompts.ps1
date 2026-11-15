#Requires -Version 7
# Claude Code のセッション記録（~/.claude/projects/**/*.jsonl）から Bash / PowerShell / MCP の呼び出しを集計し、
# 今の許可リストで承認画面が出たと見込まれる呼び出しを原因ごとに数える。読み取りだけで、何も書き換えない。
# 使い方（PowerShell 7 で実行する）: pwsh -NoProfile -File tools/claude/survey-permission-prompts.ps1 [-MaxFiles 50] [-RepoRoot <path>]
# -RepoRoot は許可リストを読むリポジトリ（省略時はこのスクリプトのあるリポジトリ。worktree から実行するときは本体を指定する。
# settings.local.json は git 管理外で worktree に無いため）。結果の見方と記録先は docs/permission-prompt-survey.md
param([int]$MaxFiles = 50, [string]$RepoRoot = (Join-Path $PSScriptRoot '../..'))

$repoRoot = (Resolve-Path $RepoRoot).Path
$settingsFiles = @(
    (Join-Path $env:USERPROFILE '.claude/settings.json'),
    (Join-Path $env:USERPROFILE '.claude/settings.local.json'),
    (Join-Path $repoRoot '.claude/settings.json'),
    (Join-Path $repoRoot 'unity/.claude/settings.json'),
    (Join-Path $repoRoot 'unity/.claude/settings.local.json')
)
$allow = @{ Bash = @(); PowerShell = @(); Other = @() }
foreach ($f in $settingsFiles) {
    if (-not (Test-Path $f)) { continue }
    $j = Get-Content $f -Raw | ConvertFrom-Json
    foreach ($r in $j.permissions.allow) {
        if ($r -match '^(Bash|PowerShell)\((.*)\)$') { $allow[$Matches[1]] += $Matches[2] } else { $allow.Other += $r }
    }
}

# Claude Code が許可リストなしで通す読み取り系のコマンド（Bash のみ。主なものだけ）
$autoBash = 'cat','head','tail','wc','stat','ls','cd','echo','diff','sort','grep','rg','find','jq','uniq','sed','date','which',
            'true','false','sleep','printf','test','basename','dirname','realpath','cut','tr','seq','file','pwd','xargs','tree'
$gitRead = 'status','log','diff','show','blame','branch','tag','remote','ls-files','ls-remote','rev-parse','describe',
           'reflog','shortlog','cat-file','for-each-ref'

function Test-Rule([string]$tool, [string]$segment) {
    foreach ($r in $allow[$tool]) {
        $re = '^' + ([regex]::Escape($r) -replace '\\ \\\*$', '( .*)?' -replace '\\\*', '.*') + '$'
        if ($segment -match $re) { return $true }
    }
    return $false
}

function Get-PromptReason([string]$tool, [string]$cmd) {
    # 引用符の中（コミットメッセージなど）は判定から外す
    $s = $cmd -replace "'[^']*'", "''" -replace '"[^"]*"', '""'
    if ($s -match "<<-?\s*''?\w+") { return 'heredoc（python - <<EOF など）' }
    if ($s -match '(^|[\s;&|(])git\s+-C\s') { return 'git -C' }
    if ($s -match '(^|[\s;&|(])(cd|Set-Location|pushd|Push-Location)\s' -and $s -match '(^|[\s;&|(])(git|gh)\s') { return 'cd と git / gh の組み合わせ' }
    if ($s -match '(^|[\s;&|(])(for|foreach|while|until)\s') { return 'ループ（for / while / until）' }
    if ($s -match '\$\(') { return 'コマンド置換 $(...)' }
    if ($tool -eq 'PowerShell' -and $s -match '(^|;)\s*\$\w+\s*=') { return 'PowerShell の変数代入' }
    if ($s -match "@'|@`"") { return 'PowerShell の here-string' }
    $parts = $s -split '&&|\|\||;|\||\r?\n' |
        ForEach-Object { ($_.Trim() -replace '\s*\d?>&\d|\s*2>\S+', '').Trim() } | Where-Object { $_ }
    foreach ($p in $parts) {
        $tok = $p -split '\s+'
        if ($tool -eq 'Bash' -and $autoBash -contains $tok[0]) {
            if ($tok[0] -eq 'sed' -and $p -match '\s-i') { return 'sed -i（シェルでの書き込み）' }
            continue
        }
        if ($tool -eq 'Bash' -and $tok[0] -eq 'git' -and $gitRead -contains $tok[1] -and $p -notmatch 'branch\s+-[Dd]') { continue }
        if (Test-Rule $tool $p) { continue }
        $k = if ($tok[0] -match '^(git|gh|unity)$' -and $tok.Count -gt 1) { "$($tok[0]) $($tok[1])" } else { $tok[0] }
        return "許可リストに無い: $k"
    }
    if ($s -match '[^2&]>\s*[^&\s]' -and $s -notmatch '>\s*(/dev/null|\$null)') { return 'リダイレクトでの書き込み' }
    return $null
}

$files = Get-ChildItem (Join-Path $env:USERPROFILE '.claude/projects') -Recurse -Filter *.jsonl |
    Sort-Object LastWriteTime -Descending | Select-Object -First $MaxFiles
$tools = @{}; $total = @{ Bash = 0; PowerShell = 0 }; $prompted = @{ Bash = 0; PowerShell = 0 }
$reasons = @{}; $examples = @{}; $mcpNotAllowed = @{}
foreach ($f in $files) {
    foreach ($line in [System.IO.File]::ReadLines($f.FullName)) {
        if ($line -notmatch '"tool_use"') { continue }
        try { $o = $line | ConvertFrom-Json } catch { continue }
        if ($o.type -ne 'assistant') { continue }
        foreach ($c in $o.message.content) {
            if ($c.type -ne 'tool_use') { continue }
            $n = $c.name
            $tools[$n] = 1 + [int]$tools[$n]
            if ($n -like 'mcp__*' -and $allow.Other -notcontains $n) { $mcpNotAllowed[$n] = 1 + [int]$mcpNotAllowed[$n] }
            if ($n -ne 'Bash' -and $n -ne 'PowerShell') { continue }
            $cmd = [string]$c.input.command
            $total[$n]++
            $why = Get-PromptReason $n $cmd
            if (-not $why) { continue }
            $prompted[$n]++
            $k = "$n`t$why"
            $reasons[$k] = 1 + [int]$reasons[$k]
            if (-not $examples[$k]) { $one = $cmd -replace '\s+', ' '; $examples[$k] = $one.Substring(0, [Math]::Min(100, $one.Length)) }
        }
    }
}

$oldest = ($files | Select-Object -Last 1).LastWriteTime
"## 対象: $($files.Count) セッション（最も古い記録の更新日 $($oldest.ToString('yyyy-MM-dd'))）"
"Bash $($total.Bash) 回 / PowerShell $($total.PowerShell) 回、承認画面が出たと見込まれるもの: Bash $($prompted.Bash) 回 / PowerShell $($prompted.PowerShell) 回"
''
'## 承認画面の原因（件数 / ツール / 原因 / 例）'
$reasons.GetEnumerator() | Sort-Object Value -Descending | ForEach-Object { "$($_.Value)`t$($_.Key)`t| $($examples[$_.Key])" }
''
'## 許可リストに無い MCP ツール（件数 / 名前）'
$mcpNotAllowed.GetEnumerator() | Sort-Object Value -Descending | ForEach-Object { "$($_.Value)`t$($_.Key)" }
''
'## ツール別の呼び出し回数（上位 20）'
$tools.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First 20 | ForEach-Object { "$($_.Value)`t$($_.Key)" }
