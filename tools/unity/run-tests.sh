#!/usr/bin/env bash
# Unity をバッチモードで起動して EditMode テストを実行する
# 使い方: tools/unity/run-tests.sh [テスト名のフィルタ]
# Editor でプロジェクトを開いているときは使えない（その場合は unity-mcp の run_tests を使う）
set -u
source "$(dirname "${BASH_SOURCE[0]}")/common.sh"

LOG="$LOG_DIR/run-tests.log"
RESULTS="$LOG_DIR/editmode-results.xml"
rm -f "$RESULTS"

args=(-batchmode -nographics -projectPath "$PROJECT_PATH"
      -runTests -testPlatform EditMode -assemblyNames AdvancedGears.Tests.EditMode
      -testResults "$RESULTS")
if [ $# -ge 1 ]; then
    args+=(-testFilter "$1")
fi

echo "Unity $UNITY_VERSION で EditMode テストを実行しています（ログ: ${LOG#$REPO_ROOT/}）"
run_unity "$LOG" "${args[@]}"
status=$?

if [ ! -f "$RESULTS" ]; then
    grep -E 'error CS[0-9]+' "$LOG" | sort -u
    echo "テスト結果が出力されませんでした（終了コード $status）。コンパイルエラーなどをログで確認してください。" >&2
    exit 1
fi

# NUnit3 形式の結果から集計と失敗したテストを表示する
summary="$(grep -o '<test-run [^>]*>' "$RESULTS" | head -1)"
attr() { echo "$summary" | sed -n "s/.* $1=\"\([^\"]*\)\".*/\1/p"; }
echo "合計 $(attr total) 件: 成功 $(attr passed) / 失敗 $(attr failed) / スキップ $(attr skipped)"
grep -o '<test-case [^>]*result="Failed"[^>]*>' "$RESULTS" | sed -n 's/.* fullname="\([^"]*\)".*/  失敗: \1/p'

# 終了コード: 0 = 全件成功、2 = 失敗あり、それ以外 = 実行エラー
exit $status
