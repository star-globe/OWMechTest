using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AdvancedGears.Tests
{
    public class PartsAssembleDataTests
    {
        const string SaveKey = "PlayerAssembleData";

        string savedPrefs;
        bool hadPrefs;
        readonly List<Object> created = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            // テストが実際の PlayerPrefs を壊さないよう退避する
            hadPrefs = PlayerPrefs.HasKey(SaveKey);
            savedPrefs = PlayerPrefs.GetString(SaveKey, string.Empty);
            PlayerPrefs.DeleteKey(SaveKey);
        }

        [TearDown]
        public void TearDown()
        {
            if (hadPrefs)
                PlayerPrefs.SetString(SaveKey, savedPrefs);
            else
                PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();

            foreach (var o in created)
                Object.DestroyImmediate(o);
            created.Clear();
        }

        static Dictionary<PartsAttachType, int> Dic(params (PartsAttachType type, int id)[] pairs)
        {
            var dic = new Dictionary<PartsAttachType, int>();
            foreach (var p in pairs)
                dic[p.type] = p.id;
            return dic;
        }

        /// <summary>シリアライズ済みの初期値を持つアセットを作る</summary>
        T CreateAsset<T>(params (PartsAttachType type, int id)[] pairs) where T : PartsAssembleDataAsset
        {
            var asset = ScriptableObject.CreateInstance<T>();
            created.Add(asset);

            var so = new SerializedObject(asset);
            var tuples = so.FindProperty("data.tuples");
            tuples.arraySize = pairs.Length;
            for (int i = 0; i < pairs.Length; i++)
            {
                var e = tuples.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("attachType").intValue = (int)pairs[i].type;
                e.FindPropertyRelative("partId").intValue = pairs[i].id;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            // 初期値の書き込みで付いた dirty を消し、以降の操作でアセットが変更されたかを判定できるようにする
            EditorUtility.ClearDirty(asset);
            return asset;
        }

        static int SerializedPartId(PartsAssembleDataAsset asset, PartsAttachType type)
        {
            var tuples = new SerializedObject(asset).FindProperty("data.tuples");
            for (int i = 0; i < tuples.arraySize; i++)
            {
                var e = tuples.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("attachType").intValue == (int)type)
                    return e.FindPropertyRelative("partId").intValue;
            }
            return -1;
        }

        // ── PartsAssembleData ──────────────────────────

        [Test]
        public void CreateFromDictionary_SkipsUnequippedSlots()
        {
            var data = PartsAssembleData.CreateFromDictionary(Dic(
                (PartsAttachType.Booster, 201001),
                (PartsAttachType.Weapon_Left, -1)));

            var dic = data.GetDictionary();
            Assert.AreEqual(1, dic.Count);
            Assert.AreEqual(201001, dic[PartsAttachType.Booster]);
            Assert.IsFalse(dic.ContainsKey(PartsAttachType.Weapon_Left));
        }

        [Test]
        public void CreateFromDictionary_SurvivesJsonRoundTrip()
        {
            var data = PartsAssembleData.CreateFromDictionary(Dic(
                (PartsAttachType.Booster, 201001),
                (PartsAttachType.Weapon_Right, 301001)));

            var restored = JsonUtility.FromJson<PartsAssembleData>(JsonUtility.ToJson(data));

            CollectionAssert.AreEquivalent(data.GetDictionary(), restored.GetDictionary());
        }

        [Test]
        public void SetPartId_UpdatesDictionaryAndSerializedTuples()
        {
            var data = PartsAssembleData.CreateFromDictionary(Dic((PartsAttachType.Booster, 201001)));

            data.SetPartId(PartsAttachType.Weapon_Sub, 301001);

            Assert.AreEqual(301001, data.GetDictionary()[PartsAttachType.Weapon_Sub]);
            var restored = JsonUtility.FromJson<PartsAssembleData>(JsonUtility.ToJson(data));
            Assert.AreEqual(301001, restored.GetDictionary()[PartsAttachType.Weapon_Sub]);
        }

        // ── PartsAssembleDataAsset ─────────────────────

        [Test]
        public void GetPartId_ReturnsMinusOneForMissingSlot()
        {
            var asset = CreateAsset<PartsAssembleDataAsset>((PartsAttachType.Booster, 201001));

            Assert.AreEqual(201001, asset.GetPartId(PartsAttachType.Booster));
            Assert.AreEqual(-1, asset.GetPartId(PartsAttachType.Weapon_Sub));
        }

        // ── PlayerPartsAssembleDataAsset（#39 / #40） ──

        [Test]
        public void Load_WithoutSaveData_UsesAssetDefaults()
        {
            var asset = CreateAsset<PlayerPartsAssembleDataAsset>(
                (PartsAttachType.Booster, 201001),
                (PartsAttachType.Weapon_Left, 301001));

            Assert.IsFalse(asset.Load());
            Assert.AreEqual(301001, asset.GetPartId(PartsAttachType.Weapon_Left));
        }

        [Test]
        public void Save_OverridesRuntimeDataWithoutChangingSerializedAsset()
        {
            var asset = CreateAsset<PlayerPartsAssembleDataAsset>(
                (PartsAttachType.Booster, 201001),
                (PartsAttachType.Weapon_Left, 301001));

            asset.Save(PartsAssembleData.CreateFromDictionary(Dic(
                (PartsAttachType.Booster, 201001),
                (PartsAttachType.Weapon_Left, -1))));

            // 実行時は保存したデータ（左腕なし）が使われる
            Assert.AreEqual(-1, asset.GetPartId(PartsAttachType.Weapon_Left));
            // アセットのシリアライズ済みデータは書き換わらない（#40）
            Assert.AreEqual(301001, SerializedPartId(asset, PartsAttachType.Weapon_Left));
            Assert.IsFalse(EditorUtility.IsDirty(asset));
        }

        [Test]
        public void Load_ReadsSavedDataIntoAnotherInstance()
        {
            var saver = CreateAsset<PlayerPartsAssembleDataAsset>((PartsAttachType.Weapon_Left, 301001));
            saver.Save(PartsAssembleData.CreateFromDictionary(Dic((PartsAttachType.Weapon_Right, 301001))));

            // 再起動後に相当: 実行時データを持たない別インスタンスが PlayerPrefs から読み込む（#39）
            var loader = CreateAsset<PlayerPartsAssembleDataAsset>((PartsAttachType.Weapon_Left, 301001));
            Assert.IsTrue(loader.Load());
            Assert.AreEqual(301001, loader.GetPartId(PartsAttachType.Weapon_Right));
            Assert.AreEqual(-1, loader.GetPartId(PartsAttachType.Weapon_Left));
        }

        [Test]
        public void GetDefaultPartId_IgnoresSavedData()
        {
            var asset = CreateAsset<PlayerPartsAssembleDataAsset>((PartsAttachType.Head, 101001));
            asset.Save(PartsAssembleData.CreateFromDictionary(Dic((PartsAttachType.Booster, 201001))));

            // 保存データには頭部が無いが、初期値は読める（機体本体のスロットの補完に使う。#68）
            Assert.AreEqual(-1, asset.GetPartId(PartsAttachType.Head));
            Assert.AreEqual(101001, asset.GetDefaultPartId(PartsAttachType.Head));
            Assert.AreEqual(-1, asset.GetDefaultPartId(PartsAttachType.Booster));
        }

        [Test]
        public void GetEquippedPartId_FillsOnlyFrameSlotsWithDefaults()
        {
            var asset = CreateAsset<PlayerPartsAssembleDataAsset>(
                (PartsAttachType.Leg, 104001),
                (PartsAttachType.Weapon_Left, 301001));
            // 本体のスロットが無い古い保存データ（左腕武装は外した）
            asset.Save(PartsAssembleData.CreateFromDictionary(Dic((PartsAttachType.Booster, 201001))));

            Assert.AreEqual(104001, asset.GetEquippedPartId(PartsAttachType.Leg));
            // 武器は外せるので初期値で補わない
            Assert.AreEqual(-1, asset.GetEquippedPartId(PartsAttachType.Weapon_Left));
            Assert.AreEqual(201001, asset.GetEquippedPartId(PartsAttachType.Booster));
        }

        [Test]
        public void GetEquippedPartId_PrefersSavedFramePart()
        {
            var asset = CreateAsset<PlayerPartsAssembleDataAsset>((PartsAttachType.Head, 101001));
            asset.Save(PartsAssembleData.CreateFromDictionary(Dic((PartsAttachType.Head, 101003))));

            Assert.AreEqual(101003, asset.GetEquippedPartId(PartsAttachType.Head));
        }

        [Test]
        public void Load_AfterSaveDataDeleted_FallsBackToAssetDefaults()
        {
            var asset = CreateAsset<PlayerPartsAssembleDataAsset>((PartsAttachType.Weapon_Left, 301001));
            asset.Save(PartsAssembleData.CreateFromDictionary(Dic((PartsAttachType.Weapon_Left, -1))));
            PlayerPrefs.DeleteKey(SaveKey);

            Assert.IsFalse(asset.Load());
            Assert.AreEqual(301001, asset.GetPartId(PartsAttachType.Weapon_Left));
        }
    }
}
