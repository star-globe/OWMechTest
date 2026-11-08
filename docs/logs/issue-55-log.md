---
Issue: #55 export_to_unity.py を Blender MCP から呼べる API にする（改善プラン P7）
実装日: 2026-10-15
変更ファイル:
- tools/blender/export_to_unity.py
- docs/blender-unity-pipeline.md
- CLAUDE.md
- .gitignore
- docs/ai-dev-improvement-plan.md

実装方針:
- `export_to_unity.py` を import して使えるモジュールにした（PR #56）。処理は `__main__` のときだけ実行する
  - `resolve_base_path`：エクスポート先を引数 → 環境変数 `OWMECH_UNITY_MODELS` → リポジトリ内の `unity/Assets/Models`（スクリプト、Text Editor のテキストのファイルパス、`.blend` の場所から探す）の順で決める
  - `validate_objects`：命名規約（ファイル名、マテリアルスロット名、メッシュ・アーマチュアの有無、大文字、負のスケール）を error / warning で返す
  - `export_objects(objects, base_path, validate, dry_run)` / `export_selected`：子孫も同じ FBX に含め、error があればエクスポートしない。終わったら選択状態を元に戻す
- 手順書に「エクスポート用スクリプト」節、CLAUDE.md の「Blender 連携」に MCP からの使い方を追加。`.gitignore` に `__pycache__/`
- 確認：Blender でテスト用の `.blend` を作り、検証・dry run・エクスポート・Text Editor からの実行をすべて確認した（リポジトリの Models には書き出していない）

詰まった点・解決策:
- Text Editor から実行すると `__file__` が `<blend>\export_to_unity.py` になり、リポジトリの場所が分からない → テキストのファイルパスと `.blend` の場所も探す候補にした
- MCP のコードには UI のコンテキストが無く、`bpy.context.object` が使えない → オブジェクトは名前か `bpy.data` で渡す API にした。Text Editor の実行は `temp_override` で TEXT_EDITOR を指定して再現した

残課題・関連Issue:
- #57：コマンドライン（バックグラウンド）からの実行
---
