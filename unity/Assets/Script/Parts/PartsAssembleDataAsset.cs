using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
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
    }

    public class PlayerPartsAssembleDataAsset : PartsAssembleDataAsset
    {
        public void Load()
        {
            var text = PlayerPrefs.GetString("PlayerAssembleData", string.Empty);
            byte[] data = Encoding.ASCII.GetBytes(text);

            PartsAssembleData assembleData = null;

            var b = new BinaryFormatter();
            using (var stream = new MemoryStream(data))
            {
                assembleData = (PartsAssembleData) b.Deserialize(stream);
            }

            if (assembleData == null)
            {
                Debug.LogError("There is no PlayerAssembleData");
                return;
            }

            SetData(assembleData);
        }
    }

    [Serializable]
    public class PartsAssembleData
    {
        [SerializeField]
        PartsTuple[] tuples;

        Dictionary<PartsAttachType, int> attachDic = null;

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
                    attachDic.Add(t.attachType, t.partId);
            }

            return attachDic;
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
    }
}
