# Claude Code を Remote Control で起動するスクリプト（ひな形）

ターミナルアプリが自動で更新される問題を避けるため、作業マシンでは PowerShell から直接 Claude Code を起動する。起動の手間を省くためのスクリプトのひな形をここに置く。

セッション名はマシンごとに変えるので、スクリプト本体はリポジトリにコミットしない（`.gitignore` で `unity/start_remote_claude.ps1` を除外している）。各マシンでこのひな形から作る。

## 作り方

1. 下のスクリプトを `unity/start_remote_claude.ps1` として保存する
   - 日本語のコメントを含むため、PowerShell 5.1 で使う場合は「UTF-8（BOM 付き）」で保存する
2. `$sessionName` をそのマシンの名前に書き換える（例: `OWMechTest-Logic`、`OWMechTest-Design`）

## スクリプト

```powershell
# Claude Code を Remote Control モードで起動する
# 使い方: unity/ に置いて .\start_remote_claude.ps1 を実行する
#   実行ポリシーで止められるときは: powershell -NoProfile -ExecutionPolicy Bypass -File .\start_remote_claude.ps1

# マシンごとに変える
$sessionName = "OWMechTest-<マシン名>"

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
# どこから実行しても unity/ を作業フォルダにする（このファイルを unity/ に置く前提。CLAUDE.md: Claude Code は unity/ で起動する）
Set-Location -Path $PSScriptRoot

if (-not (Get-Command claude -ErrorAction SilentlyContinue)) {
    Write-Output "[start_remote_claude] claude コマンドが見つかりません。Claude Code をインストールし、PATH を確認してください。"
    exit 1
}

claude remote-control --name $sessionName --spawn same-dir
exit $LASTEXITCODE
```

## 実行

`unity/` で次を実行する。

```powershell
.\start_remote_claude.ps1
```

実行ポリシーで止められるときは次のように実行する。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\start_remote_claude.ps1
```
