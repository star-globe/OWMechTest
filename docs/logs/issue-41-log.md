---
Issue: #41 Unity と Blender の切り替え手順の追加（同時起動の回避）
実装日: 2026-10-13
変更ファイル:
- unity/.claude/scripts/switch-to-blender.ps1
- unity/.claude/scripts/switch-to-unity.ps1
- unity/.claude/skills/switch-app/SKILL.md
- unity/.claude/hooks/ensure-unity-editor.ps1
- unity/.claude/settings.json
- CLAUDE.md

実装方針:
- ローカルマシンのメモリ（約 16 GB、Unity だけで約 3.5 GB）を節約するため、Unity と Blender を同時に起動しない仕組みを追加（PR #42）
- 切り替えスクリプトと `/switch-app <blender|unity>` スキルを追加。未保存の作業があれば中断し、強制終了はしない
- SessionStart フックは Blender 起動中なら Unity を自動起動しない
- Unity → Blender、Blender → Unity、未保存時の中断、自動保存、無題シーンの中断を実機で確認

詰まった点・解決策:
- Unity CLI の `quit`（MCP の `quit` ツールも同じ）は Editor モードで失敗し、Editor の登録だけが外れる（`com.unity.pipeline` が `DontDestroyOnLoad` を呼ぶ不具合）→ `CloseMainWindow`（× ボタンと同じ）で終了
- MCP（Python）で行った Blender の変更は `bpy.data.is_dirty` に反映されず、閉じても保存確認が出ないまま失われる（実際に確認）→ スクリプトがアドオンのソケットに直接 `save_mainfile()` を送り、名前付きファイルは必ず保存。無題シーンは中断し、`-DiscardUntitled` 指定時だけ閉じる
- PowerShell 5.1 では `$ErrorActionPreference = 'Stop'` だと外部コマンドの stderr でスクリプトが止まる → `Continue` に変更
- セッション開始時に Blender MCP サーバー（Claude Code 側）の起動に失敗していると自動では再起動されない → `/mcp` から再接続が必要な旨を CLAUDE.md とスキルに記載

残課題・関連Issue:
- `com.unity.pipeline` の `quit` コマンドの不具合は Unity 側の修正待ち
---
