# Unity → Blender に切り替える。メモリ節約のため Unity と Blender は同時に起動しない。
#  1. このプロジェクトの Unity Editor が起動中なら、未保存シーンが無いことを確認して quit する（未保存なら中断）
#  2. Blender を起動し、MCP アドオン（既定 localhost:9876）が応答するまで待つ
# 使い方: powershell -NoProfile -ExecutionPolicy Bypass -File switch-to-blender.ps1 [-BlendFile <path>]
# Blender の実行ファイルは環境変数 BLENDER_EXE で指定可能（未指定なら Program Files から最新版を探す）
param(
    [string]$BlendFile = ''
)
# 外部コマンド（unity CLI）の stderr 出力で中断しないよう Continue にする（PowerShell 5.1 の仕様）
$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$mcpHost = if ($env:BLENDER_MCP_HOST) { $env:BLENDER_MCP_HOST } else { 'localhost' }
$mcpPort = if ($env:BLENDER_MCP_PORT) { [int]$env:BLENDER_MCP_PORT } else { 9876 }

function Test-Port([string]$h, [int]$p) {
    $c = New-Object System.Net.Sockets.TcpClient
    try { return $c.ConnectAsync($h, $p).Wait(1000) } catch { return $false } finally { $c.Dispose() }
}

# --- 1. Unity を終了 ---
$instance = $null
if (Get-Command unity -ErrorAction SilentlyContinue) {
    $status = unity status --json 2>$null | ConvertFrom-Json
    $instance = $status.data.instances | Where-Object { $_.project -eq $projectPath } | Select-Object -First 1
}

if ($instance) {
    Push-Location $projectPath
    try {
        $scenes = (unity command list_open_scenes --caller plugin --skill unity-cli --json 2>$null | ConvertFrom-Json).data.result.scenes
        $dirty = @($scenes | Where-Object { $_.isDirty })
        if ($dirty.Count -gt 0) {
            Write-Output "[switch] 中断: Unity に未保存のシーンがあります: $(($dirty | ForEach-Object { $_.path }) -join ', ')"
            Write-Output '[switch] CLAUDE.md「デバッグ実行」の手順で保存または破棄してから再実行してください。'
            exit 1
        }
        if ($instance.state -match 'play') {
            Write-Output '[switch] 中断: Unity が Play 中です。editor_stop で停止してから再実行してください。'
            exit 1
        }
    } finally { Pop-Location }

    # unity の quit コマンドは Editor モードで失敗する（com.unity.pipeline が DontDestroyOnLoad を呼ぶ不具合）ため、
    # ウィンドウを閉じる（× ボタンと同じ）方法で終了する。未保存シーンは上で確認済み。
    Write-Output "[switch] Unity Editor を終了します（PID $($instance.pid)）..."
    $proc = Get-Process -Id $instance.pid -ErrorAction SilentlyContinue
    if ($proc) { [void]$proc.CloseMainWindow() }
    if ($proc -and -not $proc.WaitForExit(120000)) {
        Write-Output '[switch] 中断: Unity が 120 秒以内に終了しませんでした。保存確認などのダイアログが出ていないか Editor の画面を確認してください。'
        exit 1
    }
    Write-Output '[switch] Unity Editor を終了しました。'
} else {
    Write-Output '[switch] このプロジェクトの Unity Editor は起動していません。'
}

# --- 2. Blender を起動 ---
if (Test-Port $mcpHost $mcpPort) {
    Write-Output "[switch] Blender MCP アドオンは既に応答しています（${mcpHost}:${mcpPort}）。"
    exit 0
}

if (-not (Get-Process blender -ErrorAction SilentlyContinue)) {
    $exe = $env:BLENDER_EXE
    if (-not $exe) {
        $exe = Get-ChildItem 'C:\Program Files\Blender Foundation\*\blender.exe' -ErrorAction SilentlyContinue |
            Sort-Object { [version](($_.Directory.Name -replace '[^0-9.]', '') + '.0') } -Descending |
            Select-Object -First 1 -ExpandProperty FullName
    }
    if (-not $exe -or -not (Test-Path $exe)) {
        Write-Output '[switch] 中断: Blender が見つかりません。環境変数 BLENDER_EXE に blender.exe のパスを設定してください。'
        exit 1
    }
    $argList = @()
    if ($BlendFile) { $argList += "`"$((Resolve-Path $BlendFile).Path)`"" }
    Write-Output "[switch] Blender を起動します: $exe $($argList -join ' ')"
    if ($argList.Count -gt 0) { Start-Process -FilePath $exe -ArgumentList $argList } else { Start-Process -FilePath $exe }
} else {
    Write-Output '[switch] Blender は起動済みです。MCP アドオンの応答を待ちます。'
}

$deadline = (Get-Date).AddSeconds(90)
while ((Get-Date) -lt $deadline) {
    if (Test-Port $mcpHost $mcpPort) {
        Write-Output "[switch] Blender MCP アドオンに接続できます（${mcpHost}:${mcpPort}）。"
        exit 0
    }
    Start-Sleep -Seconds 2
}
Write-Output "[switch] Blender は起動しましたが ${mcpHost}:${mcpPort} が応答しません。MCP アドオンの Auto Start が有効か確認してください（README「開発環境セットアップ > Blender MCP」）。"
exit 1
