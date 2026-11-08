#!/usr/bin/env bash
# .blend を GUI を開かずに（blender --background）検証・エクスポートする
# 使い方: tools/blender/export.sh <file.blend> [export_to_unity.py のオプション]
#   例: tools/blender/export.sh model.blend --all --dry-run
#       tools/blender/export.sh model.blend --objects body_standard
#       tools/blender/export.sh model.blend --all --validate-only
# 標準出力: 結果の JSON（export_to_unity.py の run_cli の戻り値）
# 終了コード: 0 = 成功、2 = 規約違反（error）があり何もしなかった、1 = その他のエラー
# Blender のログは unity/Logs/blender-export.log に保存する
# Blender の実行ファイルは環境変数 BLENDER_EXE で上書きできる。未指定なら既定のインストール先から最新版を探す
set -u

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

if [ $# -lt 1 ] || [ "$1" = "-h" ] || [ "$1" = "--help" ]; then
    sed -n '2,10p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
    exit 1
fi

BLEND_FILE="$1"
shift
if [ ! -f "$BLEND_FILE" ]; then
    echo ".blend が見つかりません: $BLEND_FILE" >&2
    exit 1
fi

if [ -z "${BLENDER_EXE:-}" ]; then
    # Windows の既定のインストール先（Blender X.Y フォルダ）はバージョンの新しい順に探す
    while IFS= read -r candidate; do
        if [ -n "$candidate" ] && [ -x "$candidate" ]; then
            BLENDER_EXE="$candidate"
            break
        fi
    done < <(
        ls -d "/c/Program Files/Blender Foundation/Blender "* 2>/dev/null | sort -V -r | sed 's|$|/blender.exe|'
        echo "/Applications/Blender.app/Contents/MacOS/Blender"
        command -v blender 2>/dev/null
    )
fi
if [ -z "${BLENDER_EXE:-}" ] || [ ! -x "$BLENDER_EXE" ]; then
    echo "Blender が見つかりません。環境変数 BLENDER_EXE に Blender の実行ファイルを指定してください。" >&2
    exit 1
fi

LOG_DIR="$REPO_ROOT/unity/Logs"
mkdir -p "$LOG_DIR"
LOG="$LOG_DIR/blender-export.log"

# --factory-startup: ユーザーのアドオン（MCP アドオンの Auto Start など）を読み込まない
"$BLENDER_EXE" --background --factory-startup "$BLEND_FILE" \
    --python-exit-code 1 \
    --python "$SCRIPT_DIR/export_to_unity.py" -- "$@" > "$LOG" 2>&1
status=$?

result="$(sed -n 's/^OWMECH_RESULT://p' "$LOG" | tail -n 1)"
if [ -z "$result" ]; then
    echo "結果が出力されませんでした（終了コード $status）。ログ: $LOG" >&2
    tail -n 20 "$LOG" >&2
    [ $status -eq 0 ] && status=1
    exit $status
fi
echo "$result"
exit $status
