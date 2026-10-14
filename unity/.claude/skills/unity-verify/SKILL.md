---
name: unity-verify
description: Unity Editor でコンパイルエラーとコンソールのエラーを確認する。引数に play を付けると MainScene から Play して実行時エラーも確認する。C# スクリプトやアセットを変更した後、PR 作成前の検証、「コンパイル通るか確認して」と言われたときに使う。
argument-hint: "[play]"
---

# /unity-verify

Unity CLI MCP（`unity-mcp`）のツールで検証する。MCP ツールが使えない場合は `unity command <コマンド名> --caller plugin --skill unity-cli` で同じ操作を行う。コマンド名は推測せず、`unity command` の一覧で確認する。

## 手順

1. **Editor の状態を確認する**
   - `unity status` で対象プロジェクトの Editor が `ready` であること
   - 起動していなければ `unity/` で `unity open .` を実行し、`ready` になるまで待つ（Monitor の until ループで `unity status` を確認する）

2. **コンパイルする**
   - 変更が Editor に反映されていなければ `recompile` を実行し、`recompile_status` で完了を待つ

3. **エラーを確認する**
   - `console_status` でエラー件数を確認し、`console` でエラーと警告の内容を読む
   - 今回の変更に起因するエラーは修正して手順 2 からやり直す。変更前からある警告は報告だけでよい

4. **EditMode テストを実行する**
   - `run_tests` を `mode: editor`、`filter_type: assembly`、`filter: AdvancedGears.Tests.EditMode` で実行し、全件 green であることを確認する
   - 失敗したテストは、テスト側の不備か実装のリグレッションかを切り分けて修正する。テストを消して通すことはしない

5. **（引数に `play` がある場合）実行時エラーを確認する**
   - **Play 直前にシーンの保存状態を確認する**（CLAUDE.md「デバッグ実行」の手順）。`list_open_scenes` で `isDirty` を見て、意図した変更なら `save_scene`、意図しない変更（自動再シリアライズなど）なら `EditorSceneManager.OpenScene(path)` で開き直して破棄する。すべて false になるまで Play しない（未保存だと確認ダイアログで Editor が止まる）
   - `menu` で `WarGame/Play Main Scene` を実行する（`editor_play` で任意のシーンから直接 Play しない）
   - `editor_status` で Play 中であることを確認し、操作が必要な検証が無ければ数秒待ってから `console` で例外・エラーを確認する
   - `editor_stop` で停止する

6. **結果を報告する**
   - コンパイルエラー件数、テストの結果（件数と失敗）、今回の変更に起因するエラーの有無、Play 確認の結果（実施した場合）を簡潔に報告する
   - PR を作る場合は、この結果を PR 本文の「確認」欄に書く
