using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "TestProject/Assemble/PartsAssembleDataAsset", order = 0)]
    public class PartsAssembleDataAsset : ScriptableObject
    {
        [SerializeField]
        PartsAssembleData data;

        protected void SetData(PartsAssembleData assembleData)
        {
            this.data = assembleData;
        }

        public int GetPartId(PartsAttachType type)
        {
            if (data == null)
            {
                Debug.LogError("PartsAssembleData is not Attached");
                return -1;
            }

            var dic = this.data.GetDictionary();
            if (dic == null)
                return -1;

            if (dic.TryGetValue(type, out int partId) == false)
            {
                Debug.LogErrorFormat("There is no PartId. PartsAttachType:{0}", type);
                return -1;
            }

            return partId;
        }

        public Dictionary<PartsAttachType, int> GetPartId_All()
        {
            return data?.GetDictionary();
        }
    }

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

    [Serializable]
    public class PartsAssembleData
    {
        [SerializeField]
        PartsTuple[] tuples;

        Dictionary<PartsAttachType, int> attachDic = null;

        public static PartsAssembleData CreateFromDictionary(Dictionary<PartsAttachType, int> dic)
        {
            var data = new PartsAssembleData();
            var list = new List<PartsTuple>();
            foreach (var kvp in dic)
                list.Add(new PartsTuple { attachType = kvp.Key, partId = kvp.Value });
            data.tuples = list.ToArray();
            return data;
        }

        public Dictionary<PartsAttachType, int> GetDictionary()
        {
            if (tuples == null)
            {
                Debug.LogError("Tuples are null.");
                return null;
            }

            if (attachDic == null)
            {
                attachDic = new Dictionary<PartsAttachType, int>();
                foreach (var t in tuples)
                    attachDic[t.attachType] = t.partId;
            }

            return attachDic;
        }

        public void SetPartId(PartsAttachType type, int partId)
        {
            var dic = GetDictionary();
            if (dic == null) return;
            dic[type] = partId;
            // tuples を同期
            var list = new List<PartsTuple>();
            foreach (var kvp in dic)
                list.Add(new PartsTuple { attachType = kvp.Key, partId = kvp.Value });
            tuples = list.ToArray();
        }
    }

    [Serializable]
    public class PartsTuple
    {
        public PartsAttachType attachType;
        public int partId;
    }

    public enum PartsAttachType
    {
        None = 0,

        Head = 101,
        Core,
        Arm,
        Leg,

        Booster = 201,

        Weapon_Right = 301,
        Weapon_Left,
        Weapon_Sub,
    }
}
