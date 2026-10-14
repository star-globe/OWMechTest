using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "TestProject/Assemble/PartsAssembleDataAsset", order = 0)]
    public class PartsAssembleDataAsset : ScriptableObject
    {
        // アセットに保存された初期値。実行時に書き換えない（Editor では git 管理下のアセットに書き戻されるため）
        [SerializeField]
        PartsAssembleData data;

        /// <summary>現在有効な組み合わせ。派生クラスで実行時のデータに差し替える。</summary>
        protected virtual PartsAssembleData CurrentData => data;

        /// <summary>スロットのパーツIDを返す。未装備（データに無い）スロットは -1。</summary>
        public int GetPartId(PartsAttachType type)
        {
            var current = CurrentData;
            if (current == null)
            {
                Debug.LogError("PartsAssembleData is not Attached");
                return -1;
            }

            var dic = current.GetDictionary();
            if (dic == null)
                return -1;

            return dic.TryGetValue(type, out int partId) ? partId : -1;
        }

        public Dictionary<PartsAttachType, int> GetPartId_All()
        {
            return CurrentData?.GetDictionary();
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
            {
                // 負の ID は未装備。データには含めない
                if (kvp.Value < 0)
                    continue;
                list.Add(new PartsTuple { attachType = kvp.Key, partId = kvp.Value });
            }
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
