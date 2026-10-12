---
name: switch-app
description: Unity と Blender を切り替える（メモリ節約のため同時に起動しない）。Blender でモデリング・エクスポートを始めるとき、Blender 作業を終えて Unity に戻るときに使う。「Blender で作業して」「モデルを作って」「Unity に戻って」と言われたときも使う。
argument-hint: "<blender|unity> [.blend ファイル]"
---

# /switch-app

引数: 切り替え先（`blender` または `unity`）。`blender` の場合は 2 つ目の引数で開く `.blend` ファイルを指定できる。

メモリ節約のため、Unity Editor と Blender は同時に起動しない（CLAUDE.md「Unity と Blender の切り替え」）。切り替えは以下のスクリプトで行い、未保存の変更がある場合は終了せずに止まる。**強制終了（`Stop-Process` など）はしない。**

## Unity → Blender（`/switch-app blender`）

1. **Unity の未保存の変更を片付ける**（Unity が起動している場合）
   - Play 中なら `editor_stop` で停止する
   - `list_open_scenes` で `isDirty` を確認し、CLAUDE.md「デバッグ実行」の手順で保存または破棄する
2. **切り替えスクリプトを実行する**
   - `powershell -NoProfile -ExecutionPolicy Bypass -File unity/.claude/scripts/switch-to-blender.ps1`（`.blend` を開く場合は `-BlendFile <path>` を付ける）
   - Unity の終了 → Blender の起動 → MCP アドオン（`localhost:9876`）の応答まで待つ。数十秒〜2分かかるので timeout は長め（300000ms 程度）にする
   - `中断:` で始まる出力が出たら、内容に従って対処してから再実行する
3. **Blender MCP のツールが使えるか確認する**
   - `mcp__blender__*` のツール（例: `get_objects_summary`）を呼んで応答を確認する
   - ツールが一覧に無い、または接続エラーになる場合は、セッション開始時に blender MCP サーバーの起動に失敗している。人間に `/mcp` から `blender` の再接続を依頼する

## Blender → Unity（`/switch-app unity`）

> **注意**: MCP（`execute_blender_code`）で行った変更は `bpy.data.is_dirty` に反映されず、Blender を閉じても保存確認が出ないまま失われる。`is_dirty` で保存の要否を判断しないこと。

1. **Blender の作業内容をファイルに残す**
   - Blender 作業は、**開始時に名前付きの `.blend` として保存しておく**（既存ファイルを開く、または `bpy.ops.wm.save_as_mainfile(filepath=...)`）。保存先が判断できなければ人間に確認する
   - Unity に取り込むモデルは、終了前に `docs/blender-unity-pipeline.md` の手順で FBX をエクスポートしておく
2. **切り替えスクリプトを実行する**
   - `powershell -NoProfile -ExecutionPolicy Bypass -File unity/.claude/scripts/switch-to-unity.ps1`
   - スクリプトが MCP アドオン経由で `.blend` を**必ず保存**（`save_mainfile`）してから Blender を閉じ、Unity を起動して `ready` まで待つ。Unity の起動には数分かかるので timeout は最大（600000ms）にする
   - シーンが無題（未保存の新規ファイル）の場合は中断する。残す作業があれば手順 1 で保存して再実行し、残す必要が無い（Blender を起動しただけ等）ときだけ `-DiscardUntitled` を付けて再実行する
   - Blender が終了しない場合は確認ダイアログなどで止まっている。人間に Blender の画面確認を依頼する
3. **Unity 側を確認する**
   - unity-mcp のツールが再接続されたら、`console_status` でコンパイルエラーが無いことを確認する
   - FBX を取り込んだ場合は、インポート結果（`find_assets` など）を確認する
