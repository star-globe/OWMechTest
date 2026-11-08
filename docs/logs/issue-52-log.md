---
Issue: #52 バッチモードでのコンパイル・テスト用スクリプト（改善プラン P4、CI は保留）
実装日: 2026-10-15
変更ファイル:
- tools/unity/common.sh（新規）
- tools/unity/compile-check.sh（新規）
- tools/unity/run-tests.sh（新規）
- unity/.claude/settings.json
- CLAUDE.md
- README.md
- docs/ai-dev-improvement-plan.md

実装方針:
- Unity Editor を開いていないとき（Blender 作業中など）に、Unity をバッチモードで起動してコンパイルと EditMode テストを確認するスクリプトを追加した（PR #53）
  - `common.sh`：`UNITY_EXE` の解決（環境変数 → `ProjectVersion.txt` のバージョンを Unity Hub の既定の場所から探す）、標準出力も保存する `run_unity`、Editor が開いているときに案内を出す `check_editor_closed`
  - `compile-check.sh`（約 20 秒）、`run-tests.sh [フィルタ]`（約 15 秒。終了コード 0 = 成功、2 = 失敗あり）
- README に「Editor を開かずにコンパイル・テストする」節、CLAUDE.md の「検証方法」に使い分けを追加し、許可リストに登録した

詰まった点・解決策:
- Editor がプロジェクトを開いていると、バッチモードは「another Unity instance is running」で失敗し、このエラーはログファイルに出ないことがある → 標準出力も保存して両方を確認し、unity-mcp で確認するよう案内を出す
- GameCI（GitHub Actions）のワークフローも作ったが、git 管理外の有料アセット（ParticlePack / CleanFlatIcon）の扱いと、公開リポジトリに Unity ライセンスの Secret を置く問題があるため、指示により保留にして削除した（コミット `b303161` から復元できる）

残課題・関連Issue:
- #54（保留）：GameCI による CI。再開するときは、有料アセットの扱い、外部アクションを SHA で固定すること、CI 専用アカウント、`pull_request_target` を使わないことを検討する
---
