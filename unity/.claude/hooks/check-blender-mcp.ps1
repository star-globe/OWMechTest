# SessionStart hook: Blender MCP アドオン（ブリッジサーバー）への接続可否を確認して通知する。
# blender MCP サーバー（.mcp.json）はツール呼び出し時に接続するため、Blender を後から起動しても再接続は不要。
$ErrorActionPreference = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$mcpHost = if ($env:BLENDER_MCP_HOST) { $env:BLENDER_MCP_HOST } else { 'localhost' }
$mcpPort = if ($env:BLENDER_MCP_PORT) { [int]$env:BLENDER_MCP_PORT } else { 9876 }
$mcpDir  = if ($env:BLENDER_MCP_DIR)  { $env:BLENDER_MCP_DIR }  else { 'C:/blender_mcp/mcp' }

if (-not (Test-Path $mcpDir)) {
    Write-Output "[blender] MCP サーバーが見つかりません: $mcpDir（環境変数 BLENDER_MCP_DIR で指定可能）"
    exit 0
}

$client = New-Object System.Net.Sockets.TcpClient
$connected = $false
try { $connected = $client.ConnectAsync($mcpHost, $mcpPort).Wait(1000) } catch {}
$client.Dispose()

if ($connected) {
    Write-Output "[blender] Blender MCP アドオンに接続可能: ${mcpHost}:${mcpPort}"
} elseif (Get-Process blender -ErrorAction SilentlyContinue) {
    Write-Output "[blender] Blender は起動中ですが ${mcpHost}:${mcpPort} に接続できません。アドオン設定で MCP サーバーを Start するか Auto Start を有効にしてください。"
} else {
    Write-Output '[blender] Blender は未起動です。Blender 作業時は起動すれば自動で接続されます（MCP アドオンの Auto Start を有効にしておくこと）。'
}
exit 0
