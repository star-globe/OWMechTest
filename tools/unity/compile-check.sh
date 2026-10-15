#!/usr/bin/env bash
# Unity をバッチモードで起動してスクリプトをコンパイルし、コンパイルエラーの有無を確認する
# 使い方: tools/unity/compile-check.sh
# Editor でプロジェクトを開いているときは使えない（その場合は unity-mcp の console_status を使う）
set -u
source "$(dirname "${BASH_SOURCE[0]}")/common.sh"

LOG="$LOG_DIR/compile-check.log"
echo "Unity $UNITY_VERSION でコンパイルしています（ログ: ${LOG#$REPO_ROOT/}）"

run_unity "$LOG" -batchmode -nographics -quit -projectPath "$PROJECT_PATH"
status=$?

errors="$(grep -E 'error CS[0-9]+' "$LOG" | sort -u)"
warnings="$(grep -E 'warning CS[0-9]+' "$LOG" | grep -v 'Library/PackageCache' | sort -u)"

if [ -n "$warnings" ]; then
    echo "--- 警告 ---"
    echo "$warnings"
fi

if [ -n "$errors" ]; then
    echo "--- コンパイルエラー ---"
    echo "$errors"
    exit 1
fi

if [ $status -ne 0 ]; then
    echo "Unity が終了コード $status で終了しました。ログを確認してください。" >&2
    exit $status
fi

echo "コンパイルエラー 0 件"
