#!/usr/bin/env bash
# SessionStart hook: wiki サブモジュール（unity/docs/wiki）が未初期化なら取得する。
# クラウド環境・新規 clone 直後でも仕様書を読めるようにするため。Windows では Git Bash で実行される。
repo_root=$(git -C "${CLAUDE_PROJECT_DIR:-.}" rev-parse --show-toplevel 2>/dev/null) || exit 0
wiki_dir="$repo_root/unity/docs/wiki"

if [ -n "$(ls -A "$wiki_dir" 2>/dev/null)" ]; then
    exit 0
fi

if git -C "$repo_root" submodule update --init unity/docs/wiki >/dev/null 2>&1; then
    echo "[wiki] unity/docs/wiki が空だったため、サブモジュールを初期化しました。"
else
    echo "[wiki] unity/docs/wiki の初期化に失敗しました。git submodule update --init unity/docs/wiki を手動で実行してください。"
fi
exit 0
