using System;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// プレイヤー機体の組み合わせ。PlayerPrefs に JSON で保存・読み込みする。
    /// 読み込んだデータは非シリアライズのフィールドで保持し、アセット（保存データが無いときの初期値）は書き換えない。
    /// </summary>
    [CreateAssetMenu(menuName = "TestProject/Assemble/PlayerPartsAssembleDataAsset", order = 1)]
    public class PlayerPartsAssembleDataAsset : PartsAssembleDataAsset
    {
        const string SaveKey = "PlayerAssembleData";

        [NonSerialized]
        PartsAssembleData savedData;

        protected override PartsAssembleData CurrentData => savedData ?? base.CurrentData;

        /// <summary>PlayerPrefs から保存データを読み込む。保存データが無ければアセットの初期値を使う。</summary>
        /// <returns>保存データを読み込めた場合 true</returns>
        public bool Load()
        {
            savedData = null;

            var json = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return false;

            var assembleData = JsonUtility.FromJson<PartsAssembleData>(json);
            if (assembleData == null)
            {
                Debug.LogError("[PlayerPartsAssembleDataAsset] JSONのデシリアライズに失敗しました。");
                return false;
            }

            savedData = assembleData;
            return true;
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

            savedData = assembleData;
        }
    }
}
