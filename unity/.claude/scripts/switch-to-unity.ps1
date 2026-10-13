# Blender → Unity に切り替える。メモリ節約のため Unity と Blender は同時に起動しない。
#  1. Blender が起動中なら、MCP アドオン経由で .blend を保存してからウィンドウを閉じ、終了を待つ
#     （名前付きファイルは必ず保存。無題なら中断。終了しなければ中断し、強制終了はしない）
#  2. このプロジェクトの Unity Editor を起動し、ready になるまで待つ
# 使い方: powershell -NoProfile -ExecutionPolicy Bypass -File switch-to-unity.ps1 [-DiscardUntitled]
#   -DiscardUntitled: Blender のシーンが無題（未保存の新規）でも閉じる。残す作業が無いときだけ指定する
param(
    [switch]$DiscardUntitled
)
# 外部コマンド（unity CLI）の stderr 出力で中断しないよう Continue にする（PowerShell 5.1 の仕様）
$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$projectPath = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

# Blender MCP アドオンに Python コードを送って実行する（blender_mcp の connection.py と同じプロトコル: JSON + "\0"）
function Invoke-BlenderCode([string]$code) {
    $h = if ($env:BLENDER_MCP_HOST) { $env:BLENDER_MCP_HOST } else { 'localhost' }
    $p = if ($env:BLENDER_MCP_PORT) { [int]$env:BLENDER_MCP_PORT } else { 9876 }
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        if (-not $client.ConnectAsync($h, $p).Wait(3000)) { return $null }
        $stream = $client.GetStream()
        $stream.ReadTimeout = 60000
        $req = (@{ type = 'execute'; code = $code; strict_json = $true } | ConvertTo-Json -Compress) + [char]0
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($req)
        $stream.Write($bytes, 0, $bytes.Length)
        $buf = New-Object System.IO.MemoryStream
        $chunk = New-Object byte[] 65536
        while ($true) {
            $n = $stream.Read($chunk, 0, $chunk.Length)
            if ($n -le 0) { break }
            $buf.Write($chunk, 0, $n)
            if ([Array]::IndexOf($chunk, [byte]0, 0, $n) -ge 0) { break }
        }
        $text = [System.Text.Encoding]::UTF8.GetString($buf.ToArray()).TrimEnd([char]0)
        return $text | ConvertFrom-Json
    } catch { return $null } finally { $client.Dispose() }
}

# --- 1. Blender を終了 ---
$blender = @(Get-Process blender -ErrorAction SilentlyContinue)
if ($blender.Count -gt 0) {
    # MCP（Python）で行った変更は bpy.data.is_dirty に反映されず、閉じても保存確認が出ないまま失われる。
    # そのため is_dirty に頼らず、名前付きファイルなら必ず保存し、無題なら中断する。
    $res = Invoke-BlenderCode @'
import bpy
if bpy.data.filepath:
    bpy.ops.wm.save_mainfile()
    result = {"state": "saved", "path": bpy.data.filepath}
else:
    result = {"state": "untitled", "objects": len(bpy.data.objects)}
'@
    if (-not $res -or $res.status -ne 'ok') {
        Write-Output "[switch] 中断: Blender MCP アドオンに接続できず、.blend を保存できませんでした。Blender で保存してから再実行してください。$(if ($res) { $res.message })"
        exit 1
    }
    $r = $res.result
    if ($r.state -eq 'saved') {
        Write-Output "[switch] .blend を保存しました: $($r.path)"
    } elseif ($DiscardUntitled) {
        Write-Output "[switch] 無題のシーン（オブジェクト $($r.objects) 個）を保存せずに閉じます（-DiscardUntitled 指定）。"
    } else {
        Write-Output "[switch] 中断: Blender のシーンが無題（未保存の新規ファイル）です。残す作業があれば Blender MCP で bpy.ops.wm.save_as_mainfile(filepath=...) で保存してから再実行してください。残す必要がなければ -DiscardUntitled を付けて再実行してください。"
        exit 1
    }

    Write-Output "[switch] Blender を終了します（PID $(($blender | ForEach-Object { $_.Id }) -join ', ')）..."
    foreach ($p in $blender) { [void]$p.CloseMainWindow() }
    foreach ($p in $blender) {
        if (-not $p.WaitForExit(30000)) {
            Write-Output '[switch] 中断: Blender が 30 秒以内に終了しませんでした。未保存の変更の確認ダイアログが出ている可能性があります。Blender MCP で保存してから再実行するか、Blender の画面を確認してください。'
            exit 1
        }
    }
    Write-Output '[switch] Blender を終了しました。'
} else {
    Write-Output '[switch] Blender は起動していません。'
}

# --- 2. Unity を起動 ---
if (-not (Get-Command unity -ErrorAction SilentlyContinue)) {
    Write-Output '[switch] 中断: unity CLI が見つかりません。'
    exit 1
}

function Get-Instance {
    $status = unity status --json 2>$null | ConvertFrom-Json
    return $status.data.instances | Where-Object { $_.project -eq $projectPath } | Select-Object -First 1
}

$instance = Get-Instance
if (-not $instance) {
    Write-Output '[switch] Unity Editor を起動します...'
    Start-Process -FilePath unity -ArgumentList @('open', "`"$projectPath`"") -WindowStyle Hidden
}

$deadline = (Get-Date).AddSeconds(600)
while ((Get-Date) -lt $deadline) {
    $instance = Get-Instance
    if ($instance -and $instance.state -eq 'ready') {
        Write-Output "[switch] Unity Editor が ready になりました（port $($instance.port)）。unity-mcp のツールが使えます。"
        exit 0
    }
    Start-Sleep -Seconds 5
}
Write-Output '[switch] Unity Editor が 600 秒以内に ready になりませんでした。unity status で状態を確認してください（コンパイルエラーによる Safe Mode の可能性もあります）。'
exit 1
