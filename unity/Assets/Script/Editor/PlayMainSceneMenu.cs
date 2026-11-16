using UnityEditor;
using UnityEditor.SceneManagement;

namespace AdvancedGears
{
    /// <summary>
    /// シーンを開いて Play するメニュー。通常は MainScene から、デバッグ用のシーン（MainScene を通らずに起動する前提で作ったもの）は直接 Play する
    /// </summary>
    public static class PlayMainSceneMenu
    {
        private const string MainScenePath = "Assets/Scenes/MainScene.unity";
        private const string MenuPath = "WarGame/Play Main Scene";

        // カスタマイズ画面は、MainScene を通らずに Play するとデバッグモードになる（CustomizeDebugMode）
        private const string CustomizeScenePath = "Assets/Scenes/Customize.unity";
        private const string CustomizeMenuPath = "WarGame/Play Customize (Debug)";

        private const string EffectCheckScenePath = "Assets/Scenes/Debug/EffectCheck.unity";
        private const string EffectCheckMenuPath = "WarGame/Play Effect Check";

        [MenuItem(MenuPath, priority = 1)]
        public static void PlayMainScene()
        {
            PlayScene(MainScenePath);
        }

        [MenuItem(MenuPath, validate = true)]
        public static bool ValidatePlayMainScene()
        {
            return CanPlay();
        }

        [MenuItem(CustomizeMenuPath, priority = 2)]
        public static void PlayCustomizeDebug()
        {
            PlayScene(CustomizeScenePath);
        }

        [MenuItem(CustomizeMenuPath, validate = true)]
        public static bool ValidatePlayCustomizeDebug()
        {
            return CanPlay();
        }

        [MenuItem(EffectCheckMenuPath, priority = 3)]
        public static void PlayEffectCheck()
        {
            PlayScene(EffectCheckScenePath);
        }

        [MenuItem(EffectCheckMenuPath, validate = true)]
        public static bool ValidatePlayEffectCheck()
        {
            return CanPlay();
        }

        /// <summary>Play 中なら止め、未保存のシーンを確認してから、指定のシーンを開いて Play する</summary>
        private static void PlayScene(string scenePath)
        {
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != scenePath)
                EditorSceneManager.OpenScene(scenePath);

            EditorApplication.isPlaying = true;
        }

        private static bool CanPlay()
        {
            return !EditorApplication.isCompiling;
        }
    }
}
