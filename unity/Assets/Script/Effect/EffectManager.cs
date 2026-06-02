using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// エフェクトのプール管理と再生を一元管理するシングルトン。
    /// 呼び出し側は PlayEffect(EffectID.*, pos, rot) を使う。
    /// </summary>
    public class EffectManager : SingletonMonoBehaviour<EffectManager>
    {
        [SerializeField] int defaultPoolSize = 5;

        // effectId → プール済みインスタンスのスタック
        readonly Dictionary<int, Stack<ParticleSystem>> pool
            = new Dictionary<int, Stack<ParticleSystem>>();

        // 再生中インスタンスの管理（自動返却用）
        readonly List<(ParticleSystem ps, int id)> active
            = new List<(ParticleSystem, int)>();

        /// <summary>指定IDのエフェクトをワールド座標に再生する。</summary>
        public void PlayEffect(int effectId, Vector3 position, Quaternion rotation)
        {
            var settings = EffectMaster.Instance.GetSettings(effectId);
            if (settings == null || settings.EffectPrefab == null)
            {
                Debug.LogWarning($"[EffectManager] effectId={effectId} のプレハブが未設定です。");
                return;
            }

            var ps = Rent(effectId, settings.EffectPrefab);
            ps.transform.SetPositionAndRotation(position, rotation);
            ps.gameObject.SetActive(true);
            ps.Play(withChildren: true);
            active.Add((ps, effectId));
        }

        /// <summary>ワールド座標にエフェクトを再生する（回転なし版）。</summary>
        public void PlayEffect(int effectId, Vector3 position)
            => PlayEffect(effectId, position, Quaternion.identity);

        void Update()
        {
            // 再生終了したインスタンスをプールに返却
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var (ps, id) = active[i];
                if (ps == null || ps.IsAlive(withChildren: true)) continue;

                Return(id, ps);
                active.RemoveAt(i);
            }
        }

        // ---- プール内部実装 ----

        ParticleSystem Rent(int effectId, GameObject prefab)
        {
            if (!pool.TryGetValue(effectId, out var stack))
            {
                stack = new Stack<ParticleSystem>(defaultPoolSize);
                pool[effectId] = stack;
            }

            if (stack.Count > 0)
                return stack.Pop();

            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            return go.GetComponent<ParticleSystem>();
        }

        void Return(int effectId, ParticleSystem ps)
        {
            ps.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);

            if (!pool.TryGetValue(effectId, out var stack))
            {
                stack = new Stack<ParticleSystem>(defaultPoolSize);
                pool[effectId] = stack;
            }
            stack.Push(ps);
        }
    }
}
