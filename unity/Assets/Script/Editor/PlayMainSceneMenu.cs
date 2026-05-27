using UnityEditor;
using UnityEditor.SceneManagement;

public static class PlayMainSceneMenu
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string MenuPath = "WarGame/Play Main Scene";

    [MenuItem(MenuPath, priority = 1)]
    public static void PlayMainScene()
    {
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != MainScenePath)
            EditorSceneManager.OpenScene(MainScenePath);

        EditorApplication.isPlaying = true;
    }

    [MenuItem(MenuPath, validate = true)]
    public static bool ValidatePlayMainScene()
    {
        return !EditorApplication.isCompiling;
    }
}
