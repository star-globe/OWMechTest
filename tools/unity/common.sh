# tools/unity/*.sh から source する共通処理
# - REPO_ROOT / PROJECT_PATH / UNITY_EXE を設定する
# - UNITY_EXE は環境変数で上書きできる。未指定なら ProjectVersion.txt のバージョンを Unity Hub の既定の場所から探す

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROJECT_PATH="$REPO_ROOT/unity"

UNITY_VERSION="$(sed -n 's/^m_EditorVersion: *//p' "$PROJECT_PATH/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"

if [ -z "${UNITY_EXE:-}" ]; then
    for candidate in \
        "/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity.exe" \
        "/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity" \
        "$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity"; do
        if [ -x "$candidate" ]; then
            UNITY_EXE="$candidate"
            break
        fi
    done
fi

if [ -z "${UNITY_EXE:-}" ] || [ ! -x "$UNITY_EXE" ]; then
    echo "Unity $UNITY_VERSION が見つかりません。環境変数 UNITY_EXE に Unity の実行ファイルを指定してください。" >&2
    exit 1
fi

LOG_DIR="$PROJECT_PATH/Logs"
mkdir -p "$LOG_DIR"

# Unity を実行する。標準出力は "$1.stdout" に保存する（プロジェクトのロックなど、ログファイルに出ないエラーがあるため）
# 使い方: run_unity <ログファイル> <Unity の引数...>
run_unity() {
    local log="$1"
    shift
    "$UNITY_EXE" "$@" -logFile "$log" > "$log.stdout" 2>&1
    local status=$?
    check_editor_closed "$log"
    return $status
}

# Unity は同じプロジェクトを2つ同時に開けないので、Editor が起動中なら案内して終了する
check_editor_closed() {
    local log="$1"
    if grep -qs "another Unity instance is running" "$log" "$log.stdout"; then
        echo "Unity Editor がこのプロジェクトを開いているため、バッチモードで実行できません。" >&2
        echo "Editor が起動中なら unity-mcp の console_status / run_tests で確認してください。" >&2
        exit 1
    fi
}
