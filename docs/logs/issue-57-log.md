---
Issue: #57 Blender を CLI（バックグラウンド実行）から使えるようにする
実装日: 2026-11-08
変更ファイル:
- tools/blender/export_to_unity.py
- tools/blender/export.sh（新規）
- unity/.mcp.json
- unity/.claude/settings.json
- CLAUDE.md
- README.md
- docs/blender-unity-pipeline.md
- docs/ai-dev-improvement-plan.md

実装方針:
- `export_to_unity.py` をバックグラウンド（`bpy.app.background`）で実行したときは、`--` 以降の引数（`--objects` / `--all` / `--validate-only` / `--dry-run` / `--base-path` / `--json-out`）を読むようにした（PR #58）。結果は `OWMECH_RESULT:` で始まる 1 行の JSON で、終了コードは 0 = 成功、2 = 規約違反、1 = その他のエラー
- ラッパー `tools/blender/export.sh`：Blender を探して（`BLENDER_EXE` → 既定のインストール先の最新版）`--factory-startup` で起動し、標準出力には JSON だけを出す。ログは `unity/Logs/blender-export.log`
- MCP の `_for_cli` ツールを調査した。MCP サーバーが `blender --background` を起動して `result` の dict を返すもので、Blender の場所は環境変数 `BLENDER_PATH` から読む → `.mcp.json` に既定値を追加した。使い分けは、エクスポート・検証は `export.sh`、GUI を開かずに `.blend` を調べるのは `_for_cli`
- メモリを計測した（小さなテスト用ファイルで約 200 MB、約 2 秒）。Unity の起動中は空きメモリが約 2.1 GB しかないため、**Unity の起動中は実行しない**と判断された → `export.sh` は Unity Editor が起動中なら実行せずに終了する
- 確認：検証・dry run・エクスポート・各種エラーで、想定どおりの JSON と終了コードになることを確認した

詰まった点・解決策:
- Blender の場所を探す処理で、`for` の展開が「Program Files」の空白で分割されて見つからなかった → `while read` でパスを 1 行ずつ読む形にした
- Unity Editor の起動判定を `Unity.exe` の名前だけで行うと、常に動いている Unity CLI（`unity.exe`、MCP サーバー）も含まれてしまう → 実行ファイルのパス（`...\Editor\Unity.exe`）で判定した
- 動作確認の後に Unity を起動し直したとき、メモリ不足で Claude Code が起動コマンドを止めた（Editor 自体は起動済み）。メモリの余裕が少ないことの裏付けになった

残課題・関連Issue:
- `BLENDER_PATH` がこのマシンではまだ有効になっていない（`.mcp.json` の既定値は MCP の再接続後に反映される）。人間がタイミングを見て設定する。それまでは `_for_cli` ツールが「Blender executable not found」で失敗する
- Text Editor からの実行（処理を関数に移しただけ）は、GUI では確認していない
- CI での `.blend` の検証は範囲外（リポジトリに `.blend` / `.fbx` がまだないため）
---
