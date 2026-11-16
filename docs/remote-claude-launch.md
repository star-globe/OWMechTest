# Claude Code を Remote Control で起動するバッチファイル（ひな形）

ターミナルアプリが自動で更新される問題を避けるため、作業マシンでは PowerShell から直接 Claude Code を起動する。起動の手間を省くためのバッチファイルのひな形をここに置く。

セッション名はマシンごとに変えるので、バッチファイル本体はリポジトリにコミットしない（`.gitignore` で `unity/start_remote_claude.bat` を除外している）。各マシンでこのひな形から作る。

## 作り方

1. 下の内容を `unity/start_remote_claude.bat` として保存する
2. `SESSION_NAME` の `MACHINE` をそのマシンの名前に書き換える（例: `OWMechTest-Logic`、`OWMechTest-Design`）
   - cmd は UTF-8 の日本語をうまく読めないことがあるため、バッチファイルには日本語（コメントを含む）を書かない

## バッチファイル

```bat
@echo off
set "SESSION_NAME=OWMechTest-MACHINE"
cd /d "%~dp0"
where pwsh >nul 2>nul
if %errorlevel%==0 (set "PS=pwsh") else (set "PS=powershell")
start "Claude Remote Control" %PS% -NoLogo -NoExit -Command "claude remote-control --name '%SESSION_NAME%' --spawn same-dir"
```

- `cd /d "%~dp0"`: バッチファイルを置いたフォルダ（`unity/`）を作業フォルダにする（CLAUDE.md: Claude Code は `unity/` で起動する）
- `pwsh`（PowerShell 7）があればそれを使い、無ければ Windows PowerShell を使う
- `start` で PowerShell の新しいウィンドウを開き、その中で claude を起動する
- `-NoExit`: Ctrl+C で claude を止めた後も PowerShell のウィンドウが残り、そのまま操作を続けられる

## 実行

エクスプローラーで `unity/start_remote_claude.bat` をダブルクリックする。止めるときは、開いた PowerShell のウィンドウで Ctrl+C を押す。

- `.ps1` のスクリプトを右クリックの「PowerShell で実行」で起動する方法は使わない。この起動方法では Ctrl+C で claude を止められなかった
