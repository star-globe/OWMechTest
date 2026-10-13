---
Issue: #32 .claude 設定の整備：permissions・スキル・wiki 自動初期化（改善プラン P5）
実装日: 2026-10-12
変更ファイル:
- .gitattributes
- unity/.claude/settings.json
- unity/.claude/hooks/ensure-wiki.sh
- unity/.claude/hooks/ensure-unity-editor.ps1
- unity/.claude/hooks/check-blender-mcp.ps1
- unity/.claude/skills/issue-start/SKILL.md
- unity/.claude/skills/unity-verify/SKILL.md

実装方針:
- `docs/ai-dev-improvement-plan.md` の P5 の残作業を実施（PR #35）
- 読み取り系の git / gh / unity CLI と unity-mcp の読み取り系ツールを permissions の allowlist に追加（書き込み系は従来どおり確認）
- `/issue-start`（開発フローの着手前確認）と `/unity-verify`（コンパイル・実行時エラーの確認）のスキルを追加
- wiki サブモジュールが空なら初期化する SessionStart フックを bash で追加（Windows の Git Bash と Linux の両方で動く）

詰まった点・解決策:
- 既存の SessionStart フックが `pwsh` で起動していたが、`pwsh` が未インストールのため一度も実行されていなかった → Windows 標準の `powershell`（5.1）に変更
- PowerShell 5.1 は BOM なし UTF-8 を ANSI として読み、日本語が文字化けする → フックの ps1 を BOM 付き UTF-8 で保存
- Windows（core.autocrlf=true）で checkout すると .sh が CRLF になり bash で動かない → `.gitattributes` に `*.sh text eol=lf` を追加
- wiki フックは、サブモジュールなしで clone した一時リポジトリで動作確認した

残課題・関連Issue:
- なし
---
