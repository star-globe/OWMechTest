using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// プレイヤー機体の組み合わせ。PlayerPrefs に JSON で保存・読み込みする。
    /// </summary>
    [CreateAssetMenu(menuName = "TestProject/Assemble/PlayerPartsAssembleDataAsset", order = 1)]
    public class PlayerPartsAssembleDataAsset : PartsAssembleDataAsset
    {
        const string SaveKey = "PlayerAssembleData";

        public void Load()
        {
            var json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[PlayerPartsAssembleDataAsset] 保存データが存在しません。");
                return;
            }

            var assembleData = JsonUtility.FromJson<PartsAssembleData>(json);
            if (assembleData == null)
            {
                Debug.LogError("[PlayerPartsAssembleDataAsset] JSONのデシリアライズに失敗しました。");
                return;
            }

            SetData(assembleData);
        }

        public void Save(PartsAssembleData assembleData)
        {
            if (assembleData == null)
            {
                Debug.LogError("[PlayerPartsAssembleDataAsset] 保存するデータがnullです。");
                return;
            }

            var json = JsonUtility.ToJson(assembleData);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();

            SetData(assembleData);
        }
    }
}
