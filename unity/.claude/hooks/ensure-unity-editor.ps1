# SessionStart hook: このプロジェクトの Unity Editor が起動していなければバックグラウンドで起動する。
# unity-mcp（.mcp.json）は Editor 未起動でも待機し、Editor が ready になるとツールが有効化される。
$ErrorActionPreference = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

if (-not (Get-Command unity -ErrorAction SilentlyContinue)) {
    Write-Output '[unity-mcp] unity CLI が見つかりません。Unity CLI をインストールしてください。'
    exit 0
}

$status = unity status --json 2>$null | ConvertFrom-Json
$instance = $status.data.instances | Where-Object { $_.project -eq $projectPath } | Select-Object -First 1

if ($instance) {
    Write-Output "[unity-mcp] Unity Editor 接続先: port $($instance.port) / state=$($instance.state)"
} else {
    Start-Process -FilePath unity -ArgumentList @('open', "`"$projectPath`"") -WindowStyle Hidden
    Write-Output '[unity-mcp] Unity Editor が未起動のため起動しました。ready になると unity-mcp のツールが使えるようになります（unity status で確認）。'
}
exit 0
