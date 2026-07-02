using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupChecker
{
    private static readonly List<(string folder, string displayName, string assetStoreUrl)> RequiredAssets = new()
    {
        (
            "Assets/Plugins/UniRx",
            "UniRx (Reactive Extensions for Unity)",
            "https://assetstore.unity.com/packages/tools/integration/unirx-reactive-extensions-for-unity-17276"
        ),
        (
            "Assets/UnityTechnologies/ParticlePack",
            "Unity Particle Pack",
            "https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-particle-pack-127325"
        ),
        (
            "Assets/CleanFlatIcon",
            "Clean Flat Icons",
            "https://assetstore.unity.com/packages/2d/gui/icons/clean-flat-icons-98117"
        ),
    };

    static SetupChecker()
    {
        EditorApplication.delayCall += CheckRequiredAssets;
    }

    private static void CheckRequiredAssets()
    {
        var missing = new List<(string name, string url)>();
        foreach (var (folder, name, url) in RequiredAssets)
        {
            if (!Directory.Exists(folder))
                missing.Add((name, url));
        }

        if (missing.Count == 0)
            return;

        var message = "以下のAsset Storeパッケージがインストールされていません。\n" +
                      "Unity Package Manager > My Assets からインポートしてください。\n\n";
        foreach (var (name, _) in missing)
            message += $"  • {name}\n";

        bool open = EditorUtility.DisplayDialog(
            "セットアップが必要です",
            message,
            "Asset Storeを開く",
            "後で"
        );

        if (open)
        {
            // 最初の不足アセットのページを開く
            Application.OpenURL(missing[0].url);
        }
    }
}
