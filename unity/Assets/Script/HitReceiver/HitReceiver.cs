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
        protected void SetInitHitPoint(int hit)
        {
            hitPoint = hit;
        }

        public void Damage(int damage)
        {
            hitPoint -= damage;
            OnDamaged(damage);

            if (hitPoint <= 0)
            {
                EffectManager.Instance?.PlayEffect(EffectID.Explosion, transform.position);
                deadComp?.DeadAction();
                hitPoint = 0;
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
