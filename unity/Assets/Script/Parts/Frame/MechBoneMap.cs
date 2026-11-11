using System.Collections.Generic;

namespace AdvancedGears
{
    /// <summary>
    /// パーツのボーンを、基準リグの同じ名前のボーンに対応付ける（Unity に依存しない）。
    /// 全パーツが共通のスケルトンでスキニングされている前提で、ボーン名をキーにする。
    /// </summary>
    public static class MechBoneMap
    {
        /// <summary>
        /// 名前とボーンの組から辞書を作る。同じ名前が複数あるときは先のものを使い、後の名前を duplicates に入れる
        /// </summary>
        public static Dictionary<string, T> BuildDictionary<T>(IEnumerable<KeyValuePair<string, T>> bones, List<string> duplicates = null)
        {
            var dic = new Dictionary<string, T>();
            foreach (var kvp in bones)
            {
                if (dic.ContainsKey(kvp.Key))
                {
                    duplicates?.Add(kvp.Key);
                    continue;
                }
                dic.Add(kvp.Key, kvp.Value);
            }
            return dic;
        }

        /// <summary>
        /// パーツのボーン名の並び（SkinnedMeshRenderer.bones の順）に合わせて、基準リグのボーンを並べ直す。
        /// 基準リグに無い名前は null にして missing に入れる
        /// </summary>
        public static T[] Remap<T>(IReadOnlyList<string> sourceBoneNames, IReadOnlyDictionary<string, T> targetBones, List<string> missing = null)
            where T : class
        {
            var result = new T[sourceBoneNames.Count];
            for (int i = 0; i < sourceBoneNames.Count; i++)
            {
                var name = sourceBoneNames[i];
                if (name != null && targetBones.TryGetValue(name, out var bone))
                {
                    result[i] = bone;
                }
                else
                {
                    missing?.Add(name);
                }
            }
            return result;
        }
    }
}
