namespace AdvancedGears
{
    /// <summary>
    /// MainScene の初期化（InitializeObject）を通らずに Play したデバッグ用のシーンで、必要な初期化を行う。
    /// カスタマイズ画面のデバッグモードやエフェクト確認シーンで使う
    /// </summary>
    public static class DebugBootstrap
    {
        /// <summary>
        /// MainScene の初期化を通ったか。シングルトンの置き場（SingletonMonoObject）は MainScene にしか無いので、
        /// 無ければマスタも読み込まれていない
        /// </summary>
        public static bool IsInitialized => SingletonMonoObject.Instance != null && MasterManager.Instance.IsLoaded;

        /// <summary>初期化されていなければ、シングルトンの置き場を作ってマスタを読み込む</summary>
        /// <returns>ここで初期化した（MainScene を通っていなかった）場合 true</returns>
        public static bool EnsureInitialized()
        {
            if (IsInitialized)
                return false;

            SingletonMonoObject.EnsureExists();
            MasterManager.Instance.LoadAll();
            return true;
        }
    }
}
