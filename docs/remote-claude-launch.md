# Claude Code を Remote Control で起動するバッチファイル（ひな形）

ターミナルアプリが自動で更新される問題を避けるため、作業マシンでは PowerShell から直接 Claude Code を起動する。起動の手間を省くためのバッチファイルのひな形をここに置く。

セッション名はマシンごとに変えるので、バッチファイル本体はリポジトリにコミットしない（`.gitignore` で `unity/start_remote_claude.bat` を除外している）。各マシンでこのひな形から作る。

## 作り方

1. 下の内容を `unity/start_remote_claude.bat` として保存する
2. `SESSION_NAME` の `ROLE` を担当に、`MACHINE` をそのマシンの識別名に書き換える（例: `OWMechTest-Logic-Main`、`OWMechTest-Design-MSI`。名前の決まりは下の「セッション名の決まり」）
   - cmd は UTF-8 の日本語をうまく読めないことがあるため、バッチファイルには日本語（コメントを含む）を書かない

## セッション名の決まり

セッション名は `OWMechTest-<担当>-<識別名>` とする。

- 担当は次のどれか。CLAUDE.md の「ラベル」の「作業できる環境」に対応する
  - `Logic`: `GameLogic` の作業をするマシン
  - `Design`: `Design` の作業をするマシン（Blender / Unity Editor でのモデリング・エフェクト・UI の見た目）
  - `Both`: 両方を受け持つマシン
- 識別名は、同じ担当のマシンを見分けるためのもの。自由に付けてよい
- 管理用セッションは、`list_sessions` で見えるこの名前から担当を判断して作業を割り振る（`docs/management-session-policy.md` の「7. 作業用セッションに指示を出す」）。担当が名前から読めないと、管理用セッションは指示を送る前に人間に確認する
- どのマシンがどの担当かはリポジトリに書かない。担当を変えたら、そのマシンのバッチファイルの `SESSION_NAME` を書き換えて Remote Control を起動し直す

## バッチファイル

```bat
@echo off
set "SESSION_NAME=OWMechTest-ROLE-MACHINE"
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
