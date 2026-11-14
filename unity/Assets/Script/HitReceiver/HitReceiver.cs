using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public abstract class HitReceiver : MonoBehaviour
    {
        [SerializeField]
        DeadComponent deadComp = null;

        private int hitPoint;
        private bool isDead;

        protected void SetInitHitPoint(int hit)
        {
            hitPoint = hit;
            isDead = false;
        }

        public void Damage(int damage)
        {
            // 撃破した後に当たっても、爆発のエフェクトや撃破の処理を繰り返さない
            // （機体は撃破の演出が終わるまで消えないため。CharacterDeadComponent）
            if (isDead)
                return;

            hitPoint -= damage;
            OnDamaged(damage);

            if (hitPoint <= 0)
            {
                EffectManager.Instance?.PlayEffect(EffectID.Explosion, transform.position);
                deadComp?.DeadAction();
                hitPoint = 0;
                isDead = true;
            }
            else
            {
                EffectManager.Instance?.PlayEffect(EffectID.ExplosionSmall, transform.position);
            }
        }

        /// <summary>ダメージを受けたときに呼ぶ（派生クラスで本体のパラメーターに反映する）</summary>
        protected virtual void OnDamaged(int damage) { }
    }
}
