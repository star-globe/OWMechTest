using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// パーツ・弾丸など各コンポーネントから EffectManager を呼び出す薄いラッパー。
    /// effectId を設定して Play() を呼ぶだけでよい。
    /// </summary>
    public class EffectPlayer : MonoBehaviour
    {
        [SerializeField] int effectId;

        /// <summary>自分のTransformの位置・回転でエフェクトを再生する。</summary>
        public void Play()
        {
            EffectManager.Instance.PlayEffect(effectId, transform.position, transform.rotation);
        }

        /// <summary>指定位置でエフェクトを再生する。</summary>
        public void Play(Vector3 position)
        {
            EffectManager.Instance.PlayEffect(effectId, position, transform.rotation);
        }

        /// <summary>指定位置・回転でエフェクトを再生する。</summary>
        public void Play(Vector3 position, Quaternion rotation)
        {
            EffectManager.Instance.PlayEffect(effectId, position, rotation);
        }
    }
}
