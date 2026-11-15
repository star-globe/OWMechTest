# このプロジェクトの Unity Editor が ready になるまで待つ。起動していなければ起動する（Blender が起動中なら起動しない）。
# sleep / Start-Sleep やループで unity status を何度も呼ぶと、そのたびに承認画面が出るため、待機はこのスクリプトで行う。
# 使い方: powershell -NoProfile -ExecutionPolicy Bypass -File unity/.claude/scripts/wait-unity-ready.ps1 [-TimeoutSeconds 600]
# 終了コード: 0 = ready / 1 = 時間切れ・起動できない
param(
    [int]$TimeoutSeconds = 600
)
# 外部コマンド（unity CLI）の stderr 出力で中断しないよう Continue にする（PowerShell 5.1 の仕様）
$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

if (-not (Get-Command unity -ErrorAction SilentlyContinue)) {
    Write-Output '[wait] unity CLI が見つかりません。'
    exit 1
}

function Get-Instance {
    $status = unity status --json 2>$null | ConvertFrom-Json
    return $status.data.instances | Where-Object { $_.project -eq $projectPath } | Select-Object -First 1
}

$instance = Get-Instance
if (-not $instance) {
    if (Get-Process blender -ErrorAction SilentlyContinue) {
        # メモリ節約のため Unity と Blender は同時に起動しない（CLAUDE.md「Unity と Blender の切り替え」）
        Write-Output '[wait] Blender が起動中のため Unity Editor を起動しません。/switch-app unity で切り替えてください。'
        exit 1
    }
    Write-Output '[wait] Unity Editor を起動します...'
    Start-Process -FilePath unity -ArgumentList @('open', "`"$projectPath`"") -WindowStyle Hidden
}

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    $instance = Get-Instance
    if ($instance -and $instance.state -eq 'ready') {
        Write-Output "[wait] Unity Editor が ready です（port $($instance.port)）。"
        exit 0
    }
    Start-Sleep -Seconds 5
}
$state = if ($instance) { $instance.state } else { '未起動' }
Write-Output "[wait] $TimeoutSeconds 秒以内に ready になりませんでした（state=$state）。コンパイルエラーによる Safe Mode の可能性があります。unity pipeline list で確認してください。"
exit 1
